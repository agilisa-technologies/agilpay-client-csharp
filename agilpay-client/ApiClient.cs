
using agilpay.client.models;
using agilpay.models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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


        private static readonly object locker = new object();
        private static ApiClient _instance;
        
        public static ApiClient Instance
        {
            get
            {
                lock (locker)
                {
                    if(_instance == null)
                    {
                        throw new Exception("You must call InitSingleton first");
                    }

                    return _instance;
                }
            }
        }

        public static async Task<ApiClient> InitSingleton(string baseUrl, string clientId, string clientSecret)
        {
            lock (locker)
            {
                if(_instance != null)
                {
                    return _instance;
                }
                _instance = new ApiClient(baseUrl);
            }
            await _instance.Init(clientId, clientSecret);

            return _instance;
        }


        public ApiClient(string baseUrl)
        {
            BaseUrl = baseUrl;
            var options = new RestClientOptions(baseUrl)
            {
                ThrowOnAnyError = false,
                ThrowOnDeserializationError = false,
                FailOnDeserializationError = false,
                BaseUrl = new Uri(baseUrl)

            };
            client = new RestClient(options);
        }

        public async Task Init(string clientId, string clientSecret)
        {
            session_id = Guid.NewGuid().ToString();
            ClientId = clientId;
            ClientSecret = clientSecret;
            await GetOAuth2TokenAsync(BaseUrl, ClientId, ClientSecret);
        }

       /* public async Task<bool> Init(string clientId, string clientSecret)
        {
            session_id = Guid.NewGuid().ToString();
            ClientId = clientId;
            ClientSecret = clientSecret;
            //Token = await GetOAuth2TokenAsync(BaseUrl, ClientId, ClientSecret);
            return (Token != null);
        }*/

        private async Task GetOAuth2TokenAsync(string _baseUrl, string _clientId, string _clientSecret)
        {
            string result = null;
            try
            {

                var client = new HttpClient() { BaseAddress = new Uri(_baseUrl) };

                /*var request = new RestRequest("oauth/token").AddParameter("grant_type", "client_credentials");
                request.AddParameter("client_id", _clientId);
                request.AddParameter("client_secret", _clientSecret);
                */

                Dictionary<string, string> args = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret }
                };


                var response = await client.PostAsync("oauth/token", new FormUrlEncodedContent(args));


                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(await response.Content.ReadAsStringAsync());
                }

                var token = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());

                if(token == null)
                {
                    throw new Exception("Cannot get Auth token at this time, please try later");
                }
                result = $"{token.token_type} {token.access_token}";
                Token = result;
                TokenExpireTime = DateTime.UtcNow.AddSeconds(token.expires_in);

                /*if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var token = JsonConvert.DeserializeObject<TokenResponse>(response.Content);

                    if(token == null)
                    {
                        return;
                    }

                    result = $"{token.token_type} {token.access_token}";

                    Token = result;
                    TokenExpireTime = DateTime.UtcNow.AddSeconds(token.expires_in);
                }*/
                    
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
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
                await GetOAuth2TokenAsync(BaseUrl, ClientId, ClientSecret);
            }
        }

        public async Task<Transaction> AuthorizePayment(AuthorizationRequest AuthorizationRequest)
        {
            var request = new RestRequest("Payment6.1/Autorize") { Method = Method.Post };
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
                return new Transaction
                {
                    Message = response.Content,
                    ResponseCode = "99",
                    Status = "REJECTED"
                };
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
                return new Transaction
                {
                    ResponseCode = "99",
                    Message = response.Content,
                    Status = "REJECTED"
                };
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

        public async Task<CustomerAccount> RegisterToken(RegisterTokenRequest args)
        {
            var request = new RestRequest("v6/RegisterToken") { Method = Method.Post };

            SetHeader(request);

            request.AddJsonBody(args);

            await CheckTokenExpiration();

            RestResponse response = await client.ExecuteAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return JsonConvert.DeserializeObject<CustomerAccount>(response.Content);
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

        public async Task<Transaction> Refund(AuthorizationRequest args)
        {
            var request = new RestRequest("Payment6/Refund") { Method = Method.Post };

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

        public async Task<Transaction> RefundToken(AuthorizationTokenRequest args)
        {
            var request = new RestRequest("Payment6/RefundToken") { Method = Method.Post };

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

        public async Task<Transaction> RefundByID(VoidByIdRequest args)
        {
            var request = new RestRequest("Payment6/RefundByID") { Method = Method.Post };

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

        private void SetHeader(RestRequest request)
        {
            request.AddHeader("Content-Type", "application/json")
                                .AddHeader("SessionId", session_id)
                                .AddHeader("SiteId", ClientId)
                                .AddHeader("Authorization", Token);
        }
    }
}