using Microsoft.AspNetCore.Mvc;
using Moq;
using IntelligenceHub.Controllers;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Business.Interfaces;

namespace IntelligenceHub.Tests.Unit.Controllers
{
    public class RagControllerTests
    {
        private readonly Mock<IRagLogic> _mockRagLogic;
        private readonly RagController _controller;

        public RagControllerTests()
        {
            _mockRagLogic = new Mock<IRagLogic>();
            _controller = new RagController(_mockRagLogic.Object);
        }

        [Fact]
        public async Task Get_ValidName_ReturnsOkResult()
        {
            // Arrange
            var name = "testIndex";
            var expectedResponse = new IndexMetadata();
            _mockRagLogic.Setup(r => r.GetRagIndex(name)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Get(name);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task Get_ValidName_ReturnsNotFound()
        {
            // Arrange
            var name = "testIndex";
            _mockRagLogic.Setup(r => r.GetRagIndex(name)).ReturnsAsync((IndexMetadata)null);

            // Act
            var result = await _controller.Get(name);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetAll_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var expectedResponse = new List<IndexMetadata> { new IndexMetadata() };
            _mockRagLogic.Setup(r => r.GetAllIndexesAsync()).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task GetAll_NoIndexes_ReturnsNotFound()
        {
            // Arrange
            _mockRagLogic.Setup(r => r.GetAllIndexesAsync()).ReturnsAsync(new List<IndexMetadata>());

            // Act
            var result = await _controller.GetAll();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateIndex_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var indexDefinition = new IndexMetadata();
            _mockRagLogic.Setup(r => r.CreateIndex(indexDefinition)).ReturnsAsync(true);

            // Act
            var result = await _controller.CreateIndex(indexDefinition);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task CreateIndex_FailedRequest_ReturnsBadRequest()
        {
            // Arrange
            var indexDefinition = new IndexMetadata();
            _mockRagLogic.Setup(r => r.CreateIndex(indexDefinition)).ReturnsAsync(false);

            // Act
            var result = await _controller.CreateIndex(indexDefinition);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task DeleteIndex_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var index = "testIndex";
            _mockRagLogic.Setup(r => r.DeleteIndex(index)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteIndex(index);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task DeleteIndex_FailedRequest_ReturnsNotFound()
        {
            // Arrange
            var index = "testIndex";
            _mockRagLogic.Setup(r => r.DeleteIndex(index)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteIndex(index);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetDocument_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var index = "testIndex";
            var document = "doc1";
            var expectedResponse = new IndexDocument();
            _mockRagLogic.Setup(r => r.GetDocument(index, document)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetDocument(index, document);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task GetDocument_ValidRequest_ReturnsNotFound()
        {
            // Arrange
            var index = "testIndex";
            var document = "doc1";
            _mockRagLogic.Setup(r => r.GetDocument(index, document)).ReturnsAsync((IndexDocument)null);

            // Act
            var result = await _controller.GetDocument(index, document);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpsertDocuments_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var index = "testIndex";
            var documentUpsertRequest = new RagUpsertRequest();
            _mockRagLogic.Setup(r => r.UpsertDocuments(index, documentUpsertRequest)).ReturnsAsync(true);

            // Act
            var result = await _controller.UpsertDocuments(index, documentUpsertRequest);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpsertDocuments_FailedRequest_ReturnsBadRequest()
        {
            // Arrange
            var index = "testIndex";
            var documentUpsertRequest = new RagUpsertRequest();
            _mockRagLogic.Setup(r => r.UpsertDocuments(index, documentUpsertRequest)).ReturnsAsync(false);

            // Act
            var result = await _controller.UpsertDocuments(index, documentUpsertRequest);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task DeleteDocuments_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var index = "testIndex";
            var commaDelimitedDocNames = "doc1,doc2";
            var expectedResponse = 2;
            _mockRagLogic.Setup(r => r.DeleteDocuments(index, It.IsAny<string[]>())).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.DeleteDocuments(index, commaDelimitedDocNames);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task DeleteDocuments_FailedRequest_ReturnsNotFound()
        {
            // Arrange
            var index = "testIndex";
            var commaDelimitedDocNames = "doc1,doc2";
            _mockRagLogic.Setup(r => r.DeleteDocuments(index, It.IsAny<string[]>())).ReturnsAsync(0);

            // Act
            var result = await _controller.DeleteDocuments(index, commaDelimitedDocNames);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
