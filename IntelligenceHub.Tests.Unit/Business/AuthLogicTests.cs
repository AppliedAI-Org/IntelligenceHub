using IntelligenceHub.API.DTOs.Auth;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Client.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class AuthLogicTests
    {
        private readonly Mock<IAIAuth0Client> _auth0ClientMock;
        private readonly AuthLogic _authLogic;

        public AuthLogicTests()
        {
            _auth0ClientMock = new Mock<IAIAuth0Client>();
            _authLogic = new AuthLogic(_auth0ClientMock.Object);
        }

        [Fact]
        public async Task GetDefaultAuthToken_ShouldReturnAuth0Response()
        {
            // Arrange
            var expectedResponse = new Auth0Response
            {
                AccessToken = "default_token",
                ExpiresIn = 3600,
                TokenType = "Bearer"
            };
            _auth0ClientMock.Setup(client => client.RequestAuthToken())
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authLogic.GetDefaultAuthToken();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.AccessToken, result?.AccessToken);
            Assert.Equal(expectedResponse.ExpiresIn, result?.ExpiresIn);
            Assert.Equal(expectedResponse.TokenType, result?.TokenType);
        }

        [Fact]
        public async Task GetAdminAuthToken_ShouldReturnAuth0Response()
        {
            // Arrange
            var expectedResponse = new Auth0Response
            {
                AccessToken = "admin_token",
                ExpiresIn = 3600,
                TokenType = "Bearer"
            };
            _auth0ClientMock.Setup(client => client.RequestElevatedAuthToken())
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _authLogic.GetAdminAuthToken();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.AccessToken, result?.AccessToken);
            Assert.Equal(expectedResponse.ExpiresIn, result?.ExpiresIn);
            Assert.Equal(expectedResponse.TokenType, result?.TokenType);
        }
    }
}
