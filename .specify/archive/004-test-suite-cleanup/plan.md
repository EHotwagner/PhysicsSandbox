# Implementation Plan: Test Suite Cleanup

**Branch**: `004-test-suite-cleanup` | **Date**: 2026-03-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-test-suite-cleanup/spec.md`

## Summary

Restructure the PhysicsSandbox test suite (~358 tests across 7 projects) to eliminate redundant tests, consolidate fragmented integration test files, extract shared test data builders, and split oversized test files into focused units. This is a behavior-preserving structural cleanup — no new coverage, no behavioral changes.

## Technical Context

**Language/Version**: F# on .NET 10.0 (unit tests), C# on .NET 10.0 (integration tests)
**Primary Dependencies**: xUnit 2.x, Aspire.Hosting.Testing 10.x
**Storage**: N/A
**Testing**: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
**Target Platform**: Linux (container with GPU passthrough)
**Project Type**: Test infrastructure refactoring (no production code changes)
**Performance Goals**: N/A (test suite execution time should not increase)
**Constraints**: F# compilation order matters — shared helpers must appear first in .fsproj `<Compile>` items. SharedTestHelpers.fs is linked via `<Compile Include="../SharedTestHelpers.fs" Link="SharedTestHelpers.fs" />` pattern.
**Scale/Scope**: 7 test projects, ~63 test files, ~463 tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | N/A | No service changes |
| II. Contract-First | N/A | No contract changes |
| III. Shared Nothing | N/A | No cross-service dependency changes |
| IV. Spec-First Delivery | PASS | Spec created and clarified |
| V. Compiler-Enforced Structural Contracts | PASS | Surface area tests preserved (restructured, not removed). No .fsi changes. |
| VI. Test Evidence | PASS | All tests preserved. Behavior-preserving restructuring. Duplicates consolidated, not deleted. |
| VII. Observability | N/A | No service changes |

**Gate result: PASS** — No violations. No complexity tracking needed.

## Project Structure

### Documentation (this feature)

```text
specs/004-test-suite-cleanup/
├── plan.md              # This file
├── research.md          # Phase 0: detailed analysis of duplicates and split points
├── quickstart.md        # Quick reference for implementation
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (affected files)

```text
tests/
├── SharedTestHelpers.fs                          # EXTEND: add surface area assertion helper
├── CommonTestBuilders.fs                         # NEW: shared test data builders (F#)
├── PhysicsServer.Tests/
│   └── StateStreamOptimizationTests.fs           # MODIFY: use CommonTestBuilders
├── PhysicsSimulation.Tests/
│   ├── SimulationWorldTests.fs                   # SPLIT → SimulationWorldBasicsTests.fs + SimulationWorldForcesTests.fs
│   ├── ExtendedFeatureTests.fs                   # SPLIT → ShapeConversionTests.fs + ConstraintTests.fs + KinematicBodyTests.fs
│   └── StateDecompositionTests.fs                # MODIFY: use CommonTestBuilders
├── PhysicsViewer.Tests/
│   ├── SceneManagerTests.fs                      # SPLIT → ShapeRenderingTests.fs + SceneStateBehaviorTests.fs
│   └── CameraControllerTests.fs                  # SPLIT → CameraBasicsTests.fs + CameraModeTests.fs
├── PhysicsClient.Tests/
│   └── MeshResolverTests.fs                      # MODIFY: add missing duplicate-handling test from Viewer
├── PhysicsSandbox.Mcp.Tests/
│   └── SurfaceAreaTests.fs                       # MODIFY: use shared assertion helper
├── PhysicsSandbox.Scripting.Tests/
│   └── SurfaceAreaTests.fs                       # MODIFY: use shared assertion helper
└── PhysicsSandbox.Integration.Tests/
    ├── DiagnosticsIntegrationTests.cs            # DELETE: merge into MetricsIntegrationTests.cs
    ├── ComparisonIntegrationTests.cs             # DELETE: merge into BatchIntegrationTests.cs
    ├── StressTestIntegrationTests.cs             # DELETE: merge into BatchIntegrationTests.cs
    ├── StaticBodyTests.cs                        # DELETE: merge into SimulationConnectionTests.cs
    ├── RestartIntegrationTests.cs                # DELETE: merge into SimulationConnectionTests.cs
    ├── BatchIntegrationTests.cs                  # MODIFY: absorb Comparison + Stress tests
    ├── MetricsIntegrationTests.cs                # MODIFY: absorb Diagnostics test
    └── SimulationConnectionTests.cs              # MODIFY: absorb StaticBody + Restart tests
```

**Structure Decision**: No new projects or directories. Changes are file-level: split oversized files, merge undersized files, add one shared builder file, extend one existing shared helper file.

## Design Decisions

### D1: Shared test builders location

