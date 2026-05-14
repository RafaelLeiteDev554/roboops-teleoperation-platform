using System.Collections.Concurrent;
using RoboOps.Api.Models;

namespace RoboOps.Api.Services;

public sealed class RoboOpsStore
{
    private readonly ConcurrentDictionary<Guid, Robot> _robots = new();
    private readonly ConcurrentDictionary<Guid, TaskDefinition> _tasks = new();
    private readonly ConcurrentDictionary<Guid, TeleoperationSession> _sessions = new();
    private readonly ConcurrentDictionary<Guid, List<DataQualityFeedback>> _feedback = new();
    private readonly ConcurrentDictionary<Guid, RobotTelemetry> _telemetry = new();
    private readonly ConcurrentDictionary<Guid, RobotCommand> _lastCommands = new();

    public RoboOpsStore()
    {
        var robotA = new Robot(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Hikari-01",
            "Warehouse Rover",
            "Tokyo Lab A",
            RobotStatus.Online,
            new RobotConfiguration("2026.05.1", "front-wide-720p", "imu-lidar-camera", "5g-low-latency", 3));

        var robotB = new Robot(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Sora-02",
            "Mobile Manipulator",
            "Osaka Test Cell",
            RobotStatus.Paused,
            new RobotConfiguration("2026.04.7", "dual-camera-1080p", "arm-force-camera", "wifi6-private", 5));

        _robots[robotA.Id] = robotA;
        _robots[robotB.Id] = robotB;

        var taskA = new TaskDefinition(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Warehouse aisle navigation",
            "Collect navigation data while avoiding pallets and marked safety zones.",
            "Indoor warehouse",
            ["path-clear", "obstacle", "operator-correction"]);

        var taskB = new TaskDefinition(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Object handoff inspection",
            "Collect video and control traces for assisted manipulation handoff tasks.",
            "Manipulation bench",
            ["grasp-attempt", "handoff-success", "unsafe-motion"]);

        _tasks[taskA.Id] = taskA;
        _tasks[taskB.Id] = taskB;

        foreach (var robot in _robots.Values)
        {
            _telemetry[robot.Id] = CreateInitialTelemetry(robot);
        }
    }

    public IReadOnlyCollection<Robot> GetRobots() => _robots.Values.OrderBy(robot => robot.Name).ToArray();

    public Robot? GetRobot(Guid id) => _robots.GetValueOrDefault(id);

    public IReadOnlyCollection<TaskDefinition> GetTasks() => _tasks.Values.OrderBy(task => task.Name).ToArray();

    public IReadOnlyCollection<TeleoperationSession> GetSessions() => _sessions.Values.OrderByDescending(session => session.StartedAt).ToArray();

    public TeleoperationSession? GetSession(Guid id) => _sessions.GetValueOrDefault(id);

    public IReadOnlyCollection<DataQualityFeedback> GetFeedback(Guid sessionId) =>
        _feedback.TryGetValue(sessionId, out var items) ? items.ToArray() : [];

    public IReadOnlyCollection<RobotTelemetry> GetTelemetry() => _telemetry.Values.OrderBy(item => item.RobotId).ToArray();

    public RobotTelemetry? GetTelemetry(Guid robotId) => _telemetry.GetValueOrDefault(robotId);

    public RobotCommand? GetLastCommand(Guid robotId) => _lastCommands.GetValueOrDefault(robotId);

    public TeleoperationSession StartSession(StartSessionRequest request)
    {
        if (!_robots.ContainsKey(request.RobotId))
        {
            throw new InvalidOperationException("Robot does not exist.");
        }

        if (!_tasks.ContainsKey(request.TaskId))
        {
            throw new InvalidOperationException("Task definition does not exist.");
        }

        var session = new TeleoperationSession(
            Guid.NewGuid(),
            request.RobotId,
            request.TaskId,
            request.Operator,
            DateTimeOffset.UtcNow,
            null,
            IngestionStatus.Captured,
            72);

        _sessions[session.Id] = session;
        UpdateRobotStatus(request.RobotId, RobotStatus.Teleoperated);

        return session;
    }

