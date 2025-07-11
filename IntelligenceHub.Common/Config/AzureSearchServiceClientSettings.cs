namespace IntelligenceHub.Common.Config
{
    /// <summary>
    /// Settings for configuring the Azure Cognitive Search client.
    /// </summary>
    public class AzureSearchServiceClientSettings
    {
        /// <summary>
        /// Gets or sets the service endpoint URL.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the API key used to authenticate with the service.
        /// </summary>
        public string Key { get; set; }
    }
}

