using Microsoft.AspNetCore.SignalR;
using RoboOps.Api.Realtime;

namespace RoboOps.Api.Services;

public sealed class RobotTelemetrySimulator(
    RoboOpsStore store,
    IHubContext<TelemetryHub> hubContext,
    ILogger<RobotTelemetrySimulator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Robot telemetry simulator started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var robot in store.GetRobots())
            {
                var telemetry = store.AdvanceTelemetry(robot.Id);
                await hubContext.Clients.Group(TelemetryHub.GroupName(robot.Id)).SendAsync("telemetryUpdated", telemetry, stoppingToken);
                await hubContext.Clients.Group("dashboard").SendAsync("telemetryUpdated", telemetry, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
