using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Client.Implementations;
using IntelligenceHub.Client.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Business.Implementations
{
    public class AGIClientFactory : IAGIClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AGIClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IAGIClient GetClient(AGIServiceHosts? host)
        {
            if (host == AGIServiceHosts.OpenAI) return _serviceProvider.GetRequiredService<OpenAIClient>();
            else if (host == AGIServiceHosts.Azure) return _serviceProvider.GetRequiredService<AzureAIClient>();
            else if (host == AGIServiceHosts.Anthropic) return _serviceProvider.GetRequiredService<AnthropicAIClient>();
            else throw new ArgumentException($"Invalid service name: {host}");
        }
    }
}