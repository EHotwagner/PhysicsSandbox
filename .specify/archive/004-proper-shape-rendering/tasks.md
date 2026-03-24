# Tasks: Proper Shape Rendering

**Input**: Design documents from `/specs/004-proper-shape-rendering/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Tests included per Constitution Principle VI (test evidence required for behavior-changing code).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Add new dependency and prepare shared infrastructure

- [x] T001 Add MIConvexHull PackageReference to `src/PhysicsViewer/PhysicsViewer.fsproj`
- [x] T002 Verify MIConvexHull restores and builds cleanly: `dotnet build src/PhysicsViewer -p:StrideCompilerSkipBuild=true`

**Checkpoint**: PhysicsViewer builds with new dependency

---

## Phase 2: Foundational (Custom Mesh Infrastructure)

**Purpose**: Core custom mesh generation and Stride Model creation pipeline that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T003 Define `CustomMeshData` record type (Vertices: VertexPositionNormalColor[], Indices: int[], WireframeVertices: Vector3[], WireframeIndices: int[]) in `src/PhysicsViewer/Rendering/ShapeGeometry.fsi`
- [x] T004 Implement `CustomMeshData` record and helper `computeFaceNormal` (cross product of triangle edges) in `src/PhysicsViewer/Rendering/ShapeGeometry.fs`
- [x] T005 Define `buildCustomMesh: Shape -> Color -> CustomMeshData option` dispatcher signature in `src/PhysicsViewer/Rendering/ShapeGeometry.fsi`
- [x] T006 Implement `buildCustomMesh` stub that returns `None` for all shapes (to be filled per story) in `src/PhysicsViewer/Rendering/ShapeGeometry.fs`
- [x] T007 Define `isCustomShape: Shape -> bool` function signature in `src/PhysicsViewer/Rendering/ShapeGeometry.fsi`
- [x] T008 Implement `isCustomShape` returning true for Triangle, Mesh, ConvexHull in `src/PhysicsViewer/Rendering/ShapeGeometry.fs`
- [x] T009 Add `createModelFromMeshData: GraphicsDevice -> CustomMeshData -> Material -> Model` function that creates MeshDraw from vertex/index buffers, wraps in Mesh and Model, in `src/PhysicsViewer/Rendering/SceneManager.fsi` and `src/PhysicsViewer/Rendering/SceneManager.fs`
- [x] T010 Branch `createEntity` in `src/PhysicsViewer/Rendering/SceneManager.fs`: if `isCustomShape` then use `buildCustomMesh` + `createModelFromMeshData`, else use existing `Create3DPrimitive` path
- [x] T011 Add `createCustomWireframe: Game -> Scene -> CustomMeshData -> Vector3 -> Quaternion -> Entity` that creates LineList MeshDraw from wireframe vertices/indices, in `src/PhysicsViewer/Rendering/DebugRenderer.fsi` and `src/PhysicsViewer/Rendering/DebugRenderer.fs`
- [x] T012 Branch `createPrimitiveWireframe` in `src/PhysicsViewer/Rendering/DebugRenderer.fs`: if `isCustomShape` then use `buildCustomMesh` + `createCustomWireframe`, else use existing primitive wireframe path

### Foundation Tests

- [x] T013 [P] Add test: `buildCustomMesh` returns `None` for Sphere, Box, Capsule, Cylinder, Plane in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T014 [P] Add test: `isCustomShape` returns true for Triangle, Mesh, ConvexHull and false for primitives in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`

**Checkpoint**: Foundation ready — custom shape → Model pipeline compiles and primitive shapes still render correctly

---

## Phase 3: User Story 1 — Triangle and Mesh Rendering (Priority: P1) MVP

**Goal**: Triangle and Mesh shapes render with actual geometry (surfaces + wireframes) instead of bounding boxes

**Independent Test**: Create triangle and mesh bodies. Verify actual triangular surfaces appear, not cubes. Enable debug wireframes and verify edges trace triangle boundaries.

### Tests for User Story 1

