# Implementation Plan: Codebase Cleanup and Refactoring

**Branch**: `004-codebase-cleanup-refactor` | **Date**: 2026-03-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-codebase-cleanup-refactor/spec.md`

## Summary

Eliminate code duplication, consolidate shared utilities, and split oversized modules across the PhysicsSandbox codebase. The approach is incremental per-project consolidation: extract shared conversion and shape-building modules within existing project boundaries, consolidate MeshResolver and IdGenerator where the dependency graph allows, and split SimulationWorld.fs (708 lines) into focused modules. All changes are structural — zero behavior changes, verified by the existing 468-test suite.

## Technical Context

**Language/Version**: F# on .NET 10.0 (services, MCP, client, scripting, viewer), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts, integration tests)
**Primary Dependencies**: BepuFSharp 0.3.0, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, ModelContextProtocol.AspNetCore 1.1.*, Stride.CommunityToolkit 1.0.0-preview.62
**Storage**: N/A (no storage changes)
**Testing**: xUnit 2.x (384 unit tests), Aspire.Hosting.Testing 10.x (84 integration tests)
**Target Platform**: Linux with GPU passthrough
**Project Type**: Distributed microservices (gRPC) with 3D viewer
**Performance Goals**: N/A (structural refactoring, no performance changes)
**Constraints**: Zero test regressions; no new projects; no public API changes to NuGet packages (PhysicsClient 0.4.0, Scripting)
**Scale/Scope**: 14 duplicate conversion functions across 7 files; 3 duplicate MeshResolvers; 3 duplicate ID generators; 1 file at 708 lines

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Service Independence | PASS | No cross-service coupling introduced; consolidation stays within existing dependency boundaries |
| II. Contract-First | PASS | No proto/contract changes |
| III. Shared Nothing (Except Contracts) | PASS | No new cross-service project references; MeshResolver consolidation uses existing transitive dependency chain |
| IV. Spec-First Delivery | PASS | This plan document + spec |
| V. Compiler-Enforced Structural Contracts | PASS | All new F# modules will have .fsi signature files; surface area baselines updated |
| VI. Test Evidence | PASS | Refactoring verified by existing 468-test suite; no new behavior = no new tests needed (surface area tests updated if module structure changes) |
| VII. Observability by Default | PASS | No observability changes |

**Post-Phase 1 Re-check**: All gates still pass. New modules (ProtoConversions, ShapeConversion, ShapeBuilders) are internal or extend existing projects — no new cross-service dependencies.

## Project Structure

### Documentation (this feature)

```text
specs/004-codebase-cleanup-refactor/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0: consolidation strategy decisions
├── data-model.md        # Phase 1: module dependency map
├── quickstart.md        # Phase 1: verification and implementation order
├── checklists/
│   └── requirements.md  # Specification quality checklist
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code Changes (repository root)

