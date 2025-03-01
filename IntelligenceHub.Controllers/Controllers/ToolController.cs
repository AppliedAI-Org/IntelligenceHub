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
        /// This controller is used to manage agent tools.
        /// </summary>
        /// <param name="profileLogic"></param>
        public ToolController(IProfileLogic profileLogic)
        {
            _profileLogic = profileLogic;
        }

        /// <summary>
        /// This endpoint is used to get a tool by name.
        /// </summary>
        /// <param name="name">The name of the tool.</param>
        /// <returns>An ObjectResult containing the tool.</returns>
        [HttpGet]
        [Route("get/{name}")]
        [SwaggerOperation(OperationId = "GetToolAsync")]
        [ProducesResponseType(typeof(Tool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTool([FromRoute] string name) // get this to work with either a string or an int
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request. Please check the route parameter for the profile name.");
                var tool = await _profileLogic.GetTool(name);
                if (tool == null) return NotFound($"No tool with the name {name} exists");
                else return Ok(tool);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to get all tools.
        /// </summary>
        /// /// <param name="page">The page number to retrieve.</param>
        /// <param name="count">The amount of tools to retrieve.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("get/all/page/{page}/count/{count}")]
        [SwaggerOperation(OperationId = "GetAllToolsAsync")]
        [ProducesResponseType(typeof(IEnumerable<Tool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllTools([FromRoute] int page, [FromRoute] int count)
        {
            try
            {
                var tools = await _profileLogic.GetAllTools(page, count);
                if (tools == null || tools.Count() < 1) return NotFound($"No tools exist. Make a post request to add some.");
                else return Ok(tools);
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
        /// <returns>An array of profile names that use the tool.</returns>
        [HttpGet]
        [Route("get/{name}/profiles")]
        [SwaggerOperation(OperationId = "GetToolProfilesAsync")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetToolProfiles(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request. Please check the route parameter for the profile name.");
                var tool = await _profileLogic.GetToolProfileAssociations(name);
                if (tool == null) return NotFound($"The tool '{name}' is not associated with any profiles, or does not exist.");
                else return Ok(tool);
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
        /// <returns>An empty ResponseObject.</returns>
        [HttpPost]
        [Route("upsert")]
        [SwaggerOperation(OperationId = "UpsertToolAsync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddOrUpdateTool([FromBody] List<Tool> toolList)
        {
            try
            {
                var errorMessage = await _profileLogic.CreateOrUpdateTools(toolList);
                if (errorMessage != null) return BadRequest(errorMessage);
                else return NoContent();
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
        /// <returns>An array of profile names that were successfully associated.</returns>
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
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request. Please check the route parameter for the profile name.");
                if (profiles == null || profiles.Count < 1) return BadRequest($"Invalid request. 'Profiles' property cannot be null or empty.");
                var errorMessage = await _profileLogic.AddToolToProfiles(name, profiles);
                if (errorMessage == null) return Ok(await _profileLogic.GetToolProfileAssociations(name));
                else return NotFound(errorMessage);
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
        /// <returns>An ObjectResult containing a list of profiles that were successfully dissasociated.</returns>
        [HttpPost]
        [Route("dissociate/{name}")]
        [SwaggerOperation(OperationId = "DissociateToolFromProfilesAsync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveToolFromProfiles([FromRoute] string name, List<string> profiles)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request. Please check the route parameter for the profile name.");
                if (profiles == null || profiles.Count < 1) return BadRequest($"Invalid request. 'Profiles' property cannot be null or empty.");
                var errorMessage = await _profileLogic.DeleteToolAssociations(name, profiles);
                if (errorMessage == null) return NoContent();
                else return NotFound(errorMessage);
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
        /// <returns>An empty ObjectResult.</returns>
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
                if (string.IsNullOrEmpty(name)) return BadRequest($"Invalid request. Please check the route parameter for the profile name.");
                var success = await _profileLogic.DeleteTool(name);
                if (success) return NoContent();
                else return NotFound($"No tool with the name {name} exists");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
