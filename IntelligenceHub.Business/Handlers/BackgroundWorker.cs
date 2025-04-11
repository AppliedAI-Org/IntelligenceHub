using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.Business.Handlers
{
    /// <summary>
    /// Background worker that processes tasks from the queue.
    /// </summary>
    public class BackgroundWorker : BackgroundService
    {
        private readonly IBackgroundTaskQueueHandler _taskQueue;

        /// <summary>
        /// Constructor for the background worker.
        /// </summary>
        /// <param name="taskQueue">The task queue.</param>
        public BackgroundWorker(IBackgroundTaskQueueHandler taskQueue)
        {
            _taskQueue = taskQueue;
        }

        /// <summary>
        /// Processes the tasks from the queue.
        /// </summary>
        /// <param name="stoppingToken">A stop token to cancel the task.</param>
        /// <returns>An awaitable task.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                await workItem(stoppingToken);
            }
        }
    }
}
