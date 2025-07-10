namespace IntelligenceHub.Common
{
    /// <summary>
    /// Contains global variables and enumerations used throughout the IntelligenceHub application.
    /// </summary>
    public static class GlobalVariables
    {
        /// <summary>
        /// Specifies the format of the response.
        /// </summary>
        public enum ResponseFormat
        {
            Json,
            Text
        }

        /// <summary>
        /// Specifies the requirement for tool execution.
        /// </summary>
        public enum ToolExecutionRequirement
        {
            Required,
            Auto,
            None,
        }

        /// <summary>
        /// Specifies the reasons for finishing a process.
        /// </summary>
        public enum FinishReasons
        {
            Stop,
            Length,
            ToolCalls,
            ContentFilter,
            Error,
            TooManyRequests
        }

        /// <summary>
        /// Specifies the roles in the system.
        /// </summary>
        public enum Role
        {
            User,
            Assistant,
            System,
            Tool
        }

        /// <summary>
        /// Specifies the tools available in the system.
        /// </summary>
        public enum SystemTools
        {
            Image_Gen,
            Chat_Recursion,
        }

        /// <summary>
        /// Specifies the hosts for AGI services.
        /// </summary>
        public enum AGIServiceHost
        {
            Azure,
            OpenAI,
            Anthropic,
            None
        }

        /// <summary>
        /// Specifies the vector database provider.
        /// </summary>
        public enum RagServiceHost
        {
            Azure,
            Weaviate,
            None
        }

        /// <summary>
        /// Specifies the types of queries.
        /// </summary>
        public enum QueryType
        {
            Simple,
            Full,
            Semantic,
            Vector,
            VectorSemanticHybrid,
            VectorSimpleHybrid
        }

        /// <summary>
        /// Specifies the methods of search interpolation.
        /// </summary>
        public enum SearchInterpolation
        {
            Linear,
            Constant,
            Quadratic,
            Logarithmic
        }

        /// <summary>
        /// Specifies the methods of search aggregation.
        /// </summary>
        public enum SearchAggregation
        {
            Sum,
            Average,
            Minimum,
            Maximum,
            FirstMatching
        }

        /// <summary>
        /// Specifies the client policies.
        /// </summary>
        public enum ClientPolicies
        {
            AzureAIClientPolicy,
            OpenAIClientPolicy,
            AnthropicAIClientPolicy,
            ToolClientPolicy
        }

        /// <summary>
        /// Specifies the status codes for API responses.
        /// </summary>
        public enum APIResponseStatusCodes
        {
            Ok,
            BadRequest,
            NotFound,
            InternalError,
            TooManyRequests,
        }

        /// <summary>
        /// The default model for Anthropic.
        /// </summary>
        public const string DefaultAnthropicModel = "claude-3-7-sonnet-20250219";

        /// <summary>
        /// The default model for OpenAI.
        /// </summary>
        public const string DefaultOpenAIModel = "gpt-4o";

        /// <summary>
        /// The valid models for Anthropic.
        /// </summary>
        public static readonly string[] ValidAnthropicModels =
        {
                "claude-3-7-sonnet-latest",
                "claude-3-7-sonnet-20250219",
                "claude-3-5-sonnet-latest",
                "claude-3-5-sonnet-20241022",
                "claude-3-5-sonnet-20240620",
                "claude-3-5-haiku-latest",
                "claude-3-5-haiku-20241022",
                "claude-3-opus-latest",
                "claude-3-opus-20240229",
                "claude-3-haiku-20240307"
            };

        /// <summary>
        /// The valid models and context limits for OpenAI.
        /// </summary>
        public static readonly Dictionary<string, int> ValidOpenAIModelsAndContextLimits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "o1", 4096 },
                { "o1-2024-12-17", 4096 },
                { "o1-mini", 2048 },
                { "o1-mini-2024-09-12", 2048 },
                { "o1-preview", 4096 },
                { "o1-preview-2024-09-12", 4096 },
                { "o3-mini", 2048 },
                { "o3-mini-2025-01-31", 2048 },
                { "gpt-4.5-preview", 8192 },
                { "gpt-4.5-preview-2025-02-27", 8192 },
                { "gpt-4o", 8192 },
                { "chatgpt-4o-latest", 8192 },
                { "gpt-4o-2024-11-20", 8192 },
                { "gpt-4o-2024-08-06", 8192 },
                { "gpt-4o-2024-05-13", 8192 },
                { "gpt-4o-mini", 4096 },
                { "gpt-4o-mini-2024-07-18", 4096 },
                { "gpt-4", 8192 },
                { "gpt-4-0613", 8192 },
                { "gpt-4-0314", 8192 },
                { "gpt-4-turbo", 8192 },
                { "gpt-4-turbo-2024-04-09", 8192 },
                { "gpt-4-turbo-preview", 8192 },
                { "gpt-4-0125-preview", 8192 },
                { "gpt-4-1106-preview", 8192 },
                { "gpt-3.5-turbo", 4096 },
                { "gpt-3.5-turbo-0125", 4096 },
                { "gpt-3.5-turbo-1106", 4096 }
            };

        /// <summary>
        /// The system message for RAG requests.
        /// </summary>
        public const string RagMetadataGenSystemMessage =
            "You are part of an API that chunks documents for retrieval augmented generation tasks. Your job " +
            "is to take the requests, which are sent to you programatically, and shorten the data into a topic, " +
            "keywords, or another form of data. Take care to only provide the data requested in the completion, " +
            "as any words unrelated to the completion request will be interpreted as part of the topic or " +
            "keyword. It is vital that you keep all responses brief as well, as any response from you that " +
            "exceeds 255 characters will be truncated.";

        /// <summary>
        /// The prepended instructions for RAG requests.
        /// </summary>
        public const string RagRequestPrependedInstructions =
            "Below you will find a set of documents, each delimited with tripple backticks. " +
            "Please use these documents to inform your response to the dialogue below the documents. " +
            "If you use one of the sources, please reference it in your response using markdown like so: [SourceName](SourceLink)." +
            "If no SourceLink is present, only provide the sourcename. If no documents were provided, simply respond to the user's" +
            "message like you normally would.";

        /// <summary>
        /// System message instructions for generating a RAG search intent.
        /// </summary>
        public const string RagIntentGenSystemMessage =
            "You are part of an API that generates search intents for retrieval augmented generation tasks. Your job is to look at all of the current context," +
            "and attempt to create a simple query consisting of a few key words, or a short search intent, similar to values that you would feed into a search" +
            "engine. Only provide the query itself with no additional data, as your output will be fed directly to an Azure AI search service query request, " +
            "and this will obfuscate the result.";

        /// <summary>
        /// The default vector scoring profile name.
        /// </summary>
        public const string DefaultVectorScoringProfile = "vector-search-profile";

        /// <summary>
        /// The default model for embeddings.
        /// </summary>
        public const string DefaultAzureSearchEmbeddingModel = "text-embedding-3-large";

        /// <summary>
        /// The default embedding model used when indexing with Weaviate.
        /// </summary>
        public const string DefaultWeaviateEmbeddingModel = "Snowflake/snowflake-arctic-embed-l-v2.0"; // currently this value is only used in Validation and saving database defaults. The Weaviate client defaults to this value automatically, and therefore doesn't require it

        /// <summary>
        /// The default model for image generation.
        /// </summary>
        public const string DefaultImageGenModel = "dall-e-3";

        /// <summary>
        /// The default exception message.
        /// </summary>
        public const string DefaultExceptionMessage = "Internal Server Error, please reattempt. If this issue persists please contact the system administrator.";

        // RAG Settings

        /// <summary>
        /// The default number of attachments for RAG.
        /// </summary>
        public const int DefaultRagAttachmentNumber = 3;

        /// <summary>
        /// The default chunk overlap for RAG.
        /// </summary>
        public const double DefaultChunkOverlap = .1;

        /// <summary>
        /// The maximum number of retries for AI search service.
        /// </summary>
        public const int AISearchServiceMaxRetries = 5;

        /// <summary>
        /// The initial delay for AI search service retries.
        /// </summary>
        public const int AISearchServiceInitialDelay = 2;

        /// <summary>
        /// The maximum delay for AI search service retries.
        /// </summary>
        public const int AISearchServiceMaxDelay = 20;

        /// <summary>
        /// The default freshness boost for scoring.
        /// </summary>
        public const int DefaultScoringFreshnessBoost = 1;

        /// <summary>
        /// The default duration in days for scoring boost.
        /// </summary>
        public const int DefaultScoringBoostDurationDays = 180;

        /// <summary>
        /// The default tag boost for scoring.
        /// </summary>
        public const int DefaultScoringTagBoost = 1;
    }
}
