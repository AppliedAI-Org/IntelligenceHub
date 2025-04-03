using IntelligenceHub.API.DTOs.Auth;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Client.Interfaces;
using System.Threading.Tasks;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Provides authentication logic for the application.
    /// </summary>
    public class AuthLogic : IAuthLogic
    {
        private readonly IAIAuth0Client _auth0Client;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthLogic"/> class.
        /// </summary>
        /// <param name="auth0Client">The Auth0 client used for authentication requests.</param>
        public AuthLogic(IAIAuth0Client auth0Client)
        {
            _auth0Client = auth0Client;
        }

        /// <summary>
        /// Gets the default authentication token.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Auth0 response.</returns>
        public async Task<Auth0Response?> GetDefaultAuthToken()
        {
            return await _auth0Client.RequestAuthToken();
        }

        /// <summary>
        /// Gets the admin authentication token.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Auth0 response.</returns>
        public async Task<Auth0Response?> GetAdminAuthToken()
        {
            return await _auth0Client.RequestElevatedAuthToken();
        }
    }
}
