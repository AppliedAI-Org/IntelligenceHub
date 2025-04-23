using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Client.Implementations
{
    /// <summary>
    /// A Azure AI Search Services client oriented around RAG construction and consumption.
    /// </summary>
    public class AISearchServiceClient : IAISearchServiceClient
    {
        private readonly SearchIndexClient _indexClient;
        private readonly SearchIndexerClient _indexerClient;

        private readonly string _sqlRagDbConnectionString;
        private readonly string _openaiKey;
        private readonly string _openaiUrl;

        private readonly int _defaultRagDimensions = 3072; // move to globalvariables
        private readonly string _defaultVectorSearchProfile = "vector-search-profile";
        private readonly string _defaultVectorAlgConfig = "hnsw";
        private readonly string _defaultKnnSearchProfile = "ExhaustiveKnnProfile";
        private readonly string _defaultKnnConfig = "ExhaustiveKnn";
        private readonly string _defaultVectorizer = "Vectorizer";

        private readonly string _indexerSuffix  = "-indexer";
        private readonly string _skillsetSuffix  = "-skillset";

        // Rag indexes are created with lower case values
        private enum RagField
        {
            title,
            topic,
            keywords,
            created,
            modified,
            source,
            chunk,
            contentVector,
            keywordsVector,
            topicVector,
            titleVector,
            pages,
            chunk_id,
            parent_id,
        }

        private enum RagFieldType
        {
            text,
            textItems,
            embedding
        }

        /// <summary>
        /// The default constructor for the AISearchServiceClient class.
        /// </summary>
        /// <param name="searchClientSettings">The search service client resolved from DI.</param>
        /// <param name="agiClientSettings">The settings for the client resolved from DI.</param>
        /// <param name="settings">The application settings passed in from DI. Only required for the DB connection string.</param>
        public AISearchServiceClient(IOptionsMonitor<SearchServiceClientSettings> searchClientSettings, IOptionsMonitor<AGIClientSettings> agiClientSettings, IOptionsMonitor<Settings> settings)
        {
            var credential = new AzureKeyCredential(searchClientSettings.CurrentValue.Key);

            var options = new SearchClientOptions()
            {
                RetryPolicy = new RetryPolicy(AISearchServiceMaxRetries, DelayStrategy.CreateExponentialDelayStrategy(TimeSpan.FromSeconds(AISearchServiceInitialDelay), TimeSpan.FromSeconds(AISearchServiceMaxDelay)))
            };

            _indexClient = new SearchIndexClient(new Uri(searchClientSettings.CurrentValue.Endpoint), credential, options);
            _indexerClient = new SearchIndexerClient(new Uri(searchClientSettings.CurrentValue.Endpoint), credential, options);

            _sqlRagDbConnectionString = settings.CurrentValue.DbConnectionString;
            _openaiUrl = agiClientSettings.CurrentValue.SearchServiceCompletionServiceEndpoint;
            _openaiKey = agiClientSettings.CurrentValue.SearchServiceCompletionServiceKey;
        }

        // index operations

        /// <summary>
        /// Retrieves the metadata for a RAG index.
        /// </summary>
        /// <returns>A list of RAG index names.</returns>
        public async Task<List<string>> GetAllIndexNames()
        {
            var indexNames = new List<string>();
            var responseCollection = _indexClient.GetIndexNamesAsync();

            await foreach (var response in responseCollection) indexNames.Add(response);
            return indexNames;
        }

        /// <summary>
        /// Retrieves the metadata for a RAG index.
        /// </summary>
        /// <param name="index">The definition of the RAG index.</param>
        /// <param name="query">The query to search against the RAG index.</param>
        /// <returns>Returns the search results retrieved from the RAG index.</returns>
        public async Task<SearchResults<IndexDefinition>> SearchIndex(IndexMetadata index, string query)
        {
            var searchClient = _indexClient.GetSearchClient(index.Name);

            var queryType = SearchQueryType.Simple;
            if (index.QueryType.ToString().Equals(SearchQueryType.Full.ToString(), StringComparison.OrdinalIgnoreCase)) queryType = SearchQueryType.Full;
            else if (index.QueryType.ToString().Equals(SearchQueryType.Semantic.ToString(), StringComparison.OrdinalIgnoreCase)) queryType = SearchQueryType.Semantic;

            var vectorProfile = new VectorSearchOptions();
            var vectorizableQuery = new VectorizableTextQuery(query);
            vectorizableQuery.Fields.Add(RagField.contentVector.ToString());
            vectorizableQuery.Fields.Add(RagField.titleVector.ToString());
            vectorizableQuery.Fields.Add(RagField.topicVector.ToString());
            vectorizableQuery.Fields.Add(RagField.keywordsVector.ToString());
            vectorProfile.Queries.Add(vectorizableQuery);

            var options = new SearchOptions()
            {
                ScoringProfile = index.ScoringProfile?.Name,
                Size = index.MaxRagAttachments,
                QueryType = queryType
            };

            if (index.QueryType.ToString().Equals(SearchQueryType.Semantic.ToString(), StringComparison.OrdinalIgnoreCase)
                || index.QueryType.ToString().Equals("VectorSemanticHybrid", StringComparison.OrdinalIgnoreCase))
            {
                options.SemanticSearch = new SemanticSearchOptions()
                {
                    ErrorMode = SemanticErrorMode.Partial,
                    SemanticConfigurationName = SearchQueryType.Semantic.ToString()
                };
            }

            if (index.QueryType.ToString().Equals("Vector", StringComparison.OrdinalIgnoreCase)
                || index.QueryType.ToString().Equals("VectorSimpleHybrid", StringComparison.OrdinalIgnoreCase)
                || index.QueryType.ToString().Equals("VectorSemanticHybrid", StringComparison.OrdinalIgnoreCase))
            {
                options.VectorSearch = vectorProfile;
            }

            options.HighlightFields.Add(RagField.chunk.ToString().ToLower());

            options.SearchFields.Add(RagField.chunk.ToString().ToLower());
            options.SearchFields.Add(RagField.title.ToString().ToLower());
            options.SearchFields.Add(RagField.topic.ToString().ToLower());
            options.SearchFields.Add(RagField.keywords.ToString().ToLower());

            var response = await searchClient.SearchAsync<IndexDefinition>(query, options);
            return response.Value;
        }

        /// <summary>
        /// Creates or updates a RAG index.
        /// </summary>
        /// <param name="indexDefinition">The definition of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public async Task<bool> UpsertIndex(IndexMetadata indexDefinition)
        {
            var searchIndex = GetIndexDefinition(indexDefinition);

            if (indexDefinition.ScoringProfile != null)
            {
                if (string.IsNullOrEmpty(indexDefinition.ScoringProfile.Name)) indexDefinition.ScoringProfile.Name = _defaultVectorSearchProfile;
                var scoringProfile = new ScoringProfile(indexDefinition.ScoringProfile?.Name);
                if (indexDefinition.ScoringProfile?.Weights != null && indexDefinition.ScoringProfile.Weights.Any()) scoringProfile.TextWeights = new TextWeights(indexDefinition.ScoringProfile.Weights);

                // assign function aggregation
                if (indexDefinition.ScoringProfile?.SearchAggregation.ToString()?.ToLower() == ScoringFunctionAggregation.Sum.ToString().ToLower()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Sum;
                else if (indexDefinition.ScoringProfile?.SearchAggregation.ToString()?.ToLower() == ScoringFunctionAggregation.FirstMatching.ToString().ToLower()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.FirstMatching;
                else if (indexDefinition.ScoringProfile?.SearchAggregation.ToString()?.ToLower() == ScoringFunctionAggregation.Average.ToString().ToLower()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Average;
                else if (indexDefinition.ScoringProfile?.SearchAggregation.ToString()?.ToLower() == ScoringFunctionAggregation.Minimum.ToString().ToLower()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Minimum;
                else if (indexDefinition.ScoringProfile?.SearchAggregation.ToString()?.ToLower() == ScoringFunctionAggregation.Maximum.ToString().ToLower()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Maximum;

                // assign interpolation
                var interpolation = ScoringFunctionInterpolation.Linear;
                if (indexDefinition.ScoringProfile?.SearchInterpolation.ToString()?.ToLower() == ScoringFunctionInterpolation.Constant.ToString().ToLower()) interpolation = ScoringFunctionInterpolation.Constant;
                else if (indexDefinition.ScoringProfile?.SearchInterpolation.ToString()?.ToLower() == ScoringFunctionInterpolation.Quadratic.ToString().ToLower()) interpolation = ScoringFunctionInterpolation.Quadratic;
                else if (indexDefinition.ScoringProfile?.SearchInterpolation.ToString()?.ToLower() == ScoringFunctionInterpolation.Logarithmic.ToString().ToLower()) interpolation = ScoringFunctionInterpolation.Logarithmic;

                // add scoring functions to the profile
                if (indexDefinition.ScoringProfile?.FreshnessBoost > 1) scoringProfile.Functions.Add(new FreshnessScoringFunction(RagField.modified.ToString(), indexDefinition.ScoringProfile.FreshnessBoost, new FreshnessScoringParameters(TimeSpan.FromDays(indexDefinition.ScoringProfile.BoostDurationDays)))
                {
                    Interpolation = interpolation,
                });
                else if (indexDefinition.ScoringProfile?.TagBoost > 1)
                {
                    scoringProfile.Functions.Add(new TagScoringFunction(RagField.title.ToString(), indexDefinition.ScoringProfile.TagBoost, new TagScoringParameters(RagField.keywords.ToString()))
                    {
                        Interpolation = interpolation
                    });
                    scoringProfile.Functions.Add(new TagScoringFunction(RagField.chunk.ToString(), indexDefinition.ScoringProfile.TagBoost, new TagScoringParameters(RagField.keywords.ToString()))
                    {
                        Interpolation = interpolation
                    });
                }
                searchIndex.ScoringProfiles.Add(scoringProfile);
            }

            var response = await _indexClient.CreateOrUpdateIndexAsync(searchIndex);
            return true;
        }

        /// <summary>
        /// Deletes a RAG index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public async Task<bool> DeleteIndex(string indexName)
        {
            var response = await _indexClient.DeleteIndexAsync(indexName);
            return response.Status > 199 && response.Status < 300;
        }

        /// <summary>
        /// Deletes a RAG indexer and the associated skillset.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public async Task<bool> DeleteIndexer(string indexName)
        {
            var skillsetDeletionResponse = await _indexerClient.DeleteSkillsetAsync(indexName.ToLower() + _skillsetSuffix );
            if (skillsetDeletionResponse == null || skillsetDeletionResponse.IsError) return false;

            var response = await _indexerClient.DeleteIndexerAsync(indexName + _indexerSuffix );
            return response.Status > 199 && response.Status < 300;
        }

        /// <summary>
        /// Creates a datasource connection to the SQL database table created for this index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public async Task<bool> CreateDatasource(string indexName)
        {
            try
            {
                var container = new SearchIndexerDataContainer(indexName);
                var connection = new SearchIndexerDataSourceConnection(indexName, SearchIndexerDataSourceType.AzureSql, _sqlRagDbConnectionString, container)
                {
                    DataChangeDetectionPolicy = new SqlIntegratedChangeTrackingPolicy(),
                };
                var response = await _indexerClient.CreateDataSourceConnectionAsync(connection);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes a datasource connection to the SQL database table created for this index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public async Task<bool> DeleteDatasource(string indexName)
        {
            var response = await _indexerClient.DeleteDataSourceConnectionAsync(indexName);
            return response.Status > 199 && response.Status < 300;
        }

        /// <summary>
        /// Updates the data in a RAG index, syncing with the corresponding SQL table.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public async Task<bool> RunIndexer(string indexName)
        {
            var response = await _indexerClient.RunIndexerAsync(indexName + _indexerSuffix );
            return response.Status > 199 && response.Status < 300;
        }

        /// <summary>
        /// Creates or updates a RAG indexer.
        /// </summary>
        /// <param name="index">The new definition of the index.</param>
        /// <returns>A boolean indicating success or failure of the operation.</returns>
        public async Task<bool> UpsertIndexer(IndexMetadata index)
        {
            // Define the indexer
            var interval = TimeSpan.FromDays(1);
            if (index.IndexingInterval.HasValue) interval = index.IndexingInterval.Value;
            var indexer = new SearchIndexer($"{index.Name}{_indexerSuffix }", index.Name, index.Name) { Schedule = new IndexingSchedule(interval) };

            // Map the existing "Id" column to the parent_id and chunk_id fields
            indexer.FieldMappings.Add(new FieldMapping("Id") { TargetFieldName = RagField.parent_id.ToString() });
            indexer.FieldMappings.Add(new FieldMapping("Id") { TargetFieldName = RagField.chunk_id.ToString() });

            indexer.SkillsetName = await UpsertSkillset(index);
            await _indexerClient.CreateOrUpdateIndexerAsync(indexer);

            return true;
        }

        /// <summary>
        /// Creates or updates a RAG skillset.
        /// </summary>
        /// <param name="index">The definition of the index.</param>
        /// <returns>The name of the skillset associated with the new index.</returns>
        private async Task<string> UpsertSkillset(IndexMetadata index)
        {
            var chunkingLengthInChars = 1312; // (avg chars per token = 3.5) * (average recommended chunk size = 375) = 1312.5
            var ragDimensions = 3072;
            if (string.IsNullOrEmpty(index.EmbeddingModel)) index.EmbeddingModel = DefaultEmbeddingModel;
            if (index.EmbeddingModel?.ToLower() != DefaultEmbeddingModel.ToLower()) ragDimensions = 1536; // only text-embedding-3-large supports 3072 dimensions

            var skillsetName = index.Name.ToLower() + _indexerSuffix ;
            var skills = new List<SearchIndexerSkill>();
            var projectionInputMappings = new List<InputFieldMappingEntry>();

            // Add default skills and associated projections
            projectionInputMappings.Add(new InputFieldMappingEntry(RagField.chunk.ToString()) { Source = "/document/pages/*" });
            projectionInputMappings.Add(new InputFieldMappingEntry(RagField.title.ToString()) {  Source = "/document/title" });
            projectionInputMappings.Add(new InputFieldMappingEntry(RagField.topic.ToString()) { Source = "/document/topic" });
            projectionInputMappings.Add(new InputFieldMappingEntry(RagField.keywords.ToString()) { Source = "/document/keywords" });
            projectionInputMappings.Add(new InputFieldMappingEntry(RagField.source.ToString()) { Source = "/document/source" });
            projectionInputMappings.Add(new InputFieldMappingEntry(RagField.created.ToString()) { Source = "/document/created" });
            projectionInputMappings.Add(new InputFieldMappingEntry(RagField.modified.ToString()) { Source = "/document/modified" });

            skills.Add(
                new SplitSkill(
                    new List<InputFieldMappingEntry> { new InputFieldMappingEntry(RagFieldType.text.ToString()) { Source = "/document/Content" } },
                    new List<OutputFieldMappingEntry> { new OutputFieldMappingEntry(RagFieldType.textItems.ToString()) { TargetName = RagField.pages.ToString() } }
                )
                {
                    Context = "/document",
                    TextSplitMode = TextSplitMode.Pages,
                    MaximumPageLength = chunkingLengthInChars,
                    PageOverlapLength = (int)(chunkingLengthInChars * index.ChunkOverlap ?? .1),
                }
            );

            // Add conditional vector fields
            if (index.GenerateContentVector ?? false)
            {
                skills.Add(
                    new AzureOpenAIEmbeddingSkill(
                        new List<InputFieldMappingEntry> { new InputFieldMappingEntry(RagFieldType.text.ToString()) { Source = "/document/pages/*" } },
                        new List<OutputFieldMappingEntry> { new OutputFieldMappingEntry(RagFieldType.embedding.ToString()) { TargetName = RagField.contentVector.ToString() } }
                    )
                    {
                        Context = "/document/pages/*",
                        ResourceUri = new Uri(_openaiUrl),
                        ApiKey = _openaiKey,
                        ModelName = index.EmbeddingModel,
                        DeploymentName = index.EmbeddingModel
                    }
                );
                projectionInputMappings.Add(new InputFieldMappingEntry(RagField.contentVector.ToString()) { Source = "/document/pages/*/contentVector" });
            }

            if (index.GenerateTitleVector ?? false)
            {
                skills.Add(
                    new AzureOpenAIEmbeddingSkill(
                        new List<InputFieldMappingEntry> { new InputFieldMappingEntry(RagFieldType.text.ToString()) { Source = "/document/title" } },
                        new List<OutputFieldMappingEntry> { new OutputFieldMappingEntry(RagFieldType.embedding.ToString()) { TargetName = RagField.titleVector.ToString() } }
                    )
                    {
                        Context = "/document/pages/*",
                        ResourceUri = new Uri(_openaiUrl),
                        ApiKey = _openaiKey,
                        ModelName = index.EmbeddingModel,
                        DeploymentName = index.EmbeddingModel
                    }
                );
                projectionInputMappings.Add(new InputFieldMappingEntry(RagField.titleVector.ToString()) { Source = "/document/pages/*/titleVector" });
            }

            if (index.GenerateTopicVector ?? false)
            {
                skills.Add(
                    new AzureOpenAIEmbeddingSkill(
                        new List<InputFieldMappingEntry> { new InputFieldMappingEntry(RagFieldType.text.ToString()) { Source = "/document/topic" } },
                        new List<OutputFieldMappingEntry> { new OutputFieldMappingEntry(RagFieldType.embedding.ToString()) { TargetName = RagField.topicVector.ToString() } }
                    )
                    {
                        Context = "/document/pages/*",
                        ResourceUri = new Uri(_openaiUrl),
                        ApiKey = _openaiKey,
                        ModelName = index.EmbeddingModel,
                        DeploymentName = index.EmbeddingModel
                    }
                );
                projectionInputMappings.Add(new InputFieldMappingEntry(RagField.topicVector.ToString()) { Source = "/document/pages/*/topicVector" });
            }

            if (index.GenerateKeywordVector ?? false)
            {
                skills.Add(
                    new AzureOpenAIEmbeddingSkill(
                        new List<InputFieldMappingEntry> { new InputFieldMappingEntry(RagFieldType.text.ToString()) { Source = "/document/keywords" } },
                        new List<OutputFieldMappingEntry> { new OutputFieldMappingEntry(RagFieldType.embedding.ToString()) { TargetName = RagField.keywordsVector.ToString() } }
                    )
                    {
                        Context = "/document/pages/*",
                        ResourceUri = new Uri(_openaiUrl),
                        ApiKey = _openaiKey,
                        ModelName = index.EmbeddingModel,
                        DeploymentName = index.EmbeddingModel
                    }
                );
                projectionInputMappings.Add(new InputFieldMappingEntry(RagField.keywordsVector.ToString()) { Source = "/document/pages/*/keywordsVector" });
            }

            var skillset = new SearchIndexerSkillset(skillsetName, skills)
            {
                IndexProjection = new SearchIndexerIndexProjection(new[]
                {
                    new SearchIndexerIndexProjectionSelector(index.Name, parentKeyFieldName: RagField.parent_id.ToString(), sourceContext: "/document/pages/*", mappings: projectionInputMappings)
                })
                {
                    Parameters = new SearchIndexerIndexProjectionsParameters { ProjectionMode = IndexProjectionMode.SkipIndexingParentDocuments }
                }
            };
            var result = await _indexerClient.CreateOrUpdateSkillsetAsync(skillset);
            return skillsetName;
        }

        /// <summary>
        /// Creates a new SearchIndex to define the RAG index in Azure AI Search Services.
        /// </summary>
        /// <param name="index">The definition of the index.</param>
        /// <returns>The SearchIndex object.</returns>
        private SearchIndex GetIndexDefinition(IndexMetadata index)
        {
            // Choose the appropriate rag dimensions for the given embedding model
            var ragDimensions = _defaultRagDimensions;
            if (string.IsNullOrEmpty(index.EmbeddingModel)) index.EmbeddingModel = DefaultEmbeddingModel;
            if (index.EmbeddingModel?.ToLower() != DefaultEmbeddingModel.ToLower()) ragDimensions = 1536; // only text-embedding-3-large supports 3072 dimensions

            var searchIndex = new SearchIndex(index.Name)
            {
                DefaultScoringProfile = index.ScoringProfile?.Name,
                VectorSearch = new VectorSearch()
                {
                    Profiles =
                    {
                        new VectorSearchProfile(_defaultVectorSearchProfile, _defaultVectorAlgConfig) { VectorizerName = _defaultVectorizer, },
                        new VectorSearchProfile(_defaultKnnSearchProfile, _defaultKnnConfig)
                    },
                    Algorithms =
                    {
                        new HnswAlgorithmConfiguration(_defaultVectorAlgConfig),
                        new ExhaustiveKnnAlgorithmConfiguration(_defaultKnnConfig)
                    },
                    Vectorizers =
                    {
                        new AzureOpenAIVectorizer(_defaultVectorizer)
                        {
                            Parameters = new AzureOpenAIVectorizerParameters()
                            {
                                ResourceUri = new Uri(_openaiUrl),
                                ApiKey = _openaiKey,
                                ModelName = index.EmbeddingModel,
                                DeploymentName = index.EmbeddingModel
                            }
                        }
                    }
                },
                SemanticSearch = new()
                {
                    DefaultConfigurationName = SearchQueryType.Semantic.ToString(),
                    Configurations =
                    {
                        new SemanticConfiguration(SearchQueryType.Semantic.ToString(), new()
                        {
                            TitleField = new SemanticField(fieldName: RagField.title.ToString()),
                            ContentFields =
                            {
                                new SemanticField(fieldName: RagField.chunk.ToString()),
                                new SemanticField(fieldName: RagField.keywords.ToString()),
                                new SemanticField(fieldName: RagField.topic.ToString())
                            },
                        })
                    },
                },
                Fields =
                {
                    new SearchableField(RagField.parent_id.ToString()) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                    new SearchableField(RagField.chunk_id.ToString()) { IsKey = true, IsFilterable = true, IsSortable = true, IsFacetable = true, AnalyzerName = LexicalAnalyzerName.Keyword },
                    new SearchableField(RagField.title.ToString()),
                    new SearchableField(RagField.chunk.ToString()),
                    new SearchableField(RagField.topic.ToString()),
                    new SearchableField(RagField.keywords.ToString()),
                    new SimpleField(RagField.source.ToString(), SearchFieldDataType.String),
                    new SimpleField(RagField.created.ToString(), SearchFieldDataType.DateTimeOffset),
                    new SimpleField(RagField.modified.ToString(), SearchFieldDataType.DateTimeOffset),
                    new SearchField(RagField.contentVector.ToString(), SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = ragDimensions,
                        VectorSearchProfileName = _defaultVectorSearchProfile
                    },
                    new SearchField(RagField.titleVector.ToString(), SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = ragDimensions,
                        VectorSearchProfileName = _defaultVectorSearchProfile
                    },
                    new SearchField(RagField.topicVector.ToString(), SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = ragDimensions,
                        VectorSearchProfileName = _defaultVectorSearchProfile
                    },
                    new SearchField(RagField.keywordsVector.ToString(), SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = ragDimensions,
                        VectorSearchProfileName = _defaultVectorSearchProfile
                    },
                },
            };
            return searchIndex;
        }
    }
}
