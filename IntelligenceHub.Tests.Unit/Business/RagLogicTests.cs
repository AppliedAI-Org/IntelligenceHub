using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using global::IntelligenceHub.API.DTOs.RAG;
using global::IntelligenceHub.Business;
using global::IntelligenceHub.Client;
using global::IntelligenceHub.DAL.Models;
using global::IntelligenceHub.DAL;
using Moq;
using Xunit;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.DAL.Interfaces;

namespace IntelligenceHub.Tests.Unit.Business
{

    public class RagLogicTests
    {
        private readonly Mock<IIndexMetaRepository> _mockMetaRepository;
        private readonly Mock<IAISearchServiceClient> _mockSearchClient;
        private readonly Mock<IAGIClient> _mockAiClient;
        private readonly Mock<IIndexRepository> _mockRagRepository;
        private readonly RagLogic _ragLogic;

        public RagLogicTests()
        {
            _mockSearchClient = new Mock<IAISearchServiceClient>();
            _mockAiClient = new Mock<IAGIClient>();
            _mockMetaRepository = new Mock<IIndexMetaRepository>();
            _mockRagRepository = new Mock<IIndexRepository>();

            _ragLogic = new RagLogic(_mockAiClient.Object, _mockSearchClient.Object, _mockMetaRepository.Object, _mockRagRepository.Object);
        }

        [Fact]
        public async Task GetRagIndex_ShouldReturnIndexMetadata_WhenIndexExists()
        {
            // Arrange
            var indexName = "testIndex";
            var dbIndexMetadata = new DbIndexMetadata { Name = indexName };
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(dbIndexMetadata);

            // Act
            var result = await _ragLogic.GetRagIndex(indexName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(indexName, result.Name);
        }

        [Fact]
        public async Task GetRagIndex_ShouldReturnNull_WhenIndexDoesNotExist()
        {
            // Arrange
            var indexName = "nonExistentIndex";
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync((DbIndexMetadata)null);

            // Act
            var result = await _ragLogic.GetRagIndex(indexName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateIndex_ShouldReturnTrue_WhenIndexIsCreatedSuccessfully()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "newIndex" };
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexMetadata.Name)).ReturnsAsync((DbIndexMetadata)null);
            _mockMetaRepository.Setup(repo => repo.AddAsync(It.IsAny<DbIndexMetadata>())).ReturnsAsync(new DbIndexMetadata());
            _mockRagRepository.Setup(repo => repo.CreateIndexAsync(indexMetadata.Name)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.CreateIndex(indexMetadata)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.CreateDatasource(indexMetadata.Name)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.CreateIndexer(indexMetadata)).ReturnsAsync(true);

            // Act
            var result = await _ragLogic.CreateIndex(indexMetadata);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CreateIndex_ShouldReturnFalse_WhenIndexNameIsInvalid()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "invalid index name" };

            // Act
            var result = await _ragLogic.CreateIndex(indexMetadata);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CreateIndex_ShouldReturnFalse_WhenIndexAlreadyExists()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "existingIndex" };
            var dbIndexMetadata = new DbIndexMetadata { Name = indexMetadata.Name };
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexMetadata.Name)).ReturnsAsync(dbIndexMetadata);

            // Act
            var result = await _ragLogic.CreateIndex(indexMetadata);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteIndex_ShouldReturnTrue_WhenIndexIsDeletedSuccessfully()
        {
            // Arrange
            var indexName = "testIndex";
            var dbIndexMetadata = new DbIndexMetadata { Name = indexName };
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(dbIndexMetadata);
            _mockRagRepository.Setup(repo => repo.DeleteIndexAsync(indexName)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.DeleteIndexer(indexName, It.IsAny<string>())).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.DeleteDatasource(indexName)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.DeleteIndex(indexName)).ReturnsAsync(true);
            _mockMetaRepository.Setup(repo => repo.DeleteAsync(dbIndexMetadata, indexName)).ReturnsAsync(1);

            // Act
            var result = await _ragLogic.DeleteIndex(indexName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteIndex_ShouldReturnFalse_WhenIndexNameIsInvalid()
        {
            // Arrange
            var indexName = "invalid index name";

            // Act
            var result = await _ragLogic.DeleteIndex(indexName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteIndex_ShouldReturnFalse_WhenIndexDoesNotExist()
        {
            // Arrange
            var indexName = "nonExistentIndex";
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync((DbIndexMetadata)null);

            // Act
            var result = await _ragLogic.DeleteIndex(indexName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpsertDocuments_ShouldReturnTrue_WhenDocumentsAreUpsertedSuccessfully()
        {
            // Arrange
            var indexName = "testIndex";
            var dbIndexMetadata = new DbIndexMetadata { Name = indexName };
            var document = new IndexDocument { Title = "testDocument", Content = "testContent" };
            var upsertRequest = new RagUpsertRequest { Documents = new List<IndexDocument> { document } };
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(dbIndexMetadata);
            _mockRagRepository.Setup(repo => repo.GetDocumentAsync(indexName, document.Title)).ReturnsAsync((DbIndexDocument?)null);
            _mockRagRepository.Setup(repo => repo.AddAsync(It.IsAny<DbIndexDocument>(), indexName)).ReturnsAsync(new DbIndexDocument());

            // Act
            var result = await _ragLogic.UpsertDocuments(indexName, upsertRequest);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpsertDocuments_ShouldReturnFalse_WhenIndexNameIsInvalid()
        {
            // Arrange
            var indexName = "invalid index name";
            var upsertRequest = new RagUpsertRequest();

            // Act
            var result = await _ragLogic.UpsertDocuments(indexName, upsertRequest);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpsertDocuments_ShouldReturnFalse_WhenIndexDoesNotExist()
        {
            // Arrange
            var indexName = "nonExistentIndex";
            var upsertRequest = new RagUpsertRequest();
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync((DbIndexMetadata?)null);

            // Act
            var result = await _ragLogic.UpsertDocuments(indexName, upsertRequest);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteDocuments_ShouldReturnDeletedCount_WhenDocumentsAreDeletedSuccessfully()
        {
            // Arrange
            var indexName = "testIndex";
            var documentNames = new[] { "doc1", "doc2" };
            var dbDocument = new DbIndexDocument();
            _mockRagRepository.Setup(repo => repo.GetDocumentAsync(indexName, It.IsAny<string>())).ReturnsAsync(dbDocument);
            _mockRagRepository.Setup(repo => repo.DeleteAsync(dbDocument, indexName)).ReturnsAsync(1);

            // Act
            var result = await _ragLogic.DeleteDocuments(indexName, documentNames);

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task DeleteDocuments_ShouldReturnNegativeOne_WhenIndexNameIsInvalid()
        {
            // Arrange
            var indexName = "invalid index name";
            var documentNames = new[] { "doc1", "doc2" };

            // Act
            var result = await _ragLogic.DeleteDocuments(indexName, documentNames);

            // Assert
            Assert.Equal(-1, result);
        }
    }
}
