using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using static IntelligenceHub.Common.GlobalVariables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IntelligenceHub.Controllers
{
    /// <summary>
    /// This controller is used to manage agent tools.
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    [Authorize(Policy = ElevatedAuthPolicy)]
    public class ToolController : ControllerBase
    {
        private readonly IProfileLogic _profileLogic;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolController"/> class.
        /// </summary>
        /// <param name="profileLogic">The profile logic.</param>
        public ToolController(IProfileLogic profileLogic)
        {
            _profileLogic = profileLogic;
        }

        /// <summary>
        /// This endpoint is used to get a tool by name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>An <see cref="IActionResult"/> containing the tool.</returns>
        [HttpGet]
        [Route("get/{name}")]
        [SwaggerOperation(OperationId = "GetToolAsync")]
        [ProducesResponseType(typeof(Tool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTool([FromRoute] string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest("Invalid request. Please check the route parameter for the profile name.");
                var response = await _profileLogic.GetTool(name);

                if (response.IsSuccess) return Ok(response.Data);
                else return NotFound(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to get all tools.
        /// </summary>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="count">The amount of tools to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of tools.</returns>
        [HttpGet]
        [Route("get/all/page/{page}/count/{count}")]
        [SwaggerOperation(OperationId = "GetAllToolsAsync")]
        [ProducesResponseType(typeof(IEnumerable<Tool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllTools([FromRoute] int page, [FromRoute] int count)
        {
            try
            {
                if (page < 1) return BadRequest("The page must be greater than 0.");
                if (count < 1) return BadRequest("The count must be greater than 0.");
                var tools = await _profileLogic.GetAllTools(page, count);
                return Ok(tools.Data ?? new List<Tool>());
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to get the profiles associated with a tool.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of profile names that use the tool.</returns>
        [HttpGet]
        [Route("get/{name}/profiles")]
        [SwaggerOperation(OperationId = "GetToolProfilesAsync")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetToolProfiles(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest("Invalid request. Please check the route parameter for the profile name.");
                var response = await _profileLogic.GetToolProfileAssociations(name);
                if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                return Ok(response.Data ?? new List<string>());
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to create or update a tool.
        /// </summary>
        /// <param name="toolList">An array of tool definitions to add or update.</param>
        /// <returns>An <see cref="IActionResult"/> containing the updated list of tools.</returns>
        [HttpPost]
        [Route("upsert")]
        [SwaggerOperation(OperationId = "UpsertToolAsync")]
        [ProducesResponseType(typeof(List<Tool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddOrUpdateTool([FromBody] List<Tool> toolList)
        {
            try
            {
                var response = await _profileLogic.CreateOrUpdateTools(toolList);
                if (!response.IsSuccess) return BadRequest(response.ErrorMessage);
                else return Ok(toolList);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to associate a tool with profiles that already exist in the database.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <param name="profiles">An array of profile names.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of profile names that were successfully associated.</returns>
        [HttpPost]
        [Route("associate/{name}")]
        [SwaggerOperation(OperationId = "AssociateToolWithProfilesAsync")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddToolToProfiles([FromRoute] string name, List<string> profiles)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest("Invalid request. Please check the route parameter for the profile name.");
                if (profiles == null || profiles.Count < 1) return BadRequest("Invalid request. 'Profiles' property cannot be null or empty.");
                var response = await _profileLogic.AddToolToProfiles(name, profiles);
                if (response.IsSuccess)
                {
                    var newProfileToolsResponse = await _profileLogic.GetToolProfileAssociations(name);
                    if (newProfileToolsResponse.IsSuccess) return Ok(newProfileToolsResponse.Data ?? new List<string>());
                    return StatusCode(StatusCodes.Status500InternalServerError, newProfileToolsResponse.ErrorMessage);
                }
                else if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to dissociate a tool from profiles that already exist in the database.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <param name="profiles">An array of profile names.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of profiles that were successfully dissociated.</returns>
        [HttpPost]
        [Route("dissociate/{name}")]
        [SwaggerOperation(OperationId = "DissociateToolFromProfilesAsync")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveToolFromProfiles([FromRoute] string name, List<string> profiles)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest("Invalid request. Please check the route parameter for the profile name.");
                if (profiles == null || profiles.Count < 1) return BadRequest("Invalid request. 'Profiles' property cannot be null or empty.");
                var response = await _profileLogic.DeleteToolAssociations(name, profiles);
                if (response.IsSuccess) return Ok(response.Data);
                else return NotFound(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to delete a tool.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>An empty <see cref="IActionResult"/>.</returns>
        [HttpDelete]
        [Route("delete/{name}")]
        [SwaggerOperation(OperationId = "DeleteToolAsync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTool([FromRoute] string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest("Invalid request. Please check the route parameter for the profile name.");
                var response = await _profileLogic.DeleteTool(name);
                if (response.IsSuccess) return NoContent();
                else if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
