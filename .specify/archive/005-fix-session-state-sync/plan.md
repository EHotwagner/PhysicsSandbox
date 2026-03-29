# Implementation Plan: Fix Session State and Cache Synchronization

**Branch**: `005-fix-session-state-sync` | **Date**: 2026-03-29 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-fix-session-state-sync/spec.md`

## Summary

Fix the broken reset-and-recreate workflow in the scripting library. The core issue is that `resetSimulation` returns before the server finishes processing the reset command (fire-and-forget via `submitCommand`), causing ID collisions, stale query results, and silent batch failures. The fix adds a confirmed reset path using the existing query-response infrastructure, clears client caches eagerly, and surfaces batch operation results to callers.

## Technical Context

**Language/Version**: F# on .NET 10.0 (PhysicsClient, Scripting), C# on .NET 10.0 (Contracts, integration tests)
**Primary Dependencies**: Grpc.Net.Client 2.x, Google.Protobuf 3.x, Grpc.AspNetCore.Server 2.x, xUnit 2.x, Aspire.Hosting.Testing 10.x
**Storage**: N/A (in-memory state only)
**Testing**: xUnit (unit + integration via Aspire DistributedApplicationTestingBuilder)
**Target Platform**: Linux (container with GPU passthrough)
**Project Type**: Library (PhysicsClient) + Library (PhysicsSandbox.Scripting)
**Performance Goals**: Reset-and-recreate cycle completes in < 500ms for typical workloads
**Constraints**: Must not break existing demo scripts, NuGet package API, or MCP tool behavior
**Scale/Scope**: 4 source files changed, 2 new proto messages, ~6 affected modules

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | Changes confined to client-side libraries and one proto contract extension. No cross-service state sharing introduced. |
| II. Contract-First | PASS | New proto messages for confirmed reset defined before implementation. |
| III. Shared Nothing | PASS | Only `PhysicsSandbox.Shared.Contracts` is shared (existing pattern). |
| IV. Spec-First Delivery | PASS | Spec written and approved before planning. |
| V. Compiler-Enforced Structural Contracts | PASS | All changed public modules have `.fsi` files that will be updated. Surface area baselines will be updated. |
| VI. Test Evidence | PASS | Integration tests planned for reset reliability. Unit tests for batch result reporting. |
| VII. Observability | PASS | Reset confirmation includes diagnostic logging. Batch failures surfaced (no longer silent). |

## Project Structure

### Documentation (this feature)

```text
specs/005-fix-session-state-sync/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (from /speckit.tasks)
```

### Source Code (repository root)

```text
src/
  PhysicsSandbox.Shared.Contracts/
    Protos/physics_hub.proto          # New ResetSimulation RPC + ConfirmedResetResponse
  PhysicsServer/
    Services/PhysicsHubService.fs     # Implement ConfirmedReset RPC handler
    Hub/MessageRouter.fs              # Add confirmed reset path (wait for simulation processing)
    Hub/MessageRouter.fsi             # Signature update
  PhysicsClient/
    Commands/SimulationCommands.fs    # Add confirmedReset, update clearAll to also clear BodyPropertiesCache
    Commands/SimulationCommands.fsi   # Signature update
    Connection/Session.fs             # Add clearCaches helper
    Connection/Session.fsi            # Signature update
  PhysicsSandbox.Scripting/
    SimulationLifecycle.fs            # Use confirmed reset path
    SimulationLifecycle.fsi           # No signature change (resetSimulation stays unit)
    BatchOperations.fs                # Return BatchResult list instead of unit
    BatchOperations.fsi               # Signature change: batchAdd returns BatchResult list

tests/
  PhysicsClient.Tests/
    SimulationCommandsTests.fs        # Tests for confirmed reset, cache clearing
  PhysicsSandbox.Scripting.Tests/
    BatchOperationsTests.fs           # Tests for result reporting
    SurfaceAreaTests.fs               # Updated baselines
  PhysicsSandbox.Integration.Tests/
    ResetReliabilityTests.cs          # New: end-to-end reset + recreate cycle
```

**Structure Decision**: Changes span existing projects. No new projects introduced. The confirmed reset RPC is the only new proto contract.

## Complexity Tracking

No constitution violations. No complexity justifications needed.
