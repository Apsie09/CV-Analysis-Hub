using CVAnalysisHub.Application.AnalysisRuns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CVAnalysisHub.Infrastructure.AnalysisRuns;

public sealed class QueuedAnalysisBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<QueuedAnalysisBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IAnalysisRunProcessor>();
            var processed = await processor.ProcessNextQueuedAsync(stoppingToken);

            if (processed)
            {
                continue;
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Queued analysis background service is stopping.");
                break;
            }
        }
    }
}
