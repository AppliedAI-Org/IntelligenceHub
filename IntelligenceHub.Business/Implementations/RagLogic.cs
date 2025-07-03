using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Factories;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL.Interfaces;
using IntelligenceHub.DAL.Models;
using IntelligenceHub.DAL;
using Microsoft.Extensions.Options;
using static IntelligenceHub.Common.GlobalVariables;
using IntelligenceHub.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using IntelligenceHub.Business.Handlers;

namespace IntelligenceHub.Business.Implementations
{
    /// <summary>
    /// Business logic for handling RAG operations.
    /// </summary>
    public class RagLogic : IRagLogic
    {
        private readonly IAGIClientFactory _agiClientFactory;
        private readonly IAISearchServiceClient _searchClient;
        private readonly IIndexMetaRepository _metaRepository;
        private readonly IIndexRepository _ragRepository;
        private readonly IValidationHandler _validationHandler;
        private readonly IBackgroundTaskQueueHandler _backgroundTaskQueue;
        private readonly IntelligenceHubDbContext _dbContext;

        private readonly string _defaultAzureModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="RagLogic"/> class.
        /// </summary>
        /// <param name="settings">The application settings.</param>
        /// <param name="agiFactory">Client factory used to construct an AGI client according to the specified host.</param>
        /// <param name="profileRepository">A repository containing profile data.</param>
        /// <param name="aISearchServiceClient">A client representing the search service.</param>
        /// <param name="metaRepository">A repository containing metadata about the RAG indexes.</param>
        /// <param name="indexRepository">A special repository designed to perform CRUD operations on tables for RAG applications these tables are associated with a RAG database within the Search Service client.</param>
        /// <param name="validationHandler">A class that can be used to validate DTO bodies passed to the API.</param>
        /// <param name="backgroundTaskQueue">A background task handler useful for performing operations without tying up resources.</param>
        /// <param name="context">DAL context from EFCore used for some more specialized scenarios.</param>
        public RagLogic(IOptionsMonitor<Settings> settings, IAGIClientFactory agiFactory, IProfileRepository profileRepository, IAISearchServiceClient aISearchServiceClient, IIndexMetaRepository metaRepository, IIndexRepository indexRepository, IValidationHandler validationHandler, IBackgroundTaskQueueHandler backgroundTaskQueue, IntelligenceHubDbContext context)
        {
            _defaultAzureModel = settings.CurrentValue.ValidAGIModels.FirstOrDefault() ?? string.Empty;
            _agiClientFactory = agiFactory;
            _searchClient = aISearchServiceClient;
            _metaRepository = metaRepository;
            _ragRepository = indexRepository;
            _validationHandler = validationHandler;
            _backgroundTaskQueue = backgroundTaskQueue;
            _dbContext = context;
        }

        /// <summary>
        /// Retrieves the metadata for a RAG index.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <returns>An <see cref="APIResponseWrapper{IndexMetadata}"/> containing the index definition, if one exists.</returns>
        public async Task<APIResponseWrapper<IndexMetadata>> GetRagIndex(string index)
        {
            if (!_validationHandler.IsValidIndexName(index)) return APIResponseWrapper<IndexMetadata>.Failure("The provided index name is invalid. Please avoid reserved SQL words.", APIResponseStatusCodes.BadRequest);
            var dbIndexData = await _metaRepository.GetByNameAsync(index);
            if (dbIndexData == null) return APIResponseWrapper<IndexMetadata>.Failure($"No index by the name '{index}' was found.", APIResponseStatusCodes.NotFound);
            return APIResponseWrapper<IndexMetadata>.Success(DbMappingHandler.MapFromDbIndexMetadata(dbIndexData));
        }

