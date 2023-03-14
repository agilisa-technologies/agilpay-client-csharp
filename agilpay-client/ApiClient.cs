using agilpay.models;
using Newtonsoft.Json;
using RestSharp;

namespace agilpay
{
    public class ApiClient
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Token { get; set; }
        public string BaseUrl { get; set; }
        private RestClient client { get; set; }
        private string session_id { get; set; }

        public ApiClient(string baseUrl)
        {
            BaseUrl = baseUrl;
            client = new RestClient(BaseUrl);
        }

        public async Task<bool> Init(string clientId, string clientSecret)
        {
            session_id = Guid.NewGuid().ToString();
            ClientId = clientId;
            ClientSecret = clientSecret;
            Token = await GetTokenAsync(BaseUrl, ClientId, ClientSecret);
            return (Token != null);
        }

        public async Task<string> GetTokenAsync(string _baseUrl, string _clientId, string _clientSecret)
        {
            string result = null;
            try
            {
                var client = new RestClient(_baseUrl);

                var request = new RestRequest("oauth/token").AddParameter("grant_type", "client_credentials");
                request.AddParameter("client_id", _clientId);
                request.AddParameter("client_secret", _clientSecret);

                TokenResponse response = await client.PostAsync<TokenResponse>(request);

                result = $"{response.TokenType} {response!.AccessToken}";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return result;
        }

        public async Task<AuthorizationResponse> Authorize(AuthorizationRequest AuthorizationRequest)
        {
            try
            {
                var request = new RestRequest("v6/Authorize");
                SetHeader(request);

                request.AddJsonBody(AuthorizationRequest);

                RestResponse response = await client.PostAsync(request);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var rest = JsonConvert.DeserializeObject<AuthorizationResponse>(response.Content);
                    return rest;
                }
                else
                {
                    Console.WriteLine(response.Content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public async Task<BalanceResponse> GetBalance(BalanceRequest balanceRequest)
        {
            try
            {
                var request = new RestRequest("v6/GetBalance");
                SetHeader(request);

                request.AddJsonBody(balanceRequest);

                RestResponse response = await client.PostAsync(request);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var rest = JsonConvert.DeserializeObject<BalanceResponse>(response.Content);
                    return rest;
                }
                else
                {
                    Console.WriteLine(response.Content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
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