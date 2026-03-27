# Implementation Plan: Contracts gRPC Package Cleanup

**Branch**: `005-contracts-grpc-cleanup` | **Date**: 2026-03-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-contracts-grpc-cleanup/spec.md`

## Summary

Replace the overly broad `Grpc.AspNetCore` package reference in `PhysicsSandbox.Shared.Contracts` with the narrower `Grpc.Net.Client`. The Contracts project only needs proto compilation (already has `Grpc.Tools`) and client stub types (`Grpc.Net.Client`) — it does not host a gRPC server. The only server-side project (PhysicsServer) already has its own direct `Grpc.AspNetCore.Server` reference.

## Technical Context

**Language/Version**: C# on .NET 10.0 (Shared.Contracts is the only C# project affected)
**Primary Dependencies**: Grpc.Net.Client 2.*, Google.Protobuf 3.*, Grpc.Tools 2.* (PrivateAssets=All)
**Storage**: N/A
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x (existing suite — 468 tests across 7 test projects)
**Target Platform**: .NET 10.0 (Linux container + dev workstation)
**Project Type**: Shared library (NuGet-packable, version 0.5.0)
**Performance Goals**: N/A (build-time change only)
**Constraints**: Zero test regressions, zero build errors across entire solution
**Scale/Scope**: Single file change (`PhysicsSandbox.Shared.Contracts.csproj`)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | No cross-service coupling change |
| II. Contract-First | PASS | Proto file unchanged, contracts unaffected |
| III. Shared Nothing | PASS | Contracts project remains the only shared artifact |
| IV. Spec-First Delivery | PASS | Spec written, plan in progress |
| V. Compiler-Enforced Structural Contracts | N/A | No F# public modules changed, no .fsi changes needed |
| VI. Test Evidence | PASS | Existing test suite validates the change — all 468 tests must pass |
| VII. Observability | N/A | No runtime behavior changes |

**Engineering Constraints**:
- Dependencies minimized: Removing unnecessary `Grpc.AspNetCore` (bundles server+client+tools) in favor of the narrower `Grpc.Net.Client` (client only). Net reduction in transitive dependencies.
- Library must remain packable: `dotnet pack` must succeed.
- No public API surface change: Generated proto types are identical regardless of which gRPC package provides the client stubs.

**Gate result**: PASS — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/005-contracts-grpc-cleanup/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (minimal — no data model changes)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
└── PhysicsSandbox.Shared.Contracts/
    ├── PhysicsSandbox.Shared.Contracts.csproj  # MODIFIED: swap Grpc.AspNetCore → Grpc.Net.Client
    └── Protos/
        └── physics_hub.proto                    # UNCHANGED
```

**Structure Decision**: No new files or directories. Single `.csproj` modification in the existing Contracts project.

## Complexity Tracking

No constitution violations — table not needed.
