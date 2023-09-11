using System.Text.Json.Serialization;

namespace agilpay.models
{
    record TokenResponse
    {
        [JsonPropertyName("token_type")]
        public string token_type { get; init; }
        [JsonPropertyName("access_token")]
        public string access_token { get; init; }

        public long expires_in { get; set; }
    }
}
