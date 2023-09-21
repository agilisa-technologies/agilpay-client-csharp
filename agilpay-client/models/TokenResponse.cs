using System.Text.Json.Serialization;

namespace agilpay.client.models
{
    public class TokenResponse
    {
        [JsonPropertyName("token_type")]
        public string token_type { get; set; }
        [JsonPropertyName("access_token")]
        public string access_token { get; set; }

        public long expires_in { get; set; }
    }
}
