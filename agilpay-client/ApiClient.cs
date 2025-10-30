using ClientModels = agilpay.client.models;
using ServerModels = agilpay.models;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace agilpay
{
    public class ApiClient : IApiClient, IDisposable
    {
        private string ClientId { get; set; }
        private string ClientSecret { get; set; }
        private string Token { get; set; }
        private DateTime TokenExpireTime { get; set; }
        private string BaseUrl { get; set; }
        private HttpClient _httpClient { get; set; }
        private string Session_id { get; set; }

        private readonly SemaphoreSlim _tokenSemaphore = new SemaphoreSlim(1,1);
        private bool _disposed;
        private readonly ILogger<ApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiClient(HttpClient httpClient, ApiClientOptions options, ILogger<ApiClient> logger = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            BaseUrl = options.BaseUrl ?? throw new ArgumentException("BaseUrl is required", nameof(options));
            ClientId = options.ClientId;
            ClientSecret = options.ClientSecret;

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.BaseAddress = new Uri(BaseUrl);

            Session_id = Guid.NewGuid().ToString();
            _logger = logger;

            // set headers that do not change per request
            if (_httpClient.DefaultRequestHeaders.Contains("SessionId"))
                _httpClient.DefaultRequestHeaders.Remove("SessionId");
            _httpClient.DefaultRequestHeaders.Add("SessionId", Session_id);

            if (!string.IsNullOrWhiteSpace(ClientId))
            {
                if (_httpClient.DefaultRequestHeaders.Contains("SiteId"))
                    _httpClient.DefaultRequestHeaders.Remove("SiteId");
                _httpClient.DefaultRequestHeaders.Add("SiteId", ClientId);
            }

            // Default Accept header
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Optional timeout from options (falls back to default if not provided)
            if (options.TimeoutSeconds.HasValue && options.TimeoutSeconds.Value >0)
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds.Value);
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task InitAsync()
        {
            _logger?.LogInformation("Initializing ApiClient for ClientId={ClientId}", ClientId);
            await GetOAuth2TokenAsync().ConfigureAwait(false);
            _logger?.LogInformation("Initialization completed; token expires at {ExpireTime}", TokenExpireTime);
        }

        private async Task GetOAuth2TokenAsync()
        {
            _logger?.LogDebug("Requesting OAuth2 token for ClientId={ClientId}", ClientId);

            var args = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", ClientId },
                { "client_secret", ClientSecret }
            };

            using (var content = new FormUrlEncodedContent(args))
            using (var response = await _httpClient.PostAsync("oauth/token", content).ConfigureAwait(false))
            {
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var truncated = Truncate(responseBody);
                    _logger?.LogError("Failed to obtain token for ClientId={ClientId}. Status={Status}. Body(Truncated)={Body}", ClientId, response.StatusCode, truncated);
                    throw new HttpRequestException($"POST oauth/token failed with status {(int)response.StatusCode} {response.ReasonPhrase}");
                }

                var token = JsonSerializer.Deserialize<ClientModels.TokenResponse>(responseBody, _jsonOptions);

                if (token == null)
                {
                    _logger?.LogError("Token response deserialization returned null for ClientId={ClientId}", ClientId);
                    throw new Exception("Cannot get Auth token at this time, please try later");
                }

                Token = token.access_token;

                // Add a refresh skew to avoid expiry races
                var skewSeconds =60;
                if (token.expires_in >0 && token.expires_in <= skewSeconds)
                {
                    // for very short tokens keep at least half their lifespan
                    skewSeconds = Math.Max(0, (int)(token.expires_in /2));
                }
                var effectiveLifetime = Math.Max(0, (int)(token.expires_in - skewSeconds));
                TokenExpireTime = DateTime.UtcNow.AddSeconds(effectiveLifetime);

                // set Authorization header on HttpClient to avoid per-request header manipulation
                var scheme = token.token_type ?? "Bearer";
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, Token);

                _logger?.LogInformation("Obtained OAuth2 token for ClientId={ClientId}; expires at {ExpireTime}", ClientId, TokenExpireTime);
            }
        }

        private async Task EnsureValidTokenAsync()
        {
            if (!string.IsNullOrWhiteSpace(Token) && TokenExpireTime > DateTime.UtcNow)
            {
                _logger?.LogDebug("Token is valid until {ExpireTime}", TokenExpireTime);
                return;
            }

            _logger?.LogDebug("Token expired or missing; attempting refresh for ClientId={ClientId}", ClientId);
            await _tokenSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!string.IsNullOrWhiteSpace(Token) && TokenExpireTime > DateTime.UtcNow)
                {
                    _logger?.LogDebug("Token was refreshed by another caller; expire time {ExpireTime}", TokenExpireTime);
                    return;
                }

                await GetOAuth2TokenAsync().ConfigureAwait(false);
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }

        private async Task<T> ExecuteRequestAsync<T>(string path, HttpMethod method, object body = null)
        {
            await EnsureValidTokenAsync().ConfigureAwait(false);

            _logger?.LogDebug("Executing request {Method} {Path} (ClientId={ClientId})", method, path, ClientId);

            using (var request = new HttpRequestMessage(method, path))
            {
                if (body != null)
                {
                    var json = JsonSerializer.Serialize(body, _jsonOptions);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                using (var response = await _httpClient.SendAsync(request).ConfigureAwait(false))
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger?.LogDebug("Request succeeded {Method} {Path} Status={StatusCode}", method, path, response.StatusCode);

                        // If caller expects raw string, return content directly
                        if (typeof(T) == typeof(string))
                        {
                            return (T)(object)content;
                        }

                        if (string.IsNullOrWhiteSpace(content)) return default(T);

                        var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                        _logger?.LogTrace("Deserialized response for {Path} into {Type}", path, typeof(T).FullName);
                        return result;
                    }

                    var truncated = Truncate(content);
                    _logger?.LogWarning("Request failed {Method} {Path} Status={StatusCode} Body(Truncated)={Body}", method, path, response.StatusCode, truncated);
                    throw new HttpRequestException($"{method} {path} failed with status {(int)response.StatusCode} {response.ReasonPhrase}");
                }
            }
        }

        public async Task<ClientModels.Transaction> AuthorizePayment(ClientModels.AuthorizationRequest AuthorizationRequest)
        {
            var path = "v6/Autorize";

            try
            {
                var response = await ExecuteRequestAsync<ClientModels.Transaction>(path, HttpMethod.Post, AuthorizationRequest).ConfigureAwait(false);
                return response ?? new ClientModels.Transaction { ResponseCode = "99", Status = "REJECTED", Message = "Empty response" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AuthorizePayment failed for MerchantKey={MerchantKey}", AuthorizationRequest?.MerchantKey);
                return new ClientModels.Transaction { Message = ex.Message, ResponseCode = "99", Status = "REJECTED" };
            }
        }

        public async Task<ClientModels.Transaction> AuthorizePaymentToken(ClientModels.AuthorizationTokenRequest AuthorizationRequest)
        {
            var path = "v6/AuthorizeToken";

            try
            {
                var response = await ExecuteRequestAsync<ClientModels.Transaction>(path, HttpMethod.Post, AuthorizationRequest).ConfigureAwait(false);
                return response ?? new ClientModels.Transaction { ResponseCode = "99", Status = "REJECTED", Message = "Empty response" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AuthorizePaymentToken failed for MerchantKey={MerchantKey}", AuthorizationRequest?.MerchantKey);
                return new ClientModels.Transaction { Message = ex.Message, ResponseCode = "99", Status = "REJECTED" };
            }
        }

        public Task<List<ServerModels.CustomerAccount>> GetCustomerTokens(string CustomerID)
        {
            var path = $"v6/GetCustomerTokens?CustomerID={Uri.EscapeDataString(CustomerID)}";
            return ExecuteRequestAsync<List<ServerModels.CustomerAccount>>(path, HttpMethod.Get);
        }

        public Task<ClientModels.BalanceResponse> GetBalance(ClientModels.BalanceRequest balanceRequest)
        {
            var path = "Payment6/GetBalance";
            return ExecuteRequestAsync<ClientModels.BalanceResponse>(path, HttpMethod.Post, balanceRequest);
        }

        public async Task<bool> IsValidCard(string cardNumber)
        {
            var path = $"v6/IsValidCard?CardNumber={Uri.EscapeDataString(cardNumber)}";
            var result = await ExecuteRequestAsync<string>(path, HttpMethod.Get).ConfigureAwait(false);
            return bool.TryParse(result?.Trim(), out var ok) && ok;
        }

        public async Task<bool> IsValidRoutingNumber(string routingNumber)
        {
            var path = $"v6/IsValidRoutingNumber?RoutingNumber={Uri.EscapeDataString(routingNumber)}";
            var result = await ExecuteRequestAsync<string>(path, HttpMethod.Get).ConfigureAwait(false);
            return bool.TryParse(result?.Trim(), out var ok) && ok;
        }

        public async Task<bool> DeleteCustomerCard(ClientModels.DeleteTokenRequest deleteRequest)
        {
            var path = "v6/DeleteCustomerToken";
            await ExecuteRequestAsync<string>(path, HttpMethod.Post, deleteRequest).ConfigureAwait(false);
            return true;
        }

        public Task<ServerModels.CustomerAccount> RegisterToken(ServerModels.RegisterTokenRequest args)
        {
            var path = "v6/RegisterToken";
            return ExecuteRequestAsync<ServerModels.CustomerAccount>(path, HttpMethod.Post, args);
        }

        public Task<string> CloseBatchResumen(string MerchantKey)
        {
            var path = "v6/CloseBatchResumen";
            return ExecuteRequestAsync<string>(path, HttpMethod.Post, new { MerchantKey });
        }

        public Task<ClientModels.Transaction> VoidById(ClientModels.VoidByIdRequest args)
        {
            var path = "v6/VoidByID";
            return ExecuteRequestAsync<ClientModels.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<ClientModels.Transaction> VoidSale(ClientModels.VoidSaleRequest args)
        {
            var path = "v6/VoidSale";
            return ExecuteRequestAsync<ClientModels.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<ClientModels.Transaction> CaptureByID(ClientModels.VoidByIdRequest args)
        {
            var path = "v6/CaptureByID";
            return ExecuteRequestAsync<ClientModels.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<ClientModels.Transaction> CaptureAdjustmendByID(ClientModels.CaptureAdjustmendByIDRequest args)
        {
            var path = "v6/CaptureAdjustmendByID";
            return ExecuteRequestAsync<ClientModels.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<ClientModels.Transaction> GetTransactionByID(string MerchantKey, string IDTransaction)
        {
            var path = $"v6/GetTransactionByID?MerchantKey={Uri.EscapeDataString(MerchantKey)}&IDTransaction={Uri.EscapeDataString(IDTransaction)}";
            return ExecuteRequestAsync<ClientModels.Transaction>(path, HttpMethod.Get);
        }

        public Task<ClientModels.RecurringScheduleAddResponse> RecurringScheduleAdd(ClientModels.RecurringScheduleAddRequest args)
        {
            var path = "v6/Recurring/Add";
            return ExecuteRequestAsync<ClientModels.RecurringScheduleAddResponse>(path, HttpMethod.Post, args);
        }

        public Task<ClientModels.RecurringSchedule> RecurringScheduleGet(string MerchantKey, string Service, string CustomerId)
        {
            var path = $"v6/Recurring/Get?MerchantKey={Uri.EscapeDataString(MerchantKey)}&Service={Uri.EscapeDataString(Service)}&CustomerId={Uri.EscapeDataString(CustomerId)}";
            return ExecuteRequestAsync<ClientModels.RecurringSchedule>(path, HttpMethod.Get);
        }

        public Task<ClientModels.RecurringScheduleAddResponse> RecurringScheduleChangeStatus(ClientModels.RecurringScheduleChangeStatusRequest args)
        {
            var path = "v6/Recurring/Change";
            return ExecuteRequestAsync<ClientModels.RecurringScheduleAddResponse>(path, HttpMethod.Post, args);
        }

        public Task<ClientModels.RecurringScheduleAddResponse> RecurringScheduleUpdate(ClientModels.RecurringSchedule args)
        {
            var path = "v6/Recurring/Update";
            return ExecuteRequestAsync<ClientModels.RecurringScheduleAddResponse>(path, HttpMethod.Post, args);
        }

        public Task<ClientModels.Transaction> Refund(ClientModels.AuthorizationRequest args)
        {
            var path = "Payment6/Refund";
            return ExecuteRequestAsync<ClientModels.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<ClientModels.Transaction> RefundToken(ClientModels.AuthorizationTokenRequest args)
        {
            var path = "Payment6/RefundToken";
            return ExecuteRequestAsync<ClientModels.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<ClientModels.Transaction> RefundByID(ClientModels.VoidByIdRequest args)
        {
            var path = "Payment6/RefundByID";
            return ExecuteRequestAsync<ClientModels.Transaction>(path, HttpMethod.Post, args);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _tokenSemaphore?.Dispose();
                // Do not dispose injected HttpClient as DI container owns it
                // Dispose other managed resources here if needed
            }

            // Free unmanaged resources here if any

            _disposed = true;
        }

        private static string Truncate(string value, int maxLength =512)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}