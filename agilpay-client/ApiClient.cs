using agilpay.client.models;
using agilpay.models;
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
using System.Web;

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
        private string session_id { get; set; }

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

            session_id = Guid.NewGuid().ToString();
            _logger = logger;

            // set headers that do not change per request
            if (_httpClient.DefaultRequestHeaders.Contains("SessionId"))
                _httpClient.DefaultRequestHeaders.Remove("SessionId");
            _httpClient.DefaultRequestHeaders.Add("SessionId", session_id);

            if (!string.IsNullOrWhiteSpace(ClientId))
            {
                if (_httpClient.DefaultRequestHeaders.Contains("SiteId"))
                    _httpClient.DefaultRequestHeaders.Remove("SiteId");
                _httpClient.DefaultRequestHeaders.Add("SiteId", ClientId);
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
                    _logger?.LogError("Failed to obtain token for ClientId={ClientId}. Status={Status}, Body={Body}", ClientId, response.StatusCode, responseBody);
                    throw new Exception(responseBody);
                }

                var token = JsonSerializer.Deserialize<TokenResponse>(responseBody, _jsonOptions);

                if (token == null)
                {
                    _logger?.LogError("Token response deserialization returned null for ClientId={ClientId}", ClientId);
                    throw new Exception("Cannot get Auth token at this time, please try later");
                }

                Token = token.access_token;
                TokenExpireTime = DateTime.UtcNow.AddSeconds(token.expires_in);

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

                    _logger?.LogWarning("Request failed {Method} {Path} Status={StatusCode} Body={Body}", method, path, response.StatusCode, content);
                    throw new Exception(content);
                }
            }
        }

        public async Task<client.models.Transaction> AuthorizePayment(AuthorizationRequest AuthorizationRequest)
        {
            var path = "v6/Autorize";

            try
            {
                var response = await ExecuteRequestAsync<client.models.Transaction>(path, HttpMethod.Post, AuthorizationRequest).ConfigureAwait(false);
                return response ?? new client.models.Transaction { ResponseCode = "99", Status = "REJECTED", Message = "Empty response" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AuthorizePayment failed for MerchantKey={MerchantKey}", AuthorizationRequest?.MerchantKey);
                return new client.models.Transaction { Message = ex.Message, ResponseCode = "99", Status = "REJECTED" };
            }
        }

        public async Task<client.models.Transaction> AuthorizePaymentToken(AuthorizationTokenRequest AuthorizationRequest)
        {
            var path = "v6/AuthorizeToken";

            try
            {
                var response = await ExecuteRequestAsync<client.models.Transaction>(path, HttpMethod.Post, AuthorizationRequest).ConfigureAwait(false);
                return response ?? new client.models.Transaction { ResponseCode = "99", Status = "REJECTED", Message = "Empty response" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AuthorizePaymentToken failed for MerchantKey={MerchantKey}", AuthorizationRequest?.MerchantKey);
                return new client.models.Transaction { Message = ex.Message, ResponseCode = "99", Status = "REJECTED" };
            }
        }

        public Task<List<CustomerAccount>> GetCustomerTokens(string CustomerID)
        {
            var path = $"v6/GetCustomerTokens?CustomerID={Uri.EscapeDataString(CustomerID)}";
            return ExecuteRequestAsync<List<CustomerAccount>>(path, HttpMethod.Get);
        }

        public Task<BalanceResponse> GetBalance(BalanceRequest balanceRequest)
        {
            var path = "Payment6/GetBalance";
            return ExecuteRequestAsync<BalanceResponse>(path, HttpMethod.Post, balanceRequest);
        }

        public async Task<bool> IsValidCard(string cardNumber)
        {
            var path = $"v6/IsValidCard?CardNumber={Uri.EscapeDataString(cardNumber)}";
            var result = await ExecuteRequestAsync<string>(path, HttpMethod.Get).ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(result) && result.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> IsValidRoutingNumber(string routingNumber)
        {
            var path = $"v6/IsValidRoutingNumber?RoutingNumber={Uri.EscapeDataString(routingNumber)}";
            var result = await ExecuteRequestAsync<string>(path, HttpMethod.Get).ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(result) && result.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> DeleteCustomerCard(DeleteTokenRequest deleteRequest)
        {
            var path = "v6/DeleteCustomerToken";
            await ExecuteRequestAsync<string>(path, HttpMethod.Post, deleteRequest).ConfigureAwait(false);
            return true;
        }

        public Task<CustomerAccount> RegisterToken(RegisterTokenRequest args)
        {
            var path = "v6/RegisterToken";
            return ExecuteRequestAsync<CustomerAccount>(path, HttpMethod.Post, args);
        }

        public Task<string> CloseBatchResumen(string MerchantKey)
        {
            var path = "v6/CloseBatchResumen";
            return ExecuteRequestAsync<string>(path, HttpMethod.Post, new { MerchantKey });
        }

        public Task<client.models.Transaction> VoidById(VoidByIdRequest args)
        {
            var path = "v6/VoidByID";
            return ExecuteRequestAsync<client.models.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<client.models.Transaction> VoidSale(VoidSaleRequest args)
        {
            var path = "v6/VoidSale";
            return ExecuteRequestAsync<client.models.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<client.models.Transaction> CaptureByID(VoidByIdRequest args)
        {
            var path = "v6/CaptureByID";
            return ExecuteRequestAsync<client.models.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<client.models.Transaction> CaptureAdjustmendByID(CaptureAdjustmendByIDRequest args)
        {
            var path = "v6/CaptureAdjustmendByID";
            return ExecuteRequestAsync<client.models.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<client.models.Transaction> GetTransactionByID(string MerchantKey, string IDTransaction)
        {
            var path = $"v6/GetTransactionByID?MerchantKey={HttpUtility.UrlEncode(MerchantKey)}&IDTransaction={HttpUtility.UrlEncode(IDTransaction)}";
            return ExecuteRequestAsync<client.models.Transaction>(path, HttpMethod.Get);
        }

        public Task<RecurringScheduleAddResponse> RecurringScheduleAdd(RecurringScheduleAddRequest args)
        {
            var path = "v6/Recurring/Add";
            return ExecuteRequestAsync<RecurringScheduleAddResponse>(path, HttpMethod.Post, args);
        }

        public Task<client.models.RecurringSchedule> RecurringScheduleGet(string MerchantKey, string Service, string CustomerId)
        {
            var path = $"/v6/Recurring/Get?MerchantKey={HttpUtility.UrlEncode(MerchantKey)}&Service={HttpUtility.UrlEncode(Service)}&CustomerId={HttpUtility.UrlEncode(CustomerId)}";
            return ExecuteRequestAsync<client.models.RecurringSchedule>(path, HttpMethod.Get);
        }

        public Task<RecurringScheduleAddResponse> RecurringScheduleChangeStatus(RecurringScheduleChangeStatusRequest args)
        {
            var path = "v6/Recurring/Change";
            return ExecuteRequestAsync<RecurringScheduleAddResponse>(path, HttpMethod.Post, args);
        }

        public Task<RecurringScheduleAddResponse> RecurringScheduleUpdate(RecurringSchedule args)
        {
            var path = "v6/Recurring/Update";
            return ExecuteRequestAsync<RecurringScheduleAddResponse>(path, HttpMethod.Post, args);
        }

        public Task<client.models.Transaction> Refund(AuthorizationRequest args)
        {
            var path = "Payment6/Refund";
            return ExecuteRequestAsync<client.models.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<client.models.Transaction> RefundToken(AuthorizationTokenRequest args)
        {
            var path = "Payment6/RefundToken";
            return ExecuteRequestAsync<client.models.Transaction>(path, HttpMethod.Post, args);
        }

        public Task<client.models.Transaction> RefundByID(VoidByIdRequest args)
        {
            var path = "Payment6/RefundByID";
            return ExecuteRequestAsync<client.models.Transaction>(path, HttpMethod.Post, args);
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
    }
}