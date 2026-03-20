# Implementation Plan: Client REPL Library

**Branch**: `004-client-repl` | **Date**: 2026-03-20 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-client-repl/spec.md`

## Summary

Build the PhysicsClient library — an F# library loadable in FSI that provides a comprehensive, REPL-friendly API for controlling the physics simulation and 3D viewer. The library wraps the existing PhysicsHub gRPC service with ergonomic functions for body creation (including presets and random generators), steering, state querying with Spectre.Console formatted output, and viewer control. Uses contract-first gRPC (standard Grpc.Net.Client against the existing proto-generated C# types in PhysicsSandbox.Shared.Contracts).

## Technical Context

**Language/Version**: F# on .NET 10.0
**Primary Dependencies**: Grpc.Net.Client 2.x, Google.Protobuf 3.x, Spectre.Console (latest, for formatted tables/live display), PhysicsSandbox.Shared.Contracts (proto-generated C# types)
**Storage**: N/A (stateless client; in-memory body ID registry per session)
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x
**Target Platform**: Linux x64 (FSI / F# scripts)
**Project Type**: Library (loadable in FSI, also registered as Aspire project for orchestrated runs)
**Performance Goals**: Commands should feel instant (<100ms round-trip to local server)
**Constraints**: Must work in FSI without building full application; Result-based error handling (no unhandled exceptions)
**Scale/Scope**: Single-user REPL sessions against a local server; up to ~100 bodies typical

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | Library is a standalone client project. Communicates only via gRPC through PhysicsHub. No shared mutable state. |
| II. Contract-First | PASS | Uses existing proto contracts in PhysicsSandbox.Shared.Contracts. No new proto changes needed — all commands already defined. |
| III. Shared Nothing | PASS | Only references Shared.Contracts (the permitted shared artifact). No direct project references to other services. |
| IV. Spec-First Delivery | PASS | Full spec → plan → tasks workflow. |
| V. Compiler-Enforced Structural Contracts | PASS | All public F# modules will have `.fsi` signature files. Surface area baseline tests will be added. |
| VI. Test Evidence | PASS | Unit tests for all pure functions (presets, steering math, formatting). Integration tests via Aspire for connection and command round-trips. |
| VII. Observability | PASS | References ServiceDefaults for structured logging via ILogger. The library is a client, not a hosted service, but logs connection events and errors. |

## Project Structure

### Documentation (this feature)

```text
specs/004-client-repl/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
  PhysicsClient/
    PhysicsClient.fsproj            # Library project (net10.0)
    Connection/
      Session.fsi                   # Session type + connect/disconnect/reconnect
      Session.fs
    Commands/
      SimulationCommands.fsi        # All simulation command wrappers
      SimulationCommands.fs
      ViewCommands.fsi              # Camera, zoom, wireframe wrappers
      ViewCommands.fs
    Bodies/
      Presets.fsi                   # Ready-made body presets (bowlingBall, crate, etc.)
      Presets.fs
      Generators.fsi                # Random body generators + scene builders
      Generators.fs
      IdGenerator.fsi               # Auto-incrementing human-readable ID generation
      IdGenerator.fs
    Steering/
      Steering.fsi                  # Push, launch, spin, stop
      Steering.fs
    Display/
      StateDisplay.fsi              # Formatted state output (Spectre.Console tables)
      StateDisplay.fs
      LiveWatch.fsi                 # Cancellable live-watch mode
      LiveWatch.fs

tests/
  PhysicsClient.Tests/
    PhysicsClient.Tests.fsproj
    IdGeneratorTests.fs
    PresetsTests.fs
    GeneratorsTests.fs
    SteeringTests.fs
    StateDisplayTests.fs
    SessionTests.fs
    SurfaceAreaTests.fs
```

**Structure Decision**: Single F# library project with logical module folders mirroring the functional domains. The library references Shared.Contracts and ServiceDefaults. Tests are a separate xUnit project. The library is also added to AppHost for Aspire orchestration (though it can run standalone from FSI).

## Complexity Tracking

No constitution violations — no entries needed.
