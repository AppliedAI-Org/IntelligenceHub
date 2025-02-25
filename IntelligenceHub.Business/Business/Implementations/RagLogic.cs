using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Business.Handlers;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common;
using IntelligenceHub.Common.Config;
using IntelligenceHub.DAL;
using IntelligenceHub.DAL.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using IntelligenceHub.DAL.Models;
using System.Reflection.Metadata;
using System.Xml.Linq;
using static IntelligenceHub.Common.GlobalVariables;
using System.Transactions;
using Microsoft.EntityFrameworkCore;

namespace IntelligenceHub.Business.Implementations
{
    public class RagLogic : IRagLogic
    {
        private readonly IAISearchServiceClient _searchClient;
        private readonly IAGIClient _aiClient;
        private readonly IIndexMetaRepository _metaRepository;
        private readonly IIndexRepository _ragRepository;
        private readonly IValidationHandler _validationHandler;
        private readonly IBackgroundTaskQueueHandler _backgroundTaskQueue;
        private readonly IntelligenceHubDbContext _dbContext;

        public RagLogic(IAGIClient agiClient, IAISearchServiceClient aISearchServiceClient, IIndexMetaRepository metaRepository, IIndexRepository indexRepository, IValidationHandler validationHandler, IBackgroundTaskQueueHandler backgroundTaskQueue, IntelligenceHubDbContext context)
        {
            _searchClient = aISearchServiceClient;
            _aiClient = agiClient;
            _metaRepository = metaRepository;
            _ragRepository = indexRepository;
            _validationHandler = validationHandler;
            _backgroundTaskQueue = backgroundTaskQueue;
            _dbContext = context;
        }

        public async Task<IndexMetadata?> GetRagIndex(string index)
        {
            if (!_validationHandler.IsValidIndexName(index)) return null;
            var dbIndexData = await _metaRepository.GetByNameAsync(index);
            if (dbIndexData == null) return null;
            return DbMappingHandler.MapFromDbIndexMetadata(dbIndexData);
        }

        public async Task<IEnumerable<IndexMetadata>> GetAllIndexesAsync()
        {
            var allIndexes = new List<IndexMetadata>();
            var allDbIndexes = await _metaRepository.GetAllAsync();
            foreach (var dbIndex in allDbIndexes) allIndexes.Add(DbMappingHandler.MapFromDbIndexMetadata(dbIndex));
            return allIndexes;
        }

        public async Task<bool> CreateIndex(IndexMetadata indexDefinition)
        {
            var errorMessage = _validationHandler.ValidateIndexDefinition(indexDefinition);
            if (!string.IsNullOrEmpty(errorMessage)) return false;

            var existing = await _metaRepository.GetByNameAsync(indexDefinition.Name);
            if (existing != null) return false;

            // add index entry for metadata
            var newDbIndex = DbMappingHandler.MapToDbIndexMetadata(indexDefinition);
            var response = await _metaRepository.AddAsync(newDbIndex);
            if (response == null) return false;

            // create a new table for the index
            var success = await _ragRepository.CreateIndexAsync(indexDefinition.Name);
            if (!success) return false;

            success = await _ragRepository.EnableChangeTrackingAsync(indexDefinition.Name);
            if (!success) return false;

            // create the index in Azure AI Search
            success = await _searchClient.UpsertIndex(indexDefinition);
            if (!success) return false;

            // Create a datasource for the SQL DB in Azure AI Search
            success = await _searchClient.CreateDatasource(indexDefinition.Name);
            if (!success) return false;

            // create the indexer to run scheduled ingestions of the datasource
            return await _searchClient.UpsertIndexer(indexDefinition);
        }

