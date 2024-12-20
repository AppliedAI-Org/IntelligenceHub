using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Common.Config;
using Microsoft.AspNetCore.Authorization;
using IntelligenceHub.Common;
using IntelligenceHub.Business.Interfaces;
using Microsoft.AspNetCore.Http;

namespace IntelligenceHub.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminPolicy")]
    public class ProfileController : ControllerBase
    {
        private IProfileLogic _profileLogic;

        public ProfileController(IProfileLogic profileLogic)
        {
            _profileLogic = profileLogic;
        }

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
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
                return Ok(await _profileLogic.GetAllProfiles());
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
            
        }

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
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
