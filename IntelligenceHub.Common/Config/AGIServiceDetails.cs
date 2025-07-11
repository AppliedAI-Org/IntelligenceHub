namespace IntelligenceHub.Common.Config
{
    /// <summary>
    /// Contains endpoint and authentication information for an AGI service.
    /// </summary>
    public class AGIServiceDetails
    {
        /// <summary>
        /// Gets or sets the service endpoint URL.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the API key used to authenticate.
        /// </summary>
        public string Key { get; set; }
    }
}

