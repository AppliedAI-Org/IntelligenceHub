namespace IntelligenceHub.Tests.Stress.Config
{
    public class StressTestingSettings
    {
        public int TotalRequests { get; set; }
        public int ConcurrencyLevel { get; set; }
        public int ConcurrencyDelaySeconds { get; set; }
    }
}
