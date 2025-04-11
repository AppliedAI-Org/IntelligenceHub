using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.Business.Handlers
{
    /// <summary>
    /// A handler for managing background tasks.
    /// </summary>
    public class BackgroundTaskQueueHandler : IBackgroundTaskQueueHandler
    {
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new();
        private readonly SemaphoreSlim _signal = new(0);

        /// <summary>
        /// Queues a background work item.
        /// </summary>
        /// <param name="workItem">The work item to queue.</param>
        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        /// <summary>
        /// Dequeues a background work item.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token associated with the work item.</param>
        /// <returns>A task that represents the dequeue operation.</returns>
        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);
            return workItem!;
        }
    }
}
