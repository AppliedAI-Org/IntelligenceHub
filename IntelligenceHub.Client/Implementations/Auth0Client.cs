using IntelligenceHub.API.DTOs.Auth;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IntelligenceHub.Client.Implementations
{
    /// <summary>
    /// Client for interacting with Auth0 authentication services.
    /// </summary>
    public class Auth0Client : IAIAuth0Client
    {
        private readonly HttpClient _client;
        private readonly Auth0Request _request;
        private readonly Auth0Request _elevatedRequest;
        private readonly string _authEndpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="Auth0Client"/> class.
        /// </summary>
        /// <param name="settings">The Auth0 settings.</param>
        /// <param name="factory">The HTTP client factory.</param>
        public Auth0Client(IOptionsMonitor<AuthSettings> settings, IHttpClientFactory factory)
        {
            _client = factory.CreateClient();
            _authEndpoint = settings.CurrentValue.Domain + "/oauth/token";

            _request = new Auth0Request()
            {
                GrantType = "client_credentials",
                ClientId = settings.CurrentValue.DefaultClientId,
                ClientSecret = settings.CurrentValue.DefaultClientSecret,
                Audience = settings.CurrentValue.Audience,
            };

            _elevatedRequest = new Auth0Request()
            {
                GrantType = "client_credentials",
                ClientId = settings.CurrentValue.AdminClientId,
                ClientSecret = settings.CurrentValue.AdminClientSecret,
                Audience = settings.CurrentValue.Audience,
            };
        }

        /// <summary>
        /// Requests a basic authentication token that is safe to share with front end clients.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Auth0 response.</returns>
        public async Task<Auth0Response?> RequestAuthToken()
        {
            var json = JsonSerializer.Serialize(_request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = await _client.PostAsync(_authEndpoint, content);

                var jsonString = await response.Content.ReadAsStringAsync();

                var authToken = JsonSerializer.Deserialize<Auth0Response>(jsonString, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                return authToken ?? null;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        /// <summary>
        /// Requests an elevated authentication token that should not be shared with front end clients.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Auth0 response.</returns>
        public async Task<Auth0Response?> RequestElevatedAuthToken()
        {
            var json = JsonSerializer.Serialize(_elevatedRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = await _client.PostAsync(_authEndpoint, content);

                var jsonString = await response.Content.ReadAsStringAsync();

                var authToken = JsonSerializer.Deserialize<Auth0Response>(jsonString, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                return authToken ?? null;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
