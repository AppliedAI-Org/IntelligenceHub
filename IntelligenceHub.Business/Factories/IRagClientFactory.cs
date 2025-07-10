using IntelligenceHub.Client.Interfaces;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Factories
{
    /// <summary>
    /// Factory for creating RAG clients.
    /// </summary>
    public interface IRagClientFactory
    {
        /// <summary>
        /// Returns an AI search client based on the rag host.
        /// </summary>
        IAISearchServiceClient GetClient(RagServiceHost? host);
    }
}
