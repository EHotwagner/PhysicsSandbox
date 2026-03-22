# Implementation Plan: Scripting Library NuGet Package

**Branch**: `004-scripting-nuget-package` | **Date**: 2026-03-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-scripting-nuget-package/spec.md`

## Summary

Publish PhysicsSandbox.Shared.Contracts, PhysicsSandbox.ServiceDefaults, PhysicsClient, and PhysicsSandbox.Scripting as local NuGet packages following the established BepuFSharp pattern. Migrate consumer projects (MCP server, test project) from ProjectReferences to PackageReferences. Convert all F# script/demo DLL paths to version-agnostic `#r "nuget: ..."` references. Fix port inconsistencies (replace `localhost:5000` with canonical ports 5180/7180) across all scripts and documentation.

## Technical Context

**Language/Version**: F# on .NET 10.0 (PhysicsClient, Scripting), C# on .NET 10.0 (Contracts, ServiceDefaults)
**Primary Dependencies**: Grpc.Net.Client 2.x, Google.Protobuf 3.x, Grpc.AspNetCore 2.x, Spectre.Console 0.49.x, OpenTelemetry 1.14.x, Microsoft.Extensions.ServiceDiscovery 10.1.0
**Storage**: N/A (local NuGet feed at `~/.local/share/nuget-local/`)
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x (19 scripting tests, 42 integration tests)
**Target Platform**: Linux (container with GPU passthrough)
**Project Type**: Library packaging + reference migration
**Performance Goals**: N/A (build/packaging workflow)
**Constraints**: Must follow BepuFSharp local NuGet pattern; pack with `-p:NoWarn=NU5104`
**Scale/Scope**: 4 packages to publish, 2 consumer projects to migrate, ~30 scripts/demos to update

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | No cross-service state changes; packaging is orthogonal to service boundaries |
| II. Contract-First | PASS | Shared.Contracts (proto files) already exists; packaging it preserves the contract-first model |
| III. Shared Nothing (Except Contracts) | PASS | Constitution explicitly states: "For multi-repo scenarios, this project is published as a NuGet package." This feature formalizes that for local consumption |
| IV. Spec-First Delivery | PASS | Spec and plan completed before implementation |
| V. Compiler-Enforced Structural Contracts | PASS | All F# public modules already have .fsi files; packaging doesn't change public API surfaces |
| VI. Test Evidence | PASS | Existing tests (19 scripting + 42 integration) serve as regression validation |
| VII. Observability by Default | PASS | ServiceDefaults package preserves OpenTelemetry/health check infrastructure |
| Engineering: Every library MUST be packable | PASS | This feature directly implements this constraint for 4 projects |

**Pre-design gate: PASS** — No violations. Constitution engineering constraint "Every dotnet project that produces a library MUST be packable via `dotnet pack`" directly mandates this work.

## Project Structure

### Documentation (this feature)

```text
specs/004-scripting-nuget-package/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (files modified by this feature)

```text
src/
├── PhysicsSandbox.Shared.Contracts/
│   └── PhysicsSandbox.Shared.Contracts.csproj  # Add packaging metadata
├── PhysicsSandbox.ServiceDefaults/
│   └── PhysicsSandbox.ServiceDefaults.csproj   # Add packaging metadata
├── PhysicsClient/
│   └── PhysicsClient.fsproj                    # ProjectRef→PackageRef for Contracts, ServiceDefaults
├── PhysicsSandbox.Scripting/
│   └── PhysicsSandbox.Scripting.fsproj         # ProjectRef→PackageRef for PhysicsClient
├── PhysicsSandbox.Mcp/
│   └── PhysicsSandbox.Mcp.fsproj               # ProjectRef→PackageRef for Scripting
│
Scripting/
├── scripts/
│   └── Prelude.fsx                              # #r "nuget: PhysicsSandbox.Scripting" + port fix
├── demos/
│   ├── Prelude.fsx                              # #r "nuget: ..." + port fix
│   ├── AutoRun.fsx                              # #r "nuget: ..." + port fix
│   └── Demo*.fsx                                # Port fixes
└── demos_py/
    ├── prelude.py                               # Port fix (5000→5180)
    ├── auto_run.py                              # Port fix
    └── run_all.py                               # Port fix

tests/
└── PhysicsSandbox.Scripting.Tests/
    └── PhysicsSandbox.Scripting.Tests.fsproj    # ProjectRef→PackageRef for Scripting

~/.local/share/nuget-local/                      # Published packages destination
```

**Structure Decision**: No new directories or projects created. This feature modifies existing project files, script files, and publishes packages to the pre-existing local NuGet feed.

## Packaging Dependency Order

```text
Layer 0 (no internal deps):
  PhysicsSandbox.Shared.Contracts  ──┐
  PhysicsSandbox.ServiceDefaults   ──┤
                                     ▼
Layer 1:
  PhysicsClient (depends on Layer 0) ─┐
                                      ▼
Layer 2:
  PhysicsSandbox.Scripting (depends on PhysicsClient)
```

Pack and publish must proceed top-to-bottom. Each layer must be in the local feed before the next layer can resolve its PackageReferences.

## Reference Migration Strategy

**Within the packaging chain** (projects being packaged):
- PhysicsClient: ProjectRef to Contracts/ServiceDefaults → PackageRef
- Scripting: ProjectRef to PhysicsClient → PackageRef

**Consumer projects** (using the packages):
- PhysicsSandbox.Mcp: ProjectRef to Scripting → PackageRef (remove direct refs to PhysicsClient/Contracts/ServiceDefaults if now transitive)
- PhysicsSandbox.Scripting.Tests: ProjectRef to Scripting → PackageRef

**Unchanged** (service projects keep ProjectReferences for local development):
- PhysicsServer, PhysicsSimulation, PhysicsViewer → keep ProjectRefs to Contracts/ServiceDefaults
- PhysicsSandbox.AppHost → keep existing references

## Port Canonicalization

| File/Pattern | Current | Target |
|-------------|---------|--------|
| Scripting/demos/Prelude.fsx + AutoRun.fsx | `http://localhost:5000` | `http://localhost:5180` |
| Scripting/demos/Demo11-15*.fsx | `http://localhost:5000` | `http://localhost:5180` |
| Scripting/demos_py/prelude.py | `http://localhost:5000` | `http://localhost:5180` |
| Scripting/demos_py/auto_run.py, run_all.py | `http://localhost:5000` | `http://localhost:5180` |
| Scripting/scripts/HelloDrop.fsx | `http://localhost:5180` | Already correct |
| src/PhysicsClient/Program.fs | `http://localhost:5000` fallback | `http://localhost:5180` |
| src/PhysicsViewer/Program.fs | `http://localhost:5000` fallback | `http://localhost:5180` |
| src/PhysicsClient/PhysicsClient.fsx | `http://localhost:5000` | `http://localhost:5180` |
| .mcp.json | `http://localhost:5000/sse` | `http://localhost:5180/sse` |

**Note**: `https://localhost:7180` references (MCP server default, getting-started docs) are already correct for the HTTPS profile.

## Complexity Tracking

No constitution violations — table not applicable.
