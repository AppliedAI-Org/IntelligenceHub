using System;

namespace IntelligenceHub.Common.Config
{
    /// <summary>
    /// Settings used to configure the Weaviate search service client.
    /// </summary>
    public class WeaviateSearchServiceClientSettings
    {
        /// <summary>
        /// Gets or sets the service endpoint.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the API key used for authentication.
        /// </summary>
        public string Key { get; set; }
    }
}
