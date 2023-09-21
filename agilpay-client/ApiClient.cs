using agilpay.models;
using agilpay.Util;
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;

namespace agilpay
{
    public class ApiClient
    {
        private string ClientId { get; set; }
        private string ClientSecret { get; set; }
        private string Token { get; set; }
        private DateTime TokenExpireTime { get; set; }
        private string BaseUrl { get; set; }
        private RestClient client { get; set; }
        private string session_id { get; set; }

        private static object locker = new { };

        private static ApiClient? _instance;
        public static ApiClient Instance
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_instance?.BaseUrl))
                {
                    throw new Exception("You must call initialize first");
                }

                lock(locker)
                {
                    if(_instance == null)
                    {
                        _instance = new ApiClient();
                    }

                    return _instance;
                }
            }
        }

        public ApiClient()
        {
            //BaseUrl = baseUrl;
            //client = new RestClient(BaseUrl);
        }

        public static async Task Initialize(string baseUrl, string clientId, string clientSecret)
        {
            Instance.BaseUrl = baseUrl;

            var options = new RestClientOptions(baseUrl)
            {
                ThrowOnAnyError = false,
                ThrowOnDeserializationError = false,
                FailOnDeserializationError= false

            };

            Instance.ClientId = clientId;
            Instance.ClientSecret = clientSecret;
            await Instance.GetOAuth2TokenAsync();
        }

        public async Task<bool> Init(string baseUrl, string clientId, string clientSecret)
        {
            BaseUrl = baseUrl;
            session_id = Guid.NewGuid().ToString();
            ClientId = clientId;
            ClientSecret = clientSecret;
            await GetOAuth2TokenAsync();
            return (Token != null);
        }

        private async Task GetOAuth2TokenAsync()
        {
            string result = null;
            try
            {
                client = new RestClient(BaseUrl);
                var request = new RestRequest("oauth/token").AddParameter("grant_type", "client_credentials");
                request.AddParameter("client_id", ClientId);
                request.AddParameter("client_secret", ClientSecret);

                var response = await client.PostAsync(request);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var token = JsonConvert.DeserializeObject<TokenResponse>(response.Content);

                    if(token == null)
                    {
                        return;
                    }

                    result = $"{token.token_type} {token!.access_token}";

                    Token = result;
                    TokenExpireTime = DateTime.UtcNow.AddSeconds(token.expires_in);
                }
                    
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            return;
        }

        private async Task CheckTokenExpiration()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                return;
            }

            if(DateTime.Compare(TokenExpireTime, DateTime.UtcNow) < 0)
            {
                await GetOAuth2TokenAsync();
            }
        }

        public async Task<Transaction> AuthorizePayment(AuthorizationRequest AuthorizationRequest)
        {
            var request = new RestRequest("v6/Authorize") { Method = Method.Post };
            SetHeader(request);

            request.AddJsonBody(AuthorizationRequest);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);


            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var rest = JsonConvert.DeserializeObject<Transaction>(response.Content);
                return rest;
            }
            else
            {
                Console.WriteLine(response.Content);
                throw new Exception(response.Content);
            }
        }

        public async Task<Transaction> AuthorizePaymentToken(AuthorizationTokenRequest AuthorizationRequest)
        {
            var request = new RestRequest("v6/AuthorizeToken") { Method = Method.Post };
            SetHeader(request);

            request.AddJsonBody(AuthorizationRequest);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var rest = JsonConvert.DeserializeObject<Transaction>(response.Content);
                return rest;
            }
            else
            {
                Console.WriteLine(response.Content);
                throw new Exception(response.Content);
            }
        }

        public async Task<List<CustomerAccount>> GetCustomerTokens(string CustomerID)
        {
            var request = new RestRequest("v6/GetCustomerTokens") { Method = Method.Get };
            SetHeader(request);

            request.AddParameter("CustomerID", CustomerID);


            await CheckTokenExpiration();

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var rest = JsonConvert.DeserializeObject<List<CustomerAccount>>(response.Content);
                return rest;
            }
            else
            {
                Console.WriteLine(response.Content);
                throw new Exception(response.Content);
            }
        }

        public async Task<BalanceResponse> GetBalance(BalanceRequest balanceRequest)
        {
            var request = new RestRequest("Payment6/GetBalance") { Method = Method.Post };
            SetHeader(request);

            request.AddJsonBody(balanceRequest);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);


            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var rest = JsonConvert.DeserializeObject<BalanceResponse>(response.Content);
                return rest;
            }
            else
            {
                Console.WriteLine(response.Content);
                throw new Exception(response.Content);
            }
        }

        public async Task<bool> IsValidCard(string cardNumber)
        {
            var request = new RestRequest("v6/IsValidCard?CardNumber=" + cardNumber) { Method = Method.Get };

            SetHeader(request);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Card number is invalid");
            }

            var result = response.Content;

            return !string.IsNullOrWhiteSpace(result) && result.ToLower().Trim() == "true";
        }

        public async Task<bool> IsValidRoutingNumber(string routingNumber)
        {
            var request = new RestRequest("v6/IsValidRoutingNumber?RoutingNumber=" + routingNumber) { Method = Method.Get };

            SetHeader(request);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Routing number is invalid");
            }

            var result = response.Content;

            return !string.IsNullOrWhiteSpace(result) && result.ToLower().Trim() == "true";
        }

        public async Task<bool> DeleteCustomerCard(DeleteTokenRequest deleteRequest)
        {
            var request = new RestRequest("v6/DeleteCustomerToken"){ Method = Method.Post };

            SetHeader(request);

            request.AddJsonBody(deleteRequest);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return true;
        }

        public async Task<string> CloseBatchResumen(string MerchantKey)
        {
            var request = new RestRequest("v6/CloseBatchResumen") { Method = Method.Post };

            SetHeader(request);

            request.AddJsonBody(new { MerchantKey });


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return response.Content;
        }

        public async Task<Transaction> VoidById(VoidByIdRequest args)
        {
            var request = new RestRequest("v6/VoidByID") { Method = Method.Post };

            SetHeader(request);

            request.AddJsonBody(args);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return JsonConvert.DeserializeObject<Transaction>(response.Content);
        }

        public async Task<Transaction> VoidSale(VoidSaleRequest args)
        {
            var request = new RestRequest("v6/VoidSale") { Method = Method.Post };

            SetHeader(request);

            request.AddJsonBody(args);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return JsonConvert.DeserializeObject<Transaction>(response.Content);
        }

        public async Task<Transaction> CaptureByID(VoidByIdRequest args)
        {
            var request = new RestRequest("v6/CaptureByID") { Method = Method.Post };

            SetHeader(request);

            request.AddJsonBody(args);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return JsonConvert.DeserializeObject<Transaction>(response.Content);
        }

        public async Task<Transaction> CaptureAdjustmendByID(CaptureAdjustmendByIDRequest args)
        {
            var request = new RestRequest("v6/CaptureAdjustmendByID") { Method = Method.Post };

            SetHeader(request);

            request.AddJsonBody(args);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return JsonConvert.DeserializeObject<Transaction>(response.Content);
        }

        public async Task<Transaction> GetTransactionByID(string MerchantKey, string IDTransaction)
        {
            var request = new RestRequest("v6/GetTransactionByID?MerchantKey=" + HttpUtility.UrlEncode(MerchantKey) + "&IDTransaction=" + HttpUtility.UrlEncode(IDTransaction)) { Method = Method.Get };

            SetHeader(request);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return JsonConvert.DeserializeObject<Transaction>(response.Content);
        }

        public async Task<RecurringScheduleAddResponse> RecurringScheduleAdd(RecurringScheduleAddRequest args)
        {
            var request = new RestRequest("v6/Recurring/Add") { Method = Method.Post };

            SetHeader(request);

            request.AddJsonBody(args);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return JsonConvert.DeserializeObject<RecurringScheduleAddResponse>(response.Content);
        }

        public async Task<RecurringSchedule> RecurringScheduleGet(string MerchantKey, string Service, string CustomerId)
        {
            var request = new RestRequest("/v6/Recurring/Get?MerchantKey=" + HttpUtility.UrlEncode(MerchantKey) + "&Service=" + HttpUtility.UrlEncode(Service) + "&CustomerId=" + HttpUtility.UrlEncode(CustomerId)) { Method = Method.Get };

            SetHeader(request);

            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);


            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return JsonConvert.DeserializeObject<RecurringSchedule>(response.Content);
        }

        public async Task<RecurringScheduleAddResponse> RecurringScheduleChangeStatus(RecurringScheduleChangeStatusRequest args)
        {
            var request = new RestRequest("v6/Recurring/Change") { Method = Method.Post };

            SetHeader(request);

            request.AddJsonBody(args);


            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);


            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return JsonConvert.DeserializeObject<RecurringScheduleAddResponse>(response.Content);
        }

        public async Task<RecurringScheduleAddResponse> RecurringScheduleUpdate(RecurringSchedule args)
        {
            var request = new RestRequest("v6/Recurring/Update") { Method = Method.Post };

            SetHeader(request);

            request.AddJsonBody(args);

            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return JsonConvert.DeserializeObject<RecurringScheduleAddResponse>(response.Content);
        }

        private void SetHeader(RestRequest request)
        {
            request.AddHeader("Content-Type", "application/json")
                                .AddHeader("SessionId", session_id)
                                .AddHeader("SiteId", ClientId)
                                .AddHeader("Authorization", Token);
        }
    }
}