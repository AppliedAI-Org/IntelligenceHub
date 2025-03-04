using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.Tools;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Controllers
{
    public class ToolControllerTests
    {
        private readonly ToolController _controller;
        private readonly Mock<IProfileLogic> _profileLogicMock;
        private readonly Mock<ILogger<ToolController>> _loggerMock;

        public ToolControllerTests()
        {
            _profileLogicMock = new Mock<IProfileLogic>();
            _loggerMock = new Mock<ILogger<ToolController>>();
            _controller = new ToolController(_profileLogicMock.Object);
        }

        #region GetTool Tests
        [Fact]
        public async Task GetTool_ReturnsBadRequest_WhenNameIsNullOrEmpty()
        {
            // Arrange
            string name = null;

            // Act
            var result = await _controller.GetTool(name);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request. Please check the route parameter for the profile name.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetTool_ReturnsNotFound_WhenToolDoesNotExist()
        {
            // Arrange
            var name = "nonexistentTool";
            _profileLogicMock.Setup(p => p.GetTool(name)).ReturnsAsync(APIResponseWrapper<Tool>.Failure($"No tool with the name {name} exists", APIResponseStatusCodes.NotFound));

            // Act
            var result = await _controller.GetTool(name);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"No tool with the name {name} exists", notFoundResult.Value);
        }

        [Fact]
        public async Task GetTool_ReturnsOk_WhenToolExists()
        {
            // Arrange
            var name = "tool1";
            var tool = new Tool { Function = new Function() { Name = "tool1" } };
            _profileLogicMock.Setup(p => p.GetTool(name)).ReturnsAsync(APIResponseWrapper<Tool>.Success(tool));

            // Act
            var result = await _controller.GetTool(name);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(tool, okResult.Value);
        }
        #endregion

        #region GetAllTools Tests
        [Fact]
        public async Task GetAllTools_ReturnsEmptySet_WhenNoToolsExist()
        {
            // Arrange
            _profileLogicMock.Setup(p => p.GetAllTools(1, 10)).ReturnsAsync(APIResponseWrapper<IEnumerable<Tool>>.Success(Enumerable.Empty<Tool>()));

            // Act
            var result = await _controller.GetAllTools(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tools = Assert.IsAssignableFrom<IEnumerable<Tool>>(okResult.Value);
            Assert.Empty(tools);
        }

        [Fact]
        public async Task GetAllTools_ReturnsOk_WhenToolsExist()
        {
            // Arrange
            var tools = new List<Tool> { new Tool { Function = new Function() { Name = "tool1" } }, new Tool { Function = new Function() { Name = "tool2" } } };
            var mockResponse = APIResponseWrapper<IEnumerable<Tool>>.Success(tools);
            _profileLogicMock.Setup(p => p.GetAllTools(1, 10)).ReturnsAsync(mockResponse);

            // Act
            var result = await _controller.GetAllTools(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(tools, okResult.Value);
        }
        #endregion

        #region GetToolProfiles Tests
        [Fact]
        public async Task GetToolProfiles_ReturnsBadRequest_WhenNameIsNullOrEmpty()
        {
            // Arrange
            string name = null;

            // Act
            var result = await _controller.GetToolProfiles(name);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request. Please check the route parameter for the profile name.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetToolProfiles_ReturnsNotFound_WhenNoProfilesFound()
        {
            // Arrange
            var name = "tool1";
            _profileLogicMock.Setup(p => p.GetToolProfileAssociations(name)).ReturnsAsync(APIResponseWrapper<List<string>>.Success(new List<string>()));

            // Act
            var result = await _controller.GetToolProfiles(name);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var profiles = Assert.IsType<List<string>>(okResult.Value);
            Assert.Empty(profiles);
        }

        [Fact]
        public async Task GetToolProfiles_ReturnsOk_WhenProfilesExist()
        {
            // Arrange
            var name = "tool1";
            var profiles = new List<string> { "profile1", "profile2" };
            _profileLogicMock.Setup(p => p.GetToolProfileAssociations(name)).ReturnsAsync(APIResponseWrapper<List<string>>.Success(profiles));

            // Act
            var result = await _controller.GetToolProfiles(name);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(profiles, okResult.Value);
        }
        #endregion

        #region AddOrUpdateTool Tests
        [Fact]
        public async Task AddOrUpdateTool_ReturnsBadRequest_WhenErrorMessageIsNotNull()
        {
            // Arrange
            var toolList = new List<Tool> { new Tool { Function = new Function() { Name = "tool1" }  } };
            _profileLogicMock.Setup(p => p.CreateOrUpdateTools(toolList)).ReturnsAsync(APIResponseWrapper<string>.Failure("Error adding/updating tool", IntelligenceHub.Common.GlobalVariables.APIResponseStatusCodes.BadRequest));

            // Act
            var result = await _controller.AddOrUpdateTool(toolList);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error adding/updating tool", badRequestResult.Value);
        }

        [Fact]
        public async Task AddOrUpdateTool_ReturnsOk_WhenSuccess()
        {
            // Arrange
            var toolList = new List<Tool> { new Tool { Function = new Function() { Name = "tool1" } } };
            _profileLogicMock.Setup(p => p.CreateOrUpdateTools(toolList)).ReturnsAsync(APIResponseWrapper<string>.Success("tool1"));

            // Act
            var result = await _controller.AddOrUpdateTool(toolList);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(toolList, okResult.Value);
        }
        #endregion

        #region AddOrUpdateTool Tests
        [Fact]
        public async Task AddToolToProfiles_ReturnsBadRequest_WhenProfilesIsNullOrEmpty()
        {
            // Arrange
            var name = "tool1";
            var profiles = new List<string>();

            // Act
            var result = await _controller.AddToolToProfiles(name, profiles);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request. 'Profiles' property cannot be null or empty.", badRequestResult.Value);
        }

        [Fact]
        public async Task AddToolToProfiles_ReturnsOk_WhenToolAndProfilesAddedSuccessfully()
        {
            // Arrange
            var name = "tool1";
            var profiles = new List<string> { "profile1" };
            var profileAssociations = new List<string> { "profile1", "profile2" };

            _profileLogicMock.Setup(p => p.AddToolToProfiles(name, profiles)).ReturnsAsync(APIResponseWrapper<List<string>>.Success(profileAssociations));
            _profileLogicMock.Setup(p => p.GetToolProfileAssociations(name)).ReturnsAsync(APIResponseWrapper<List<string>>.Success(profileAssociations));

            // Act
            var result = await _controller.AddToolToProfiles(name, profiles);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<List<string>>(okResult.Value);
            Assert.Equal(profileAssociations, okResult.Value);
        }
        #endregion

        #region RemoveToolFromProfiles Tests
        [Fact]
        public async Task RemoveToolFromProfiles_ReturnsBadRequest_WhenProfilesIsNullOrEmpty()
        {
            // Arrange
            var name = "tool1";
            var profiles = new List<string>();

            // Act
            var result = await _controller.RemoveToolFromProfiles(name, profiles);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request. 'Profiles' property cannot be null or empty.", badRequestResult.Value);
        }

        [Fact]
        public async Task RemoveToolFromProfiles_ReturnsOk_WhenSuccess()
        {
            // Arrange
            var name = "tool1";
            var profiles = new List<string> { "profile1" };
            _profileLogicMock.Setup(p => p.DeleteToolAssociations(name, profiles)).ReturnsAsync(APIResponseWrapper<List<string>>.Success(profiles));

            // Act
            var result = await _controller.RemoveToolFromProfiles(name, profiles);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(profiles, okResult.Value);
        }
        #endregion

        #region DeleteTool Tests
        [Fact]
        public async Task DeleteTool_ReturnsBadRequest_WhenNameIsNullOrEmpty()
        {
            // Arrange
            string name = null;

            // Act
            var result = await _controller.DeleteTool(name);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid request. Please check the route parameter for the profile name.", badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteTool_ReturnsNotFound_WhenToolDoesNotExist()
        {
            // Arrange
            var name = "nonexistentTool";
            _profileLogicMock.Setup(p => p.DeleteTool(name)).ReturnsAsync(APIResponseWrapper<bool>.Failure($"No tool with the name {name} exists", APIResponseStatusCodes.NotFound));

            // Act
            var result = await _controller.DeleteTool(name);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"No tool with the name {name} exists", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteTool_ReturnsNoContent_WhenDeletedSuccessfully()
        {
            // Arrange
            var name = "tool1";
            _profileLogicMock.Setup(p => p.DeleteTool(name)).ReturnsAsync(APIResponseWrapper<bool>.Success(true));

            // Act
            var result = await _controller.DeleteTool(name);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }
        [Fact]
        public async Task GetAllTools_ReturnsOk_WhenNoToolsExist()
        {
            // Arrange
            _profileLogicMock.Setup(p => p.GetAllTools(1, 10)).ReturnsAsync(APIResponseWrapper<IEnumerable<Tool>>.Success(Enumerable.Empty<Tool>()));

            // Act
            var result = await _controller.GetAllTools(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((IEnumerable<Tool>)okResult.Value);
        }
        #endregion
    }
}


