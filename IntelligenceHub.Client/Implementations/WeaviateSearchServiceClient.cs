using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.Search.Documents.Models;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Common.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using static IntelligenceHub.Common.GlobalVariables;
using System.Globalization;
using Azure.Core;

namespace IntelligenceHub.Client.Implementations
{
    /// <summary>
    /// Simple Weaviate client implementing the IAISearchServiceClient interface.
    /// </summary>
    public class WeaviateSearchServiceClient : IAISearchServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _apiKey;

        /// <summary>
        /// Default vectorizer module name used when creating Weaviate schemas.
        /// </summary>
        private const string _defaultWeaviateVectorizerModule = "text2vec-weaviate";

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaviateSearchServiceClient"/> class.
        /// </summary>
        /// <param name="factory">Factory used to create <see cref="HttpClient"/> instances.</param>
        /// <param name="settings">Monitored settings for the Weaviate client.</param>
        public WeaviateSearchServiceClient(IHttpClientFactory factory, IOptionsMonitor<WeaviateSearchServiceClientSettings> settings)
        {
            _httpClient = factory.CreateClient();
            _endpoint = settings.CurrentValue.Endpoint.TrimEnd('/');
            _apiKey = settings.CurrentValue.Key;
        }

        /// <summary>
        /// Creates an HTTP request for the Weaviate API.
        /// </summary>
        /// <param name="method">HTTP method to use.</param>
        /// <param name="path">Relative path of the endpoint.</param>
        /// <param name="body">Optional request body object which will be serialized as JSON.</param>
        /// <returns>A configured <see cref="HttpRequestMessage"/>.</returns>
        private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body = null)
        {
            var request = new HttpRequestMessage(method, $"{_endpoint}{path}");
            if (!string.IsNullOrEmpty(_apiKey)) request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            request.Headers.Add("X-Weaviate-Cluster-Url", _endpoint);

            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return request;
        }

        /// <summary>
        /// Executes a search query against the specified index.
        /// </summary>
        /// <param name="index">Index metadata describing the target search index.</param>
        /// <param name="query">Text query to search for.</param>
        /// <returns>Search results containing <see cref="IndexDefinition"/> objects.</returns>
        public async Task<SearchResults<IndexDefinition>> SearchIndex(IndexMetadata index, string query)
        {
            string searchClause;
            if (index.QueryType == QueryType.Vector)
            {
                searchClause = $"nearText: {{ concepts: [\"{query}\"] }}";
            }
            else if (index.QueryType == QueryType.VectorSimpleHybrid)
            {
                // Hybrid search combines BM25 and vector similarity. Using a default alpha of 0.5
                searchClause = $"hybrid: {{ query: \"{query}\", alpha: 0.5 }}";
            }
            else
            {
                // Default to BM25 text search for simple/full/semantic queries
                searchClause = $"bm25: {{ query: \"{query}\" }}";
            }

            var className = char.ToUpper(index.Name[0]) + index.Name.Substring(1); // Weaviate is case sensative
            var gql = new
            {
                query = $"{{ Get {{ {className}({searchClause}) {{ title chunk topic keywords source created modified }} }} }}"
            };
            var req = CreateRequest(HttpMethod.Post, "/v1/graphql", gql);
            var res = await _httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();
            var root = JObject.Parse(content);
            var getObj = root["data"]?["Get"];

            JToken? resultsToken = getObj?[className] ?? getObj?.Children<JProperty>().FirstOrDefault(p => p.Name.Equals(index.Name, StringComparison.OrdinalIgnoreCase))?.Value;


            var results = new List<SearchResult<IndexDefinition>>();
            if (resultsToken is JArray rows)
            {
                foreach (var row in rows)
                {
                    var doc = new IndexDefinition
                    {
                        title = row.Value<string>("title"),
                        chunk = row.Value<string>("chunk"),
                        topic = row.Value<string>("topic"),
                        keywords = row.Value<string>("keywords"),
                        source = row.Value<string>("source"),
                        created = ParseIsoUtcDate(row, "created"),
                        modified = ParseIsoUtcDate(row, "modified")
                    };
                    var searchResult = SearchModelFactory.SearchResult(doc, score: null, highlights: null);
                    results.Add(searchResult);
                }
            }
            return SearchModelFactory.SearchResults(values: results, totalCount: results.Count, facets: null, coverage: null, rawResponse: null);
        }

        /// <summary>
        /// Safely parses ISO-8601 timestamps (with or without 'Z') to UTC DateTime.
        /// Returns DateTime.MinValue if the field is missing or malformed.
        /// </summary>
        private static DateTime ParseIsoUtcDate(JToken token, string field)
        {
            string? iso = token.Value<string>(field);
            if (string.IsNullOrWhiteSpace(iso)) return DateTime.MinValue;

            // First try DateTimeOffset (handles timezone and "Z")
            if (DateTimeOffset.TryParse(
                    iso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var dto))
            {
                return dto.UtcDateTime;
            }

            // Fallback: plain DateTime
            return DateTime.TryParse(
                       iso,
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                       out var dt)
                   ? dt
                   : DateTime.MinValue;
        }

        /// <summary>
        /// Creates or updates an index schema in Weaviate based on the provided metadata.
        /// </summary>
        /// <param name="indexDefinition">Definition of the index to upsert.</param>
        /// <returns><c>true</c> if the operation succeeded; otherwise, <c>false</c>.</returns>
        public async Task<bool> UpsertIndex(IndexMetadata indexDefinition)
        {
            var schema = new
            {
                @class = indexDefinition.Name,
                vectorizer = _defaultWeaviateVectorizerModule,
                moduleConfig = new Dictionary<string, object>
                {
                    [_defaultWeaviateVectorizerModule] = new
                    {
                        model = DefaultWeaviateEmbeddingModel
                    }
                },
                properties = new List<SchemaProperty>
                {
                    new SchemaProperty
                    {
                        name = "title",
                        dataType = new[]{"text"},
                        moduleConfig = new Dictionary<string, object>
                        {
                            [_defaultWeaviateVectorizerModule] = new { skip = !(indexDefinition.GenerateTitleVector ?? false) }
                        }
                    },
                    new SchemaProperty
                    {
                        name = "chunk",
                        dataType = new[]{"text"},
                        moduleConfig = new Dictionary<string, object>
                        {
                            [_defaultWeaviateVectorizerModule] = new { skip = !(indexDefinition.GenerateContentVector ?? false) }
                        }
                    },
                    new SchemaProperty
                    {
                        name = "topic",
                        dataType = new[]{"text"},
                        moduleConfig = new Dictionary<string, object>
                        {
                            [_defaultWeaviateVectorizerModule] = new { skip = !(indexDefinition.GenerateTopicVector ?? false) }
                        }
                    },
                    new SchemaProperty
                    {
                        name = "keywords",
                        dataType = new[]{"text"},
                        moduleConfig = new Dictionary<string, object>
                        {
                            [_defaultWeaviateVectorizerModule] = new { skip = !(indexDefinition.GenerateKeywordVector ?? false) }
                        }
                    },
                    new SchemaProperty { name = "source",   dataType = new[]{"text"} },
                    new SchemaProperty { name = "created",  dataType = new[]{"date"} },
                    new SchemaProperty { name = "modified", dataType = new[]{"date"} }
                }
            };
            var req = CreateRequest(HttpMethod.Post, "/v1/schema", schema);
            var res = await _httpClient.SendAsync(req);

            var content = await res.Content.ReadAsStringAsync();

            return res.IsSuccessStatusCode;
        }

        /// <summary>
        /// Deletes an index schema from Weaviate.
        /// </summary>
        /// <param name="indexName">Name of the index to remove.</param>
        /// <returns><c>true</c> if the deletion succeeded; otherwise, <c>false</c>.</returns>
        public async Task<bool> DeleteIndex(string indexName)
        {
            var req = CreateRequest(HttpMethod.Delete, $"/v1/schema/{indexName}");
            var res = await _httpClient.SendAsync(req);
            return res.IsSuccessStatusCode;
        }

        /// <summary>
        /// Stub implementation to satisfy the interface. Weaviate does not use indexers.
        /// </summary>
        public Task<bool> UpsertIndexer(IndexMetadata index) => Task.FromResult(true);
        /// <summary>
        /// Stub implementation to satisfy the interface. Weaviate does not use indexers.
        /// </summary>
        public Task<bool> RunIndexer(string indexName) => Task.FromResult(true);
        /// <summary>
        /// Stub implementation to satisfy the interface. Weaviate does not use indexers.
        /// </summary>
        public Task<bool> DeleteIndexer(string indexName) => Task.FromResult(true);
        /// <summary>
        /// Stub implementation to satisfy the interface. Weaviate manages datasources automatically.
        /// </summary>
        public Task<bool> CreateDatasource(string databaseName) => Task.FromResult(true);
        /// <summary>
        /// Stub implementation to satisfy the interface. Weaviate manages datasources automatically.
        /// </summary>
        public Task<bool> DeleteDatasource(string indexName) => Task.FromResult(true);

        /// <summary>
        /// Converts an integer ID to a deterministic UUID string.
        /// </summary>
        /// <param name="id">Integer identifier.</param>
        /// <returns>UUID string derived from the integer.</returns>
        private static string IntToUuid(int id)
        {
            return $"00000000-0000-0000-0000-{id:D12}";
        }

        /// <summary>
        /// Extracts the integer portion of a deterministic UUID created with <see cref="IntToUuid"/>.
        /// </summary>
        /// <param name="uuid">UUID string to parse.</param>
        /// <returns>The integer ID encoded in the UUID; 0 if parsing fails.</returns>
        private static int UuidToInt(string uuid)
        {
            var parts = uuid.Split('-');
            if (parts.Length != 5) return 0;
            return int.TryParse(parts[4], out var id) ? id : 0;
        }

        /// <summary>
        /// Retrieves all documents stored in the specified index.
        /// </summary>
        /// <param name="indexName">Name of the index.</param>
        /// <returns>List of <see cref="IndexDocument"/> instances.</returns>
        public async Task<List<IndexDocument>> GetAllDocuments(string indexName)
        {
            var upperCaseName = char.ToUpper(indexName[0]) + indexName.Substring(1);
            var gql = new
            {
                query = $"{{ Get {{ {upperCaseName} {{ _additional {{ id }} title chunk topic keywords source created modified }} }} }}"
            };
            var req = CreateRequest(HttpMethod.Post, "/v1/graphql", gql);
            var res = await _httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();
            var j = JObject.Parse(content);
            var resultsToken = j["data"]?["Get"]?[upperCaseName];
            var results = new List<IndexDocument>();
            if (resultsToken != null)
            {
                foreach (var item in resultsToken)
                {
                    var idStr = item["_additional"]?["id"]?.Value<string>() ?? string.Empty;
                    var id = UuidToInt(idStr);
                    results.Add(new IndexDocument
                    {
                        Id = id,
                        Title = item.Value<string>("title") ?? string.Empty,
                        Content = item.Value<string>("chunk") ?? string.Empty,
                        Topic = item.Value<string>("topic"),
                        Keywords = item.Value<string>("keywords"),
                        Source = item.Value<string>("source") ?? string.Empty,
                        Created = ParseIsoUtcDate(item, "created"),
                        Modified = ParseIsoUtcDate(item, "modified")
                    });
                }
            }
            return results;
        }

        /// <summary>
        /// Creates or updates a document in the specified index.
        /// </summary>
        /// <param name="indexName">Name of the index.</param>
        /// <param name="document">Document to upsert.</param>
        /// <returns><c>true</c> if the operation succeeded; otherwise, <c>false</c>.</returns>
        public async Task<bool> UpsertDocument(string indexName, IndexDocument document)
        {
            var uuid = IntToUuid(document.Id);
            var body = new
            {
                id = uuid,
                @class = indexName,
                properties = new
                {
                    title = document.Title,
                    chunk = document.Content,
                    topic = document.Topic,
                    keywords = document.Keywords,
                    source = document.Source,
                    created = document.Created,
                    modified = document.Modified
                }
            };

            var upperCaseName = char.ToUpper(indexName[0]) + indexName.Substring(1);
            var req = CreateRequest(HttpMethod.Put, $"/v1/objects/{upperCaseName}/{uuid}", body);
            var res = await _httpClient.SendAsync(req);
            if (res.IsSuccessStatusCode) return true;

            req = CreateRequest(HttpMethod.Post, "/v1/objects", body);
            res = await _httpClient.SendAsync(req);

            var content = await res.Content.ReadAsStringAsync();

            return res.IsSuccessStatusCode;
        }

        /// <summary>
        /// Deletes a document from the specified index.
        /// </summary>
        /// <param name="indexName">Name of the index containing the document.</param>
        /// <param name="id">Identifier of the document to remove.</param>
        /// <returns><c>true</c> if the deletion succeeded; otherwise, <c>false</c>.</returns>
        public async Task<bool> DeleteDocument(string indexName, int id)
        {
            var uuid = IntToUuid(id);
            var req = CreateRequest(HttpMethod.Delete, $"/v1/objects/{uuid}");
            var res = await _httpClient.SendAsync(req);
            return res.IsSuccessStatusCode;
        }

        // Move/Refactor
        /// <summary>
        /// Internal helper class representing schema properties when creating indexes.
        /// </summary>
        private class SchemaProperty
        {
            public string name { get; set; }
            public string[] dataType { get; set; }
            public Dictionary<string, object>? moduleConfig { get; set; }  // nullable to allow absence
        }
    }
}
