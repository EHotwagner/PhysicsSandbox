# Merged Features Log

## Contracts and Server Hub — 2026-03-20
**Branch:** 001-server-hub
**Spec:** specs/001-server-hub

**What was added:**
- Aspire AppHost orchestrator with Podman support
- Shared gRPC contracts (PhysicsHub + SimulationLink services, all message types)
- PhysicsServer hub — central message router with state caching and single-simulation enforcement
- Shared ServiceDefaults (health checks, OpenTelemetry, service discovery, resilience)
- 10 unit tests (F#) + 3 integration tests (C#, Aspire)

**New Components:**
- `src/PhysicsSandbox.AppHost/` — C# Aspire orchestrator
- `src/PhysicsSandbox.ServiceDefaults/` — C# shared infrastructure
- `src/PhysicsSandbox.Shared.Contracts/` — Proto gRPC contracts
- `src/PhysicsServer/` — F# server hub (Hub/StateCache, Hub/MessageRouter, Services/PhysicsHubService, Services/SimulationLinkService)
- `tests/PhysicsServer.Tests/` — F# unit tests
- `tests/PhysicsSandbox.Integration.Tests/` — C# integration tests

**Tasks Completed:** 34/34 tasks
