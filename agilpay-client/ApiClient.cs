using agilpay.models;
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
        private static string ClientId { get; set; }
        private static string ClientSecret { get; set; }
        private string Token { get; set; }
        private static string BaseUrl { get; set; }
        private static RestClient client { get; set; }
        private static string session_id { get; set; }

        private static object locker = new { };

        private static ApiClient? _instance;
        public static ApiClient Instance
        {
            get
            {
                if (string.IsNullOrWhiteSpace(BaseUrl))
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

        private ApiClient()
        {
            //BaseUrl = baseUrl;
            //client = new RestClient(BaseUrl);
        }

        public static async Task Initialize(string baseUrl, string clientId, string clientSecret)
        {
            BaseUrl = baseUrl;

            var options = new RestClientOptions(baseUrl)
            {
                ThrowOnAnyError = false,
                ThrowOnDeserializationError = false,
                FailOnDeserializationError= false

            };
            client = new RestClient(options);            

            session_id = Guid.NewGuid().ToString();
            ClientId = clientId;
            ClientSecret = clientSecret;
            await Instance.GetOAuth2TokenAsync(BaseUrl, ClientId, ClientSecret);
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
                var client = new RestClient(_baseUrl);
                var request = new RestRequest("oauth/token").AddParameter("grant_type", "client_credentials");
                request.AddParameter("client_id", _clientId);
                request.AddParameter("client_secret", _clientSecret);

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
                }
                    
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            return;
        }

        public async Task<AuthorizationResponse> AuthorizePayment(AuthorizationRequest AuthorizationRequest)
        {
            var request = new RestRequest("v6/Authorize") { Method = Method.Post };
            SetHeader(request);

            request.AddJsonBody(AuthorizationRequest);

            RestResponse response = await client.ExecuteAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Instance.GetOAuth2TokenAsync(BaseUrl, ClientId, ClientSecret);
                return await AuthorizePayment(AuthorizationRequest);
            }

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var rest = JsonConvert.DeserializeObject<AuthorizationResponse>(response.Content);
                return rest;
            }
            else
            {
                Console.WriteLine(response.Content);
                throw new Exception(response.Content);
            }
        }

        public async Task<AuthorizationResponse> AuthorizePaymentToken(AuthorizationTokenRequest AuthorizationRequest)
        {
            var request = new RestRequest("v6/AuthorizeToken") { Method = Method.Post };
            SetHeader(request);

            request.AddJsonBody(AuthorizationRequest);

            RestResponse response = await client.ExecuteAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Instance.GetOAuth2TokenAsync(BaseUrl, ClientId, ClientSecret);
                return await AuthorizePaymentToken(AuthorizationRequest);
            }

            if (response.IsSuccessStatusCode && response.Content != null)
            {
                var rest = JsonConvert.DeserializeObject<AuthorizationResponse>(response.Content);
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
            var request = new RestRequest("Payment5/GetCustomerTokens") { Method = Method.Get };
            SetHeader(request);

            request.AddParameter("CustomerID", CustomerID);

            var response = await client.ExecuteAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Instance.GetOAuth2TokenAsync(BaseUrl, ClientId, ClientSecret);
                return await GetCustomerTokens(CustomerID);
            }

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

            RestResponse response = await client.ExecuteAsync(request);


            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Instance.GetOAuth2TokenAsync(BaseUrl, ClientId, ClientSecret);
                return await GetBalance(balanceRequest);
            }

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
            var request = new RestRequest("Payment5/IsValidCard?CardNumber=" + cardNumber) { Method = Method.Get };

            SetHeader(request);

            RestResponse response = await client.ExecuteAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Instance.GetOAuth2TokenAsync(BaseUrl, ClientId, ClientSecret);
                return await IsValidCard(cardNumber);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Card number is invalid");
            }

            var result = response.Content;

            return !string.IsNullOrWhiteSpace(result) && result.ToLower().Trim() == "true";
        }

        public async Task<bool> IsValidRoutingNumber(string routingNumber)
        {
            var request = new RestRequest("Payment5/IsValidRoutingNumber?RoutingNumber=" + routingNumber) { Method = Method.Get };

            SetHeader(request);

            RestResponse response = await client.ExecuteAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Instance.GetOAuth2TokenAsync(BaseUrl, ClientId, ClientSecret);
                return await IsValidRoutingNumber(routingNumber);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Routing number is invalid");
            }

            var result = response.Content;

            return !string.IsNullOrWhiteSpace(result) && result.ToLower().Trim() == "true";
        }

        public async Task<bool> DeleteCustomerCard(DeleteTokenRequest deleteRequest)
        {
            var request = new RestRequest("Payment5/DeleteCustomerToken"){ Method = Method.Post };

            SetHeader(request);

            request.AddJsonBody(deleteRequest);

            RestResponse response = await client.ExecuteAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Instance.GetOAuth2TokenAsync(BaseUrl, ClientId, ClientSecret);
                return await DeleteCustomerCard(deleteRequest);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.Content);
            }

            return true;
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