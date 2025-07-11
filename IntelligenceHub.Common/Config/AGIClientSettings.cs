namespace IntelligenceHub.Common.Config
{
    /// <summary>
    /// Settings used for configuring AGI service clients.
    /// </summary>
    public class AGIClientSettings
    {
        /// <summary>
        /// Gets or sets configuration details for Azure OpenAI instances.
        /// </summary>
        public List<AGIServiceDetails> AzureOpenAIServices { get; set; }

        /// <summary>
        /// Gets or sets configuration details for OpenAI instances.
        /// </summary>
        public List<AGIServiceDetails> OpenAIServices { get; set; }

        /// <summary>
        /// Gets or sets configuration details for Anthropic instances.
        /// </summary>
        public List<AGIServiceDetails> AnthropicServices { get; set; }

        /// <summary>
        /// Gets or sets the endpoint for search service completions.
        /// </summary>
        public string SearchServiceCompletionServiceEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the API key for the search service completion endpoint.
        /// </summary>
        public string SearchServiceCompletionServiceKey { get; set; }
    }
}

