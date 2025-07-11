using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Client.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Factories
{
    /// <summary>
    /// Factory for creating AGI clients.
    /// </summary>
    public class AGIClientFactory : IAGIClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor for the AGI Client Factory.
        /// </summary>
        /// <param name="serviceProvider">The name of the service provider used to return the AGI client.</param>
        public AGIClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Returns an AGI client based on the host.
        /// </summary>
        /// <param name="host">The name of the host to retrieve a client for.</param>
        /// <returns>An AGI client that can be used to perform completions, and generate images.</returns>
        /// <exception cref="ArgumentException">Thrown if the host does not match any existing client.</exception>
        public IAGIClient GetClient(AGIServiceHost? host)
        {
            if (host == AGIServiceHost.OpenAI) return _serviceProvider.GetRequiredService<OpenAIClient>();
            else if (host == AGIServiceHost.Azure) return _serviceProvider.GetRequiredService<AzureAIClient>();
            else if (host == AGIServiceHost.Anthropic) return _serviceProvider.GetRequiredService<AnthropicAIClient>();
            else throw new ArgumentException($"Invalid service name: {host}");
        }
    }
}