        /// <summary>
        /// Retrieves all RAG metadata associated with RAG indexes.
        /// </summary>
        /// <returns>An <see cref="APIResponseWrapper{IEnumerable{IndexMetadata}}"/> containing a list of index metadata.</returns>
        public async Task<APIResponseWrapper<IEnumerable<IndexMetadata>>> GetAllIndexesAsync()
        {
            var allIndexes = new List<IndexMetadata>();
            var allDbIndexes = await _metaRepository.GetAllAsync();
            foreach (var dbIndex in allDbIndexes) allIndexes.Add(DbMappingHandler.MapFromDbIndexMetadata(dbIndex));
            return APIResponseWrapper<IEnumerable<IndexMetadata>>.Success(allIndexes);
        }

        /// <summary>
        /// Creates a new RAG index.
        /// </summary>
        /// <param name="indexDefinition">The definition of the index.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating success or failure.</returns>
        public async Task<APIResponseWrapper<bool>> CreateIndex(IndexMetadata indexDefinition)
        {
            var errorMessage = _validationHandler.ValidateIndexDefinition(indexDefinition);
            if (!string.IsNullOrEmpty(errorMessage)) return APIResponseWrapper<bool>.Failure(errorMessage, APIResponseStatusCodes.BadRequest);

            var existing = await _metaRepository.GetByNameAsync(indexDefinition.Name);
            if (existing != null) return APIResponseWrapper<bool>.Failure($"An index with the name '{indexDefinition.Name}' already exists.", APIResponseStatusCodes.BadRequest);

            // add index entry for metadata
            var newDbIndex = DbMappingHandler.MapToDbIndexMetadata(indexDefinition);
            var response = await _metaRepository.AddAsync(newDbIndex);
            if (response == null) APIResponseWrapper<bool>.Failure($"Failed to add index '{indexDefinition.Name}' to the database.", APIResponseStatusCodes.InternalError);

            // create a new table for the index
            var success = await _ragRepository.CreateIndexAsync(indexDefinition.Name);
            if (!success) return APIResponseWrapper<bool>.Failure($"Partially failed to add index '{indexDefinition.Name}' to the database.", APIResponseStatusCodes.InternalError);

            success = await _ragRepository.EnableChangeTrackingAsync(indexDefinition.Name);
            if (!success) return APIResponseWrapper<bool>.Failure($"Partially failed to add index '{indexDefinition.Name}' to the database.", APIResponseStatusCodes.InternalError);

            success = await _ragRepository.CreateDatasourceViewAsync(indexDefinition.Name);
            if (!success) return APIResponseWrapper<bool>.Failure($"Partially failed to add index '{indexDefinition.Name}' to the database.", APIResponseStatusCodes.InternalError);

            // create the index in Azure AI Search
            success = await _searchClient.UpsertIndex(indexDefinition);
            if (!success) return APIResponseWrapper<bool>.Failure("Failed to add the index to the corresponding search service resource.", APIResponseStatusCodes.InternalError);

            // Create a datasource for the SQL DB in Azure AI Search
            success = await _searchClient.CreateDatasource(indexDefinition.Name);
            if (!success) return APIResponseWrapper<bool>.Failure("Failed to connect the index to the Azure AI search service resource.", APIResponseStatusCodes.InternalError);

            // create the indexer to run scheduled ingestions of the datasource
            success = await _searchClient.UpsertIndexer(indexDefinition);
            if (!success) return APIResponseWrapper<bool>.Failure("Failed to create the indexer used to ingest documents within the search service.", APIResponseStatusCodes.InternalError);
            return APIResponseWrapper<bool>.Success(true);
        }

