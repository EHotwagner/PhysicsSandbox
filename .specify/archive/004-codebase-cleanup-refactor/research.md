# Research: Codebase Cleanup and Refactoring

**Date**: 2026-03-25
**Feature**: 004-codebase-cleanup-refactor

## R1: MeshResolver Consolidation Strategy

**Decision**: Consolidate PhysicsClient and MCP MeshResolvers into PhysicsClient (canonical). Keep Viewer's MeshResolver separate (it has async + Pending tracking and Viewer doesn't reference PhysicsClient).

**Rationale**:
- MCP transitively depends on PhysicsClient (via Scripting NuGet → PhysicsClient ProjectReference)
- PhysicsViewer does NOT reference PhysicsClient — adding that dependency couples the GPU viewer to the console client library
- The spec says "no new projects" — a new shared project would violate this constraint
- PhysicsClient and MCP MeshResolvers are semantically identical (sync fetch, same state type)
- Viewer's MeshResolver has meaningful differences: async fetch, Pending dictionary for deduplication

**Alternatives considered**:
- New `PhysicsSandbox.Shared.Client` project: Cleanest architecturally but violates spec scope constraint
- All three in PhysicsClient: Requires Viewer → PhysicsClient dependency (undesirable coupling)
- Keep all separate: No improvement; maintenance burden persists

## R2: Vector Conversion Consolidation Strategy

**Decision**: Two consolidation tracks, scoped per-project to avoid cross-project dependencies:

1. **Tuple→Vec3** (PhysicsClient + Scripting): SimulationCommands.toVec3 delegates to Vec3Builders.toVec3 (Scripting is already a dependency of PhysicsClient via project reference chain)
2. **Vec3↔Vector3/Quaternion** (PhysicsSimulation): Extract internal `Conversions` module, used by both SimulationWorld and QueryHandler
3. **Proto→Stride vectors** (PhysicsViewer): Extract internal `ProtoConversions` module, used by CameraController, DebugRenderer, SceneManager

**Rationale**:
- PhysicsSimulation and PhysicsClient are sibling projects (both reference only Contracts) — no cross-project sharing possible without a new project
- The conversions differ: Client uses `float` tuples, Simulation uses `float32` System.Numerics types, Viewer uses Stride's Vector3 (also float32 but different namespace)
- Per-project consolidation eliminates duplication within each boundary without architectural changes

**Alternatives considered**:
- Shared conversion module in Contracts: Contracts is C#, would need to add System.Numerics dependency for F# consumers only
- Single cross-project conversion library: Violates "no new projects" constraint

## R3: Shape Builder Abstraction Strategy

**Decision**: Extract shape construction helpers (`mkSphere`, `mkBox`, etc.) into a `ShapeBuilders` module in PhysicsClient. Introduce a higher-order `addGenericBody` function that takes a shape factory function, eliminating per-shape boilerplate.

**Rationale**:
- All 6 primary shape add* functions in SimulationCommands follow identical patterns differing only in shape-specific field assignments (3-5 lines each)
- ClientAdapter in MCP duplicates SimulationCommands instead of delegating — can delegate after extraction
- MCP SimulationTools.add_body has a mega-function with string dispatch; it can use the same `mkShape` helpers
- Scripting.CommandBuilders already has 4 of 6 shape builders — can be completed and used as canonical

**Alternatives considered**:
- F# discriminated union for ShapeSpec: Clean but incompatible with MCP's Nullable<T> parameter pattern
- Keep per-shape functions with reduced boilerplate via helper: Still reduces 30 lines × 6 shapes

## R4: SimulationWorld Splitting Strategy

**Decision**: Extract two modules from SimulationWorld.fs:
1. `ProtoConversions.fs` — vector/quaternion conversions + proto building functions (~100 lines)
2. `ShapeConversion.fs` — `convertShape` recursive shape parser + `convertConstraintType` (~200 lines)

Core body/constraint CRUD, lifecycle, and query support remain in SimulationWorld.fs (~400 lines).

**Rationale**:
- SimulationWorld.fsi exists (107 lines) — splitting requires new .fsi files for extracted modules
- ShapeConversion and ConstraintConversion are pure functions (no world mutation) — cleanest extraction targets
- Body management (`addBody` at 160 lines) is tightly coupled to mutable world state — splitting it would scatter mutation logic
- QueryHandler depends on SimulationWorld internals — extracted modules must be accessible via internal visibility

**Alternatives considered**:
- Split body management into separate module: Too tightly coupled to World mutable state
- Split force application: Only 15 lines, not worth the overhead

## R5: ID Generator Consolidation

**Decision**: Make PhysicsClient.IdGenerator the canonical implementation. Remove duplicate `nextId` from MCP SimulationTools and GeneratorTools.

**Rationale**:
- Three independent ID generators with separate counter state creates collision risk
- PhysicsClient.IdGenerator already has the cleanest implementation with ConcurrentDictionary
- MCP transitively depends on PhysicsClient — can reference IdGenerator directly

**Alternatives considered**:
- New shared ID module: Unnecessary given existing dependency chain

## R6: Integration Test Helpers

**Decision**: Extract `CreateGrpcChannel()` private method from IntegrationTestHelpers.cs. No separate test command builder consolidation needed (CommonTestBuilders.fs already exists from 004-test-suite-cleanup).

**Rationale**:
- Channel creation code is identical across 3 methods (11 lines × 3 = 33 lines duplicated)
- Test command builders were already consolidated in the recent test suite cleanup
- Simple refactoring, zero risk

## R7: Viewer Program.fs Mutable State

**Decision**: Defer to lower priority. Group related mutable state with comments/regions but don't introduce a ViewerRuntimeState record in this feature.

**Rationale**:
- Viewer Program.fs (524 lines) is the game loop entry point — mutable state is inherent to game architecture
- Introducing a record requires refactoring the entire update loop and all streaming functions
- Risk/reward ratio is unfavorable for a cleanup feature — better suited for a dedicated viewer refactoring feature
- The file stays under the 550-line target without splitting
