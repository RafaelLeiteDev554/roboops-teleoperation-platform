# GitHub Workflow

This project uses a lightweight version of trunk-based development.

## Branches

- `main` should stay deployable and green.
- Feature branches should be short-lived.
- Use names such as `feature/operator-console`, `feature/realtime-telemetry`, `fix/auth-token-validation` or `docs/architecture`.

## Commits

Use Conventional Commits:

- `chore: initialize portfolio repository`
- `feat(api): add robot session endpoints`
- `feat(operator): add real-time telemetry panel`
- `test(api): cover data quality scoring`
- `docs: document teleoperation architecture`

## Pull requests

Every pull request should include:

- A short summary of the change.
- A test plan with commands that were run.
- Screenshots or GIFs for UI changes.
- Any follow-up work that is intentionally left out.

## Suggested initial issues

1. Initialize repository structure and documentation.
2. Add .NET API with health check and demo auth.
3. Add robot/task/session endpoints.
4. Add SignalR telemetry hub and simulator.
5. Add Vue operator console.
6. Add data-quality feedback and pipeline dashboard.
7. Add Docker Compose and CI.