        /// <summary>
        /// Configures an existing RAG index.
        /// </summary>
        /// <param name="indexDefinition">The new definition of the index.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating success or failure.</returns>
        public async Task<APIResponseWrapper<bool>> ConfigureIndex(IndexMetadata indexDefinition) // NOTE: Given the below code, disabling generative columns will not destroy existing data
        {
            var errorMessage = _validationHandler.ValidateIndexDefinition(indexDefinition);
            if (!string.IsNullOrEmpty(errorMessage)) return APIResponseWrapper<bool>.Failure(errorMessage, APIResponseStatusCodes.BadRequest);
            var existingDefinition = await _metaRepository.GetByNameAsync(indexDefinition.Name);
            if (existingDefinition == null) return APIResponseWrapper<bool>.Failure($"An index with the name '{indexDefinition.Name}' was not found.", APIResponseStatusCodes.NotFound);

            //var success = await _searchClient.UpsertIndex(indexDefinition);
            //if (!success) return APIResponseWrapper<bool>.Failure("Failed to update the index against the search service.", APIResponseStatusCodes.InternalError);

            //success = await _searchClient.UpsertIndexer(indexDefinition);
            //if (!success) return APIResponseWrapper<bool>.Failure("Failed to update the indexer against the search service.", APIResponseStatusCodes.InternalError);

            var newDefinition = DbMappingHandler.MapToDbIndexMetadata(indexDefinition);

            // Check if we an update is required - this is done before updating the SQL, as existingDefinition reflects the current state of the corresponding SQL entry
            var updateAllDocs = false;
            if (!existingDefinition.GenerateContentVector && newDefinition.GenerateContentVector) updateAllDocs = true;
            else if (!existingDefinition.GenerateTopicVector && newDefinition.GenerateTopicVector) updateAllDocs = true;
            else if (!existingDefinition.GenerateKeywordVector && newDefinition.GenerateKeywordVector) updateAllDocs = true;
            else if (!existingDefinition.GenerateTitleVector && newDefinition.GenerateTitleVector) updateAllDocs = true;

            var generateMissingFields = false;
            if (!existingDefinition.GenerateTopic && newDefinition.GenerateTopic) generateMissingFields = true;
            else if (!existingDefinition.GenerateKeywords && newDefinition.GenerateKeywords) generateMissingFields = true;

            existingDefinition.Name = newDefinition.Name;
            existingDefinition.QueryType = newDefinition.QueryType?.ToString();
            existingDefinition.GenerationHost = newDefinition.GenerationHost.ToString();
            existingDefinition.ChunkOverlap = newDefinition.ChunkOverlap;
            existingDefinition.IndexingInterval = newDefinition.IndexingInterval;
            existingDefinition.MaxRagAttachments = newDefinition.MaxRagAttachments;
            existingDefinition.EmbeddingModel = newDefinition.EmbeddingModel;
            existingDefinition.GenerateTopic = newDefinition.GenerateTopic;
            existingDefinition.GenerateKeywords = newDefinition.GenerateKeywords;
            existingDefinition.GenerateTitleVector = newDefinition.GenerateTitleVector;
            existingDefinition.GenerateContentVector = newDefinition.GenerateContentVector;
            existingDefinition.GenerateTopicVector = newDefinition.GenerateTopicVector;
            existingDefinition.GenerateKeywordVector = newDefinition.GenerateKeywordVector;
            existingDefinition.DefaultScoringProfile = newDefinition.DefaultScoringProfile ?? DefaultVectorScoringProfile;
            existingDefinition.ScoringAggregation = newDefinition.ScoringAggregation?.ToString();
            existingDefinition.ScoringInterpolation = newDefinition.ScoringInterpolation?.ToString();
            existingDefinition.ScoringFreshnessBoost = newDefinition.ScoringFreshnessBoost;
            existingDefinition.ScoringBoostDurationDays = newDefinition.ScoringBoostDurationDays;
            existingDefinition.ScoringTagBoost = newDefinition.ScoringTagBoost;
            existingDefinition.ScoringWeights = newDefinition.ScoringWeights;

            // Update the SQL entry
            await _metaRepository.UpdateAsync(existingDefinition);

            // Create missing generative data if required
            if (updateAllDocs)
            {
                if (generateMissingFields) _ = GenerateMissingRagFields(indexDefinition);
                await _ragRepository.MarkIndexForUpdateAsync(newDefinition.Name);
                var indexerRepsonse = await _searchClient.RunIndexer(newDefinition.Name);
                if (!indexerRepsonse) return APIResponseWrapper<bool>.Failure("Failed to update the indexer against the search service.", APIResponseStatusCodes.InternalError);
            }
            return APIResponseWrapper<bool>.Success(true);
        }

