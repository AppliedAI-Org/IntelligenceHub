using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelligenceHub.Business.Handlers
{
    public class BackgroundWorker : BackgroundService
    {
        private readonly IBackgroundTaskQueueHandler _taskQueue;

        public BackgroundWorker(IBackgroundTaskQueueHandler taskQueue)
        {
            _taskQueue = taskQueue;
        }

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
