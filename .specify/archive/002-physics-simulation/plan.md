# Implementation Plan: Physics Simulation Service

**Branch**: `002-physics-simulation` | **Date**: 2026-03-20 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-physics-simulation/spec.md`

## Summary

Add a physics simulation service that connects to the existing PhysicsServer hub via the `SimulationLink` bidirectional gRPC stream. The service wraps BepuFSharp (BepuPhysics2 F# wrapper) to provide rigid body dynamics with lifecycle control (play/pause/step), body management (add/remove), force application (persistent forces, one-shot impulses, torques), and gravity. State is streamed to the server after every simulation step. The existing proto contracts are extended with new command types (RemoveBody, ApplyImpulse, ApplyTorque, ClearForces) and new Body fields (angular_velocity, orientation).

## Technical Context

**Language/Version**: F# on .NET 10.0 (simulation service), C# on .NET 10.0 (proto contracts)
**Primary Dependencies**: BepuFSharp 0.1.0 (local NuGet), Grpc.Net.Client, PhysicsSandbox.Shared.Contracts, PhysicsSandbox.ServiceDefaults
**Storage**: N/A (in-memory physics world only)
**Testing**: xUnit 2.x (unit tests), Aspire.Hosting.Testing 10.x (integration tests)
**Target Platform**: Linux (Aspire-managed .NET service)
**Project Type**: Background worker service (gRPC client)
**Performance Goals**: 60Hz fixed-step simulation, stable with 100+ bodies
**Constraints**: Must connect to server within 5 seconds; state streamed every step with zero missed
**Scale/Scope**: Single simulation instance, single server connection

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | PhysicsSimulation is independently deployable; communicates only via gRPC |
| II. Contract-First | PASS | Proto extensions defined before implementation (see `contracts/proto-extensions.md`) |
| III. Shared Nothing | PASS | Only shared artifact is `PhysicsSandbox.Shared.Contracts`; BepuFSharp via NuGet package, not project reference |
| IV. Spec-First Delivery | PASS | Full spec → plan → tasks workflow |
| V. Compiler-Enforced Contracts | PASS | `.fsi` signature files planned for all public modules (see `contracts/simulation-modules.md`) |
| VI. Test Evidence | PASS | Unit tests for physics logic, integration tests via Aspire |
| VII. Observability by Default | PASS | Uses `ServiceDefaults` for OpenTelemetry, health checks, structured logging |

**Post-Phase-1 Re-check**: All principles remain satisfied. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/002-physics-simulation/
├── plan.md              # This file
├── research.md          # Phase 0: technology decisions
├── data-model.md        # Phase 1: entity model and command mapping
├── quickstart.md        # Phase 1: setup and build instructions
├── contracts/
│   ├── proto-extensions.md     # Proto contract changes
│   └── simulation-modules.md  # F# module signatures
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── PhysicsSandbox.AppHost/              # MODIFIED: add simulation project registration
│   └── AppHost.cs
├── PhysicsSandbox.Shared.Contracts/     # MODIFIED: extend proto with new commands/fields
│   └── Protos/
│       └── physics_hub.proto
├── PhysicsSimulation/                   # NEW: F# simulation service
│   ├── PhysicsSimulation.fsproj
│   ├── Program.fs                       # Entry point, Aspire service defaults, run client
│   ├── World/
│   │   ├── SimulationWorld.fsi          # Public API signature
│   │   └── SimulationWorld.fs           # BepuFSharp world wrapper
│   ├── Commands/
│   │   ├── CommandHandler.fsi           # Public API signature
│   │   └── CommandHandler.fs            # Command dispatch to world operations
│   └── Client/
│       ├── SimulationClient.fsi         # Public API signature
│       └── SimulationClient.fs          # gRPC client, simulation loop, state streaming
├── PhysicsServer/                       # UNCHANGED (routes commands opaquely)
└── PhysicsSandbox.ServiceDefaults/      # UNCHANGED

tests/
├── PhysicsSimulation.Tests/             # NEW: F# unit tests
│   ├── PhysicsSimulation.Tests.fsproj
│   ├── SimulationWorldTests.fs          # World lifecycle, body management, state readout
│   └── CommandHandlerTests.fs           # Command dispatch, edge cases
├── PhysicsSandbox.Integration.Tests/    # MODIFIED: add simulation integration tests
│   └── SimulationTests.cs              # NEW: end-to-end via Aspire
├── PhysicsServer.Tests/                 # UNCHANGED
```

**Structure Decision**: Follows the existing mono-repo pattern. One new F# service project (`PhysicsSimulation`) with three modules organized by responsibility (World, Commands, Client). One new test project for unit tests. Integration tests added to the existing Aspire test project.

### NuGet Configuration

```text
NuGet.config                             # NEW or MODIFIED: add local feed source
```

### fsGRPC Skills Usage

| Task | Skill | Purpose |
|------|-------|---------|
| Extend proto contract | `/fsgrpc-proto` | Add new messages and oneof variants to physics_hub.proto |
| Implement gRPC client | `/fsgrpc-client` | Bidirectional streaming client for SimulationLink |

## Complexity Tracking

No constitution violations to justify. The design adds one new project (PhysicsSimulation) and one new test project (PhysicsSimulation.Tests), following the established pattern from spec 001.
