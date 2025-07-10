using Azure.Search.Documents.Models;
using global::IntelligenceHub.API.DTOs.RAG;
using global::IntelligenceHub.DAL.Models;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Factories;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;
using System.Web.Razor.Generator;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Business
{

    public class RagLogicTests
    {
        private readonly Mock<IAGIClientFactory> _mockClientFactory;
        private readonly Mock<IProfileRepository> _mockProfileRepository;
        private readonly Mock<IIndexMetaRepository> _mockMetaRepository;
        private readonly Mock<IAISearchServiceClient> _mockSearchClient;
        private readonly Mock<IRagClientFactory> _mockRagClientFactory;
        private readonly Mock<IIndexRepository> _mockRagRepository;
        private readonly Mock<IValidationHandler> _mockValidationHandler;
        private readonly Mock<IBackgroundTaskQueueHandler> _mockBackgroundTaskQueueHandler;
        private readonly Mock<IOptionsMonitor<Settings>> _mockIOptions;
        private readonly Mock<IntelligenceHubDbContext> _context;
        private readonly RagLogic _ragLogic;

        public RagLogicTests()
        {
            _mockClientFactory = new Mock<IAGIClientFactory>();
            _mockProfileRepository = new Mock<IProfileRepository>();
            _mockSearchClient = new Mock<IAISearchServiceClient>();
            _mockRagClientFactory = new Mock<IRagClientFactory>();
            _mockRagClientFactory.Setup(f => f.GetClient(It.IsAny<RagServiceHost?>())).Returns(_mockSearchClient.Object);
            _mockMetaRepository = new Mock<IIndexMetaRepository>();
            _mockRagRepository = new Mock<IIndexRepository>();
            _mockValidationHandler = new Mock<IValidationHandler>();
            _mockBackgroundTaskQueueHandler = new Mock<IBackgroundTaskQueueHandler>();
            var mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockIOptions = new Mock<IOptionsMonitor<Settings>>();
            _context = new Mock<IntelligenceHubDbContext>();

            var settings = new Settings { ValidAGIModels = new[] { "Model1", "Model2" } };
            _mockIOptions.Setup(m => m.CurrentValue).Returns(settings);

            _ragLogic = new RagLogic(_mockIOptions.Object, _mockClientFactory.Object, _mockProfileRepository.Object, _mockRagClientFactory.Object, _mockMetaRepository.Object, _mockRagRepository.Object, _mockValidationHandler.Object, _mockBackgroundTaskQueueHandler.Object, _context.Object, null!, mockScopeFactory.Object);
        }

        [Fact]
        public async Task GetRagIndex_ShouldReturnIndexMetadata_WhenIndexExists()
        {
            // Arrange
            var indexName = "testIndex";
            var dbIndexMetadata = new DbIndexMetadata { Name = indexName, GenerationHost = AGIServiceHost.Azure.ToString() };
            _mockValidationHandler.Setup(repo => repo.IsValidIndexName(indexName)).Returns(true);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(dbIndexMetadata);

            // Act
            var result = await _ragLogic.GetRagIndex(indexName);

            // Assert
            Assert.NotNull(result.Data);
            Assert.Equal(indexName, result.Data.Name);
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
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetAllIndexesAsync_ShouldReturnAllIndexes_WhenIndexesExist()
        {
            // Arrange
            var dbIndexes = new List<DbIndexMetadata>
    {
        new DbIndexMetadata { Name = "index1", GenerationHost = AGIServiceHost.Azure.ToString() },
        new DbIndexMetadata { Name = "index2", GenerationHost = AGIServiceHost.Azure.ToString() }
    };
            _mockMetaRepository.Setup(repo => repo.GetAllAsync(null, null)).ReturnsAsync(dbIndexes);

            // Act
            var result = await _ragLogic.GetAllIndexesAsync();

            // Assert
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count());
            Assert.Contains(result.Data, index => index.Name == "index1");
            Assert.Contains(result.Data, index => index.Name == "index2");
        }

        [Fact]
        public async Task GetAllIndexesAsync_ShouldReturnEmptyList_WhenNoIndexesExist()
        {
            // Arrange
            var dbIndexes = new List<DbIndexMetadata>();
            _mockMetaRepository.Setup(repo => repo.GetAllAsync(null, null)).ReturnsAsync(dbIndexes);

            // Act
            var result = await _ragLogic.GetAllIndexesAsync();

            // Assert
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetAllIndexesAsync_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            _mockMetaRepository.Setup(repo => repo.GetAllAsync(null, null)).ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _ragLogic.GetAllIndexesAsync());
        }

        [Fact]
        public async Task CreateIndex_ShouldReturnTrue_WhenIndexIsCreatedSuccessfully()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "newIndex", QueryType = QueryType.Simple };
            var dbIndexMetadata = new DbIndexMetadata() { Name = indexMetadata.Name, QueryType = QueryType.Simple.ToString(), ChunkOverlap = DefaultChunkOverlap, GenerationHost = AGIServiceHost.Azure.ToString(), EmbeddingModel = DefaultAzureSearchEmbeddingModel };

            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexMetadata.Name)).ReturnsAsync((DbIndexMetadata)null);
            _mockMetaRepository.Setup(repo => repo.AddAsync(It.IsAny<DbIndexMetadata>())).ReturnsAsync(dbIndexMetadata);
            _mockRagRepository.Setup(repo => repo.CreateIndexAsync(indexMetadata.Name)).ReturnsAsync(true);
            _mockRagRepository.Setup(repo => repo.EnableChangeTrackingAsync(indexMetadata.Name)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.UpsertIndex(indexMetadata)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.CreateDatasource(indexMetadata.Name)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.UpsertIndexer(indexMetadata)).ReturnsAsync(true);

            // Act
            var result = await _ragLogic.CreateIndex(indexMetadata);

            // Assert
            Assert.True(result.Data);
        }

        [Fact]
        public async Task CreateIndex_ShouldReturnFalse_WhenIndexNameIsInvalid()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "invalid index name" };

            // Act
            var result = await _ragLogic.CreateIndex(indexMetadata);

            // Assert
            Assert.False(result.Data);
        }

        [Fact]
        public async Task CreateIndex_ShouldReturnFalse_WhenIndexAlreadyExists()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "existingIndex" };
            var dbIndexMetadata = new DbIndexMetadata { Name = indexMetadata.Name, GenerationHost = AGIServiceHost.Azure.ToString() };
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexMetadata.Name)).ReturnsAsync(dbIndexMetadata);

            // Act
            var result = await _ragLogic.CreateIndex(indexMetadata);

            // Assert
            Assert.False(result.Data);
        }

        [Fact]
        public async Task ConfigureIndex_ShouldReturnFalse_WhenIndexDefinitionIsInvalid()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "invalidIndex" };
            _mockValidationHandler.Setup(v => v.ValidateIndexDefinition(indexMetadata)).Returns("Invalid definition");

            // Act
            var result = await _ragLogic.ConfigureIndex(indexMetadata);

            // Assert
            Assert.False(result.Data);
        }

        [Fact]
        public async Task ConfigureIndex_ShouldReturnFalse_WhenIndexDoesNotExist()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "nonExistentIndex" };
            _mockValidationHandler.Setup(v => v.ValidateIndexDefinition(indexMetadata)).Returns(string.Empty);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexMetadata.Name)).ReturnsAsync((DbIndexMetadata)null);

            // Act
            var result = await _ragLogic.ConfigureIndex(indexMetadata);

            // Assert
            Assert.False(result.Data);
        }

        [Fact]
        public async Task ConfigureIndex_ShouldReturnFalse_WhenUpsertIndexFails()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "testIndex" };
            var dbIndexMetadata = new DbIndexMetadata { Name = indexMetadata.Name, GenerationHost = AGIServiceHost.Azure.ToString() };
            _mockValidationHandler.Setup(v => v.ValidateIndexDefinition(indexMetadata)).Returns(string.Empty);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexMetadata.Name)).ReturnsAsync(dbIndexMetadata);
            _mockSearchClient.Setup(client => client.UpsertIndex(indexMetadata)).ReturnsAsync(false);

            // Act
            var result = await _ragLogic.ConfigureIndex(indexMetadata);

            // Assert
            Assert.False(result.Data);
        }

        [Fact]
        public async Task ConfigureIndex_ShouldReturnFalse_WhenUpsertIndexerFails()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "testIndex" };
            var dbIndexMetadata = new DbIndexMetadata { Name = indexMetadata.Name, GenerationHost = AGIServiceHost.Azure.ToString() };
            _mockValidationHandler.Setup(v => v.ValidateIndexDefinition(indexMetadata)).Returns(string.Empty);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexMetadata.Name)).ReturnsAsync(dbIndexMetadata);
            _mockSearchClient.Setup(client => client.UpsertIndex(indexMetadata)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.UpsertIndexer(indexMetadata)).ReturnsAsync(false);

            // Act
            var result = await _ragLogic.ConfigureIndex(indexMetadata);

            // Assert
            Assert.False(result.Data);
        }

        [Fact]
        public async Task ConfigureIndex_ShouldReturnTrue_WhenConfigurationIsSuccessful()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "testIndex" };
            var dbIndexMetadata = new DbIndexMetadata { Name = indexMetadata.Name, GenerationHost = AGIServiceHost.Azure.ToString(), GenerateKeywords = false, GenerateTopic = false }; // To vastly simplify testing, these are set to false for the time being
            _mockValidationHandler.Setup(v => v.ValidateIndexDefinition(indexMetadata)).Returns(string.Empty);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexMetadata.Name)).ReturnsAsync(dbIndexMetadata);
            _mockSearchClient.Setup(client => client.UpsertIndex(indexMetadata)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.UpsertIndexer(indexMetadata)).ReturnsAsync(true);
            _mockMetaRepository.Setup(repo => repo.UpdateAsync(It.IsAny<DbIndexMetadata>())).ReturnsAsync(dbIndexMetadata);
            _mockSearchClient.Setup(repo => repo.RunIndexer(indexMetadata.Name)).ReturnsAsync(true);

            // Act
            var result = await _ragLogic.ConfigureIndex(indexMetadata);

            // Assert
            Assert.True(result.Data);
        }

        [Fact]
        public async Task ConfigureIndex_ShouldRunIndexer_WhenGenerativeFieldsAreUpdated()
        {
            // Arrange
            var indexMetadata = new IndexMetadata { Name = "testIndex", GenerateContentVector = true };
            var dbIndexMetadata = new DbIndexMetadata { Name = indexMetadata.Name, GenerateContentVector = false, GenerationHost = AGIServiceHost.Azure.ToString() };
            _mockValidationHandler.Setup(v => v.ValidateIndexDefinition(indexMetadata)).Returns(string.Empty);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexMetadata.Name)).ReturnsAsync(dbIndexMetadata);
            _mockSearchClient.Setup(client => client.UpsertIndex(indexMetadata)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.UpsertIndexer(indexMetadata)).ReturnsAsync(true);
            _mockMetaRepository.Setup(repo => repo.UpdateAsync(It.IsAny<DbIndexMetadata>())).ReturnsAsync(dbIndexMetadata);
            _mockRagRepository.Setup(repo => repo.MarkIndexForUpdateAsync(indexMetadata.Name)).ReturnsAsync(true);
            _mockSearchClient.Setup(client => client.RunIndexer(indexMetadata.Name)).ReturnsAsync(true);

            // Act
            var result = await _ragLogic.ConfigureIndex(indexMetadata);

            // Assert
            Assert.True(result.Data);
            _mockRagRepository.Verify(repo => repo.MarkIndexForUpdateAsync(indexMetadata.Name), Times.Once);
            _mockSearchClient.Verify(client => client.RunIndexer(indexMetadata.Name), Times.Once);
        }

        [Fact]
        public async Task RunIndexUpdate_ShouldReturnFalse_WhenIndexNameIsInvalid()
        {
            // Arrange
            var indexName = "invalid index name";
            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(false);

            // Act
            var result = await _ragLogic.RunIndexUpdate(indexName);

            // Assert
            Assert.False(result.Data);
        }

        [Fact]
        public async Task RunIndexUpdate_ShouldReturnFalse_WhenIndexDoesNotExist()
        {
            // Arrange
            var indexName = "nonExistentIndex";
            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync((DbIndexMetadata?)null);

            // Act
            var result = await _ragLogic.RunIndexUpdate(indexName);

            // Assert
            Assert.False(result.Data);
        }

        [Fact]
        public async Task RunIndexUpdate_ShouldReturnTrue_WhenIndexerRunsSuccessfully()
        {
            // Arrange
            var indexName = "testIndex";
            var dbIndexMetadata = new DbIndexMetadata { Name = indexName, RagHost = AGIServiceHost.Azure.ToString() };
            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(dbIndexMetadata);
            _mockSearchClient.Setup(client => client.RunIndexer(indexName)).ReturnsAsync(true);
            _mockRagClientFactory.Setup(f => f.GetClient(RagServiceHost.Azure)).Returns(_mockSearchClient.Object);

            // Act
            var result = await _ragLogic.RunIndexUpdate(indexName);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
            Assert.Equal(APIResponseStatusCodes.Ok, result.StatusCode);
        }

        [Fact]
        public async Task RunIndexUpdate_ShouldReturnFalse_WhenIndexerFailsToRun()
        {
            // Arrange
            var indexName = "testIndex";
            var dbIndexMetadata = new DbIndexMetadata { Name = indexName, GenerationHost = AGIServiceHost.Azure.ToString() };
            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(dbIndexMetadata);
            _mockSearchClient.Setup(client => client.RunIndexer(indexName)).ReturnsAsync(false);

            // Act
            var result = await _ragLogic.RunIndexUpdate(indexName);

            // Assert
            Assert.False(result.Data);
        }

        [Fact]
        public async Task QueryIndex_ShouldReturnBadRequestWrapper_WhenIndexNameIsInvalid()
        {
            // Arrange
            var indexName = "invalid index name";
            var query = "test query";
            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(false);

            // Act
            var result = await _ragLogic.QueryIndex(indexName, query);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.BadRequest, result.StatusCode);
            Assert.Equal($"The supplied index name, '{indexName}' is invalid. Please avoid reserved SQL words.", result.ErrorMessage);
        }

        [Fact]
        public async Task QueryIndex_ShouldReturnBadRequestWrapper_WhenQueryIsEmpty()
        {
            // Arrange
            var indexName = "testIndex";
            var query = string.Empty;
            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);

            // Act
            var result = await _ragLogic.QueryIndex(indexName, query);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.BadRequest, result.StatusCode);
            Assert.Equal("The supplied query is null or empty.", result.ErrorMessage);
        }

        [Fact]
        public async Task QueryIndex_ShouldReturnNotFound_WhenIndexDoesNotExist()
        {
            // Arrange
            var indexName = "nonExistentIndex";
            var query = "test query";
            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync((DbIndexMetadata?)null);

            // Act
            var result = await _ragLogic.QueryIndex(indexName, query);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.NotFound, result.StatusCode);
            Assert.Equal($"No index with the name '{indexName}' was found.", result.ErrorMessage);
        }

        [Fact]
        public async Task QueryIndex_ShouldReturnDocuments_WhenQueryIsSuccessful()
        {
            // Arrange
            var indexName = "testIndex";
            var query = "test query";
            var dbIndexMetadata = new DbIndexMetadata { Name = indexName, QueryType = QueryType.Simple.ToString(), GenerationHost = AGIServiceHost.Azure.ToString() };
            var searchResults = CreateMockSimpleSearchResults();

            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(dbIndexMetadata);
            _mockSearchClient.Setup(client => client.SearchIndex(It.IsAny<IndexMetadata>(), query))
                .ReturnsAsync(searchResults);

            // Act
            var result = await _ragLogic.QueryIndex(indexName, query);

            // Assert
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            var doc = result.Data.First();
            Assert.Equal("doc1", doc.Title);
            Assert.Equal("keyword1", doc.Keywords);
            Assert.Equal("topic1", doc.Topic);
            Assert.Equal("source1", doc.Source);
            Assert.Equal("content1", doc.Content);
        }

        [Fact]
        public async Task QueryIndex_ShouldReturnDocumentsWithSemanticContent_WhenQueryTypeIsSemantic()
        {
            // Arrange
            var indexName = "testIndex";
            var query = "test query";
            var dbIndexMetadata = new DbIndexMetadata { Name = indexName, QueryType = QueryType.Semantic.ToString(), GenerationHost = AGIServiceHost.Azure.ToString() };
            var searchResults = CreateMockSemanticSearchResults();

            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(dbIndexMetadata);
            _mockSearchClient.Setup(client => client.SearchIndex(It.IsAny<IndexMetadata>(), query))
                .ReturnsAsync(searchResults);

            // Act
            var result = await _ragLogic.QueryIndex(indexName, query);

            // Assert
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            var doc = result.Data.First();
            Assert.Equal("doc1", doc.Title);
            Assert.Equal("keyword1", doc.Keywords);
            Assert.Equal("topic1", doc.Topic);
            Assert.Equal("source1", doc.Source);
            Assert.Contains("semantic content", doc.Content);
        }

        /// <summary>
        /// Creates a mock SearchResults for a simple query.
        /// </summary>
        private static SearchResults<IndexDefinition> CreateMockSimpleSearchResults()
        {
            // Create a sample document.
            var indexDefinition = new IndexDefinition
            {
                title = "doc1",
                keywords = "keyword1",
                topic = "topic1",
                source = "source1",
                chunk = "content1"
            };

            // Use the factory to create a SearchResult instance.
            var searchResult = SearchModelFactory.SearchResult(
                indexDefinition,
                score: null,
                highlights: null);

            var results = new List<SearchResult<IndexDefinition>> { searchResult };

            // Use the factory method to create a SearchResults instance.
            return SearchModelFactory.SearchResults(
                values: results,
                totalCount: null,
                facets: null,
                coverage: null,
                rawResponse: null);
        }

        /// <summary>
        /// Creates a mock SearchResults for a semantic query.
        /// </summary>
        private static SearchResults<IndexDefinition> CreateMockSemanticSearchResults()
        {
            var indexDefinition = new IndexDefinition
            {
                title = "doc1",
                keywords = "keyword1",
                topic = "topic1",
                source = "source1",
                chunk = "content1"
            };

            // Create a semantic search result with a caption using our helper method.
            var captionResult = CreateMockQueryCaptionResult("semantic content");
            var semanticSearchResult = SearchModelFactory.SemanticSearchResult(
                rerankerScore: null,
                captions: new List<QueryCaptionResult> { captionResult });

            // Use the factory to create a SearchResult with semantic data.
            var searchResult = SearchModelFactory.SearchResult(
                indexDefinition,
                score: null,
                highlights: null,
                semanticSearch: semanticSearchResult);

            var results = new List<SearchResult<IndexDefinition>> { searchResult };

            // Use the factory method to create a SearchResults instance.
            return SearchModelFactory.SearchResults(
                values: results,
                totalCount: null,
                facets: null,
                coverage: null,
                rawResponse: null);
        }

        /// <summary>
        /// Creates a mock QueryCaptionResult using reflection to bypass constructor limitations.
        /// </summary>
        private static QueryCaptionResult CreateMockQueryCaptionResult(string text)
        {
            // Create an instance using the non-public constructor.
            var captionResult = (QueryCaptionResult)Activator.CreateInstance(typeof(QueryCaptionResult), nonPublic: true);

            // Get the backing field for the read-only 'Text' property.
            var backingField = typeof(QueryCaptionResult)
                .GetField("<Text>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (backingField == null)
            {
                throw new InvalidOperationException("The backing field for Text was not found.");
            }

            // Set the backing field's value.
            backingField.SetValue(captionResult, text);

            return captionResult;
        }

        [Fact]
        public async Task DeleteIndex_ShouldReturnTrue_WhenIndexIsDeletedSuccessfully()
        {
            // Arrange
            var indexName = "testIndex";

            var dbIndexMetadata = new DbIndexMetadata
            {
                Name = indexName,
                QueryType = QueryType.Simple.ToString(),
                ChunkOverlap = DefaultChunkOverlap,
                GenerationHost = AGIServiceHost.Azure.ToString(),
                EmbeddingModel = DefaultAzureSearchEmbeddingModel,
                RagHost = RagServiceHost.Azure.ToString()
            };

            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);

            _mockMetaRepository.Setup(r => r.GetByNameAsync(indexName)).ReturnsAsync(dbIndexMetadata);

            _mockRagRepository.Setup(r => r.DeleteIndexAsync(indexName)).ReturnsAsync(true);

            _mockSearchClient.Setup(c => c.DeleteIndexer(indexName)).ReturnsAsync(true);
            _mockSearchClient.Setup(c => c.DeleteDatasource(indexName)).ReturnsAsync(true);
            _mockSearchClient.Setup(c => c.DeleteIndex(indexName)).ReturnsAsync(true);

            _mockMetaRepository.Setup(r => r.DeleteAsync(dbIndexMetadata)).ReturnsAsync(true);

            // Tell the factory which client to hand back
            _mockRagClientFactory.Setup(f => f.GetClient(RagServiceHost.Azure)).Returns(_mockSearchClient.Object);

            // Act
            var result = await _ragLogic.DeleteIndex(indexName);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Data);
        }


        [Fact]
        public async Task DeleteIndex_ShouldReturnFalse_WhenIndexNameIsInvalid()
        {
            // Arrange
            var indexName = "invalid index name";

            // Act
            var result = await _ragLogic.DeleteIndex(indexName);

            // Assert
            Assert.False(result.Data);
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
            Assert.False(result.Data);
        }

        [Fact]
        public async Task UpsertDocuments_ShouldReturnTrue_WhenDocumentsAreUpsertedSuccessfully()
        {
            // Arrange
            var indexName = "testIndex";
            var dbIndexMetadata = new DbIndexMetadata { Name = indexName, GenerationHost = AGIServiceHost.Azure.ToString() };
            var document = new IndexDocument { Title = "testDocument", Content = "testContent" };
            var upsertRequest = new RagUpsertRequest { Documents = new List<IndexDocument> { document } };
            _mockValidationHandler.Setup(repo => repo.IsValidIndexName(indexName)).Returns(true);
            _mockValidationHandler.Setup(repo => repo.IsValidRagUpsertRequest(upsertRequest)).Returns(string.Empty);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(dbIndexMetadata);
            _mockRagRepository.Setup(repo => repo.GetDocumentAsync(indexName, document.Title)).ReturnsAsync((DbIndexDocument?)null);
            _mockRagRepository.Setup(repo => repo.AddAsync(It.IsAny<DbIndexDocument>(), indexName)).ReturnsAsync(new DbIndexDocument());

            // Act
            var result = await _ragLogic.UpsertDocuments(indexName, upsertRequest);

            // Assert
            Assert.True(result.Data);
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
            Assert.False(result.Data);
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
            Assert.False(result.Data);
        }

        [Fact]
        public async Task GetDocument_ShouldReturnDocument_WhenDocumentExists()
        {
            // Arrange
            var indexName = "testIndex";
            var documentName = "testDocument";
            var dbDocument = new DbIndexDocument { Title = documentName, Content = "testContent" };
            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);
            _mockRagRepository.Setup(repo => repo.GetDocumentAsync(indexName, documentName)).ReturnsAsync(dbDocument);

            // Act
            var result = await _ragLogic.GetDocument(indexName, documentName);

            // Assert
            Assert.NotNull(result.Data);
            Assert.Equal(documentName, result.Data.Title);
            Assert.Equal("testContent", result.Data.Content);
        }

        [Fact]
        public async Task GetDocument_ShouldReturnNull_WhenIndexNameIsInvalid()
        {
            // Arrange
            var indexName = "invalid index name";
            var documentName = "testDocument";
            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(false);

            // Act
            var result = await _ragLogic.GetDocument(indexName, documentName);

            // Assert
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetDocument_ShouldReturnNull_WhenDocumentNameIsEmpty()
        {
            // Arrange
            var indexName = "testIndex";
            var documentName = string.Empty;
            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);

            // Act
            var result = await _ragLogic.GetDocument(indexName, documentName);

            // Assert
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetDocument_ShouldReturnNull_WhenDocumentDoesNotExist()
        {
            // Arrange
            var indexName = "testIndex";
            var documentName = "nonExistentDocument";
            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);
            _mockRagRepository.Setup(repo => repo.GetDocumentAsync(indexName, documentName)).ReturnsAsync((DbIndexDocument?)null);

            // Act
            var result = await _ragLogic.GetDocument(indexName, documentName);

            // Assert
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetAllDocuments_ShouldReturnDocuments_WhenIndexIsValid()
        {
            // Arrange
            var indexName = "testIndex";
            var count = 10;
            var page = 1;
            var dbDocuments = new List<DbIndexDocument>
            {
                new DbIndexDocument { Title = "doc1", Content = "content1" },
                new DbIndexDocument { Title = "doc2", Content = "content2" }
            };
            var expectedDocuments = dbDocuments.Select(DbMappingHandler.MapFromDbIndexDocument).ToList();

            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);
            _mockRagRepository.Setup(repo => repo.GetAllAsync(indexName, count, page)).ReturnsAsync(dbDocuments);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(new DbIndexMetadata { Name = indexName });

            // Act
            var result = await _ragLogic.GetAllDocuments(indexName, count, page);

            // Assert
            Assert.NotNull(result.Data);
            Assert.Equal(expectedDocuments.Count, result.Data.Count());
            Assert.Equal(expectedDocuments.First().Title, result.Data.First().Title);
            Assert.Equal(expectedDocuments.First().Content, result.Data.First().Content);
        }

        [Fact]
        public async Task GetAllDocuments_ShouldReturnNull_WhenIndexNameIsInvalid()
        {
            // Arrange
            var indexName = "invalid index name";
            var count = 10;
            var page = 1;

            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(false);

            // Act
            var result = await _ragLogic.GetAllDocuments(indexName, count, page);

            // Assert
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetAllDocuments_ShouldReturnEmptyList_WhenNoDocumentsExist()
        {
            // Arrange
            var indexName = "testIndex";
            var count = 10;
            var page = 1;
            var dbDocuments = new List<DbIndexDocument>();

            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);
            _mockRagRepository.Setup(repo => repo.GetAllAsync(indexName, count, page)).ReturnsAsync(dbDocuments);
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(new DbIndexMetadata { Name = indexName });

            // Act
            var result = await _ragLogic.GetAllDocuments(indexName, count, page);

            // Assert
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }

        [Fact]
        public async Task GetAllDocuments_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            var indexName = "testIndex";
            var count = 10;
            var page = 1;

            _mockValidationHandler.Setup(v => v.IsValidIndexName(indexName)).Returns(true);
            _mockRagRepository.Setup(repo => repo.GetAllAsync(indexName, count, page)).ThrowsAsync(new Exception("Database error"));
            _mockMetaRepository.Setup(repo => repo.GetByNameAsync(indexName)).ReturnsAsync(new DbIndexMetadata { Name = indexName });

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _ragLogic.GetAllDocuments(indexName, count, page));
        }

        [Fact]
        public async Task DeleteDocuments_ShouldReturnDeletedCount_WhenDocumentsAreDeletedSuccessfully()
        {
            // Arrange
            var indexName = "testIndex";
            var documentNames = new[] { "doc1", "doc2" };
            var dbDocument = new DbIndexDocument();
            _mockValidationHandler.Setup(repo => repo.IsValidIndexName(indexName)).Returns(true);
            _mockRagRepository.Setup(repo => repo.GetDocumentAsync(indexName, It.IsAny<string>())).ReturnsAsync(dbDocument);
            _mockRagRepository.Setup(repo => repo.DeleteAsync(dbDocument, indexName)).ReturnsAsync(true);

            // Act
            var result = await _ragLogic.DeleteDocuments(indexName, documentNames);

            // Assert
            Assert.Equal(2, result.Data);
        }

        [Fact]
        public async Task DeleteDocuments_ShouldReturnBadRequestWrapper_WhenIndexNameIsInvalid()
        {
            // Arrange
            var indexName = "invalid index name with SQL keyword such as DROP";
            var documentNames = new[] { "doc1", "doc2" };
            _mockValidationHandler.Setup(repo => repo.IsValidIndexName(indexName)).Returns(false);

            // Act
            var result = await _ragLogic.DeleteDocuments(indexName, documentNames);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(APIResponseStatusCodes.BadRequest, result.StatusCode);
            Assert.Equal($"The supplied index name, '{indexName}' is invalid. Please avoid reserved SQL words.", result.ErrorMessage);
        }
    }
}
