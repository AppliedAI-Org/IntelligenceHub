using Polly;

namespace IntelligenceHub.Host.Policies
{
    /// <summary>
    /// Class for selecting a base address for a service based on a load balancing strategy.
    /// </summary>
    public class LoadBalancingSelector
    {
        private readonly Dictionary<string, Uri[]> _serviceUris;
        private readonly Dictionary<string, int> _currentIndices;

        /// <summary>
        /// Default constructor for the LoadBalancingSelector.
        /// </summary>
        /// <param name="serviceUrls">The service urls used to select the service 
        /// based on the load balancing strategy.</param>
        public LoadBalancingSelector(Dictionary<string, string[]> serviceUrls)
        {
            _serviceUris = serviceUrls.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(url => new Uri(url)).ToArray()
            );
            _currentIndices = serviceUrls.ToDictionary(kvp => kvp.Key, kvp => 0);
        }

        /// <summary>
        /// Gets the next base address for a given service.
        /// </summary>
        /// <param name="serviceName">The name of the service to retrieve the base address for.</param>
        /// <returns>The base address of the service as a URI.</returns>
        /// <exception cref="ArgumentException">Thrown if the service name does not exist in the list of URIs.</exception>
        public Uri GetNextBaseAddress(string serviceName)
        {
            if (!_serviceUris.ContainsKey(serviceName)) throw new ArgumentException($"Service name '{serviceName}' not found.");

            var uris = _serviceUris[serviceName];
            int currentIndex = _currentIndices[serviceName];
            int nextIndex = Interlocked.Increment(ref currentIndex) % uris.Length;
            _currentIndices[serviceName] = nextIndex;

            if (nextIndex < 0) nextIndex = 0;
            return uris[nextIndex];
        }

        /// <summary>
        /// Registers an HttpClient with a policy for a given service.
        /// </summary>
        /// <param name="services">The collection of services in the DI container.</param>
        /// <param name="policyName">The name of the HttpClient policy.</param>
        /// <param name="serviceName">The name of the service being chosen to conform with the load balancing strategy.</param>
        /// <param name="policyWrap">The polciy wrap to add to the HttpClient.</param>
        public static void RegisterHttpClientWithPolicy(IServiceCollection services, string policyName, string serviceName, IAsyncPolicy<HttpResponseMessage> policyWrap)
        {
            services.AddHttpClient(policyName, (serviceProvider, client) =>
            {
                var baseAddressSelector = serviceProvider.GetRequiredService<LoadBalancingSelector>();
                client.BaseAddress = baseAddressSelector.GetNextBaseAddress(serviceName);
                Console.WriteLine($"Configured HttpClient with BaseAddress: {client.BaseAddress}");
            }).AddPolicyHandler(policyWrap);
        }
    }
}
