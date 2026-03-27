# Implementation Plan: PhysicsClient Exe vs Library Conversion

**Branch**: `006-client-exe-analysis` | **Date**: 2026-03-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-client-exe-analysis/spec.md`

## Summary

PhysicsClient currently builds as an executable and runs as an Aspire-managed service, but its entry point does zero useful work — it just stays alive for dashboard visibility. All value is delivered through library consumption (Scripting, tests, F# scripts via NuGet). This plan converts PhysicsClient to a library-only project, removes it from AppHost orchestration, and cleans up the unnecessary ServiceDefaults dependency.

## Technical Context

**Language/Version**: F# on .NET 10.0
**Primary Dependencies**: Grpc.Net.Client 2.x, Google.Protobuf 3.x, Spectre.Console 0.49.x, Microsoft.Extensions.Logging.Abstractions 10.x
**Storage**: N/A
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x
**Target Platform**: Linux (container with GPU passthrough)
**Project Type**: Library (converting from executable)
**Performance Goals**: N/A (no runtime behavior changes)
**Constraints**: Must maintain full backward compatibility for all library consumers; NuGet version bump required
**Scale/Scope**: 5 files modified, 1 file deleted, 1 NuGet repackage

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | PhysicsClient is a client library, not a service. Removing its service persona aligns better with this principle. |
| II. Contract-First | PASS | No contract changes. PhysicsClient consumes Contracts, doesn't define them. |
| III. Shared Nothing | PASS | Removing AppHost project reference to PhysicsClient reduces cross-project coupling. |
| IV. Spec-First Delivery | PASS | This spec and plan exist. |
| V. Compiler-Enforced Structural Contracts | PASS | All .fsi files unchanged. No public API surface changes. |
| VI. Test Evidence | PASS | Existing tests continue to pass. No behavior change to test. |
| VII. Observability by Default | PASS | ServiceDefaults removal from PhysicsClient is safe — library consumers don't need Aspire telemetry wired at the client library level. Services that use PhysicsClient (like Scripting) have their own observability. |

**Post-Phase 1 re-check**: All gates still pass. The design removes complexity without introducing new coupling.

## Project Structure

### Documentation (this feature)

```text
specs/006-client-exe-analysis/
├── plan.md              # This file
├── research.md          # Analysis findings and recommendation
├── data-model.md        # Dependency graph changes
├── quickstart.md        # Implementation guide
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
  PhysicsClient/
    PhysicsClient.fsproj       # MODIFY: OutputType Exe→Library, remove ServiceDefaults ref, bump version
    Program.fs                 # DELETE: no-op entry point
  PhysicsSandbox.AppHost/
    AppHost.cs                 # MODIFY: remove client resource (lines 15-17)
    PhysicsSandbox.AppHost.csproj  # MODIFY: remove PhysicsClient project reference
Scripting/
  demos/Prelude.fsx            # MODIFY: pin PhysicsClient 0.6.0
```

**Structure Decision**: No new files or directories. This is a simplification — removing an unnecessary entry point and service registration.

## Complexity Tracking

No constitution violations. This change reduces complexity.
