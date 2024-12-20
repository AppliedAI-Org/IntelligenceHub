using Polly;
using Polly.Retry;
using System.Net.Http.Headers;

namespace IntelligenceHub.Tests.Common.Auth
{
    public class AuthHandler : DelegatingHandler
    {
        private readonly AuthClient _authClient;
        private AuthResponse? _token;
        private DateTime _tokenExpiry;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        public AuthHandler(AuthClient authClient, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            _authClient = authClient;

            // could pass this into the controller using dependency injection, if a global policy is desired
            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Ensure token is valid before the request
            if (_token == null || DateTime.UtcNow.AddMinutes(1) > _tokenExpiry)
            {
                _token = await _authClient.RequestElevatedAuthToken();
                if (_token == null)
                {
                    throw new InvalidOperationException("Failed to retrieve access token.");
                }
                _tokenExpiry = DateTime.UtcNow.AddSeconds(_token.ExpiresIn);
            }

            // Attach token to the Authorization header
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);

            // Execute the request with retry policy
            return await _retryPolicy.ExecuteAsync(() => base.SendAsync(request, cancellationToken));
        }
    }
}
