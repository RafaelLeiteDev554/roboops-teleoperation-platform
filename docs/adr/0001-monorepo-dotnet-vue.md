# ADR 0001: Use a .NET and Vue monorepo

## Status

Accepted

## Context

The target role combines backend APIs, real-time robot control, operator-facing web interfaces and internal data tools. The portfolio project should demonstrate those skills in a single coherent product.

## Decision

Use a monorepo with:

- ASP.NET Core for REST APIs, JWT auth and SignalR.
- Vue 3 with TypeScript for the operator console.
- A worker project for data pipeline simulation.
- Shared documentation and CI at the repository root.

## Consequences

This keeps local development simple and makes the system easy to review during interviews. It also leaves room to split services later if persistence, media streaming or ingestion workloads become more complex.
