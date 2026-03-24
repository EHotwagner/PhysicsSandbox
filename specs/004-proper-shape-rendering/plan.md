# Implementation Plan: Proper Shape Rendering

**Branch**: `004-proper-shape-rendering` | **Date**: 2026-03-24 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-proper-shape-rendering/spec.md`

## Summary

Replace bounding-box approximations for 5 shape types (Triangle, Mesh, ConvexHull, Compound, ShapeRef/CachedRef) with accurate geometry rendering in the PhysicsViewer. The approach generates custom vertex/index buffers from proto shape data, creates Stride3D `MeshDraw` objects for non-primitive shapes, and renders compound shapes by decomposing into individually-rendered children. Convex hull face computation uses the MIConvexHull NuGet library.

## Technical Context

**Language/Version**: F# on .NET 10.0 (PhysicsViewer)
**Primary Dependencies**: Stride.CommunityToolkit 1.0.0-preview.62 (existing), MIConvexHull (new, convex hull face computation)
**Storage**: N/A (in-memory rendering only)
**Testing**: xUnit 2.x, existing PhysicsViewer.Tests (56 tests)
**Target Platform**: Linux with GPU passthrough
**Project Type**: Desktop 3D viewer (Stride3D game engine)
**Performance Goals**: Interactive frame rates with 200 mixed-shape bodies, meshes up to 10,000 triangles each
**Constraints**: Must integrate with existing Create3DPrimitive path for primitive shapes; no changes to proto contracts or server-side code
**Scale/Scope**: 4 files modified, 1 new dependency, ~6 new functions in ShapeGeometry, branching logic in SceneManager and DebugRenderer

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | Change is viewer-only, no cross-service impact |
| II. Contract-First | PASS | No new contracts needed — uses existing proto Shape types |
| III. Shared Nothing | PASS | No new cross-service dependencies |
| IV. Spec-First Delivery | PASS | Spec and plan produced before implementation |
| V. Compiler-Enforced Contracts | PASS | All modified F# modules have `.fsi` files; new public functions will be added to signatures |
| VI. Test Evidence | PASS | New unit tests for geometry generation; visual validation via demos |
| VII. Observability | PASS | No new services; viewer is a client, not a monitored service |

**Post-Phase 1 Re-check**: No new violations. The MIConvexHull dependency is justified (R2 in research.md) — pure C#, no transitive dependencies, version-pinned.

## Project Structure

### Documentation (this feature)

```text
specs/004-proper-shape-rendering/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: technology decisions
├── data-model.md        # Phase 1: entity/data structures
├── quickstart.md        # Phase 1: build/test/run guide
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/PhysicsViewer/
├── Rendering/
│   ├── ShapeGeometry.fs(i)      # MODIFY: add custom mesh generation functions
│   ├── SceneManager.fs(i)       # MODIFY: branch createEntity for custom shapes + compound children
│   └── DebugRenderer.fs(i)      # MODIFY: branch wireframe for custom edge rendering
├── Streaming/
│   └── MeshResolver.fs(i)       # NO CHANGE: existing CachedRef resolution sufficient
├── Program.fs                    # MINOR: pass RegisteredShapes to SceneManager for ShapeRef resolution
└── PhysicsViewer.fsproj          # MODIFY: add MIConvexHull PackageReference

tests/PhysicsViewer.Tests/
├── ShapeGeometryTests.fs         # ADD/EXTEND: test custom mesh generation for each shape type
└── DebugRendererTests.fs         # ADD/EXTEND: test wireframe edge generation
```

**Structure Decision**: All changes are within the existing PhysicsViewer project. No new projects, no new services. The feature extends the rendering layer only.

## Design Decisions

### D1: Custom Shape Entity Creation

**Current flow** (all shapes): `Shape → primitiveType → Create3DPrimitive → Entity`

**New flow** (branched):
```
Shape → isCustomShape?
  ├─ No  → Create3DPrimitive (unchanged)
  └─ Yes → buildCustomMesh → createModelFromMeshData → Entity
```

`isCustomShape` returns true for Triangle, Mesh, ConvexHull. Compound, CachedRef, and ShapeRef are handled by resolution/decomposition before reaching this branch.

### D2: Custom Mesh → Stride Model Pipeline

```
CustomMeshData (vertices + indices)
  → Buffer.Vertex.New(graphicsDevice, vertices)
  → Buffer.Index.New(graphicsDevice, indices)
  → MeshDraw(PrimitiveType=TriangleList, VertexBuffers=[|vb|], IndexBuffer=ib)
  → Mesh(Draw=meshDraw)
  → Model(Meshes=[mesh], Materials=[material])
  → entity.GetOrCreate<ModelComponent>().Model <- model
```

Vertex format: `VertexPositionNormalColor` (position + per-face normal + shape color).
Faces rendered double-sided (no backface culling) for safety with inconsistent winding.

### D3: Compound Shape Decomposition

Compound bodies create a **parent entity** (for body transform) with **child entities** attached:

```
Body (compound)
  → parentEntity (TransformComponent only, at body pose)
     ├─ childEntity1 (rendered shape at local offset)
     ├─ childEntity2 (rendered shape at local offset)
     └─ ...
```

Each child shape is rendered using the same dispatch (primitive or custom), with local position/rotation applied as the child entity's transform relative to the parent.

### D4: ShapeRef + CachedRef Resolution Chain

Resolution happens before shape rendering dispatch:
1. **ShapeRef**: Look up `body.Shape.ShapeRef.Name` in `RegisteredShapes` map → get resolved Shape
2. **CachedRef**: Look up `body.Shape.CachedRef.MeshId` in MeshResolver cache → get resolved Shape
3. Resolved shape is then dispatched through the normal primitive/custom branch

If resolution fails (name not found, mesh not yet fetched), render the existing placeholder.

### D5: Wireframe Edge Generation

For custom shapes, wireframes use `PrimitiveType.LineList` instead of `TriangleList`:
- **Triangle**: 3 line segments (edges)
- **Mesh**: All triangle edges (deduplicated)
- **ConvexHull**: All hull facet edges (deduplicated)

Edge deduplication prevents double-drawing shared edges between adjacent triangles.

### D6: Degenerate Shape Fallbacks

| Condition | Fallback |
|-----------|----------|
| Triangle with collinear/coincident vertices | Small colored sphere at centroid |
| Mesh with 0 triangles | Small colored sphere at body position |
| ConvexHull with < 4 points | 0-1 points: sphere; 2 points: line; 3 points: flat triangle |
| Compound with 0 children | Small colored sphere at body position |

## Complexity Tracking

No constitution violations. No complexity justifications needed.

## New Dependency Justification

| Package | Version | Purpose | Alternatives Rejected |
|---------|---------|---------|----------------------|
| MIConvexHull | latest stable | 3D convex hull face computation from point cloud | Manual implementation (16-24h, higher bug risk); BepuFSharp (no face extraction API exposed) |

Pinning strategy: Exact version pin in `.fsproj`. No transitive dependencies.