- [x] T015 [P] [US1] Add test: `buildTriangleMesh` produces 1 face (3 vertices, 3 indices) and 3 wireframe edges (6 indices) for a valid triangle in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T016 [P] [US1] Add test: `buildTriangleMesh` returns degenerate fallback for collinear vertices in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T017 [P] [US1] Add test: `buildMeshMesh` produces N faces and deduplicated edges for N-triangle mesh in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T018 [P] [US1] Add test: `buildMeshMesh` returns degenerate fallback for empty mesh (0 triangles) in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T019 [P] [US1] Add test: `buildCustomMesh` dispatches to `buildTriangleMesh` for Triangle shapes in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T020 [P] [US1] Add test: `buildCustomMesh` dispatches to `buildMeshMesh` for Mesh shapes in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T020b [P] [US1] Add test: `buildTriangleMesh` and `buildMeshMesh` produce vertices with the correct shape-type color from the color palette in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`

### Implementation for User Story 1

- [x] T021 [US1] Define `buildTriangleMesh: Shape -> Color -> CustomMeshData` signature in `src/PhysicsViewer/Rendering/ShapeGeometry.fsi`
- [x] T022 [US1] Implement `buildTriangleMesh`: extract 3 vertices from proto Triangle, compute face normal via cross product, create VertexPositionNormalColor array (3 vertices with same normal and color), indices [0;1;2], wireframe indices [0;1;1;2;2;0], handle degenerate case (collinear → None triggers fallback) in `src/PhysicsViewer/Rendering/ShapeGeometry.fs`
- [x] T023 [US1] Define `buildMeshMesh: Shape -> Color -> CustomMeshData` signature in `src/PhysicsViewer/Rendering/ShapeGeometry.fsi`
- [x] T024 [US1] Implement `buildMeshMesh`: iterate proto Mesh.Triangles, for each triangle compute face normal and emit 3 VertexPositionNormalColor vertices, build triangle indices sequentially, build wireframe edge set (deduplicate shared edges using sorted vertex-pair key), handle empty mesh (0 triangles → None triggers fallback) in `src/PhysicsViewer/Rendering/ShapeGeometry.fs`
- [x] T025 [US1] Wire `buildTriangleMesh` and `buildMeshMesh` into `buildCustomMesh` dispatcher for Triangle and Mesh cases in `src/PhysicsViewer/Rendering/ShapeGeometry.fs`
- [x] T026 [US1] Update `primitiveType` to return a sentinel/unused value for Triangle and Mesh (since custom path bypasses it) or add guard in `createEntity` to skip primitiveType for custom shapes in `src/PhysicsViewer/Rendering/ShapeGeometry.fs`
- [x] T027 [US1] Update `.fsi` surface area baseline for ShapeGeometry module in `tests/PhysicsViewer.Tests/SurfaceAreaTests.fs` (if baseline test exists)

**Checkpoint**: Triangle and Mesh shapes render as actual surfaces. Debug wireframes trace triangle edges. Degenerate cases show fallback. Run `dotnet test tests/PhysicsViewer.Tests` — all pass.

---

## Phase 4: User Story 2 — Convex Hull Rendering (Priority: P2)

**Goal**: ConvexHull shapes render as closed convex surfaces derived from input points, not bounding boxes

**Independent Test**: Create convex hull body from known points (e.g., 8 cube corners). Verify viewer shows closed surface matching the hull.

### Tests for User Story 2

- [x] T028 [P] [US2] Add test: `buildConvexHullMesh` produces correct face count for 8-point cube hull (12 triangles = 6 faces × 2 tris) in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T029 [P] [US2] Add test: `buildConvexHullMesh` produces correct face count for 4-point tetrahedron hull (4 triangles) in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T030 [P] [US2] Add test: `buildConvexHullMesh` returns degenerate fallback for < 4 points in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T031 [P] [US2] Add test: `buildCustomMesh` dispatches to `buildConvexHullMesh` for ConvexHull shapes in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`

### Implementation for User Story 2

- [x] T032 [US2] Define `buildConvexHullMesh: Shape -> Color -> CustomMeshData` signature in `src/PhysicsViewer/Rendering/ShapeGeometry.fsi`
- [x] T033 [US2] Implement `buildConvexHullMesh`: convert proto ConvexHull.Points to MIConvexHull vertex array, compute hull via `ConvexHull.Create`, extract triangular faces, compute per-face normals, build VertexPositionNormalColor + index arrays, build deduplicated wireframe edges. Handle < 4 points: 0-1 → None (fallback sphere), 2 → line, 3 → flat triangle via `buildTriangleMesh`. In `src/PhysicsViewer/Rendering/ShapeGeometry.fs`
- [x] T034 [US2] Wire `buildConvexHullMesh` into `buildCustomMesh` dispatcher for ConvexHull case in `src/PhysicsViewer/Rendering/ShapeGeometry.fs`
- [x] T035 [US2] Update `.fsi` surface area baseline for ShapeGeometry if needed in `tests/PhysicsViewer.Tests/SurfaceAreaTests.fs`

