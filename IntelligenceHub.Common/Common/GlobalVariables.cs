namespace IntelligenceHub.Common
{
    public static class GlobalVariables
    {
        public const string RagRequestSystemMessage = 
            "You are part of an API that chunks documents for retrieval augmented " +
            "generation tasks. Your job is to take the requests, which are sent to you programatically, " +
            "and shorten the data into a topic, keywords, or another form of data. Please take care to " +
            "only provide the data requested in the completion, as any words unrelated to the completion " +
            "request will be interpreted as part of the topic or keyword.";

        public const string RagRequestPrependedInstructions = 
            "Below you will find a set of documents, each delimited with tripple backticks. " +
                "Please use these documents to inform your response to the dialogue below the documents. " +
                "If you use one of the sources, please reference it in your response using markdown like so: [SourceName](SourceLink)." +
                "If no SourceLink is present, only provide the sourcename.\n\n";

        public enum ClientPolicies
        {
            AzureAIClientPolicy,
            OpenAIClientPolicy,
            AnthropicAIClientPolicy,
            ToolClientPolicy
        }

        public enum ResponseFormat
        {
            Json,
            Text
        }

        public enum ToolExecutionRequirement
        {
            Required,
            Auto,
            None,
        }

        public enum FinishReason
        {
            Stop,
            Length,
            ToolCalls,
            ContentFilter,
            Error
        }

        public enum Role
        {
            User,
            Assistant,
            System,
            Tool
        }

        public enum SystemTools
        {
            Recurse_ai_dialogue,
        }

        public enum AGIServiceHosts
        {
            Azure,
            OpenAI,
            Anthropic
        }

        public const string DefaultAGIModel = "gpt-4o-mini";
        public const string DefaultEmbeddingModel = "text-embedding-3-large";

        public const string DefaultExceptionMessage = "Internal Server Error, please reattempt. If this issue persists please contact the system administrator.";
    }
}
