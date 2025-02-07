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

namespace IntelligenceHub.Client.Implementations
{
    public class AISearchServiceClient : IAISearchServiceClient
    {
        private readonly int _defaultFreshnessBoostDuration = 365;
        private readonly int _defaultFreshnessBoost = 2;

        private readonly SearchIndexClient _indexClient;
        private readonly SearchIndexerClient _indexerClient;
        private readonly string _sqlRagDbConnectionString;
        private readonly string _openaiKey;
        private readonly string _openaiUrl;

        public AISearchServiceClient(IOptionsMonitor<SearchServiceClientSettings> searchClientSettings, IOptionsMonitor<AGIClientSettings> agiClientSettings, IOptionsMonitor<Settings> settings)
        {
            var credential = new AzureKeyCredential(searchClientSettings.CurrentValue.Key);

            var options = new SearchClientOptions()
            {
                RetryPolicy = new RetryPolicy(5, DelayStrategy.CreateExponentialDelayStrategy(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(120)))
            };

            _indexClient = new SearchIndexClient(new Uri(searchClientSettings.CurrentValue.Endpoint), credential, options);
            _indexerClient = new SearchIndexerClient(new Uri(searchClientSettings.CurrentValue.Endpoint), credential, options);
            _sqlRagDbConnectionString = settings.CurrentValue.DbConnectionString;
            _openaiUrl = agiClientSettings.CurrentValue.SearchServiceCompletionServiceEndpoint;
            _openaiKey = agiClientSettings.CurrentValue.SearchServiceCompletionServiceKey;
        }

        // index operations
        public async Task<List<string>> GetAllIndexNames()
        {
            var indexNames = new List<string>();
            var responseCollection = _indexClient.GetIndexNamesAsync();

            await foreach (var response in responseCollection) indexNames.Add(response);
            return indexNames;
        }

        public async Task<SearchResults<IndexDefinition>> SearchIndex(IndexMetadata index, string query)
        {
            var searchClient = _indexClient.GetSearchClient(index.Name);

            var queryType = SearchQueryType.Simple;
            if (index.QueryType == SearchQueryType.Full.ToString()) queryType = SearchQueryType.Full;
            else if (index.QueryType == SearchQueryType.Semantic.ToString()) queryType = SearchQueryType.Semantic;

            var vectorProfile = new VectorSearchOptions();
            vectorProfile.Queries.Add(new VectorizableTextQuery(query));

            var options = new SearchOptions()
            {
                ScoringProfile = index.ScoringProfile?.Name,
                Size = index.MaxRagAttachments,
                QueryType = queryType,
                SemanticSearch = new SemanticSearchOptions()
                {
                    ErrorMode = SemanticErrorMode.Partial,
                    SemanticConfigurationName = "semantic"
                },
                VectorSearch = vectorProfile
            };

            options.HighlightFields.Add("content");

            var response = await searchClient.SearchAsync<IndexDefinition>(query, options);
            return response.Value;
        }

        public async Task<bool> CreateIndex(IndexMetadata indexDefinition)
        {
            // build fields based off of the class and its attributes
            var fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(IndexDefinition));

            var searchIndex = new SearchIndex(indexDefinition.Name, searchFields)
            {
                DefaultScoringProfile = indexDefinition.ScoringProfile?.Name,
                SemanticSearch = new SemanticSearch()
                {
                    DefaultConfigurationName = "semantic",
                }
            };

            // build a semantic search profile
            var semanticFields = new SemanticPrioritizedFields();
            semanticFields.TitleField = new SemanticField("title"); // change this and others to global constants
            semanticFields.ContentFields.Add(new SemanticField("content"));
            semanticFields.KeywordsFields.Add(new SemanticField("keywords"));
            searchIndex.SemanticSearch.Configurations.Add(new SemanticConfiguration("semantic", semanticFields));

            // build a vector profile
            searchIndex.VectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration("hnsw"));
            searchIndex.VectorSearch.Profiles.Add(new VectorSearchProfile("vector", "hnsw"));

