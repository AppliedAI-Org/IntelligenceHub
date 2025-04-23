using IntelligenceHub.API.DTOs.Auth;
using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IntelligenceHub.Tests.Unit.Client
{
    public class Auth0ClientTests
    {
        private readonly Mock<IOptionsMonitor<AuthSettings>> _mockSettings;
        private readonly Mock<IHttpClientFactory> _mockFactory;
        private readonly Mock<HttpMessageHandler> _mockHandler;
        private readonly AuthSettings _authSettings;

        public Auth0ClientTests()
        {
            _authSettings = new AuthSettings
            {
                Domain = "https://example.com",
                Audience = "https://api.example.com",
                DefaultClientId = "default-client-id",
                DefaultClientSecret = "default-client-secret",
                AdminClientId = "admin-client-id",
                AdminClientSecret = "admin-client-secret"
            };

            _mockSettings = new Mock<IOptionsMonitor<AuthSettings>>();
            _mockSettings.Setup(s => s.CurrentValue).Returns(_authSettings);

            _mockHandler = new Mock<HttpMessageHandler>();
            _mockFactory = new Mock<IHttpClientFactory>();
            var client = new HttpClient(_mockHandler.Object);
            _mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        }

        [Fact]
        public async Task RequestAuthToken_ReturnsAuthToken()
        {
            // Arrange
            var responseContent = new StringContent("{\"access_token\":\"test-token\",\"expires_in\":3600,\"token_type\":\"Bearer\"}");
            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = responseContent
                });

            var client = new Auth0Client(_mockSettings.Object, _mockFactory.Object);

            // Act
            var result = await client.RequestAuthToken();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-token", result.AccessToken);
            Assert.Equal(3600, result.ExpiresIn);
        }

        [Fact]
        public async Task RequestAuthToken_ReturnsNullOnHttpRequestException()
        {
            // Arrange
            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException());

            var client = new Auth0Client(_mockSettings.Object, _mockFactory.Object);

            // Act
            var result = await client.RequestAuthToken();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RequestElevatedAuthToken_ReturnsAuthToken()
        {
            // Arrange
            var responseContent = new StringContent("{\"access_token\":\"elevated-test-token\",\"expires_in\":3600,\"token_type\":\"Bearer\"}");
            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = responseContent
                });

            var client = new Auth0Client(_mockSettings.Object, _mockFactory.Object);

            // Act
            var result = await client.RequestElevatedAuthToken();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("elevated-test-token", result.AccessToken);
            Assert.Equal(3600, result.ExpiresIn);
        }

        [Fact]
        public async Task RequestElevatedAuthToken_ReturnsNullOnHttpRequestException()
        {
            // Arrange
            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException());

            var client = new Auth0Client(_mockSettings.Object, _mockFactory.Object);

            // Act
            var result = await client.RequestElevatedAuthToken();

            // Assert
            Assert.Null(result);
        }
    }
}
