using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Tests.Unit.Host.Wrappers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IntelligenceHub.Tests.Unit.Host
{
    public class BasicAuthenticationHandlerTests
    {
        private const string ValidUsername = "testuser";
        private const string ValidPassword = "testpassword";
        private const string InvalidUsername = "invaliduser";
        private const string InvalidPassword = "invalidpassword";

        private async Task<TestableBasicAuthenticationHandler> CreateHandler(string expectedUsername, string expectedPassword, HttpContext context)
        {
            var authSettings = new AuthSettings
            {
                BasicUsername = expectedUsername,
                BasicPassword = expectedPassword
            };

            // Setup mocks for required dependencies.
            var optionsMock = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
            // Return a new instance when any scheme is requested.
            optionsMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

            // Use the default encoder.
            var urlEncoder = UrlEncoder.Default;

            var authOptionsMock = new Mock<IOptionsMonitor<AuthSettings>>();
            authOptionsMock.Setup(a => a.CurrentValue).Returns(authSettings);

            var handler = new TestableBasicAuthenticationHandler(
                optionsMock.Object,
                loggerFactoryMock.Object,
                urlEncoder,
                authOptionsMock.Object);

            // Create a dummy authentication scheme and initialize the handler.
            var scheme = new AuthenticationScheme("Basic", "Basic", typeof(TestableBasicAuthenticationHandler));
            await handler.InitializeAsync(scheme, context);
            return handler;
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ValidUsername}:{ValidPassword}"));
            context.Request.Headers["Authorization"] = $"Basic {authHeader}";
            var handler = await CreateHandler(ValidUsername, ValidPassword, context);

            // Act
            var result = await handler.PublicHandleAuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded, "Authentication should succeed with valid credentials.");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithInvalidCredentials_ReturnsFail()
        {
            // Arrange
            var context = new DefaultHttpContext();
            // The header contains invalid credentials while the handler is configured with valid ones.
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{InvalidUsername}:{InvalidPassword}"));
            context.Request.Headers["Authorization"] = $"Basic {authHeader}";
            var handler = await CreateHandler(ValidUsername, ValidPassword, context);

            // Act
            var result = await handler.PublicHandleAuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded, "Authentication should fail with invalid credentials.");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_MissingAuthorizationHeader_ReturnsFail()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var handler = await CreateHandler(ValidUsername, ValidPassword, context);

            // Act
            var result = await handler.PublicHandleAuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded, "Authentication should fail when the Authorization header is missing.");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_InvalidAuthorizationHeader_ReturnsFail()
        {
            // Arrange
            var context = new DefaultHttpContext();
            // Provide a header that cannot be parsed as a valid Authorization header.
            context.Request.Headers["Authorization"] = "InvalidHeader";
            var handler = await CreateHandler(ValidUsername, ValidPassword, context);

            // Act
            var result = await handler.PublicHandleAuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded, "Authentication should fail with an invalid Authorization header.");
        }
    }
}
