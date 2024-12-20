using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IntelligenceHub.Common.Config;

namespace IntelligenceHub.Host.Policies
{
    public class LoadBalancingSelector
    {
        private readonly Uri[] _backendUris;
        private int _currentIndex = 0;

        public LoadBalancingSelector(string[] backendUrls)
        {
            // Store URLs as immutable Uri instances.
            _backendUris = backendUrls.Select(url => new Uri(url)).ToArray();
        }

        public Uri GetNextBaseAddress()
        {
            // Atomically get and increment the index, wrapping on overflow.
            int nextIndex = Interlocked.Increment(ref _currentIndex) % _backendUris.Length;

            // Ensure the index is non-negative (in case of overflow).
            if (nextIndex < 0) nextIndex = 0;

            return _backendUris[nextIndex];
        }
    }

}
