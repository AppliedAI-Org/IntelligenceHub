using IntelligenceHub.API.DTOs.Auth;
using System.Threading.Tasks;

namespace IntelligenceHub.Business.Interfaces
{
    /// <summary>
    /// Defines methods for authentication logic.
    /// </summary>
    public interface IAuthLogic
    {
        /// <summary>
        /// Gets the default authentication token.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Auth0 response.</returns>
        Task<Auth0Response?> GetDefaultAuthToken();

        /// <summary>
        /// Gets the admin authentication token.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Auth0 response.</returns>
        Task<Auth0Response?> GetAdminAuthToken();
    }
}
