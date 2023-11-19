using HakuCommentViewer.WebServer.Queues;

namespace HakuCommentViewer.WebServer.Services
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger<QueuedHostedService> _logger;

        public QueuedHostedService(IBackgroundTaskQueue taskQueue,
            ILogger<QueuedHostedService> logger)
        {
            _taskQueue = taskQueue;
            _logger = logger;
        }

        public IBackgroundTaskQueue _taskQueue { get; }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxConcurrency = 100; //最大同時実行数

            _logger.LogInformation(
                $"Queued Hosted Service is running.{Environment.NewLine}" +
                $"{Environment.NewLine}Tap W to add a work item to the " +
                $"background queue.{Environment.NewLine}");

            //最大同時実行数のTaskを作成し、並列でタスクを実行できるようにする
            IEnumerable<Task> tasks = Enumerable.Range(0, maxConcurrency).Select(async n =>
                await BackgroundProcessing(stoppingToken)
            );
            await Task.WhenAll(tasks);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                try
                {
                    await workItem(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing {WorkItem}.", nameof(workItem));
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}
