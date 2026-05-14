namespace RoboOps.Worker;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    private static readonly string[] PipelineStages = ["Captured", "Processing", "Validated", "Ingested"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stageIndex = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Mock ingestion worker advanced sample session to {Stage} at {Time}",
                    PipelineStages[stageIndex],
                    DateTimeOffset.UtcNow);
            }

            stageIndex = (stageIndex + 1) % PipelineStages.Length;
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
