using IntelligenceHub.Client.Interfaces;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Factories
{
    /// <summary>
    /// Factory for creating AGI clients.
    /// </summary>
    public interface IAGIClientFactory
    {
        /// <summary>
        /// Returns an AGI client based on the host.
        /// </summary>
        /// <param name="host">The name of the host to retrieve a client for.</param>
        /// <returns>An AGI client that can be used to perform completions, and generate images.</returns>
        IAGIClient GetClient(AGIServiceHost? host);
    }
}
