using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IntelligenceHub.Client.Implementations;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Xunit;

namespace IntelligenceHub.Tests.Unit.Client
{
    public class ToolClientTests
    {
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private ToolClient _toolClient;

        public ToolClientTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _toolClient = new ToolClient(_httpClientFactoryMock.Object);
        }

        [Fact]
        public async Task CallFunction_PostMethod_ReturnsSuccess()
        {
            // Arrange
            var endpoint = "https://example.com/api";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var response = await _toolClient.CallFunction("tool", "args", endpoint);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CallFunction_GetMethod_ReturnsSuccess()
        {
            // Arrange
            var endpoint = "https://example.com/api";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var response = await _toolClient.CallFunction("tool", "args", endpoint, "Get");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CallFunction_InvalidHttpMethod_ReturnsNotFound()
        {
            // Arrange
            var endpoint = "https://example.com/api";

            // Act
            var response = await _toolClient.CallFunction("tool", "args", endpoint, "InvalidMethod");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CallFunction_BadRequest_ReturnsBadRequest()
        {
            // Arrange
            var endpoint = "https://example.com/api";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Bad request"));

            // Act
            var response = await _toolClient.CallFunction("tool", "args", endpoint);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Bad request", response.ReasonPhrase);
        }

        [Fact]
        public async Task CallFunction_RequestTimeout_ReturnsRequestTimeout()
        {
            // Arrange
            var endpoint = "https://example.com/api";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new TaskCanceledException("Request timed out"));

            // Act
            var response = await _toolClient.CallFunction("tool", "args", endpoint);

            // Assert
            Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
            Assert.Equal("Request timed out", response.ReasonPhrase);
        }

        [Fact]
        public async Task CallFunction_EmptyToolArgs_DoesNotSendBody()
        {
            // Arrange
            var endpoint = "https://example.com/api";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Content == null),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var response = await _toolClient.CallFunction("tool", null, endpoint);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CallFunction_ValidKey_SetsAuthorizationHeader()
        {
            // Arrange
            var endpoint = "https://example.com/api";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Headers.Authorization != null && req.Headers.Authorization.Parameter == "ValidKey"),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var response = await _toolClient.CallFunction("tool", "args", endpoint, key: "ValidKey");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}