**Decision**: Create `tests/CommonTestBuilders.fs` at the tests root, linked into projects via `<Compile Include="../CommonTestBuilders.fs" Link="CommonTestBuilders.fs" />` — same pattern as existing SharedTestHelpers.fs.

**Rationale**: Follows established pattern. F# compilation order requires shared files first; the Link pattern achieves this without a separate project.

**Alternatives rejected**:
- Separate SharedTestProject.fsproj: Overhead for 3 duplicated helpers. Not justified.
- Per-project copies: Current state. Causes drift.

### D2: MeshResolver test consolidation approach

**Decision**: Keep both MeshResolverTests.fs files (PhysicsClient.Tests and PhysicsViewer.Tests) but harmonize coverage. Add the "duplicate does not overwrite" test from Viewer to Client. Both test their own project's MeshResolver module.

**Rationale**: Research revealed these are NOT identical — they test different projects' MeshResolver implementations. PhysicsViewer's version has a critical cache idempotency test that PhysicsClient's lacks.

**Alternatives rejected**:
- Delete one: Would lose coverage of one project's MeshResolver.
- Merge into single file: Can't — they test different assemblies.

### D3: Integration test consolidation grouping

**Decision**: Consolidate 5 single-test files into 3 existing files by domain:
- DiagnosticsIntegrationTests → MetricsIntegrationTests (same observability domain)
- ComparisonIntegrationTests + StressTestIntegrationTests → BatchIntegrationTests (same batch/perf domain)
- StaticBodyTests + RestartIntegrationTests → SimulationConnectionTests (same simulation lifecycle domain)

**Rationale**: Groups by behavioral domain. Target files already exist and are small enough to absorb 1-2 tests without exceeding 25 tests.

### D4: Oversized file split strategy

**Decision**: Split 4 files into 2-3 files each along natural behavioral boundaries:

| Original | Split Into | Boundary |
|----------|-----------|----------|
| SceneManagerTests.fs (40 tests) | ShapeRenderingTests.fs + SceneStateBehaviorTests.fs | Shape geometry vs. state/narration/wireframe |
| ExtendedFeatureTests.fs (36 tests) | ShapeConversionTests.fs + ConstraintTests.fs + KinematicBodyTests.fs | Shape types vs. constraints vs. kinematic bodies |
| SimulationWorldTests.fs (30 tests) | SimulationWorldBasicsTests.fs + SimulationWorldForcesTests.fs | World lifecycle vs. forces/queries |
| CameraControllerTests.fs (32 tests) | CameraBasicsTests.fs + CameraModeTests.fs | Camera params vs. orbit/chase/frame modes |

**Rationale**: Each split follows existing internal grouping (comment headers, blank line separations). No test moves between unrelated groups.

### D5: Surface area test simplification

**Decision**: Add `assertModuleSurface` helper to SharedTestHelpers.fs that takes a Type and expected member list, reducing each surface area test to a single function call per module.

**Rationale**: All 6 SurfaceAreaTests.fs files use identical boilerplate (typeof → getPublicMembers → assertContains loop). Extracting this eliminates ~40 lines per file.

## Implementation Phases

### Phase 1: Shared Infrastructure (no test behavior changes)
1. Extend SharedTestHelpers.fs with `assertModuleSurface` helper
2. Create CommonTestBuilders.fs with `makeBody`, `makeState`, `makeResolver` helpers
3. Update all F# .fsproj files to link CommonTestBuilders.fs (compilation order: SharedTestHelpers → CommonTestBuilders → test files)

### Phase 2: Consolidate Duplicates
4. Update StateStreamOptimizationTests.fs and StateDecompositionTests.fs to use CommonTestBuilders
5. Harmonize MeshResolverTests: add missing idempotency test to PhysicsClient version
6. Simplify all 6 SurfaceAreaTests.fs files to use `assertModuleSurface`

### Phase 3: Merge Small Integration Files
7. Merge DiagnosticsIntegrationTests → MetricsIntegrationTests
8. Merge ComparisonIntegrationTests + StressTestIntegrationTests → BatchIntegrationTests
9. Merge StaticBodyTests + RestartIntegrationTests → SimulationConnectionTests
10. Delete the 5 now-empty source files, update .csproj

### Phase 4: Split Oversized Files
11. Split SceneManagerTests.fs → ShapeRenderingTests.fs + SceneStateBehaviorTests.fs
12. Split ExtendedFeatureTests.fs → ShapeConversionTests.fs + ConstraintTests.fs + KinematicBodyTests.fs
13. Split SimulationWorldTests.fs → SimulationWorldBasicsTests.fs + SimulationWorldForcesTests.fs
14. Split CameraControllerTests.fs → CameraBasicsTests.fs + CameraModeTests.fs
15. Update .fsproj files with new compilation order

### Phase 5: Validation
16. Run full test suite: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
17. Verify test count within 5% of baseline (~358)
18. Verify no file exceeds 25 tests
19. Verify zero single-test integration files remain
