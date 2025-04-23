using IntelligenceHub.API.Controllers;
using IntelligenceHub.API.DTOs.Auth;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.Tests.Unit.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthLogic> _authLogicMock;
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            _authLogicMock = new Mock<IAuthLogic>();
            _authController = new AuthController(_authLogicMock.Object);
        }

        [Fact]
        public async Task GetAdminToken_ReturnsOkResult_WithToken()
        {
            // Arrange
            var tokenResponse = new Auth0Response { AccessToken = "admin_token", ExpiresIn = 3600, TokenType = "Bearer" };
            _authLogicMock.Setup(x => x.GetAdminAuthToken()).ReturnsAsync(tokenResponse);

            // Act
            var result = await _authController.GetAdminToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal(tokenResponse, okResult.Value);
        }

        [Fact]
        public async Task GetAdminToken_ReturnsUnauthorized_WhenTokenIsNull()
        {
            // Arrange
            _authLogicMock.Setup(x => x.GetAdminAuthToken()).ReturnsAsync((Auth0Response)null);

            // Act
            var result = await _authController.GetAdminToken();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task GetAdminToken_ReturnsInternalServerError_OnException()
        {
            // Arrange
            _authLogicMock.Setup(x => x.GetAdminAuthToken()).ThrowsAsync(new Exception());

            // Act
            var result = await _authController.GetAdminToken();

            // Assert
            var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, internalServerErrorResult.StatusCode);
            Assert.Equal(GlobalVariables.DefaultExceptionMessage, internalServerErrorResult.Value);
        }

        [Fact]
        public async Task GetDefaultToken_ReturnsOkResult_WithToken()
        {
            // Arrange
            var tokenResponse = new Auth0Response { AccessToken = "default_token", ExpiresIn = 3600, TokenType = "Bearer" };
            _authLogicMock.Setup(x => x.GetDefaultAuthToken()).ReturnsAsync(tokenResponse);

            // Act
            var result = await _authController.GetDefaultToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal(tokenResponse, okResult.Value);
        }

        [Fact]
        public async Task GetDefaultToken_ReturnsUnauthorized_WhenTokenIsNull()
        {
            // Arrange
            _authLogicMock.Setup(x => x.GetDefaultAuthToken()).ReturnsAsync((Auth0Response)null);

            // Act
            var result = await _authController.GetDefaultToken();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
        }

        [Fact]
        public async Task GetDefaultToken_ReturnsInternalServerError_OnException()
        {
            // Arrange
            _authLogicMock.Setup(x => x.GetDefaultAuthToken()).ThrowsAsync(new Exception());

            // Act
            var result = await _authController.GetDefaultToken();

            // Assert
            var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, internalServerErrorResult.StatusCode);
            Assert.Equal(GlobalVariables.DefaultExceptionMessage, internalServerErrorResult.Value);
        }
    }
}
