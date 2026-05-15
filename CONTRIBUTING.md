# Contributing

RoboOps is a portfolio project, but changes should be handled like a professional team repository.

## Local checks

Run these before opening a pull request:

```bash
cp frontend/operator-console/.env.example frontend/operator-console/.env.local
dotnet test RoboOps.slnx
cd frontend/operator-console
npm ci
npm run build
```

## Pull request checklist

- The change is scoped to one issue or feature.
- Backend build and tests pass.
- Frontend typecheck and build pass.
- Documentation is updated when behavior or architecture changes.
- UI changes include a screenshot or short recording in the PR description.