        /// <summary>
        /// Generates missing RAG fields for an index.
        /// </summary>
        /// <param name="index">The definition of the index.</param>
        /// <returns>An awaitable task.</returns>
        private async Task GenerateMissingRagFields(IndexMetadata index)
        {
            const int pageSize = 100; // Define a reasonable chunk size for paging
            int currentPage = 1;
            bool hasMorePages;

            do
            {
                List<DbIndexDocument> pageDocs = new List<DbIndexDocument>();

                var strategy = _dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _dbContext.Database.BeginTransactionAsync();
                    pageDocs = (await _ragRepository.GetAllAsync(index.Name, pageSize, currentPage)).ToList();
                    await transaction.CommitAsync();
                });

                hasMorePages = pageDocs.Any();

                foreach (var document in pageDocs)
                {
                    _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                    {
                        await RunBackgroundDocumentUpdate(index, document);
                    });
                }
                currentPage++;
            } while (hasMorePages);
        }

        /// <summary>
        /// Runs a background task to update a document with missing RAG fields.
        /// </summary>
        /// <param name="index">The definition of the index.</param>
        /// <param name="document">The document to update.</param>
        /// <returns>An awaitable task.</returns>
        private async Task RunBackgroundDocumentUpdate(IndexMetadata index, DbIndexDocument document)
        {
            var documentDto = DbMappingHandler.MapFromDbIndexDocument(document);
            if (index.GenerateTopic ?? false && string.IsNullOrEmpty(document.Topic)) document.Topic = await GenerateDocumentMetadata("a topic", documentDto, index.GenerationHost ?? AGIServiceHosts.None);
            if (index.GenerateKeywords ?? false && string.IsNullOrEmpty(document.Keywords)) document.Keywords = await GenerateDocumentMetadata("a comma separated list of keywords", documentDto, index.GenerationHost ?? AGIServiceHosts.None);

            // Use EF Core execution strategy
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                var existingDoc = await _ragRepository.GetDocumentAsync(index.Name, document.Title);
                if (existingDoc != null)
                {
                    document.Modified = DateTimeOffset.UtcNow;
                    await _ragRepository.UpdateAsync(existingDoc.Id, document, index.Name);
                }
                else
                {
                    document.Created = DateTimeOffset.UtcNow;
                    document.Modified = DateTimeOffset.UtcNow;
                    await _ragRepository.AddAsync(document, index.Name);
                }
                await transaction.CommitAsync();
            });
        }

        /// <summary>
        /// Deletes a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating success or failure.</returns>
        public async Task<APIResponseWrapper<bool>> DeleteIndex(string index)
        {
            var success = false;
            if (!_validationHandler.IsValidIndexName(index)) return APIResponseWrapper<bool>.Failure($"The supplied index name, '{index}' is invalid. Please avoid reserved SQL words.", APIResponseStatusCodes.BadRequest);
            var indexMetadata = await _metaRepository.GetByNameAsync(index);
            if (indexMetadata == null) return APIResponseWrapper<bool>.Failure($"The index '{index}' was not found.", APIResponseStatusCodes.NotFound);
            if (await _ragRepository.DeleteIndexAsync(indexMetadata.Name))
            {
                success = await _searchClient.DeleteIndexer(indexMetadata.Name);
                if (!success) return APIResponseWrapper<bool>.Failure("Failed to delete the indexer within the search service.", APIResponseStatusCodes.InternalError);

                success = await _searchClient.DeleteDatasource(indexMetadata.Name);
                if (!success) return APIResponseWrapper<bool>.Failure("Failed to delete the datasource connection within the search service.", APIResponseStatusCodes.InternalError);

                success = await _searchClient.DeleteIndex(index);
                if (!success) return APIResponseWrapper<bool>.Failure("Failed to delete the index within the search service.", APIResponseStatusCodes.InternalError);

                success = await _metaRepository.DeleteAsync(indexMetadata);
                if (!success) return APIResponseWrapper<bool>.Failure("Failed to delete the index database.", APIResponseStatusCodes.InternalError);
                return APIResponseWrapper<bool>.Success(true);
            }
            return APIResponseWrapper<bool>.Failure("Failed to delete the index database.", APIResponseStatusCodes.InternalError);
        }

        /// <summary>
        /// Queries a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <param name="query">The query to search against the RAG index.</param>
        /// <returns>An <see cref="APIResponseWrapper{List{IndexDocument}}"/> containing a list of documents most closely matching the query.</returns>
        public async Task<APIResponseWrapper<List<IndexDocument>>> QueryIndex(string index, string query)
        {
            if (!_validationHandler.IsValidIndexName(index) || string.IsNullOrEmpty(index)) return APIResponseWrapper<List<IndexDocument>>.Failure($"The supplied index name, '{index}' is invalid. Please avoid reserved SQL words.", APIResponseStatusCodes.BadRequest);
            if (string.IsNullOrEmpty(query)) return APIResponseWrapper<List<IndexDocument>>.Failure("The supplied query is null or empty.", APIResponseStatusCodes.BadRequest);
            var indexData = await _metaRepository.GetByNameAsync(index);
            if (indexData is null) return APIResponseWrapper<List<IndexDocument>>.Failure($"No index with the name '{index}' was found.", APIResponseStatusCodes.NotFound);

            var docList = new List<IndexDocument>();
            var indexDefinition = DbMappingHandler.MapFromDbIndexMetadata(indexData);
            var response = await _searchClient.SearchIndex(indexDefinition, query);
            var results = response.GetResultsAsync();
            await foreach (var res in results)
            {
                var newDoc = new IndexDocument()
                {
                    Title = res.Document.title,
                    Keywords = res.Document.keywords,
                    Topic = res.Document.topic,
                    Source = res.Document.source,
                    Created = res.Document.created,
                    Modified = res.Document.modified
                };
                if (indexDefinition.QueryType == QueryType.Semantic) foreach (var caption in res.SemanticSearch.Captions) newDoc.Content += $"Excerpt: {caption.Text}\n\n";
                else newDoc.Content = res.Document.chunk;
                docList.Add(newDoc);
            }
            return APIResponseWrapper<List<IndexDocument>>.Success(docList);
        }

        /// <summary>
        /// Runs an update on a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating success or failure.</returns>
        public async Task<APIResponseWrapper<bool>> RunIndexUpdate(string index)
        {
            if (!_validationHandler.IsValidIndexName(index) || string.IsNullOrEmpty(index)) return APIResponseWrapper<bool>.Failure($"The supplied index name, '{index}' is invalid. Please avoid reserved SQL words.", APIResponseStatusCodes.BadRequest);
            var indexMetadata = await _metaRepository.GetByNameAsync(index);
            if (indexMetadata == null) return APIResponseWrapper<bool>.Failure($"No index with the name '{index}' was found.", APIResponseStatusCodes.NotFound);
            var success = await _searchClient.RunIndexer(index);
            if (success) return APIResponseWrapper<bool>.Success(true);
            return APIResponseWrapper<bool>.Failure($"Failed to mark the SQL index for updating.", APIResponseStatusCodes.InternalError);
        }

        /// <summary>
        /// Retrieves all documents from a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <param name="count">The number of documents to retrieve.</param>
        /// <param name="page">The current page number.</param>
        /// <returns>An <see cref="APIResponseWrapper{IEnumerable{IndexDocument}}"/> containing a list of documents in the RAG index.</returns>
        public async Task<APIResponseWrapper<IEnumerable<IndexDocument>>> GetAllDocuments(string index, int count, int page)
        {
            if (!_validationHandler.IsValidIndexName(index)) return APIResponseWrapper<IEnumerable<IndexDocument>>.Failure($"The supplied index name, '{index}' is invalid. Please avoid reserved SQL words.", APIResponseStatusCodes.BadRequest);
            var dbIndex = await _metaRepository.GetByNameAsync(index);
            if (dbIndex == null) return APIResponseWrapper<IEnumerable<IndexDocument>>.Failure($"The supplied index '{index}' does not exist.", APIResponseStatusCodes.NotFound);
            var docList = new List<IndexDocument>();
            var dbDocumentList = await _ragRepository.GetAllAsync(index, count, page);
            foreach (var dbDocument in dbDocumentList) docList.Add(DbMappingHandler.MapFromDbIndexDocument(dbDocument));
            return APIResponseWrapper<IEnumerable<IndexDocument>>.Success(docList);
        }

        /// <summary>
        /// Retrieves a single document from a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <param name="document">The title/name of the document.</param>
        /// <returns>An <see cref="APIResponseWrapper{IndexDocument}"/> containing the matching document, or null if none exists.</returns>
        public async Task<APIResponseWrapper<IndexDocument>> GetDocument(string index, string document)
        {
            if (!_validationHandler.IsValidIndexName(index)) return APIResponseWrapper<IndexDocument>.Failure($"The supplied index name, '{index}' is invalid. Please avoid reserved SQL words.", APIResponseStatusCodes.BadRequest);
            if (string.IsNullOrEmpty(document)) return APIResponseWrapper<IndexDocument>.Failure($"The required argument 'document' is null or empty", APIResponseStatusCodes.BadRequest);
            var dbDocument = await _ragRepository.GetDocumentAsync(index, document);
            if (dbDocument == null) return APIResponseWrapper<IndexDocument>.Failure($"A document in index '{index}' with the name '{document}' could not be found.", APIResponseStatusCodes.NotFound);
            return APIResponseWrapper<IndexDocument>.Success(DbMappingHandler.MapFromDbIndexDocument(dbDocument));
        }

        /// <summary>
        /// Upserts documents into a RAG index.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <param name="documentUpsertRequest">The request body containing the documents to upsert.</param>
        /// <returns>An <see cref="APIResponseWrapper{bool}"/> indicating success or failure.</returns>
        public async Task<APIResponseWrapper<bool>> UpsertDocuments(string index, RagUpsertRequest documentUpsertRequest)
        {
            if (!_validationHandler.IsValidIndexName(index)) return APIResponseWrapper<bool>.Failure($"The supplied index name, '{index}' is invalid. Please avoid reserved SQL words.", APIResponseStatusCodes.BadRequest);
            var errorMessage = _validationHandler.IsValidRagUpsertRequest(documentUpsertRequest);
            if (!string.IsNullOrEmpty(errorMessage)) return APIResponseWrapper<bool>.Failure(errorMessage, APIResponseStatusCodes.BadRequest);

            var indexData = await _metaRepository.GetByNameAsync(index);
            if (indexData == null) return APIResponseWrapper<bool>.Failure($"An index with the name '{index}' was not found.", APIResponseStatusCodes.NotFound);

            foreach (var document in documentUpsertRequest.Documents)
            {
                if (indexData.GenerateTopic) document.Topic = await GenerateDocumentMetadata("a topic", document, indexData.GenerationHost.ConvertToServiceHost());
                if (indexData.GenerateKeywords) document.Keywords = await GenerateDocumentMetadata("a comma separated list of keywords", document, indexData.GenerationHost.ConvertToServiceHost());

                var newDbDocument = DbMappingHandler.MapToDbIndexDocument(document);

                var existingDoc = await _ragRepository.GetDocumentAsync(index, document.Title);
                if (existingDoc != null)
                {
                    newDbDocument.Modified = DateTimeOffset.UtcNow;
                    var success = await _ragRepository.UpdateAsync(existingDoc.Id, newDbDocument, indexData.Name);
                    if (!success) return APIResponseWrapper<bool>.Failure($"Something went wrong when updating the document '{newDbDocument.Title}'.", APIResponseStatusCodes.InternalError);
                }
                else
                {
                    newDbDocument.Created = DateTimeOffset.UtcNow;
                    newDbDocument.Modified = DateTimeOffset.UtcNow;
                    var newDoc = await _ragRepository.AddAsync(newDbDocument, indexData.Name);
                    if (newDoc == null) return APIResponseWrapper<bool>.Failure($"Something went wrong when updating the document '{newDbDocument.Title}'.", APIResponseStatusCodes.InternalError);
                }
            }
            return APIResponseWrapper<bool>.Success(true);
        }

        /// <summary>
        /// Deletes documents from a RAG index.
        /// </summary>
        /// <param name="index">The name of the RAG index.</param>
        /// <param name="documentList">A list of document titles/names to delete.</param>
        /// <returns>An <see cref="APIResponseWrapper{int}"/> indicating the number of documents that were successfully deleted.</returns>
        public async Task<APIResponseWrapper<int>> DeleteDocuments(string index, string[] documentList)
        {
            var deletedDocuments = 0;
            if (!_validationHandler.IsValidIndexName(index)) return APIResponseWrapper<int>.Failure($"The supplied index name, '{index}' is invalid. Please avoid reserved SQL words.", APIResponseStatusCodes.BadRequest);
            foreach (var documentName in documentList)
            {
                var document = await _ragRepository.GetDocumentAsync(index, documentName);
                if (document == null) continue;

                var success = await _ragRepository.DeleteAsync(document, index);
                if (success) deletedDocuments++;
            }
            return APIResponseWrapper<int>.Success(deletedDocuments);
        }

        #region Private Methods

        /// <summary>
        /// Generates metadata for a document.
        /// </summary>
        /// <param name="dataFormat">The format of the data that the AGI client will request to be generated.</param>
        /// <param name="document">The document to generate the data for.</param>
        /// <param name="host">The host that will be used to generate the data.</param>
        /// <returns>The string generated by the AGI client based on the provided document.</returns>
        private async Task<string> GenerateDocumentMetadata(string dataFormat, IndexDocument document, AGIServiceHosts host)
        {
            if (host == AGIServiceHosts.None) return string.Empty;

            var completion = $"Please create {dataFormat} summarizing the below data delimited by triple " +
                $"backticks. Your response should only contain {dataFormat} and absolutely no other textual " +
                $"data.\n\n";

            // triple backticks to delimit the data
            completion += $"\n```";
            completion += $"\ntitle: {document.Title}";
            completion += $"\ncontent: {document.Content}";
            completion += $"\n```";

            var model = DefaultOpenAIModel;
            if (host == AGIServiceHosts.Azure) model = _defaultAzureModel;
            else if (host == AGIServiceHosts.Anthropic) model = DefaultAnthropicModel;

            var completionRequest = new CompletionRequest()
            {
                ProfileOptions = new Profile() { Model = model, ImageHost = AGIServiceHosts.None },
                Messages = new List<Message>() { new Message() { Role = Role.User, Content = completion } }
            };

            var aiClient = _agiClientFactory.GetClient(host);
            var response = await aiClient.PostCompletion(completionRequest); // create a seperate method for internal API completions
            var content = response?.Messages?.Last(m => m.Role == Role.Assistant).Content ?? string.Empty;
            return content.Length > 255 ? content.Substring(0, 255) : content; // If content exceeds SQL column size, truncate.
        }

        #endregion
    }
}
