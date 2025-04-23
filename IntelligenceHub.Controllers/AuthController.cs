using IntelligenceHub.API.DTOs.Auth;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.API.Controllers
{
    /// <summary>
    /// Controller for handling authentication-related requests.
    /// </summary>
    [ApiController]
    [Route("auth")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + ",BasicAuthentication", Policy = ElevatedAuthPolicy)]
    public class AuthController : ControllerBase
    {
        private readonly IAuthLogic _authLogic;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="authLogic">The authentication logic.</param>
        public AuthController(IAuthLogic authLogic)
        {
            _authLogic = authLogic;
        }

        /// <summary>
        /// Gets the admin authentication token.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing the admin authentication token.</returns>
        [HttpGet("admintoken")]
        [SwaggerOperation(OperationId = "GetElevatedAuthToken")]
        [ProducesResponseType(typeof(Auth0Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAdminToken()
        {
            try
            {
                var tokenResponse = await _authLogic.GetAdminAuthToken();
                if (tokenResponse == null) return Unauthorized();
                return Ok(tokenResponse);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// Gets the default authentication token.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing the default authentication token.</returns>
        [HttpGet("defaulttoken")]
        [SwaggerOperation(OperationId = "GetDefaultAuthToken")]
        [ProducesResponseType(typeof(Auth0Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDefaultToken()
        {
            try
            {
                var tokenResponse = await _authLogic.GetDefaultAuthToken();
                if (tokenResponse == null) return Unauthorized();
                return Ok(tokenResponse);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
            
        }
    }
}