```text
src/PhysicsSimulation/
├── Conversions/
│   ├── ProtoConversions.fsi     # NEW: Vec3↔Vector3, Vec4↔Quaternion, proto builders
│   └── ProtoConversions.fs      # NEW: extracted from SimulationWorld.fs lines 1-157
├── Conversions/
│   ├── ShapeConversion.fsi      # NEW: convertShape, convertConstraintType
│   └── ShapeConversion.fs       # NEW: extracted from SimulationWorld.fs lines 175-265, 267-340
├── World/
│   ├── SimulationWorld.fsi      # MODIFIED: reduced surface (delegates to new modules)
│   └── SimulationWorld.fs       # MODIFIED: ~400 lines (down from 708)
└── Queries/
    └── QueryHandler.fs          # MODIFIED: uses ProtoConversions instead of local copies

src/PhysicsClient/
├── Utilities/
│   ├── Vec3Helpers.fsi          # NEW: canonical toVec3 (tuple→proto Vec3)
│   └── Vec3Helpers.fs           # NEW: extracted from SimulationCommands + Vec3Builders
├── Shapes/
│   ├── ShapeBuilders.fsi        # NEW: mkSphere, mkBox, mkCapsule, mkCylinder, mkPlane, mkTriangle
│   └── ShapeBuilders.fs         # NEW: shape construction helpers
├── Commands/
│   ├── SimulationCommands.fsi   # MODIFIED: uses ShapeBuilders + addGenericBody
│   └── SimulationCommands.fs    # MODIFIED: reduced boilerplate (~400 lines, down from 577)
└── Bodies/
    └── IdGenerator.fs           # EXISTING: canonical ID generation (no changes)

src/PhysicsViewer/
├── Rendering/
│   ├── ProtoConversions.fsi     # NEW: protoVec3ToStride, protoQuatToStride
│   ├── ProtoConversions.fs      # NEW: shared viewer proto conversions
│   ├── CameraController.fs      # MODIFIED: uses ProtoConversions
│   ├── DebugRenderer.fs         # MODIFIED: uses ProtoConversions
│   └── SceneManager.fs          # MODIFIED: uses ProtoConversions

src/PhysicsSandbox.Mcp/
├── MeshResolver.fs              # DELETED: replaced by PhysicsClient.MeshResolver
├── MeshResolver.fsi             # DELETED
├── SimulationTools.fs           # MODIFIED: uses IdGenerator, ShapeBuilders
├── ClientAdapter.fs             # MODIFIED: delegates to SimulationCommands/ShapeBuilders
└── GeneratorTools.fs            # MODIFIED: uses IdGenerator

tests/PhysicsSandbox.Integration.Tests/
└── IntegrationTestHelpers.cs    # MODIFIED: extracted CreateGrpcChannel method
```

**Structure Decision**: No new projects created. All new modules are added to existing projects. The existing project dependency graph is preserved exactly — consolidation follows existing transitive dependency paths.

## Complexity Tracking

No constitution violations to justify. All changes stay within existing project boundaries.

## Phase-by-Phase Design

### Phase 1: PhysicsSimulation Internal Consolidation

**Goal**: Split SimulationWorld.fs (708 → ~400 lines) by extracting pure conversion functions.

**New module: ProtoConversions.fs**
- `toVector3 : Vec3 -> Vector3` (with null check)
- `fromVector3 : Vector3 -> Vec3`
- `toQuaternion : Vec4 -> Quaternion` (with null check)
- `fromQuaternion : Quaternion -> Vec4`
- `buildBodyProto : BodyRecord -> Body`
- `buildConstraintStateProto : ConstraintRecord -> ConstraintState`
- `buildState : ... -> SimulationState`
- `buildTickState : ... -> TickState`
- Type aliases for proto conflicts (ProtoSphere, ProtoBox, etc.)

**New module: ShapeConversion.fs**
- `convertShape : Shape -> BepuFSharp.ShapeDescription` (recursive, handles all 10 shape types)
- `convertConstraintType : ConstraintType -> BepuFSharp.ConstraintDescriptor` (handles all 10 constraint types)
- `toBepuMaterial : MaterialProperties -> BepuFSharp.MaterialProperties`

**Compilation order in PhysicsSimulation.fsproj**:
1. MeshIdGenerator.fsi/fs (unchanged)
2. ProtoConversions.fsi/fs (NEW — before SimulationWorld)
3. ShapeConversion.fsi/fs (NEW — before SimulationWorld, after ProtoConversions)
4. SimulationWorld.fsi/fs (MODIFIED — uses ProtoConversions, ShapeConversion)
5. QueryHandler.fsi/fs (MODIFIED — uses ProtoConversions instead of local conversions)
6. CommandHandler.fsi/fs (unchanged)
7. SimulationClient.fsi/fs (unchanged)
8. Program.fs (unchanged)

### Phase 2: PhysicsClient Shape Builders & Conversion Consolidation

