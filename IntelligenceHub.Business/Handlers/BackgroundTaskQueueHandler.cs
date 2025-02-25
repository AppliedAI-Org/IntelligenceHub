using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.Business.Handlers
{
    public class BackgroundTaskQueueHandler : IBackgroundTaskQueueHandler
    {
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new();
        private readonly SemaphoreSlim _signal = new(0);

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);
            return workItem!;
        }
    }
}
