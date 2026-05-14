using RoboOps.Api.Models;
using RoboOps.Api.Services;

namespace RoboOps.Api.Tests;

public class DataQualityScoringTests
{
    [Fact]
    public void Calculate_ReturnsFlags_WhenTelemetryQualityIsLow()
    {
        var scoring = new DataQualityScoring();
        var telemetry = new RobotTelemetry(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            86,
            180,
            18,
            4.2,
            0,
            0,
            90,
            RobotStatus.Teleoperated);

        var result = scoring.Calculate(telemetry, []);

        Assert.True(result.Score < 80);
        Assert.Contains("High control latency", result.Flags);
        Assert.Contains("Low video frame rate", result.Flags);
        Assert.Contains("Missing operator feedback", result.Flags);
    }
}
