# Merged Features Log

## Physics Simulation Service — 2026-03-20
**Branch:** 002-physics-simulation
**Spec:** specs/002-physics-simulation

**What was added:**
- PhysicsSimulation service — F# background worker connecting to server via SimulationLink bidirectional gRPC stream
- BepuFSharp (BepuPhysics2 wrapper) integration via local NuGet package for rigid body dynamics
- Lifecycle control: play, pause, single-step at 60Hz fixed timestep
- Body management: add/remove dynamic bodies (sphere, box) and static planes
- Force system: persistent forces, one-shot impulses, torques, clear-forces, global gravity
- State streaming: complete world state (position, velocity, angular velocity, orientation) after every step
- Proto contract extensions: RemoveBody, ApplyImpulse, ApplyTorque, ClearForces commands + Body angular_velocity/orientation fields
- 37 unit tests (lifecycle, bodies, forces, gravity, edge cases, 100-body stress test, surface-area baselines)

**New Components:**
- `src/PhysicsSimulation/` — F# simulation service (World, Commands, Client modules with .fsi signatures)
- `tests/PhysicsSimulation.Tests/` — F# unit tests
- `NuGet.config` — local NuGet feed for BepuFSharp

**Tasks Completed:** 39/45 tasks (6 integration tests deferred — require Aspire containers)

---

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
