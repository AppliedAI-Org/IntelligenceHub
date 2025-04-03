using IntelligenceHub.API.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.Client.Interfaces
{
    /// <summary>
    /// Interface for interacting with Auth0 authentication services.
    /// </summary>
    public interface IAIAuth0Client
    {
        /// <summary>
        /// Requests a basic authentication token that is safe to share with front end clients.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Auth0 response.</returns>
        Task<Auth0Response?> RequestAuthToken();

        /// <summary>
        /// Requests an elevated authentication token that should not be shared with front end clients.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Auth0 response.</returns>
        Task<Auth0Response?> RequestElevatedAuthToken();
    }
}

