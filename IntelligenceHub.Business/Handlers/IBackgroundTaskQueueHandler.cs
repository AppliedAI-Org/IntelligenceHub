using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.Business.Handlers
{
    /// <summary>
    /// A handler for managing background tasks.
    /// </summary>
    public interface IBackgroundTaskQueueHandler
    {
        /// <summary>
        /// Queues a background work item.
        /// </summary>
        /// <param name="workItem">The work item to queue.</param>
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

        /// <summary>
        /// Dequeues a background work item.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token associated with the work item.</param>
        /// <returns>A task that represents the dequeue operation.</returns>
        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}