        // NOTE: Given the below code, disabling generative columns will not destroy existing data
        public async Task<bool> ConfigureIndex(IndexMetadata indexDefinition)
        {
            if (!string.IsNullOrEmpty(_validationHandler.ValidateIndexDefinition(indexDefinition))) return false;
            var existingDefinition = await _metaRepository.GetByNameAsync(indexDefinition.Name);
            if (existingDefinition == null) return false;

            var success = await _searchClient.UpsertIndex(indexDefinition);
            if (!success) return false;

            success = await _searchClient.UpsertIndexer(indexDefinition);
            if (!success) return false;

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
            

            // Update the SQL entry
            var rows = await _metaRepository.UpdateAsync(existingDefinition, newDefinition);

            // Create missing generative data if required
            if (updateAllDocs)
            {
                if (generateMissingFields) _ = GenerateMissingRagFields(indexDefinition);
                await _ragRepository.MarkIndexForUpdateAsync(newDefinition.Name);
                return await _searchClient.RunIndexer(newDefinition.Name);
            }
            return true;
        }

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

        private async Task RunBackgroundDocumentUpdate(IndexMetadata index, DbIndexDocument document)
        {
            var documentDto = DbMappingHandler.MapFromDbIndexDocument(document);
            if (index.GenerateTopic ?? false && string.IsNullOrEmpty(document.Topic)) document.Topic = await GenerateDocumentMetadata("a topic", documentDto);
            if (index.GenerateKeywords ?? false && string.IsNullOrEmpty(document.Keywords)) document.Keywords = await GenerateDocumentMetadata("a comma separated list of keywords", documentDto);

            // Use EF Core execution strategy
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();

                var existingDoc = await _ragRepository.GetDocumentAsync(index.Name, document.Title);
                if (existingDoc != null)
                {
                    document.Modified = DateTimeOffset.UtcNow;
                    await _ragRepository.UpdateAsync(existingDoc, document, index.Name);
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

        public async Task<bool> RunIndexUpdate(string index)
        {
            if (!_validationHandler.IsValidIndexName(index)) return false;
            var indexMetadata = await _metaRepository.GetByNameAsync(index);
            if (indexMetadata == null) return false;
            return await _searchClient.RunIndexer(index);
        }

        public async Task<bool> DeleteIndex(string index)
        {
            if (!_validationHandler.IsValidIndexName(index)) return false;
            var indexMetadata = await _metaRepository.GetByNameAsync(index);
            if (indexMetadata == null) return false;
            if (await _ragRepository.DeleteIndexAsync(indexMetadata.Name))
            {
                var success = await _searchClient.DeleteIndexer(indexMetadata.Name, indexMetadata.EmbeddingModel ?? GlobalVariables.DefaultEmbeddingModel);
                if (!success) return false;

                success = await _searchClient.DeleteDatasource(indexMetadata.Name);
                if (!success) return false;

                success = await _searchClient.DeleteIndex(index);
                if (!success) return false;

                var rowsAffected = await _metaRepository.DeleteAsync(indexMetadata);
                if (rowsAffected > 0) return true;
            }
            return false;
        }

        public async Task<List<IndexDocument>?> QueryIndex(string index, string query)
        {
            if (!_validationHandler.IsValidIndexName(index) || string.IsNullOrEmpty(query)) return null;
            var indexData = await _metaRepository.GetByNameAsync(index);
            if (indexData is null) return null;

            var docList = new List<IndexDocument>();
            var indexDefinition = DbMappingHandler.MapFromDbIndexMetadata(indexData);
            var response = await _searchClient.SearchIndex(indexDefinition, query);
            var results = response.GetResultsAsync();
            await foreach (var res in results)
            {
                var newDoc = new IndexDocument()
                {
                    Title = res.Document.Title,
                    Keywords = res.Document.Keywords,
                    Topic = res.Document.Topic,
                    Source = res.Document.Source,
                    Created = res.Document.Created,
                    Modified = res.Document.Modified
                };
                if (indexDefinition.QueryType == QueryType.Semantic) foreach (var caption in res.SemanticSearch.Captions) newDoc.Content += $"Excerpt: {caption.Text}\n\n";
                else newDoc.Content = res.Document.chunk;
                docList.Add(newDoc);
            }
            return docList;
        }

        public async Task<IEnumerable<IndexDocument>?> GetAllDocuments(string index, int count, int page)
        {
            if (!_validationHandler.IsValidIndexName(index)) return null;
            var docList = new List<IndexDocument>();
            var dbDocumentList = await _ragRepository.GetAllAsync(index, count, page);
            foreach (var dbDocument in dbDocumentList) docList.Add(DbMappingHandler.MapFromDbIndexDocument(dbDocument));
            return docList;
        }

        public async Task<IndexDocument?> GetDocument(string index, string document)
        {
            if (!_validationHandler.IsValidIndexName(index) || string.IsNullOrEmpty(document)) return null;
            var dbDocument = await _ragRepository.GetDocumentAsync(index, document);
            if (dbDocument == null) return null;
            return DbMappingHandler.MapFromDbIndexDocument(dbDocument);
        }

        public async Task<bool> UpsertDocuments(string index, RagUpsertRequest documentUpsertRequest)
        {
            if (!_validationHandler.IsValidIndexName(index)) return false;
            if (!string.IsNullOrEmpty(_validationHandler.IsValidRagUpsertRequest(documentUpsertRequest))) return false;

            var indexData = await _metaRepository.GetByNameAsync(index);
            if (indexData == null) return false;

            foreach (var document in documentUpsertRequest.Documents)
            {
                if (indexData.GenerateTopic) document.Topic = await GenerateDocumentMetadata("a topic", document);
                if (indexData.GenerateKeywords) document.Keywords = await GenerateDocumentMetadata("a comma seperated list of keywords", document);

                var newDbDocument = DbMappingHandler.MapToDbIndexDocument(document);

                var existingDoc = await _ragRepository.GetDocumentAsync(index, document.Title);
                if (existingDoc != null)
                {
                    newDbDocument.Modified = DateTimeOffset.UtcNow;
                    var rows = await _ragRepository.UpdateAsync(existingDoc, newDbDocument, indexData.Name);
                    if (rows < 1) return false;
                }
                else
                {
                    newDbDocument.Created = DateTimeOffset.UtcNow;
                    newDbDocument.Modified = DateTimeOffset.UtcNow;
                    var newDoc = await _ragRepository.AddAsync(newDbDocument, indexData.Name);
                    if (newDoc == null) return false;
                }
            }
            return true;
        }

        public async Task<int> DeleteDocuments(string index, string[] documentList)
        {
            var deletedDocuments = 0;
            if (!_validationHandler.IsValidIndexName(index)) return -1;
            foreach (var documentName in documentList)
            {
                var document = await _ragRepository.GetDocumentAsync(index, documentName);
                if (document == null) continue;

                deletedDocuments += await _ragRepository.DeleteAsync(document, index);
            }
            return deletedDocuments;
        }

        #region Private Methods

        private async Task<string> GenerateDocumentMetadata(string dataFormat, IndexDocument document)
        {
            var completion = $"Please create {dataFormat} summarizing the below data delimited by triple " +
                $"backticks. Your response should only contain {dataFormat} and absolutely no other textual " +
                $"data.\n\n";

            // triple backticks to delimit the data
            completion += $"\n```";
            completion += $"\ntitle: {document.Title}";
            completion += $"\ncontent: {document.Content}";
            completion += $"\n```";

            var completionRequest = new CompletionRequest()
            {
                ProfileOptions = new Profile() { Name = GlobalVariables.DefaultAGIModel, System_Message = GlobalVariables.RagRequestSystemMessage, Model = DefaultAGIModel },
                Messages = new List<Message>() { new Message() { Role = GlobalVariables.Role.User, Content = completion } }
            };

            var response = await _aiClient.PostCompletion(completionRequest); // create a seperate method for internal API completions
            var content = response?.Messages.Last(m => m.Role == GlobalVariables.Role.Assistant).Content ?? string.Empty;
            return content.Length > 255 ? content.Substring(0, 255) : content; // If content exceeds SQL column size, truncate.
        }

        #endregion
    }
}
