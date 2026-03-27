# Implementation Plan: Fix FSI Assembly Version Mismatch

**Branch**: `004-fix-fsi-assembly-mismatch` | **Date**: 2026-03-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-fix-fsi-assembly-mismatch/spec.md`

## Summary

PhysicsClient's NuGet package records a transitive dependency on Microsoft.Extensions.Logging.Abstractions >= 8.0.0 (via ServiceDefaults). FSI on .NET 10 can't resolve the 8.0.0.0 assembly version because the runtime only ships 10.x and FSI doesn't apply binding redirects. Fix by adding an explicit PackageReference to Microsoft.Extensions.Logging.Abstractions 10.* in PhysicsClient.fsproj, bumping the package version to 0.5.0, and cleaning up the Prelude.fsx workaround.

## Technical Context

**Language/Version**: F# on .NET 10.0 (PhysicsClient, Scripting), C# on .NET 10.0 (ServiceDefaults, Contracts)
**Primary Dependencies**: Grpc.Net.Client 2.x, Google.Protobuf 3.x, Spectre.Console 0.49.x, Microsoft.Extensions.Logging.Abstractions 10.x (new explicit)
**Storage**: N/A (no storage changes)
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x, manual FSI script execution
**Target Platform**: Linux container with .NET 10 SDK
**Project Type**: Library (NuGet package consumed by FSI scripts)
**Performance Goals**: N/A (dependency resolution fix, no runtime perf impact)
**Constraints**: Must remain compatible with .NET 10 shared framework; must not break compiled apps
**Scale/Scope**: ~6 files changed, 1 dependency added, version bumps

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | No cross-service changes. PhysicsClient is an independent library. |
| II. Contract-First | PASS | No contract changes. Proto files unchanged. |
| III. Shared Nothing | PASS | Only shared artifact is Contracts (unchanged). |
| IV. Spec-First Delivery | PASS | Spec and plan created before implementation. |
| V. Compiler-Enforced Contracts | PASS | No public API changes — .fsi files unchanged. No surface area baseline changes needed. |
| VI. Test Evidence | PASS | Will verify with integration tests and manual FSI script execution. |
| VII. Observability | PASS | No observability changes. |

**Engineering Constraints**:
- Dependencies minimized: Adding one explicit dependency (Microsoft.Extensions.Logging.Abstractions) that was already transitive. No new dependency, just version pinning.
- Packable via `dotnet pack`: Already the case, no changes needed.

**Post-Phase 1 Re-check**: All gates still pass. No new public API surface, no contract changes.

## Project Structure

### Documentation (this feature)

```text
specs/004-fix-fsi-assembly-mismatch/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Root cause analysis and fix decision
├── data-model.md        # Dependency graph changes
├── quickstart.md        # Verification steps
└── tasks.md             # (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── PhysicsClient/
│   └── PhysicsClient.fsproj          # Add PackageReference, bump version
├── PhysicsSandbox.Shared.Contracts/
│   └── PhysicsSandbox.Shared.Contracts.csproj  # Bump version
└── PhysicsSandbox.ServiceDefaults/
    └── PhysicsSandbox.ServiceDefaults.csproj    # Bump version

Scripting/
├── demos/
│   └── Prelude.fsx                   # Remove Logging.Abstractions ref, update version
└── scripts/
    └── PhysicsClient.fsx             # Update version reference

CLAUDE.md                             # Update version references
```

**Structure Decision**: No new files or directories. All changes are to existing project files, script files, and documentation.

## Complexity Tracking

No constitution violations. No complexity justifications needed.
