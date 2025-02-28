namespace IntelligenceHub.Common.Config
{
    public class Settings
    {
        public string DbConnectionString { get; set; } = string.Empty;
        public string[] ValidOrigins { get; set; } = Array.Empty<string>();
        public int AGIClientMaxRetries { get; set; }
        public int AGIClientMaxJitter { get; set; }
        public int AGIClientInitialRetryDelay { get; set; }
        public int ToolClientMaxRetries { get; set; }
        public int ToolClientInitialRetryDelay { get; set; }
        public int MaxDbRetries { get; set; }
        public int MaxDbRetryDelay { get; set; }
        public int MaxCircuitBreakerFailures { get; set; }
        public int CircuitBreakerBreakDuration { get; set; }
    }
}
