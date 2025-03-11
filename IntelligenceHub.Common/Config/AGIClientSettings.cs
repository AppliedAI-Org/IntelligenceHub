namespace IntelligenceHub.Common.Config
{
    public class AGIClientSettings
    {
        public List<AGIServiceDetails> AzureOpenAIServices { get; set; }
        public List<AGIServiceDetails> OpenAIServices { get; set; }
        public List<AGIServiceDetails> AnthropicServices { get; set; }
        public string SearchServiceCompletionServiceEndpoint { get; set; }
        public string SearchServiceCompletionServiceKey { get; set; }
    }
}
