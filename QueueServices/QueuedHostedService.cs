namespace ElectionApi.QueueServices
{
    public sealed class QueuedHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<QueuedHostedService> _logger;

        public QueuedHostedService(
            IBackgroundTaskQueue taskQueue,
            ILogger<QueuedHostedService> logger) =>
            (_taskQueue, _logger) = (taskQueue, logger);

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(QueuedHostedService)} is running.");
            return ProcessTaskQueueAsync(stoppingToken);
        }

        private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Func<CancellationToken, ValueTask>? workItem =
                        await _taskQueue.DequeueAsync(stoppingToken);

                    await workItem(stoppingToken);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogCritical(DateTime.Now.ToString() + ": Exception " + ex.Message + " " + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(DateTime.Now.ToString() + ": Exception " + ex.Message + " " + ex.StackTrace);
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogError(string.Format("{0}: {1} is stopping.", DateTime.Now.ToString(), nameof(QueuedHostedService)));
            await base.StopAsync(stoppingToken);
        }
    }
}
