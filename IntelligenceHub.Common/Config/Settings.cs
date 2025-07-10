namespace IntelligenceHub.Common.Config
{
    /// <summary>
    /// Global application settings loaded from configuration.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Gets or sets the connection string to the SQL database.
        /// </summary>
        public string DbConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the allowed CORS origins.
        /// </summary>
        public string[] ValidOrigins { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the maximum number of retries for AGI client requests.
        /// </summary>
        public int AGIClientMaxRetries { get; set; }

        /// <summary>
        /// Gets or sets the maximum jitter for AGI client retries.
        /// </summary>
        public int AGIClientMaxJitter { get; set; }

        /// <summary>
        /// Gets or sets the initial retry delay for AGI client requests.
        /// </summary>
        public int AGIClientInitialRetryDelay { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retries for tool client requests.
        /// </summary>
        public int ToolClientMaxRetries { get; set; }

        /// <summary>
        /// Gets or sets the initial retry delay for tool client requests.
        /// </summary>
        public int ToolClientInitialRetryDelay { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of database retries.
        /// </summary>
        public int MaxDbRetries { get; set; }

        /// <summary>
        /// Gets or sets the maximum delay between database retry attempts.
        /// </summary>
        public int MaxDbRetryDelay { get; set; }

        /// <summary>
        /// Gets or sets the number of failures before the circuit breaker trips.
        /// </summary>
        public int MaxCircuitBreakerFailures { get; set; }

        /// <summary>
        /// Gets or sets the duration that the circuit breaker remains open.
        /// </summary>
        public int CircuitBreakerBreakDuration { get; set; }

        /// <summary>
        /// Gets or sets the list of valid AGI model names.
        /// </summary>
        public string[] ValidAGIModels { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the default host used for image generation.
        /// </summary>
        public string DefaultImageGenHost { get; set; } = string.Empty;
    }
}

