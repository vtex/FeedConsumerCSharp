using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VTEX.FeedConsumer.Services;

namespace VTEX.FeedConsumer.BackgroundServices
{
    internal class ConsumerBackgroundService : BackgroundService
    {

        // Max execution per period
        const int MAX_PER_PERIOD = 500;
        // This is the number os concurrent action that will be release in the first few milliseconds of each minute
        // You can set another inital value if you don't want to have a peek in each minute
        const int MAX_ACTION_CONCURRENT = 500;


        // This semaphore is to control the time
        private static SemaphoreSlim _semaphoreSlimPeriod = new SemaphoreSlim(MAX_PER_PERIOD);
        // This semaphore is to control the action.
        private static SemaphoreSlim _semaphoreSlimAction = new SemaphoreSlim(MAX_ACTION_CONCURRENT);
        // All throttlings are rest every minute
        private readonly TimeSpan PERIOD = TimeSpan.FromMinutes(1);
        private readonly ILogger<ConsumerBackgroundService> _logger;
        private readonly FeedService _feedService;

        public ConsumerBackgroundService(ILogger<ConsumerBackgroundService> logger, FeedService feedService)
        {
            _logger = logger;
            _feedService = feedService;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (MAX_PER_PERIOD < MAX_ACTION_CONCURRENT)
            {
                throw new Exception("The MAX_PER_PERIOD should be more or equal to MAX_ACTION_CONCURRENT");
            }

            try
            {
                while (true)
                {
                    // waiting an opportunity to run an action
                    await _semaphoreSlimAction.WaitAsync(stoppingToken);
                    // waiting the last period to end
                    await _semaphoreSlimPeriod.WaitAsync(stoppingToken);

                    var hasMoreInThisMinute = false;
                    try
                    {
                        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                        using var cancellationTokenLinked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, cancellationTokenSource.Token);

                        _logger.LogDebug("Executing");
                        hasMoreInThisMinute = await _feedService.DequeueProcessAndCheckIfContinueAsync(cancellationTokenLinked.Token);

                        _logger.LogDebug("Executed and {debugText} more items", hasMoreInThisMinute ? "has" : "hasn'nt");

                        _ = Task.Delay(PERIOD).ContinueWith(task =>
                        {
                            _logger.LogDebug("Release period sempahore");
                            _semaphoreSlimPeriod.Release(1);
                            if (!hasMoreInThisMinute)
                            {
                                _logger.LogDebug("Release action sempahore after no more items");
                                _semaphoreSlimAction.Release(1);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("An error has occurred when processing the feed: `{exMessage}`.", ex.Message);
                    }
                    finally
                    {
                        if (hasMoreInThisMinute)
                        {
                            _logger.LogDebug("Release action sempahore");
                            _semaphoreSlimAction.Release(1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("BackgroundWorker stopped by error `{exMessage}`.", ex.Message);
                throw;
            }
        }

    }
}
