# Implementation Plan: 3D Viewer

**Branch**: `003-3d-viewer` | **Date**: 2026-03-20 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-3d-viewer/spec.md`

## Summary

3D Viewer service using Stride3D (Community Toolkit, code-only) to render physics simulation state received via gRPC streaming from the server. The viewer subscribes to two server-streaming RPCs: `StreamState` for body positions/orientations and a new `StreamViewCommands` for camera/wireframe commands from the REPL client. It also supports interactive mouse/keyboard camera control via Stride's built-in camera controller.

## Technical Context

**Language/Version**: F# on .NET 10.0 (viewer service), C# on .NET 10.0 (proto contracts, server changes)
**Primary Dependencies**: Stride.CommunityToolkit* 1.0.0-preview.62 (4 packages), Grpc.Net.Client 2.x, Google.Protobuf 3.x
**Storage**: N/A (real-time streaming, no persistence)
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x
**Target Platform**: Linux (GPU required for runtime, headless for unit tests)
**Project Type**: Desktop 3D application (Aspire-managed)
**Performance Goals**: 60 fps rendering, <100ms state-to-display latency, up to 100 bodies
**Constraints**: OpenGL graphics API for container/GPU-passthrough; main thread for Stride3D game loop
**Scale/Scope**: Single viewer instance, single simulation, ~3 F# modules

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | Viewer is independently deployable, communicates only via gRPC |
| II. Contract-First | PASS | Proto contract extended before implementation; `StreamViewCommands` added to physics_hub.proto |
| III. Shared Nothing | PASS | Only references `PhysicsSandbox.Shared.Contracts` and `ServiceDefaults` |
| IV. Spec-First | PASS | Spec and plan completed before implementation |
| V. Compiler-Enforced | PASS | `.fsi` signature files defined for SceneManager, CameraController, ViewerClient |
| VI. Test Evidence | PASS | Unit tests for pure logic; integration tests for gRPC connectivity |
| VII. Observability | PASS (justified) | ServiceDefaults via background web host; Stride game loop on main thread |

**Post-Phase 1 Re-check**: All gates pass. The background web host for ServiceDefaults is a minor architectural adaptation justified by Stride3D's main-thread requirement — the same structured logging, tracing, and health checks are provided.

## Project Structure

### Documentation (this feature)

```text
specs/003-3d-viewer/
├── plan.md              # This file
├── research.md          # Phase 0: technology decisions
├── data-model.md        # Phase 1: entities and state transitions
├── quickstart.md        # Phase 1: build/run/test guide
├── contracts/           # Phase 1: proto extension + module signatures
│   ├── proto-extension.md
│   └── viewer-modules.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── PhysicsSandbox.Shared.Contracts/
│   └── Protos/physics_hub.proto         # MODIFIED: +StreamViewCommands RPC
│
├── PhysicsServer/                       # MODIFIED: server-side support
│   ├── Hub/
│   │   └── MessageRouter.fsi/.fs        # MODIFIED: +readViewCommand
│   └── Services/
│       └── PhysicsHubService.fsi/.fs    # MODIFIED: +StreamViewCommands override
│
├── PhysicsViewer/                       # NEW: F# Stride3D viewer
│   ├── Rendering/
│   │   ├── SceneManager.fsi/.fs         # SimulationState → Stride entities
│   │   └── CameraController.fsi/.fs     # Camera state, input, commands
│   ├── Streaming/
│   │   └── ViewerClient.fsi/.fs         # gRPC streaming client
│   ├── Program.fs                       # Entry point: host + game loop
│   └── PhysicsViewer.fsproj
│
├── PhysicsSandbox.AppHost/
│   └── AppHost.cs                       # MODIFIED: +PhysicsViewer registration

tests/
├── PhysicsViewer.Tests/                 # NEW: F# unit tests
│   ├── SceneManagerTests.fs             # State-to-entity mapping logic
│   ├── CameraControllerTests.fs         # Camera math and command application
│   ├── SurfaceAreaTests.fs              # Public API baseline
│   └── PhysicsViewer.Tests.fsproj
│
├── PhysicsSandbox.Integration.Tests/
│   └── ServerHubTests.cs                # MODIFIED: +StreamViewCommands test
```

**Structure Decision**: Follows the established per-service pattern (PhysicsServer, PhysicsSimulation). The viewer is a new F# console project with Stride3D packages, structured into Rendering (scene/camera) and Streaming (gRPC client) subdirectories. Server-side changes are minimal: one new function + one new RPC override.

### stride3d-fsharp Skill Usage

| Task | Skill Reference | Purpose |
|------|-----------------|---------|
| Project setup | SKILL.md §Project Setup | NuGet packages, .fsproj config, RuntimeIdentifier, OpenGL |
| Scene bootstrap | SKILL.md §Minimal game entry point, StrideHelpers.fs | Graphics compositor, camera, light, ground, skybox |
| Entity creation | SKILL.md §Entity creation helpers, SceneBuilder.fs | Create3DPrimitive with colors |
| Input handling | SKILL.md §Critical F# Interop, InputHandler.fs | Vector3 arithmetic, mouse/keyboard |
| Build/test | SKILL.md §Building & Testing | StrideCompilerSkipBuild, glslangValidator |
| Linux setup | SKILL.md §Linux prerequisites | FreeImage, GLSL compiler, system packages |

### fsGRPC Skills Usage

| Task | Skill | Purpose |
|------|-------|---------|
| Extend proto contract | `/fsgrpc-proto` | Add StreamViewCommands RPC to physics_hub.proto |
| Implement gRPC client | `/fsgrpc-client` | Server streaming client for StreamState and StreamViewCommands |

## Complexity Tracking

No constitution violations to justify. The design adds one new project (PhysicsViewer) and one new test project (PhysicsViewer.Tests), following the established pattern. Server-side changes are additive (one new function, one new RPC override) with no breaking changes.

The background web host for ServiceDefaults is a minor adaptation, not a violation — it provides the same observability surface as other services.
