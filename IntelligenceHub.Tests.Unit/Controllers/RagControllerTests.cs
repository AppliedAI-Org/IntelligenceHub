using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using static IntelligenceHub.Common.GlobalVariables;

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
            var metadata = new IndexMetadata();
            var expectedResponse = APIResponseWrapper<IndexMetadata>.Success(metadata);
            _mockRagLogic.Setup(r => r.GetRagIndex(name)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Get(name);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(metadata, okResult.Value);
        }

        [Fact]
        public async Task Get_ValidName_ReturnsNotFound()
        {
            // Arrange
            var name = "testIndex";
            var expectedResponse = APIResponseWrapper<IndexMetadata>.Failure(string.Empty, APIResponseStatusCodes.NotFound);
            _mockRagLogic.Setup(r => r.GetRagIndex(name)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Get(name);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(expectedResponse.ErrorMessage, notFoundResult.Value);
        }

        [Fact]
        public async Task GetAll_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var allMetadata = new List<IndexMetadata> { new IndexMetadata() };
            var expectedResponse = APIResponseWrapper<IEnumerable<IndexMetadata>>.Success(allMetadata);
            _mockRagLogic.Setup(r => r.GetAllIndexesAsync()).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(allMetadata, okResult.Value);
        }

        [Fact]
        public async Task GetAll_NoIndexes_ReturnsEmptySet()
        {
            // Arrange
            var expectedResponse = APIResponseWrapper<IEnumerable<IndexMetadata>>.Success(new List<IndexMetadata>());
            _mockRagLogic.Setup(r => r.GetAllIndexesAsync()).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedList = Assert.IsAssignableFrom<IEnumerable<IndexMetadata>>(okResult.Value);
            Assert.Empty(returnedList);
        }

        [Fact]
        public async Task CreateIndex_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var indexDefinition = new IndexMetadata();
            var expectedResponse = APIResponseWrapper<bool>.Success(true);
            _mockRagLogic.Setup(r => r.CreateIndex(indexDefinition)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateIndex(indexDefinition);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(indexDefinition, okResult.Value);
        }

        [Fact]
        public async Task CreateIndex_FailedRequest_ReturnsBadRequest()
        {
            // Arrange
            var indexDefinition = new IndexMetadata();
            var expectedResponse = APIResponseWrapper<bool>.Failure("Bad Request", APIResponseStatusCodes.BadRequest);
            _mockRagLogic.Setup(r => r.CreateIndex(indexDefinition)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateIndex(indexDefinition);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(expectedResponse.ErrorMessage, badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteIndex_ValidRequest_ReturnsNoContentResult()
        {
            // Arrange
            var index = "testIndex";
            _mockRagLogic.Setup(r => r.DeleteIndex(index)).ReturnsAsync(APIResponseWrapper<bool>.Success(true));

            // Act
            var result = await _controller.DeleteIndex(index);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteIndex_FailedRequest_ReturnsNotFound()
        {
            // Arrange
            var index = "testIndex";
            var expectedResponse = APIResponseWrapper<bool>.Failure(string.Empty, APIResponseStatusCodes.NotFound);
            _mockRagLogic.Setup(r => r.DeleteIndex(index)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.DeleteIndex(index);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(expectedResponse.ErrorMessage, notFoundResult.Value);
        }

        [Fact]
        public async Task GetDocument_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var index = "testIndex";
            var documentName = "doc1";
            var document = new IndexDocument();
            var expectedResponse = APIResponseWrapper<IndexDocument>.Success(document);
            _mockRagLogic.Setup(r => r.GetDocument(index, documentName)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetDocument(index, documentName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(document, okResult.Value);
        }

        [Fact]
        public async Task GetDocument_ValidRequest_ReturnsNotFound()
        {
            // Arrange
            var index = "testIndex";
            var document = "doc1";
            var expectedResponse = APIResponseWrapper<IndexDocument>.Failure(string.Empty, APIResponseStatusCodes.NotFound);
            _mockRagLogic.Setup(r => r.GetDocument(index, document)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetDocument(index, document);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(expectedResponse.ErrorMessage, notFoundResult.Value);
        }

        [Fact]
        public async Task UpsertDocuments_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var index = "testIndex";
            var documentToDelete = new IndexDocument() { Title = "doc1", Content = "content" };
            var documentUpsertRequest = new RagUpsertRequest() { Documents = new List<IndexDocument>() { documentToDelete } };
            _mockRagLogic.Setup(r => r.UpsertDocuments(index, documentUpsertRequest)).ReturnsAsync(APIResponseWrapper<bool>.Success(true));

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
            var documentToDelete = new IndexDocument() { Title = "doc1", Content = "content" };
            var documentUpsertRequest = new RagUpsertRequest() { Documents = new List<IndexDocument>() { documentToDelete } };
            var expectedResponse = APIResponseWrapper<bool>.Failure("Bad Request", APIResponseStatusCodes.BadRequest);
            _mockRagLogic.Setup(r => r.UpsertDocuments(index, documentUpsertRequest)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.UpsertDocuments(index, documentUpsertRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(expectedResponse.ErrorMessage, badRequestResult.Value);
        }

        [Fact]
        public async Task UpsertDocuments_EmptyRequest_ReturnsBadRequest()
        {
            // Arrange
            var index = "testIndex";
            var documentUpsertRequest = new RagUpsertRequest() { Documents = new List<IndexDocument>() };

            // Act
            var result = await _controller.UpsertDocuments(index, documentUpsertRequest);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteDocuments_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var index = "testIndex";
            var commaDelimitedDocNames = "doc1,doc2";
            var rowsAffewcted = 2;
            var expectedResponse = APIResponseWrapper<int>.Success(rowsAffewcted);
            _mockRagLogic.Setup(r => r.DeleteDocuments(index, It.IsAny<string[]>())).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.DeleteDocuments(index, commaDelimitedDocNames);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(rowsAffewcted, okResult.Value);
        }

        [Fact]
        public async Task DeleteDocuments_FailedRequest_ReturnsNotFound()
        {
            // Arrange
            var index = "testIndex";
            var commaDelimitedDocNames = "doc1,doc2";
            var expectedResponse = APIResponseWrapper<int>.Failure($"Some or all of the provided documents were not found in the index '{index}'.", APIResponseStatusCodes.NotFound);
            _mockRagLogic.Setup(r => r.DeleteDocuments(index, It.IsAny<string[]>())).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.DeleteDocuments(index, commaDelimitedDocNames);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(expectedResponse.ErrorMessage, notFoundResult.Value);
        }
    }
}
