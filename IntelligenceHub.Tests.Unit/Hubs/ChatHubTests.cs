using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using IntelligenceHub.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace IntelligenceHub.Tests.Unit.Hubs
{
    public class ChatHubTests
    {
        private static ChatHub CreateHub(Mock<ICompletionLogic> completionMock, Mock<IValidationHandler> validationMock, Mock<ISingleClientProxy> clientProxy)
        {
            var clients = new Mock<IHubCallerClients>();
            clients.Setup(c => c.Caller).Returns(clientProxy.Object);
            var hub = new ChatHub(completionMock.Object, validationMock.Object)
            {
                Clients = clients.Object,
                Context = new Mock<HubCallerContext>().Object
            };
            return hub;
        }

        private static async IAsyncEnumerable<APIResponseWrapper<CompletionStreamChunk>> GetStream(params APIResponseWrapper<CompletionStreamChunk>[] chunks)
        {
            foreach (var chunk in chunks)
            {
                yield return chunk;
                await Task.Yield();
            }
        }

        [Fact]
        public async Task Send_SendsValidationError_WhenValidationFails()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var clientProxy = new Mock<ISingleClientProxy>();
            var hub = CreateHub(completionMock, validationMock, clientProxy);
            var request = new CompletionRequest();
            const string error = "Invalid";
            validationMock.Setup(v => v.ValidateChatRequest(request)).Returns(error);

            await hub.Send(request);

            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => (string)o[0] == error), default), Times.Once);
            completionMock.Verify(c => c.StreamCompletion(It.IsAny<CompletionRequest>()), Times.Never);
        }

        [Fact]
        public async Task Send_StreamsCompletionChunks_WhenRequestValid()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var clientProxy = new Mock<ISingleClientProxy>();
            var hub = CreateHub(completionMock, validationMock, clientProxy);
            var request = new CompletionRequest();
            var chunk = APIResponseWrapper<CompletionStreamChunk>.Success(new CompletionStreamChunk { CompletionUpdate = "hi" });
            completionMock.Setup(c => c.StreamCompletion(request)).Returns(GetStream(chunk));

            await hub.Send(request);

            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => ((CompletionStreamChunk)o[0]).CompletionUpdate == "hi"), default), Times.Once);
        }

        [Fact]
        public async Task Send_SendsErrorMessage_WhenChunkIndicatesFailure()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var clientProxy = new Mock<ISingleClientProxy>();
            var hub = CreateHub(completionMock, validationMock, clientProxy);
            var request = new CompletionRequest();
            const string msg = "missing";
            var failure = APIResponseWrapper<CompletionStreamChunk>.Failure(msg, GlobalVariables.APIResponseStatusCodes.NotFound);
            completionMock.Setup(c => c.StreamCompletion(request)).Returns(GetStream(failure));

            await hub.Send(request);

            var expected = $"Response Status: {GlobalVariables.APIResponseStatusCodes.NotFound}. Error message: {msg}";
            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => (string)o[0] == expected), default), Times.Once);
        }

        [Fact]
        public async Task Send_SendsDefaultError_OnException()
        {
            var completionMock = new Mock<ICompletionLogic>();
            var validationMock = new Mock<IValidationHandler>();
            var clientProxy = new Mock<ISingleClientProxy>();
            var hub = CreateHub(completionMock, validationMock, clientProxy);
            var request = new CompletionRequest();
            completionMock.Setup(c => c.StreamCompletion(request)).Throws(new System.Exception());

            await hub.Send(request);

            var expected = $"Response Status: {500}. Error message: {GlobalVariables.DefaultExceptionMessage}";
            clientProxy.Verify(p => p.SendCoreAsync("broadcastMessage", It.Is<object[]>(o => (string)o[0] == expected), default), Times.Once);
        }
    }
}