            if (indexDefinition.ScoringProfile != null)
            {
                var scoringProfile = new ScoringProfile(indexDefinition.ScoringProfile?.Name);

                if (indexDefinition.ScoringProfile != null && indexDefinition.ScoringProfile.Weights.Any()) scoringProfile.TextWeights = new TextWeights(indexDefinition.ScoringProfile.Weights);

                // assign function aggregation
                if (indexDefinition.ScoringProfile?.Aggregation == ScoringFunctionAggregation.Sum.ToString()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Sum;
                else if (indexDefinition.ScoringProfile?.Aggregation == ScoringFunctionAggregation.FirstMatching.ToString()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.FirstMatching;
                else if (indexDefinition.ScoringProfile?.Aggregation == ScoringFunctionAggregation.Average.ToString()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Average;
                else if (indexDefinition.ScoringProfile?.Aggregation == ScoringFunctionAggregation.Minimum.ToString()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Minimum;
                else if (indexDefinition.ScoringProfile?.Aggregation == ScoringFunctionAggregation.Maximum.ToString()) scoringProfile.FunctionAggregation = ScoringFunctionAggregation.Maximum;

                // assign interpolation
                var interpolation = ScoringFunctionInterpolation.Linear;
                if (indexDefinition.ScoringProfile?.Interpolation == ScoringFunctionInterpolation.Constant.ToString()) interpolation = ScoringFunctionInterpolation.Constant;
                else if (indexDefinition.ScoringProfile?.Interpolation == ScoringFunctionInterpolation.Quadratic.ToString()) interpolation = ScoringFunctionInterpolation.Quadratic;
                else if (indexDefinition.ScoringProfile?.Interpolation == ScoringFunctionInterpolation.Logarithmic.ToString()) interpolation = ScoringFunctionInterpolation.Logarithmic;

                // add scoring functions to the profile
                var freshnessBoostDuration = TimeSpan.FromDays(indexDefinition.ScoringProfile?.BoostDurationDays ?? _defaultFreshnessBoostDuration);
                var freshnessFunction = new FreshnessScoringFunction("modified", indexDefinition.ScoringProfile?.FreshnessBoost ?? _defaultFreshnessBoostDuration, new FreshnessScoringParameters(freshnessBoostDuration))
                {
                    Interpolation = interpolation,
                };

                if (indexDefinition.ScoringProfile?.FreshnessBoost > 1) scoringProfile.Functions.Add(freshnessFunction);
                else if (indexDefinition.ScoringProfile?.TagBoost > 1)
                {
                    scoringProfile.Functions.Add(new TagScoringFunction("title", indexDefinition.ScoringProfile.TagBoost, new TagScoringParameters("keywords"))
                    {
                        Interpolation = interpolation
                    });
                    scoringProfile.Functions.Add(new TagScoringFunction("content", indexDefinition.ScoringProfile.TagBoost, new TagScoringParameters("keywords"))
                    {
                        Interpolation = interpolation
                    });
                }

                searchIndex.ScoringProfiles.Add(scoringProfile);
            }

            var response = await _indexClient.CreateIndexAsync(searchIndex);
            return true;
        }

        public async Task<bool> DeleteIndex(string indexName)
        {
            var response = await _indexClient.DeleteIndexAsync(indexName);
            return response.Status > 199 && response.Status < 300;
        }

        public async Task<bool> DeleteIndexer(string indexName, string embeddingModel)
        {
            var skillsetDeletionResponse = await _indexerClient.DeleteSkillsetAsync(embeddingModel + "-embeddingSkillset");
            if (skillsetDeletionResponse == null || skillsetDeletionResponse.IsError) return false;

            var response = await _indexerClient.DeleteIndexerAsync(indexName + "-indexer");
            return response.Status > 199 && response.Status < 300;
        }

        public async Task<bool> CreateDatasource(string databaseName)
        {
            try
            {
                var connectionString = _sqlRagDbConnectionString;

                var container = new SearchIndexerDataContainer(databaseName);
                var connection = new SearchIndexerDataSourceConnection(databaseName, SearchIndexerDataSourceType.AzureSql, connectionString, container)
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

        public async Task<bool> DeleteDatasource(string indexName)
        {
            var response = await _indexerClient.DeleteDataSourceConnectionAsync(indexName);
            return response.Status > 199 && response.Status < 300;
        }

        public async Task<bool> CreateIndexer(IndexMetadata index)
        {
            var connectionString = _sqlRagDbConnectionString;
            var chunkingLengthInChars = 1312; // (avg chars per token = 3.5) * (average recomended chunk size = 375) = 1312.5

            var indexer = new SearchIndexer($"{index.Name}-indexer", index.Name, index.Name)
            {
                IsDisabled = false, // indexer will automatically run after creation. Adjust this later if needed
                Schedule = new IndexingSchedule(index.IndexingInterval),

                // probably won't need one
                //FieldMappings = new FieldMapping[] {}

                // maybe add later
                // Parameters = new IndexingParameters(),
                // OutputFieldMappings = null,
                // ETag = null,
                // SkillsetName = null,
            };

            if (!string.IsNullOrEmpty(index.EmbeddingModel))
            {
                var skillsetName = index.EmbeddingModel + "-embeddingSkillset";
                var skills = new List<SearchIndexerSkill>();


                if (index.GenerateTitleVector) skills.Add(new AzureOpenAIEmbeddingSkill(
                    new List<InputFieldMappingEntry>()
                    {
                        new InputFieldMappingEntry("title")
                        {
                            Source = "/document/title"
                        }
                    },
                    new List<OutputFieldMappingEntry>()
                    {
                        new OutputFieldMappingEntry("titleEmbedding")
                        {
                            TargetName = "titleVector"
                        },
                    })
                {
                    ResourceUri = new Uri(_openaiUrl),
                    ApiKey = _openaiKey,
                    ModelName = index.EmbeddingModel,
                    Dimensions = 3072,

                    // if running into issues check how using the context property works
                });

                if (index.GenerateTopicVector) skills.Add(new AzureOpenAIEmbeddingSkill(
                    new List<InputFieldMappingEntry>()
                    {
                        new InputFieldMappingEntry("topic")
                        {
                            Source = "/document/topic"
                        }
                    },
                    new List<OutputFieldMappingEntry>()
                    {
                        new OutputFieldMappingEntry("topicEmbedding")
                        {
                            TargetName = "topicVector"
                        },
                    })
                {
                    ResourceUri = new Uri(_openaiUrl),
                    ApiKey = _openaiKey,
                    ModelName = index.EmbeddingModel,
                    Dimensions = 3072,
                });

                if (index.GenerateKeywordVector) skills.Add(new AzureOpenAIEmbeddingSkill(
                    new List<InputFieldMappingEntry>()
                    {
                        new InputFieldMappingEntry("keywords")
                        {
                            Source = "/document/keywords"
                        }
                    },
                    new List<OutputFieldMappingEntry>()
                    {
                        new OutputFieldMappingEntry("keywordsEmbedding")
                        {
                            TargetName = "keywordsVector"
                        },
                    })
                {
                    ResourceUri = new Uri(_openaiUrl),
                    ApiKey = _openaiKey,
                    ModelName = index.EmbeddingModel,
                    Dimensions = 3072,
                });

                //chunk content
                if (index.GenerateContentVector)
                {
                    skills.Add(new SplitSkill(
                    new List<InputFieldMappingEntry>
                    {
                        new InputFieldMappingEntry("text") { Source = "/document/content" }
                    },
                    new List<OutputFieldMappingEntry>
                    {
                        new OutputFieldMappingEntry("textItems") { TargetName = "chunkSplits" }
                    })
                    {
                        Context = "/document/content",
                        TextSplitMode = TextSplitMode.Pages,
                        MaximumPageLength = chunkingLengthInChars,
                        PageOverlapLength = (int)(chunkingLengthInChars * index.ChunkOverlap),
                    });

                    skills.Add(new AzureOpenAIEmbeddingSkill(
                        new List<InputFieldMappingEntry>()
                        {
                        new InputFieldMappingEntry("content")
                        {
                            Source = "/document/chunkSplits/*"
                        }
                        },
                        new List<OutputFieldMappingEntry>()
                        {
                        new OutputFieldMappingEntry("contentEmbedding")
                        {
                            TargetName = "contentVector"
                        },
                        })
                    {
                        ResourceUri = new Uri(_openaiUrl),
                        ApiKey = _openaiKey,
                        ModelName = index.EmbeddingModel,
                        Dimensions = 3072,
                    });
                }

                var skillset = new SearchIndexerSkillset(skillsetName, skills);
                await _indexerClient.CreateOrUpdateSkillsetAsync(skillset);
                indexer.SkillsetName = skillsetName;
            }

            var response = await _indexerClient.CreateIndexerAsync(indexer);
            return true;
        }

        #region Private Methods


        #endregion
    }
}
