using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Controllers
{
    /// <summary>
    /// This controller is used to manage agent profiles.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    [Authorize(Policy = ElevatedAuthPolicy)]
    public class ProfileController : ControllerBase
    {
        private IProfileLogic _profileLogic;

        /// <summary>
        /// This controller is used to manage agent profiles.
        /// </summary>
        /// <param name="profileLogic">The agent profiles business logic.</param>
        public ProfileController(IProfileLogic profileLogic)
        {
            _profileLogic = profileLogic;
        }

        /// <summary>
        /// This endpoint is used to get a profile by name.
        /// </summary>
        /// <param name="name">The name of the agent profile.</param>
        /// <returns>The profile object.</returns>
        [HttpGet]
        [Route("get/{name}")]
        [ProducesResponseType(typeof(Profile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfile([FromRoute] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return BadRequest("Invalid route data. Please check your input.");
                var profileDto = await _profileLogic.GetProfile(name);
                if (profileDto is not null) return Ok(profileDto);
                return NotFound($"No profile with the name {name} was found.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to get all profiles.
        /// </summary>
        /// <returns>An ObjectResult containing list of profile objects.</returns>
        [HttpGet]
        [Route("get/all")]
        [ProducesResponseType(typeof(IEnumerable<Profile>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllProfiles()
        {
            try
            {
                var result = await _profileLogic.GetAllProfiles();
                if (result is null || result.Count() < 1) return NotFound("No profiles exist. Make a post request to add some.");
                else return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
            
        }

        /// <summary>
        /// This endpoint is used to create or update a profile.
        /// </summary>
        /// <param name="profileDto">The definition of the profile.</param>
        /// <returns>An ObjectResult containing the new definition of the profile.</returns>
        [HttpPost]
        [Route("upsert")]
        [ProducesResponseType(typeof(Profile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddOrUpdateProfile([FromBody] Profile profileDto)
        {
            try
            {
                var errorMessage = await _profileLogic.CreateOrUpdateProfile(profileDto);
                if (!string.IsNullOrEmpty(errorMessage)) return BadRequest(errorMessage);
                else return Ok(await _profileLogic.GetProfile(profileDto.Name));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to associate profile with tools already existing in the database.
        /// </summary>
        /// <param name="name">The name of the profile being associated.</param>
        /// <param name="tools">A list of the tools to add the profile to.</param>
        /// <returns>An ObjectResult containing the tools that were successfully associated with the profile.</returns>
        [HttpPost]
        [Route("associate/{name}")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddProfileToTools([FromRoute] string name, List<string> tools)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request. Please check the route parameter for the profile name: '{name}'.");
                if (tools is null || tools.Count < 1) return BadRequest($"Invalid request. The 'Tools' property cannot be null or empty: '{tools}'.");
                var errorMessage = await _profileLogic.AddProfileToTools(name, tools);
                if (errorMessage is null) return Ok(await _profileLogic.GetProfileToolAssociations(name));
                else return NotFound(errorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to dissociate a profile from tools already existing in the database.
        /// </summary>
        /// <param name="name">The name of the profile to dissociate.</param>
        /// <param name="tools">A list of tool names to dissociate from the profile.</param>
        /// <returns>An ObjectResult containing a list of tools that were successfully dissociated from the profile.</returns>
        [HttpPost]
        [Route("dissociate/{name}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveProfileFromTools([FromRoute] string name, List<string> tools)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request. Please check the route parameter for the profile name: '{name}'.");
                if (tools is null || tools.Count < 1) return BadRequest($"Invalid request. The 'Profiles' property cannot be null or empty: '{tools}'.");
                var errorMessage = await _profileLogic.DeleteProfileAssociations(name, tools);
                if (errorMessage is null) return NoContent();
                else return NotFound(errorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to delete a profile.
        /// </summary>
        /// <param name="name">The name of the profile.</param>
        /// <returns>An empty ObjectResult.</returns>
        [HttpDelete]
        [Route("delete/{name}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProfile([FromRoute] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return BadRequest($"Invalid request. Please check the route parameter for the profile name: {name}.");
                var errorMessage = await _profileLogic.DeleteProfile(name);
                if (errorMessage is not null) return NotFound(errorMessage);
                else return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