**Goal**: Extract shape construction helpers; eliminate toVec3 duplication in SimulationCommands.

**New module: ShapeBuilders.fs**
- `mkSphere : radius:float -> Shape`
- `mkBox : halfX:float -> halfY:float -> halfZ:float -> Shape`
- `mkCapsule : radius:float -> length:float -> Shape`
- `mkCylinder : radius:float -> length:float -> Shape`
- `mkPlane : normal:(float * float * float) -> Shape`
- `mkTriangle : a:(float * float * float) -> b:(float * float * float) -> c:(float * float * float) -> Shape`

**Refactored SimulationCommands.fs**:
- `addGenericBody : Session -> string -> Shape -> (float * float * float) -> float -> string option -> BodyOptions -> Result<string, string>`
- Individual `addSphere`, `addBox`, etc. become thin wrappers calling `addGenericBody` with `ShapeBuilders.mkShape`
- Remove internal `toVec3` — import from `PhysicsSandbox.Scripting.Vec3Builders` (Scripting references PhysicsClient, but Vec3Builders is in Scripting which PhysicsClient doesn't reference — so instead, move `toVec3` to a utility in PhysicsClient itself or keep delegating)

**Note on toVec3 dependency direction**: PhysicsClient does NOT reference Scripting (Scripting references PhysicsClient). The canonical `toVec3` should live in PhysicsClient (since it's the lower-level library) and Scripting.Vec3Builders should delegate to it. This inverts the current arrangement but follows dependency flow correctly.

**Compilation order addition in PhysicsClient.fsproj**:
- ShapeBuilders.fsi/fs inserted before SimulationCommands

### Phase 3: PhysicsViewer Internal Consolidation

**Goal**: Eliminate 3 copies of `protoVec3ToStride` across viewer rendering modules.

**New module: ProtoConversions.fs** (in PhysicsViewer.Rendering namespace)
- `protoVec3ToStride : Vec3 -> Stride.Core.Mathematics.Vector3`
- `protoQuatToStride : Vec4 -> Stride.Core.Mathematics.Quaternion`

CameraController.fs, DebugRenderer.fs, SceneManager.fs updated to `open PhysicsViewer.Rendering.ProtoConversions`.

### Phase 4: MCP Consolidation

**Goal**: Remove MCP's duplicate MeshResolver and ID generator; use PhysicsClient canonical implementations.

**Changes**:
1. Delete `src/PhysicsSandbox.Mcp/MeshResolver.fs` and `.fsi`
2. Update `Program.fs` to use `PhysicsClient.MeshResolver` (available via transitive Scripting → PhysicsClient dependency)
3. Remove local `nextId` from SimulationTools.fs — use `PhysicsClient.IdGenerator.nextId`
4. Remove local counter from GeneratorTools.fs — use `PhysicsClient.IdGenerator.nextId`
5. Update ClientAdapter.fs to delegate to PhysicsClient.ShapeBuilders where possible
6. Update .fsproj to remove deleted files

### Phase 5: Integration Test Helpers

**Goal**: Extract duplicated gRPC channel creation.

**Changes**:
1. Add private static `CreateGrpcChannel(DistributedApplication app)` method to IntegrationTestHelpers.cs
2. Replace 3 inline channel creation blocks with calls to `CreateGrpcChannel`

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| F# compilation order breaks after module extraction | Medium | Build failure (caught immediately) | Careful .fsproj ordering; build after each phase |
| Subtle behavioral difference in "duplicate" code | Low | Test failure | Research identified differences (null checks); canonical versions include all safety checks |
| Surface area baseline tests fail after module restructuring | Medium | Test failure (expected) | Update baselines as part of each phase |
| MCP MeshResolver removal breaks MCP initialization | Low | Runtime error | Integration tests cover MCP tool execution paths |
| IdGenerator counter state changes when consolidated | Low | ID sequence differences | Functional behavior unchanged; only counter isolation changes (IDs are opaque strings) |
