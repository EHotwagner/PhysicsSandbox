# Merged Features Log

## Client REPL Library — 2026-03-20
**Branch:** 004-client-repl
**Spec:** specs/004-client-repl

**What was added:**
- PhysicsClient library — F# REPL-friendly library for controlling physics simulation and 3D viewer via gRPC
- 9 modules with .fsi signatures: Session, SimulationCommands, ViewCommands, Presets, Generators, Steering, StateDisplay, LiveWatch, IdGenerator
- Session management: connect/disconnect/reconnect, background state caching, body registry tracking
- All 9 proto simulation commands + 3 view commands wrapped as Result-returning functions
- 7 body presets (marble, bowlingBall, beachBall, crate, brick, boulder, die) with optional position/mass/ID overrides
- Random generators (spheres, boxes, mixed) with seedable RNG + scene builders (stack, row, grid, pyramid)
- Steering functions: push (named direction), pushVec, launch (toward target), spin, stop (counter-impulse)
- Spectre.Console state display: body tables, body inspection panels, simulation status, staleness timestamps
- Cancellable live-watch mode with filtering by body ID, shape type, velocity threshold
- Thread-safe human-readable ID generation ("sphere-1", "box-3")
- FSI convenience script (PhysicsClient.fsx)
- 52 unit tests (IdGenerator, Session, SimulationCommands, Presets, Generators, Steering, StateDisplay, SurfaceArea)

**New Components:**
- `src/PhysicsClient/` — F# client library (Bodies/, Connection/, Commands/, Steering/, Display/ modules)
- `tests/PhysicsClient.Tests/` — F# unit tests
- `src/PhysicsClient/PhysicsClient.fsx` — FSI convenience script

**Tasks Completed:** 51/52 tasks

---

## 3D Viewer — 2026-03-20
**Branch:** 003-3d-viewer
**Spec:** specs/003-3d-viewer

**What was added:**
- PhysicsViewer service — F# Stride3D (Community Toolkit, code-only) 3D viewer connecting to server via gRPC
- Real-time body rendering: spheres (blue), boxes (orange), unknown (red) with position/orientation from proto
- Interactive mouse/keyboard camera: left-drag orbit, scroll zoom, middle-drag pan
- REPL camera commands: SetCamera, SetZoom override interactive input
- Wireframe toggle via ToggleWireframe command (entity recreation with flat materials)
- Simulation status overlay via DebugTextSystem (time + running/paused)
- Ground grid at Y=0 (Add3DGround + AddGroundGizmo)
- Auto-reconnect with exponential backoff (1s→30s) on connection loss
- ServiceDefaults via background host for OpenTelemetry + structured logging
- Proto extended with StreamViewCommands RPC on PhysicsHub service
- Server extended with readViewCommand function and StreamViewCommands override
- 16 viewer unit tests (SceneManager, CameraController, SurfaceArea), 3 new server tests (readViewCommand), 2 new integration tests (StreamViewCommands)

**New Components:**
- `src/PhysicsViewer/` — F# viewer service (Rendering/SceneManager, Rendering/CameraController, Streaming/ViewerClient, Program)
- `tests/PhysicsViewer.Tests/` — F# unit tests with PublicApiBaseline.txt

**Tasks Completed:** 35/35 tasks

---

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
