using Polly;

namespace IntelligenceHub.Host.Policies
{
    public class LoadBalancingSelector
    {
        private readonly Dictionary<string, Uri[]> _serviceUris;
        private readonly Dictionary<string, int> _currentIndices;

        public LoadBalancingSelector(Dictionary<string, string[]> serviceUrls)
        {
            _serviceUris = serviceUrls.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(url => new Uri(url)).ToArray()
            );
            _currentIndices = serviceUrls.ToDictionary(kvp => kvp.Key, kvp => 0);
        }

        public Uri GetNextBaseAddress(string serviceName)
        {
            if (!_serviceUris.ContainsKey(serviceName))
                throw new ArgumentException($"Service name '{serviceName}' not found.");

            var uris = _serviceUris[serviceName];
            int currentIndex = _currentIndices[serviceName];
            int nextIndex = Interlocked.Increment(ref currentIndex) % uris.Length;
            _currentIndices[serviceName] = nextIndex;

            if (nextIndex < 0) nextIndex = 0;
            return uris[nextIndex];
        }

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
