using System.Text.Json.Serialization;

namespace agilpay.models
{
    record TokenResponse
    {
        [JsonPropertyName("token_type")]
        public string TokenType { get; init; }
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; }
    }
}
