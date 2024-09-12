
using agilpay.client.models;
using agilpay.models;
using Newtonsoft.Json;
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
        private static HttpClient client { get; set; }
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
            //var options = new RestClientOptions(baseUrl)
            //{
            //    ThrowOnAnyError = false,
            //    ThrowOnDeserializationError = false,
            //    FailOnDeserializationError = false,
            //    BaseUrl = new Uri(baseUrl)

            //};
            client = new HttpClient()
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromMinutes(3)
            };
        }

        public async Task Init(string clientId, string clientSecret)
        {
            session_id = Guid.NewGuid().ToString();
            ClientId = clientId;
            ClientSecret = clientSecret;
            SetHeader();
            await GetOAuth2TokenAsync(ClientId, ClientSecret);
        }


        private async Task GetOAuth2TokenAsync(string _clientId, string _clientSecret)
        {
            string result = null;
            try
            {

                //var client = new HttpClient() { BaseAddress = new Uri(_baseUrl) };

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

                if (client.DefaultRequestHeaders.Contains("Authorization"))
                {
                    client.DefaultRequestHeaders.Remove("Authorization");
                }

                client.DefaultRequestHeaders.Add("Authorization", Token);


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
                await GetOAuth2TokenAsync(ClientId, ClientSecret);
            }
        }

        public async Task<Transaction> AuthorizePayment(AuthorizationRequest AuthorizationRequest)
        {
            //var request = new RestRequest("Payment6.1/Autorize") { Method = Method.Post };
            //SetHeader(request);

            //request.AddJsonBody(AuthorizationRequest);

            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(AuthorizationRequest);

            var response = await client.PostAsync("Payment6.1/Autorize", new StringContent(json,Encoding.UTF8, "application/json"));


            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new Transaction
                {
                    Message = msg,
                    ResponseCode = "99",
                    Status = "REJECTED"
                };
            }

            return JsonConvert.DeserializeObject<Transaction>(msg);

        }

        public async Task<Transaction> AuthorizePaymentToken(AuthorizationTokenRequest AuthorizationRequest)
        {
            //var request = new RestRequest("v6/AuthorizeToken") { Method = Method.Post };
            //SetHeader(request);

            //request.AddJsonBody(AuthorizationRequest);


            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(AuthorizationRequest);

            var response = await client.PostAsync("v6/AuthorizeToken", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if(!response.IsSuccessStatusCode)
            {
                return new Transaction
                {
                    ResponseCode = "99",
                    Message = msg,
                    Status = "REJECTED"
                };
            }

            return JsonConvert.DeserializeObject<Transaction>(msg);

        }

        public async Task<List<CustomerAccount>> GetCustomerTokens(string CustomerID)
        {
            //var request = new RestRequest("v6/GetCustomerTokens") { Method = Method.Get };
            //SetHeader(request);

            //request.AddParameter("CustomerID", CustomerID);


            await CheckTokenExpiration();

            var response = await client.GetAsync("v6/GetCustomerTokens?CustomerID=" + CustomerID);

            var msg = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var rest = JsonConvert.DeserializeObject<List<CustomerAccount>>(msg);
                return rest;
            }
            else
            {
                Console.WriteLine(msg);
                throw new Exception(msg);
            }
        }

        public async Task<BalanceResponse> GetBalance(BalanceRequest balanceRequest)
        {
            //var request = new RestRequest("Payment6/GetBalance") { Method = Method.Post };
            //SetHeader(request);

            //request.AddJsonBody(balanceRequest);


            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(balanceRequest);

            var response = await client.PostAsync("Payment6/GetBalance", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var rest = JsonConvert.DeserializeObject<BalanceResponse>(msg);
                return rest;
            }
            else
            {
                Console.WriteLine(msg);
                throw new Exception(msg);
            }
        }

        public async Task<bool> IsValidCard(string cardNumber)
        {
            //var request = new RestRequest("v6/IsValidCard?CardNumber=" + cardNumber) { Method = Method.Get };

            //SetHeader(request);


            await CheckTokenExpiration();


            var response = await client.GetAsync("v6/IsValidCard?CardNumber="+cardNumber);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Card number is invalid");
            }

            var msg = await response.Content.ReadAsStringAsync();

            return !string.IsNullOrWhiteSpace(msg) && msg.ToLower().Trim() == "true";
        }

        public async Task<bool> IsValidRoutingNumber(string routingNumber)
        {
            //var request = new RestRequest("v6/IsValidRoutingNumber?RoutingNumber=" + routingNumber) { Method = Method.Get };

            //SetHeader(request);


            await CheckTokenExpiration();

            

            var response = await client.GetAsync("v6/IsValidRoutingNumber?RoutingNumber="+routingNumber);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Routing number is invalid");
            }

            var msg = await response.Content.ReadAsStringAsync();

            return !string.IsNullOrWhiteSpace(msg) && msg.ToLower().Trim() == "true";
        }

        public async Task<bool> DeleteCustomerCard(DeleteTokenRequest deleteRequest)
        {
            //var request = new RestRequest("v6/DeleteCustomerToken"){ Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(deleteRequest);


            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(deleteRequest);

            var response = await client.PostAsync("v6/DeleteCustomerToken", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return true;
        }

        public async Task<CustomerAccount> RegisterToken(RegisterTokenRequest args)
        {
            //var request = new RestRequest("v6/RegisterToken") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(args);

            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(args);

            var response = await client.PostAsync("v6/RegisterToken", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<CustomerAccount>(msg);
        }

        public async Task<List<BatchResumen>> CloseBatchResumen(string MerchantKey, string Client_Id, string Currency, bool ForzarBatch)
        {
            //var request = new RestRequest("v6/CloseBatchResumen") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(new { MerchantKey, Client_Id, Currency, ForzarBatch });


            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(new { MerchantKey, Client_Id, Currency, ForzarBatch });

            var response = await client.PostAsync("v6/CloseBatchResumen", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<List<BatchResumen>>(msg);
        }

        public async Task<Transaction> VoidById(VoidByIdRequest args)
        {
            //var request = new RestRequest("v6/VoidByID") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(args);


            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(args);

            var response = await client.PostAsync("v6/VoidByID", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<Transaction>(msg);
        }

        public async Task<Transaction> VoidSale(VoidSaleRequest args)
        {
            //var request = new RestRequest("v6/VoidSale") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(args);


            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(args);

            var response = await client.PostAsync("v6/VoidSale", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<Transaction>(msg);
        }

        public async Task<Transaction> CaptureByID(VoidByIdRequest args)
        {
            //var request = new RestRequest("v6/CaptureByID") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(args);


            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(args);

            var response = await client.PostAsync("v6/CaptureByID", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<Transaction>(msg);
        }

        public async Task<Transaction> CaptureAdjustmendByID(CaptureAdjustmendByIDRequest args)
        {
            //var request = new RestRequest("v6/CaptureAdjustmendByID") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(args);


            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(args);

            var response = await client.PostAsync("v6/CaptureAdjustmendByID", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<Transaction>(msg);
        }

        public async Task<Transaction> GetTransactionByID(string MerchantKey, string IDTransaction)
        {
            //var request = new RestRequest("v6/GetTransactionByID?MerchantKey=" + HttpUtility.UrlEncode(MerchantKey) + "&IDTransaction=" + HttpUtility.UrlEncode(IDTransaction)) { Method = Method.Get };

            //SetHeader(request);


            await CheckTokenExpiration();

            var response = await client.GetAsync("v6/GetTransactionByID?MerchantKey=" + HttpUtility.UrlEncode(MerchantKey) + "&IDTransaction=" + HttpUtility.UrlEncode(IDTransaction));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<Transaction>(msg);
        }

        public async Task<RecurringScheduleAddResponse> RecurringScheduleAdd(RecurringScheduleAddRequest args)
        {
            //var request = new RestRequest("v6/Recurring/Add") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(args);


            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(args);

            var response = await client.PostAsync("v6/Recurring/Add", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<RecurringScheduleAddResponse>(msg);
        }

        public async Task<RecurringSchedule> RecurringScheduleGet(string MerchantKey, string Service, string CustomerId)
        {
            //var request = new RestRequest("/v6/Recurring/Get?MerchantKey=" + HttpUtility.UrlEncode(MerchantKey) + "&Service=" + HttpUtility.UrlEncode(Service) + "&CustomerId=" + HttpUtility.UrlEncode(CustomerId)) { Method = Method.Get };

            //SetHeader(request);

            //await CheckTokenExpiration();

            var response = await client.GetAsync("/v6/Recurring/Get?MerchantKey=" + HttpUtility.UrlEncode(MerchantKey) + "&Service=" + HttpUtility.UrlEncode(Service) + "&CustomerId=" + HttpUtility.UrlEncode(CustomerId));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<RecurringSchedule>(msg);
        }

        public async Task<RecurringScheduleAddResponse> RecurringScheduleChangeStatus(RecurringScheduleChangeStatusRequest args)
        {
            //var request = new RestRequest("v6/Recurring/Change") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(args);


            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(args);

            var response = await client.PostAsync("v6/Recurring/Change", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<RecurringScheduleAddResponse>(msg);
        }

        public async Task<RecurringScheduleAddResponse> RecurringScheduleUpdate(RecurringSchedule args)
        {
            //var request = new RestRequest("v6/Recurring/Update") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(args);

            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(args);

            var response = await client.PostAsync("v6/Recurring/Update", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<RecurringScheduleAddResponse>(msg);
        }

        public async Task<Transaction> Refund(AuthorizationRequest args)
        {
            //var request = new RestRequest("Payment6/Refund") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(args);

            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(args);

            var response = await client.PostAsync("Payment6/Refund", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<Transaction>(msg);
        }

        public async Task<Transaction> RefundToken(AuthorizationTokenRequest args)
        {
            //var request = new RestRequest("Payment6/RefundToken") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(args);

            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(args);

            var response = await client.PostAsync("Payment6/RefundToken", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<Transaction>(msg);
        }

        public async Task<Transaction> RefundByID(VoidByIdRequest args)
        {
            //var request = new RestRequest("Payment6/RefundByID") { Method = Method.Post };

            //SetHeader(request);

            //request.AddJsonBody(args);

            await CheckTokenExpiration();

            var json = JsonConvert.SerializeObject(args);

            var response = await client.PostAsync("Payment6/RefundByID", new StringContent(json, Encoding.UTF8, "application/json"));

            var msg = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(msg);
            }

            return JsonConvert.DeserializeObject<Transaction>(msg);
        }

        private void SetHeader()
        {
            client.DefaultRequestHeaders.Add("SessionId", session_id);
            client.DefaultRequestHeaders.Add("SiteId", ClientId);
            

            //request.AddHeader("Content-Type", "application/json")
            //                    .AddHeader("SessionId", session_id)
            //                    .AddHeader("SiteId", ClientId)
            //                    .AddHeader("Authorization", Token);
        }
    }
}