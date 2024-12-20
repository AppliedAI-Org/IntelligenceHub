namespace IntelligenceHub.Common.Config
{
    public class AGIClientSettings
    {
        public List<AGIServiceDetails> Services { get; set; }
        public string SearchServiceCompletionServiceEndpoint { get; set; }
        public string SearchServiceCompletionServiceKey { get; set; }
    }
}
