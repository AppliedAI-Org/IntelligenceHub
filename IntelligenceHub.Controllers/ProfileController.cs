using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Controllers
{
    /// <summary>
    /// This controller is used to manage agent profiles.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileLogic _profileLogic;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileController"/> class.
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
        /// <returns>An <see cref="IActionResult"/> containing the profile object.</returns>
        [HttpGet]
        [Route("get/{name}")]
        [SwaggerOperation(OperationId = "GetProfileAsync")]
        [ProducesResponseType(typeof(Profile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProfile([FromRoute] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return BadRequest("Invalid route data. Please check your input.");
                var response = await _profileLogic.GetProfile(name);
                if (!response.IsSuccess) return NotFound(response.ErrorMessage);

                var profileDto = response.Data;
                return Ok(profileDto);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to get all profiles.
        /// </summary>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="count">The amount of profiles to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of profile objects.</returns>
        [HttpGet]
        [Route("get/all/page/{page}/count/{count}")]
        [SwaggerOperation(OperationId = "GetAllProfilesAsync")]
        [ProducesResponseType(typeof(IEnumerable<Profile>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllProfiles([FromRoute] int page, [FromRoute] int count)
        {
            try
            {
                if (page < 1) return BadRequest("The page must be greater than 0.");
                if (count < 1) return BadRequest("The count must be greater than 0.");
                var result = await _profileLogic.GetAllProfiles(page, count);
                return Ok(result.Data);
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
        /// <returns>An <see cref="IActionResult"/> containing the new definition of the profile.</returns>
        [HttpPost]
        [Route("upsert")]
        [SwaggerOperation(OperationId = "UpsertProfileAsync")]
        [ProducesResponseType(typeof(Profile), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddOrUpdateProfile([FromBody] Profile profileDto)
        {
            try
            {
                var updateResponse = await _profileLogic.CreateOrUpdateProfile(profileDto);
                if (!updateResponse.IsSuccess)
                {
                    if (updateResponse.StatusCode == APIResponseStatusCodes.BadRequest) return BadRequest(updateResponse.ErrorMessage);
                    if (updateResponse.StatusCode == APIResponseStatusCodes.InternalError) return StatusCode(StatusCodes.Status500InternalServerError, updateResponse.ErrorMessage);
                }

                var profileResponse = await _profileLogic.GetProfile(profileDto.Name);
                if (!profileResponse.IsSuccess) return StatusCode(StatusCodes.Status500InternalServerError, "Error returning the newly created profile details.");
                else return Ok(profileResponse.Data);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to associate a profile with tools already existing in the database.
        /// </summary>
        /// <param name="name">The name of the profile being associated.</param>
        /// <param name="tools">A list of the tools to add the profile to.</param>
        /// <returns>An <see cref="IActionResult"/> containing the tools that were successfully associated with the profile.</returns>
        [HttpPost]
        [Route("associate/{name}")]
        [SwaggerOperation(OperationId = "AssociateProfileWithToolsAsync")]
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
                var response = await _profileLogic.AddProfileToTools(name, tools);
                if (response.IsSuccess)
                {
                    var getResponse = await _profileLogic.GetProfileToolAssociations(name);
                    return Ok(getResponse.Data);
                }
                else return NotFound(response.ErrorMessage);
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
        /// <returns>An <see cref="IActionResult"/> containing a list of tools that were successfully dissociated from the profile.</returns>
        [HttpPost]
        [Route("dissociate/{name}")]
        [SwaggerOperation(OperationId = "DissociateProfileFromToolsAsync")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveProfileFromTools([FromRoute] string name, List<string> tools)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request. Please check the route parameter for the profile name: '{name}'.");
                if (tools is null || tools.Count < 1) return BadRequest($"Invalid request. The 'Tools' property cannot be null or empty: '{tools}'.");
                var response = await _profileLogic.DeleteProfileAssociations(name, tools);

                if (response.IsSuccess) return Ok(response.Data);
                else return NotFound(response.ErrorMessage);
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
        /// <returns>An empty <see cref="IActionResult"/>.</returns>
        [HttpDelete]
        [Route("delete/{name}")]
        [SwaggerOperation(OperationId = "DeleteProfileAsync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProfile([FromRoute] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return BadRequest($"Invalid request. Please check the route parameter for the profile name: {name}.");
                var response = await _profileLogic.DeleteProfile(name);

                if (!response.IsSuccess)
                {
                    if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                    if (response.StatusCode == APIResponseStatusCodes.InternalError) return StatusCode(StatusCodes.Status500InternalServerError, response.ErrorMessage);
                }
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
