# Research: Test Suite Cleanup

**Date**: 2026-03-25
**Branch**: `004-test-suite-cleanup`

## Baseline Test Inventory

| Test Project | Files | Tests | Language |
|-------------|-------|-------|----------|
| PhysicsServer.Tests | 7 | 48 | F# |
| PhysicsSimulation.Tests | 8 | 114 | F# |
| PhysicsViewer.Tests | 8 | 99 | F# |
| PhysicsClient.Tests | 11 | 77 | F# |
| PhysicsSandbox.Mcp.Tests | 4 | 19 | F# |
| PhysicsSandbox.Scripting.Tests | 5 | 26 | F# |
| PhysicsSandbox.Integration.Tests | 20 | 80 | C# |
| **Total** | **63** | **~463** | |

## Exact Duplicates Found

### 1. makeBody + makeState helpers (exact duplicate)

**Location A**: `tests/PhysicsServer.Tests/StateStreamOptimizationTests.fs`
**Location B**: `tests/PhysicsSimulation.Tests/StateDecompositionTests.fs`

Both define identical `makeBody id isStatic` (creates Body with Mass=0/1.0, Position, Velocity, AngularVelocity) and `makeState bodies` (creates SimulationState with Time=1.0, Running=true).

**Decision**: Extract to CommonTestBuilders.fs. Both files import shared version.

### 2. makeResolver() helper (exact duplicate)

**Location A**: `tests/PhysicsClient.Tests/MeshResolverTests.fs` (line 7)
**Location B**: `tests/PhysicsViewer.Tests/MeshResolverTests.fs` (line 7)

Both create resolver with `Unchecked.defaultof` client. However, the test FILES are NOT identical:
- PhysicsClient version: asserts `Shape.ShapeOneofCase.Sphere` (shape type enum)
- PhysicsViewer version: has unique "duplicate processNewMeshes does not overwrite" test (cache idempotency)

**Decision**: Extract `makeResolver()` to CommonTestBuilders.fs. Keep both test files (they test different assemblies). Add the missing idempotency test to PhysicsClient version.

### 3. Surface area test boilerplate (structural duplicate)

All 6 F# test projects have SurfaceAreaTests.fs with identical pattern:
```fsharp
let t = typeof<Module>.Assembly.GetType("Namespace.Module")
let members = getPublicMembers t
assertContains members "memberName"
// repeat per member...
```

**Decision**: Add `assertModuleSurface` to SharedTestHelpers.fs:
```fsharp
let assertModuleSurface (assemblyType: Type) (moduleName: string) (expectedMembers: string list) =
    let t = assemblyType.Assembly.GetType(moduleName)
    Assert.NotNull(t)
    let members = getPublicMembers t
    for name in expectedMembers do
        assertContains members name
```

Each SurfaceAreaTests.fs reduces to one call per module.

## Non-Duplicates (Confirmed Distinct)

| Helper | Location | Why Distinct |
|--------|----------|-------------|
| `makeSphereBody id mass radius` | SimulationWorldTests.fs | Different shape (sphere-specific) |
| `makeBody id mass shape` | ExtendedFeatureTests.fs | Generic variant (takes shape param) |
| `makeSphereCmd id` | ResetSimulationTests.fs | Command builder (different from body builder) |
| `makeSphereCmd id y` | StaticBodyTrackingTests.fs | Different signature (includes y position) |
| `makeVec3`, `makeConvexHull`, `makeMeshShape` | MeshIdGeneratorTests.fs | Mesh-specific builders, used only locally |

These remain in their respective files — single-use helpers don't benefit from extraction.

## Integration Test Consolidation Analysis

| Source File | Tests | Target File | Domain Rationale |
|-------------|-------|-------------|-----------------|
| DiagnosticsIntegrationTests.cs | 1 (GetMetrics) | MetricsIntegrationTests.cs (2 tests) | Both test observability/metrics |
| ComparisonIntegrationTests.cs | 1 (batch vs individual timing) | BatchIntegrationTests.cs (2 tests) | Both test batch command behavior |
| StressTestIntegrationTests.cs | 1 (50 bodies in batch) | BatchIntegrationTests.cs (2 tests) | Batch scaling test |
| StaticBodyTests.cs | 1 (static plane in state) | SimulationConnectionTests.cs (5 tests) | Simulation lifecycle |
| RestartIntegrationTests.cs | 1 (reset clears bodies) | SimulationConnectionTests.cs (5 tests) | Simulation lifecycle |

Post-consolidation: 5 files deleted, 3 files grow by 1-2 tests each. No file exceeds 8 tests.

## Oversized File Split Points

### SceneManagerTests.fs (374 lines, 40 tests)
- **Lines 13-88**: Shape rendering (primitiveType mapping, defaultColor by shape) → ShapeRenderingTests.fs (~20 tests)
- **Lines 90-374**: State application, narration, wireframe rendering → SceneStateBehaviorTests.fs (~20 tests)

### ExtendedFeatureTests.fs (543 lines, 36 tests)
- **Lines 20-150**: Shape conversion (capsule, cylinder, triangle, convex hull, mesh) → ShapeConversionTests.fs (~12 tests)
- **Lines 150-250**: Constraints (ball-socket, hinge, contact, etc.) → ConstraintTests.fs (~12 tests)
- **Lines 250+**: Kinematic bodies (mass=0, static tracking) → KinematicBodyTests.fs (~12 tests)

### SimulationWorldTests.fs (428 lines, 30 tests)
- **Lines 23-200**: World lifecycle (create, step, time, running, body add/remove) → SimulationWorldBasicsTests.fs (~15 tests)
- **Lines 200-428**: Forces, torques, raycast, overlap, stress → SimulationWorldForcesTests.fs (~15 tests)

### CameraControllerTests.fs (354 lines, 32 tests)
- **Lines 12-100**: Camera params (default, setCamera, setZoom) → CameraBasicsTests.fs (~10 tests)
- **Lines 100-354**: Modes (orbiting, chasing, framing, transitions) → CameraModeTests.fs (~22 tests)

## F# Compilation Order Requirements

F# requires explicit compilation order in .fsproj files. When adding CommonTestBuilders.fs:

```xml
<ItemGroup>
  <Compile Include="../SharedTestHelpers.fs" Link="SharedTestHelpers.fs" />
  <Compile Include="../CommonTestBuilders.fs" Link="CommonTestBuilders.fs" />
  <!-- test files follow -->
</ItemGroup>
```

CommonTestBuilders.fs must appear AFTER SharedTestHelpers.fs (may use its utilities) and BEFORE all test files that reference it.
