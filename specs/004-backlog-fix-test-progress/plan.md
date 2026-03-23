# Implementation Plan: Backlog Fixes and Test Progress Reporting

**Branch**: `004-backlog-fix-test-progress` | **Date**: 2026-03-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-backlog-fix-test-progress/spec.md`

## Summary

Address prioritized backlog items from the refactoring evaluation: fix 10 silent TryAdd/TryRemove failures in PhysicsClient (returning `Result.Error`), add server-side pending query expiration in MessageRouter (30s timeout with sweep timer), complete all 10 constraint builders in the Scripting library, and consolidate duplicated test helpers. Additionally, create a shell script that runs the test suite with per-project progress reporting, elapsed time, and ETA.

## Technical Context

**Language/Version**: F# on .NET 10.0 (PhysicsClient, PhysicsServer, Scripting), C# on .NET 10.0 (Integration Tests), Bash (test progress script)
**Primary Dependencies**: xUnit 2.x, Aspire.Hosting.Testing 10.x, Grpc.Net.Client 2.x, Google.Protobuf 3.x
**Storage**: N/A (in-memory ConcurrentDictionary for pending queries)
**Testing**: xUnit 2.x via `dotnet test`, 362 tests across 7 projects
**Target Platform**: Linux (container with GPU passthrough)
**Project Type**: Distributed simulation platform (gRPC services + libraries + scripts)
**Performance Goals**: Query expiration sweep <1ms per scan. Test progress script adds <2s overhead per project.
**Constraints**: No breaking changes to public API signatures (.fsi files). All 362 existing tests must continue passing.
**Scale/Scope**: 10 TryAdd/TryRemove fixes, 1 new type (PendingQueryEntry), 6 new constraint builders, ~520 LOC deduplication, 1 new shell script

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | **PASS** | Changes are within individual service boundaries. No cross-service shared state introduced. |
| II. Contract-First | **PASS** | No new proto contracts needed. Existing proto constraint types already defined. |
| III. Shared Nothing | **PASS** | No new cross-service references. Shared test helpers are test-only, not production code. |
| IV. Spec-First Delivery | **PASS** | This plan follows spec → plan → tasks workflow. |
| V. Compiler-Enforced Structural Contracts | **PASS** | New constraint builders require `.fsi` updates. PendingQueryEntry added to MessageRouter. Updated surface area baselines required. |
| VI. Test Evidence | **PASS** | Each story includes tests: unit tests for error handling, query expiration, constraint builders; surface area tests for API completeness. |
| VII. Observability by Default | **PASS** | Silent failures replaced with explicit errors (Principle VII prohibits swallowed exceptions). MeshResolver/Session cache ops get structured log warnings. |

**Post-Phase-1 Re-check**: All gates still pass. No new projects created. No cross-service dependencies. `.fsi` files updated for all public API changes.

## Project Structure

### Documentation (this feature)

```text
specs/004-backlog-fix-test-progress/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
# New files
test-progress.sh                                              # Bash test runner with progress

# Modified files — PhysicsClient (Story 2: silent failures)
src/PhysicsClient/Commands/SimulationCommands.fs              # Fix 7 TryAdd/TryRemove → Result.Error
src/PhysicsClient/Commands/SimulationCommands.fsi             # No changes (signatures already return Result)
src/PhysicsClient/Connection/MeshResolver.fs                  # Fix 2 TryAdd → log warning
src/PhysicsClient/Connection/Session.fs                       # Fix 1 TryRemove → log warning

# Modified files — PhysicsServer (Story 3: query expiration)
src/PhysicsServer/Hub/MessageRouter.fs                        # PendingQueryEntry wrapper, sweep timer
src/PhysicsServer/Hub/MessageRouter.fsi                       # Expose PendingQueryEntry type (internal)

# Modified files — Scripting (Story 4: constraint builders)
src/PhysicsSandbox.Scripting/ConstraintBuilders.fs            # Add 6 new builders + defaultMotor helper
src/PhysicsSandbox.Scripting/ConstraintBuilders.fsi           # Add 6 new builder signatures