**Checkpoint**: ConvexHull shapes render as proper convex surfaces. Run tests — all pass.

---

## Phase 5: User Story 3 — Compound Shape Rendering (Priority: P2)

**Goal**: Compound shapes render each child shape individually at correct local offset and orientation

**Independent Test**: Create compound body with 2 spheres at different offsets. Verify both appear at correct positions. Enable wireframes — each child has its own wireframe.

### Tests for User Story 3

- [x] T036 [P] [US3] Add test: compound with 2 sphere children creates 2 child entities in `tests/PhysicsViewer.Tests/SceneManagerTests.fs` or `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T037 [P] [US3] Add test: compound with 0 children falls back to placeholder in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T038 [P] [US3] Add test: compound child local positions are correctly applied in `tests/PhysicsViewer.Tests/ShapeGeometryTests.fs`
- [x] T038b [P] [US3] Add test: compound body pose update propagates to child entity transforms (parent moves → children move) in `tests/PhysicsViewer.Tests/SceneManagerTests.fs`

### Implementation for User Story 3

- [x] T039 [US3] Add `createCompoundEntity` function signature to `src/PhysicsViewer/Rendering/SceneManager.fsi`: creates parent entity with child entities for each compound child
- [x] T040 [US3] Implement `createCompoundEntity` in `src/PhysicsViewer/Rendering/SceneManager.fs`: create parent entity with TransformComponent at body pose, iterate `shape.Compound.Children`, for each child create sub-entity using existing `createEntity` dispatch (primitive or custom), apply child's LocalPosition and LocalOrientation as child entity transform relative to parent, add child entities to parent's Children collection. Handle 0 children → fallback placeholder sphere.
- [x] T041 [US3] Branch `createEntity` to call `createCompoundEntity` when `shape.ShapeCase = Compound` in `src/PhysicsViewer/Rendering/SceneManager.fs`
- [x] T042 [US3] Update `createWireframeEntities` in `src/PhysicsViewer/Rendering/DebugRenderer.fs` to use custom wireframes for non-primitive compound children (currently all children use primitive wireframes)
- [x] T043 [US3] Update `.fsi` surface area baselines for SceneManager and DebugRenderer in `tests/PhysicsViewer.Tests/SurfaceAreaTests.fs`

**Checkpoint**: Compound shapes show each child rendered individually. Children that are meshes/hulls render with proper geometry. Wireframes show per-child outlines.

---

## Phase 6: User Story 4 — CachedRef and ShapeRef Resolution (Priority: P3)

**Goal**: CachedRef resolves to actual mesh geometry (not bounding box); ShapeRef resolves to the registered shape's geometry

**Independent Test**: Create bodies resulting in CachedRef and ShapeRef. Verify viewer renders actual geometry after resolution.

### Tests for User Story 4

- [x] T044 [P] [US4] Add test: ShapeRef resolved via RegisteredShapes map renders underlying shape type in `tests/PhysicsViewer.Tests/SceneManagerTests.fs`
- [x] T045 [P] [US4] Add test: CachedRef resolved via MeshResolver renders actual mesh geometry in `tests/PhysicsViewer.Tests/SceneManagerTests.fs`
- [x] T046 [P] [US4] Add test: unresolved CachedRef renders placeholder (existing behavior preserved) in `tests/PhysicsViewer.Tests/SceneManagerTests.fs`

### Implementation for User Story 4

- [x] T047 [US4] Update `resolveShape` in `src/PhysicsViewer/Rendering/SceneManager.fs` to handle ShapeRef: look up `shape.ShapeRef.Name` in `SimulationState.RegisteredShapes`, return resolved shape. If not found, return ShapeRef as placeholder.
- [x] T048 [US4] Pass `RegisteredShapes` map to `resolveShape` — update `applyState` signature in `src/PhysicsViewer/Rendering/SceneManager.fsi` if needed, or access from SimulationState already available
- [x] T049 [US4] Verify existing CachedRef → MeshResolver path now renders with custom mesh pipeline (mesh shapes go through `buildMeshMesh` instead of bounding box) — may need no code change if T010 wiring already handles it
- [x] T050 [US4] Update DebugRenderer `createWireframeEntities` to use resolved shapes for ShapeRef and CachedRef wireframes in `src/PhysicsViewer/Rendering/DebugRenderer.fs`
- [x] T051 [US4] Update `.fsi` surface area baselines for SceneManager in `tests/PhysicsViewer.Tests/SurfaceAreaTests.fs`

**Checkpoint**: All 10 shape types render with proper geometry. No bounding-box approximations remain. SC-001 satisfied.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verify all success criteria, clean up, ensure no regressions

- [x] T052 Run full test suite: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — all existing tests pass
- [x] T053 Run demo scripts with viewer active: `dotnet fsi Scripting/demos/AutoRun.fsx` — verify complex shapes display correctly (SC-004)
- [x] T054 Visual validation: create scene with 200 mixed-shape bodies, verify frame rate (SC-003)
- [x] T055 Verify debug wireframes for all shape types trace actual geometry edges (SC-002)
- [x] T056 Review and update CLAUDE.md known issues: remove "Convex hull, mesh, and triangle shapes are rendered as bounding-box approximations" note
- [x] T057 Update `src/PhysicsViewer/Rendering/ShapeGeometry.fsi` final surface area — ensure all new public functions are in signature file

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1 Triangle/Mesh (Phase 3)**: Depends on Foundational (Phase 2)
- **US2 Convex Hull (Phase 4)**: Depends on Foundational (Phase 2). Uses `buildTriangleMesh` from US1 for < 4 point fallback, but can stub if US1 not complete.
- **US3 Compound (Phase 5)**: Depends on Foundational (Phase 2). Benefits from US1+US2 for non-primitive children, but can render primitive children without them.
- **US4 CachedRef/ShapeRef (Phase 6)**: Depends on Foundational (Phase 2). Full benefit requires US1 (mesh rendering) to be complete.
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Independent after Foundation — **MVP target**
- **US2 (P2)**: Independent after Foundation — uses `buildTriangleMesh` for edge case fallback
- **US3 (P2)**: Independent after Foundation — child shapes benefit from US1/US2 but primitive children work standalone
- **US4 (P3)**: Independent after Foundation — CachedRef benefits from US1 mesh rendering

### Within Each User Story

- Tests written first, verified to fail
- Geometry generation functions before scene integration
- `.fsi` signature updates alongside implementation
- Surface area baseline updates after all functions added

### Parallel Opportunities

- T013, T014 (foundation tests) can run in parallel
- All US1 tests (T015-T020) can run in parallel
- All US2 tests (T028-T031) can run in parallel
- All US3 tests (T036-T038) can run in parallel
- All US4 tests (T044-T046) can run in parallel
- US2, US3, US4 can start in parallel after Foundation (with minimal stubs)

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests together (write first, verify they fail):
T015: buildTriangleMesh valid triangle test
T016: buildTriangleMesh degenerate test
T017: buildMeshMesh multi-triangle test
T018: buildMeshMesh empty mesh test
T019: buildCustomMesh Triangle dispatch test
T020: buildCustomMesh Mesh dispatch test

# Then implement sequentially:
T021-T022: buildTriangleMesh (signature + implementation)
T023-T024: buildMeshMesh (signature + implementation)
T025: Wire into dispatcher
T026: Guard primitiveType for custom shapes
T027: Update surface area baseline
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational (T003-T014)
3. Complete Phase 3: US1 Triangle/Mesh (T015-T027)
4. **STOP and VALIDATE**: Triangle and Mesh bodies render with actual geometry
5. This alone eliminates the 2 most common bounding-box approximations

### Incremental Delivery

1. Setup + Foundation → Custom mesh pipeline ready
2. US1 Triangle/Mesh → MVP: most impactful shapes fixed
3. US2 Convex Hull → Second most common complex shape
4. US3 Compound → Multi-child decomposition
5. US4 CachedRef/ShapeRef → Full resolution chain complete
6. Polish → All success criteria validated

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Vertex format: `VertexPositionNormalColor` with per-face normals (flat shading)
- Double-sided rendering (no backface culling) for all custom geometry
- Edge deduplication for wireframes: use sorted vertex-pair as key to avoid drawing shared edges twice
- Degenerate fallback: small colored sphere at body position for any shape that can't produce valid geometry
- MIConvexHull: `ConvexHull.Create<Vertex3D>` returns faces with vertex indices — convert to triangle list
