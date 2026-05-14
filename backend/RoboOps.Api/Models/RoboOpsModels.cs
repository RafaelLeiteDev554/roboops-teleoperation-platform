namespace RoboOps.Api.Models;

public enum RobotStatus
{
    Online,
    Teleoperated,
    Paused,
    Offline
}

public enum IngestionStatus
{
    Captured,
    Processing,
    Validated,
    Rejected,
    Ingested
}

public enum RobotCommandType
{
    Move,
    Stop,
    Pause,
    Resume
}

public sealed record RobotConfiguration(
    string FirmwareVersion,
    string CameraProfile,
    string SensorSuite,
    string NetworkProfile,
    int Revision);

public sealed record Robot(
    Guid Id,
    string Name,
    string Model,
    string Location,
    RobotStatus Status,
    RobotConfiguration Configuration);

public sealed record TaskDefinition(
    Guid Id,
    string Name,
    string Description,
    string Environment,
    IReadOnlyList<string> RequiredLabels);

public sealed record TeleoperationSession(
    Guid Id,
    Guid RobotId,
    Guid TaskId,
    string Operator,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    IngestionStatus IngestionStatus,
    double QualityScore);

public sealed record DataQualityFeedback(
    Guid Id,
    Guid SessionId,
    string Reviewer,
    int VideoQuality,
    int ControlResponsiveness,
    int LabelCompleteness,
    string Notes,
    DateTimeOffset CreatedAt);

public sealed record RobotTelemetry(
    Guid RobotId,
    DateTimeOffset Timestamp,
    double BatteryPercent,
    double LatencyMs,
    double FramesPerSecond,
    double PacketLossPercent,
    double PositionX,
    double PositionY,
    double HeadingDegrees,
    RobotStatus Status);

public sealed record RobotCommand(
    Guid RobotId,
    RobotCommandType Type,
    double LinearVelocity,
    double AngularVelocity,
    string IssuedBy);

public sealed record StartSessionRequest(Guid RobotId, Guid TaskId, string Operator);

public sealed record DataQualityFeedbackRequest(
    string Reviewer,
    int VideoQuality,
    int ControlResponsiveness,
    int LabelCompleteness,
    string Notes);

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string AccessToken, string Role, DateTimeOffset ExpiresAt);

public sealed record WebRtcOfferRequest(Guid SessionId, string Sdp, string Type);

public sealed record WebRtcOfferResponse(string Sdp, string Type, IReadOnlyList<string> IceServers);

public sealed record PipelineRun(
    Guid SessionId,
    IngestionStatus Status,
    string DatasetName,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> QualityFlags);

public sealed record QualityScoreBreakdown(
    double Score,
    double VideoSignal,
    double ControlSignal,
    double MetadataCompleteness,
    IReadOnlyList<string> Flags);
