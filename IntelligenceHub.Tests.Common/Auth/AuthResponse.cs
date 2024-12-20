using System.Text.Json.Serialization;

namespace IntelligenceHub.Tests.Common.Auth
{
    public class AuthResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("tokens_type")]
        public string TokenType { get; set; }
    }
}
