# Implementation Plan: State Stream Bandwidth Optimization

**Branch**: `004-state-stream-optimization` | **Date**: 2026-03-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-state-stream-optimization/spec.md`

## Summary

Split the monolithic 60 Hz `SimulationState` broadcast into two data channels: a lean **continuous stream** (position, orientation, optional velocity) for dynamic bodies only, and the existing **bidirectional channel** for semi-static properties (shape, color, mass, material, collision filters, motion type), body lifecycle events (creation, removal, motion type transitions), constraints, and registered shapes. This reduces per-tick payload from ~50 KB to ~11 KB for 200 bodies (~78% reduction), with the viewer achieving ~80% reduction by opting out of velocity fields.

## Technical Context

**Language/Version**: F# on .NET 10.0 (services, MCP, client), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts, integration tests)
**Primary Dependencies**: .NET Aspire 13.1.3, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, BepuFSharp 0.2.0-beta.1, Stride.CommunityToolkit 1.0.0-preview.62, ModelContextProtocol.AspNetCore 1.1.*, Spectre.Console
**Storage**: In-memory (physics world, mesh caches, state caches). Append-only protobuf binary files for MCP recordings.
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x. F# unit tests + C# integration tests.
**Target Platform**: Linux container with GPU passthrough
**Project Type**: Distributed microservices (Aspire-orchestrated)
**Performance Goals**: 60 Hz tick stream, >=70% bandwidth reduction at 200 bodies, >=80% for viewer
**Constraints**: No breaking changes to existing demo scripts or MCP tools. All 4 client types must reconstruct full state from split channels.
**Scale/Scope**: 4-6 concurrent clients, 100-500+ bodies typical, 6 affected projects + integration tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | No new shared mutable state. Communication remains via gRPC. |
| II. Contract-First | PASS | Proto contract changes designed before implementation. New messages defined in `PhysicsSandbox.Shared.Contracts`. |
| III. Shared Nothing | PASS | Only shared artifact remains `Shared.Contracts`. No new cross-project references. |
| IV. Spec-First Delivery | PASS | Spec complete with clarifications. Plan precedes implementation. |
| V. Compiler-Enforced Structural Contracts | PASS | All changed public F# modules will have updated `.fsi` signature files. Surface area baselines updated. |
| VI. Test Evidence | PASS | Unit tests for new serialization logic, integration tests for split-channel correctness, surface area tests for API changes. |
| VII. Observability by Default | PASS | Existing OpenTelemetry/metrics infrastructure. MetricsCounter will separately track continuous vs semi-static messages. |

No violations. Complexity tracking not needed.

## Project Structure

### Documentation (this feature)

```text
specs/004-state-stream-optimization/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (proto changes)
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── PhysicsSandbox.Shared.Contracts/
│   └── Protos/physics_hub.proto           # New messages: BodyPose, TickState, BodyProperties, PropertyEvent, PropertySnapshot
├── PhysicsServer/
│   ├── Hub/MessageRouter.fs/.fsi          # Split publishState into publishTick + publishProperties
│   ├── Hub/StateCache.fs/.fsi             # Dual cache: tick state + semi-static state
│   ├── Hub/MetricsCounter.fs/.fsi         # Separate continuous vs semi-static metrics
│   └── Services/
│       ├── PhysicsHubService.fs/.fsi      # StreamState sends lean ticks; backfill via bidirectional
│       └── SimulationLinkService.fs/.fsi  # Receives split state from simulation
├── PhysicsSimulation/
│   └── World/SimulationWorld.fs/.fsi      # buildTickState (continuous) + buildPropertyUpdates (semi-static)
├── PhysicsViewer/
│   ├── Streaming/ViewerClient.fs/.fsi     # Consume lean ticks; merge with cached semi-static
│   └── Rendering/SceneManager.fs/.fsi     # applyState merges both channels
├── PhysicsClient/
│   ├── Connection/Session.fs/.fsi         # Maintain local semi-static cache; merge on display
│   └── Display/StateDisplay.fs/.fsi       # Read from merged state
└── PhysicsSandbox.Mcp/
    ├── GrpcConnection.fs/.fsi             # Subscribe to both channels
    └── Recording/RecordingEngine.fs/.fsi  # Reconstruct full state for recording

tests/
├── PhysicsServer.Tests/                   # MessageRouter split logic, StateCache dual mode
├── PhysicsSimulation.Tests/               # buildTickState, buildPropertyUpdates, delta detection
├── PhysicsViewer.Tests/                   # State merging from two channels
├── PhysicsClient.Tests/                   # State merging, surface area updates
├── PhysicsSandbox.Mcp.Tests/              # Recording with split channels
└── PhysicsSandbox.Integration.Tests/      # End-to-end split-channel streaming, late joiner backfill
```

**Structure Decision**: Existing project structure unchanged. No new projects needed. Changes are internal to each existing project — proto contract changes, server-side routing split, client-side state merging.