    public DataQualityFeedback AddFeedback(Guid sessionId, DataQualityFeedbackRequest request, DataQualityScoring scoring)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException("Session does not exist.");
        }

        var feedback = new DataQualityFeedback(
            Guid.NewGuid(),
            sessionId,
            request.Reviewer,
            NormalizeRating(request.VideoQuality),
            NormalizeRating(request.ControlResponsiveness),
            NormalizeRating(request.LabelCompleteness),
            request.Notes,
            DateTimeOffset.UtcNow);

        var items = _feedback.GetOrAdd(sessionId, _ => []);
        lock (items)
        {
            items.Add(feedback);
        }

        var telemetry = GetTelemetry(session.RobotId) ?? CreateInitialTelemetry(_robots[session.RobotId]);
        var score = scoring.Calculate(telemetry, GetFeedback(sessionId)).Score;
        _sessions[sessionId] = session with
        {
            QualityScore = score,
            IngestionStatus = score >= 80 ? IngestionStatus.Validated : IngestionStatus.Processing
        };

        return feedback;
    }

    public IReadOnlyCollection<PipelineRun> GetPipelineRuns()
    {
        return _sessions.Values
            .OrderByDescending(session => session.StartedAt)
            .Select(session =>
            {
                var telemetry = GetTelemetry(session.RobotId) ?? CreateInitialTelemetry(_robots[session.RobotId]);
                var flags = BuildPipelineFlags(session, telemetry);
                return new PipelineRun(
                    session.Id,
                    session.IngestionStatus,
                    $"teleop-{session.StartedAt:yyyyMMdd}-{session.RobotId.ToString()[..8]}",
                    session.StartedAt.AddMinutes(3),
                    flags);
            })
            .ToArray();
    }

    public RobotTelemetry AdvanceTelemetry(Guid robotId)
    {
        var robot = _robots[robotId];
        var current = _telemetry[robotId];
        var command = GetLastCommand(robotId);
        var speed = command?.Type == RobotCommandType.Move ? command.LinearVelocity : 0;
        var turn = command?.Type == RobotCommandType.Move ? command.AngularVelocity : 0;
        var nextHeading = NormalizeHeading(current.HeadingDegrees + turn * 8);

        var next = current with
        {
            Timestamp = DateTimeOffset.UtcNow,
            BatteryPercent = Math.Max(8, current.BatteryPercent - 0.03),
            LatencyMs = Random.Shared.Next(42, 145),
            FramesPerSecond = Random.Shared.Next(22, 31),
            PacketLossPercent = Math.Round(Random.Shared.NextDouble() * 3.8, 2),
            PositionX = Math.Round(current.PositionX + Math.Cos(nextHeading * Math.PI / 180) * speed, 2),
            PositionY = Math.Round(current.PositionY + Math.Sin(nextHeading * Math.PI / 180) * speed, 2),
            HeadingDegrees = nextHeading,
            Status = robot.Status
        };

        _telemetry[robotId] = next;
        return next;
    }

    public void ApplyCommand(RobotCommand command)
    {
        if (!_robots.ContainsKey(command.RobotId))
        {
            throw new InvalidOperationException("Robot does not exist.");
        }

        _lastCommands[command.RobotId] = command;

        if (command.Type == RobotCommandType.Stop)
        {
            UpdateRobotStatus(command.RobotId, RobotStatus.Paused);
        }
        else if (command.Type is RobotCommandType.Move or RobotCommandType.Resume)
        {
            UpdateRobotStatus(command.RobotId, RobotStatus.Teleoperated);
        }
    }

    private void UpdateRobotStatus(Guid robotId, RobotStatus status)
    {
        if (_robots.TryGetValue(robotId, out var robot))
        {
            _robots[robotId] = robot with { Status = status };
        }
    }

    private static IReadOnlyList<string> BuildPipelineFlags(TeleoperationSession session, RobotTelemetry telemetry)
    {
        var flags = new List<string>();

        if (session.QualityScore < 75)
        {
            flags.Add("Needs reviewer validation");
        }

        if (telemetry.LatencyMs > 130)
        {
            flags.Add("Latency above target");
        }

        if (telemetry.PacketLossPercent > 2.5)
        {
            flags.Add("Stream packet loss");
        }

        return flags;
    }

    private static RobotTelemetry CreateInitialTelemetry(Robot robot) => new(
        robot.Id,
        DateTimeOffset.UtcNow,
        Random.Shared.Next(68, 96),
        Random.Shared.Next(50, 110),
        Random.Shared.Next(24, 31),
        Math.Round(Random.Shared.NextDouble() * 1.6, 2),
        Random.Shared.Next(-4, 5),
        Random.Shared.Next(-4, 5),
        Random.Shared.Next(0, 360),
        robot.Status);

    private static int NormalizeRating(int rating) => Math.Min(5, Math.Max(1, rating));

    private static double NormalizeHeading(double heading)
    {
        var result = heading % 360;
        return result < 0 ? result + 360 : result;
    }
}
