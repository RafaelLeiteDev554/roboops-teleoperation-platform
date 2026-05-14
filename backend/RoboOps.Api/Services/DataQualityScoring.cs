using RoboOps.Api.Models;

namespace RoboOps.Api.Services;

public sealed class DataQualityScoring
{
    public QualityScoreBreakdown Calculate(RobotTelemetry telemetry, IReadOnlyCollection<DataQualityFeedback> feedback)
    {
        var videoSignal = Clamp(100 - (telemetry.PacketLossPercent * 8) + (telemetry.FramesPerSecond - 24));
        var controlSignal = Clamp(100 - Math.Max(0, telemetry.LatencyMs - 80) * 0.7);
        var metadataCompleteness = feedback.Count == 0
            ? 72
            : Clamp(feedback.Average(item => (item.VideoQuality + item.ControlResponsiveness + item.LabelCompleteness) / 3.0) * 20);

        var score = Math.Round((videoSignal * 0.4) + (controlSignal * 0.35) + (metadataCompleteness * 0.25), 1);
        var flags = BuildFlags(telemetry, feedback);

        return new QualityScoreBreakdown(score, Math.Round(videoSignal, 1), Math.Round(controlSignal, 1), Math.Round(metadataCompleteness, 1), flags);
    }

    private static IReadOnlyList<string> BuildFlags(RobotTelemetry telemetry, IReadOnlyCollection<DataQualityFeedback> feedback)
    {
        var flags = new List<string>();

        if (telemetry.LatencyMs > 150)
        {
            flags.Add("High control latency");
        }

        if (telemetry.FramesPerSecond < 20)
        {
            flags.Add("Low video frame rate");
        }

        if (telemetry.PacketLossPercent > 3)
        {
            flags.Add("Unstable stream");
        }

        if (feedback.Count == 0)
        {
            flags.Add("Missing operator feedback");
        }

        return flags;
    }

    private static double Clamp(double value) => Math.Min(100, Math.Max(0, value));
}
