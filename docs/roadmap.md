# Roadmap

## Milestone 1: Professional foundation

- Monorepo structure for .NET API, worker and Vue operator console.
- README, architecture docs, ADRs, CI and PR templates.
- Local development instructions in English.

## Milestone 2: Teleoperation MVP

- Real-time telemetry stream with SignalR.
- Operator commands through a secure hub.
- Session creation and robot/task metadata APIs.
- Operator UI with simulated video, metrics and command controls.

## Milestone 3: Data-quality workflow

- Feedback capture from reviewers/operators.
- Quality score calculation from telemetry and feedback.
- Pipeline dashboard with ingestion status and quality flags.

## Milestone 4: Persistence and observability

- Move metadata from in-memory storage to PostgreSQL.
- Use Redis for active robot state and command deduplication.
- Add OpenTelemetry traces and dashboard screenshots.

## Milestone 5: Production hardening

- Replace demo auth with OIDC.
- Add audit logs for teleoperation commands.
- Add WebRTC media server integration.
- Add Playwright tests for critical operator workflows.
