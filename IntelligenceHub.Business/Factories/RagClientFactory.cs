using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Client.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Factories
{
    /// <summary>
    /// Factory for creating RAG search service clients.
    /// </summary>
    public class RagClientFactory : IRagClientFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public RagClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public IAISearchServiceClient GetClient(RagServiceHost? host)
        {
            if (host == RagServiceHost.Weaviate) return _serviceProvider.GetRequiredService<WeaviateSearchServiceClient>();
            else if (host == RagServiceHost.Azure) return _serviceProvider.GetRequiredService<AzureAISearchServiceClient>();
            throw new ArgumentException("Could not resolve the provided RagServiceHost.");
        }
    }
}