# Modified files — Tests (Story 5: shared helpers)
tests/PhysicsServer.Tests/MeshCacheTests.fs                   # Remove getPublicMembers, use shared
tests/PhysicsSimulation.Tests/SurfaceAreaTests.fs             # Remove getPublicMembers, use shared
tests/PhysicsViewer.Tests/SurfaceAreaTests.fs                 # Remove getPublicMembers, use shared
tests/PhysicsClient.Tests/SurfaceAreaTests.fs                 # Remove getPublicMembers + assertContains
tests/PhysicsSandbox.Mcp.Tests/SurfaceAreaTests.fs            # Remove getPublicMembers, use shared
tests/PhysicsSandbox.Scripting.Tests/SurfaceAreaTests.fs      # Remove getPublicMembers + assertContains
tests/PhysicsSandbox.Integration.Tests/IntegrationTestHelpers.cs  # New: shared C# helpers
tests/PhysicsSandbox.Integration.Tests/*.cs                   # 14 files: use shared helpers

# New test files
tests/PhysicsServer.Tests/QueryExpirationTests.fs             # Tests for pending query timeout
tests/PhysicsClient.Tests/RegistryErrorTests.fs               # Tests for TryAdd/TryRemove error returns
tests/PhysicsSandbox.Scripting.Tests/ConstraintBuilderTests.fs # Tests for 6 new builders
```

**Structure Decision**: No new projects. All changes fit within existing project boundaries. F# shared test helpers go into each test project as a shared source file (linked via `<Compile Include>`) to avoid a new test utilities project. C# helpers consolidate within the existing integration test project.

## Design Decisions

### D1: TryAdd/TryRemove Error Strategy (Two-Tier)

**Body registry operations** (SimulationCommands.fs, instances 1-7): Check return value, return `Result.Error "Body '{id}' already exists in registry"` or `"Body '{id}' not found in registry"`. These are user-facing API calls.

**Cache operations** (MeshResolver.fs instances 8-9, Session.fs instance 10): Log a structured warning via `System.Diagnostics.Trace.TraceWarning` (existing pattern in codebase). Cache duplicates are expected (e.g., mesh already cached from prior fetch). Changing these to Result would require `.fsi` signature changes for no user benefit.

### D2: Query Expiration Implementation

Add a `PendingQueryEntry` record wrapping `TaskCompletionSource<QueryResponse>` with a `CreatedAt: DateTime` field. Replace the dictionary value type. Add a `System.Threading.Timer` that sweeps every 10 seconds, calling `TrySetException(TimeoutException)` on entries older than 30 seconds and removing them. Timer starts with `MessageRouter.create` and disposes with the router.

The 30s timeout is generous — typical query round-trips are <100ms. The 10s sweep interval means worst-case an entry lives 40s, which is acceptable.

### D3: Test Progress Script Design

Bash script that:
1. Parses `.slnx` for test project paths
2. Runs each project via `dotnet test <project> -p:StrideCompilerSkipBuild=true --no-build` (after initial solution build)
3. Parses the `Passed: X, Failed: Y, Skipped: Z` summary line from stdout
4. Displays: `[3/7] PhysicsClient.Tests ✓ 56 passed (12s) | ETA: ~24s`
5. On failure: shows failed test names immediately
6. Final summary: total pass/fail/skip, wall time

Build once upfront (`dotnet build`), then `--no-build` per project to avoid redundant compilation.

### D4: F# Shared Test Helpers Approach

Rather than a new project (which would need solution file changes and cross-project references), use a **shared source file** approach:
- Create `tests/SharedTestHelpers.fs` with `getPublicMembers` and `assertContains`
- Each F# test `.fsproj` adds `<Compile Include="../SharedTestHelpers.fs" Link="SharedTestHelpers.fs" />`
- Functions compile into each test assembly independently (no runtime dependency)

This avoids a new project while eliminating duplication. The functions are trivial (5 lines each) so the compiled size overhead is negligible.

### D5: Constraint Builder Pattern

Follow existing `make<Type>Cmd` pattern exactly. Add a `defaultMotor` helper (analogous to `defaultSpring`) for motor-based constraints (LinearAxisMotor, AngularMotor) with sensible defaults (maxForce=1000.0, damping=1.0). All 6 new builders return `SimulationCommand`, take `id: string -> bodyA: string -> bodyB: string -> ...` as the first three parameters.
