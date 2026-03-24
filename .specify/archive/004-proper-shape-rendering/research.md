# Research: Proper Shape Rendering

**Date**: 2026-03-24 | **Branch**: `004-proper-shape-rendering`

## R1: Custom Mesh Creation in Stride3D

**Decision**: Use Stride's `MeshDraw` with manual vertex/index buffers for custom geometry.

**Rationale**: The existing viewer uses `Create3DPrimitive` (Stride Community Toolkit) which only supports built-in primitive types. For custom geometry (triangles, meshes, convex hulls), we need to create `MeshDraw` objects from raw vertex and index data, wrap them in a `Model`, and attach via `ModelComponent`.

**Alternatives considered**:
- `MeshBuilder` (Community Toolkit) — higher-level but adds overhead per vertex; manual buffers are simpler for bulk triangle data where we already have positions and indices.
- `GeometricPrimitive` — only for standard shapes (sphere, cube, etc.), not custom geometry.

**Key API pattern**:
```
vertices: VertexPositionNormalColor[] → Buffer.Vertex.New(graphicsDevice, vertices)
indices: int[] → Buffer.Index.New(graphicsDevice, indices)
MeshDraw(PrimitiveType=TriangleList, DrawCount=indices.Length, VertexBuffers=[|binding|], IndexBuffer=binding)
Mesh(Draw=meshDraw) → Model(Meshes=[mesh], Materials=[material]) → ModelComponent(Model=model)
```

## R2: Convex Hull Face Computation

**Decision**: Use MIConvexHull NuGet package for 3D convex hull face extraction.

**Rationale**: BepuFSharp does not expose computed hull faces — it uses them internally for physics but provides no rendering API. MIConvexHull is a proven pure C# QuickHull implementation with no dependencies, O(n log n) average performance, and returns face/vertex data directly usable for mesh generation.

**Alternatives considered**:
- Port Unity QuickHull to F# — functional but 6-8 hours of porting vs. 2-4 hours for NuGet integration.
- Implement incremental 3D hull from scratch — 16-24 hours, higher risk of edge case bugs.
- Extract from BepuPhysics2 source — API not exposed, high coupling risk.

## R3: Viewer Architecture Integration Points

**Decision**: Extend `ShapeGeometry` module with custom mesh generation and modify `SceneManager.createEntity` to use custom `Model` objects instead of `Create3DPrimitive` for non-primitive shapes.

**Rationale**: The current flow is `Shape → primitiveType → Create3DPrimitive`. For custom shapes, we need a parallel path: `Shape → custom vertex/index data → MeshDraw → Model → ModelComponent`. The `SceneManager` already handles entity lifecycle (create/update/remove), so the change is localized to entity creation. `DebugRenderer` needs the same treatment for wireframes (using `PrimitiveType.LineList` instead of `TriangleList`).

**Key integration points**:
- `ShapeGeometry.fs` — add functions to generate vertex/index arrays from proto shapes
- `SceneManager.fs` — branch `createEntity` for custom vs. primitive shapes
- `DebugRenderer.fs` — branch `createPrimitiveWireframe` for custom wireframe lines
- No changes to streaming, MeshResolver, or Program.fs data flow

## R4: Compound Shape Rendering Strategy

**Decision**: Render compound shapes as multiple child entities parented to a root entity node, reusing existing shape rendering for each child.

**Rationale**: The `DebugRenderer` already handles compound children by iterating `shape.Compound.Children` and computing world transforms per child. The same pattern applies to solid rendering. Each child has a `Shape` that can be rendered using the same triangle/mesh/hull/primitive path. The root entity provides the body transform; children apply local offsets.

**Alternatives considered**:
- Merge all children into a single combined mesh — more complex, loses per-child color/material.
- Single bounding box with better size estimation — still an approximation, doesn't meet SC-001.

## R5: ShapeRef Resolution

**Decision**: Resolve ShapeRef at render time by looking up the shape name in `SimulationState.RegisteredShapes` and rendering the resolved shape's geometry.

**Rationale**: The `SimulationState` already carries a `RegisteredShapes` map (populated via PropertyEvent snapshots). The viewer has access to this via `cachedRegisteredShapes`. Resolution is a simple dictionary lookup by name, then rendering the resolved shape as any other type.

## R6: Normals and Lighting for Custom Geometry

**Decision**: Compute per-face normals (flat shading) for triangle, mesh, and convex hull shapes.

**Rationale**: Physics collision geometry is inherently faceted (triangle meshes, hull faces). Per-face normals give correct flat-shaded appearance matching the collision surface. Per-vertex smooth normals would misrepresent the actual geometry. Stride's default material pipeline handles lighting when normals are provided via `VertexPositionNormalColor` vertex format.

## R7: Double-Sided Rendering

**Decision**: Render custom geometry double-sided (no backface culling) for safety.

**Rationale**: Physics meshes may have inconsistent winding order (not guaranteed outward-facing normals). Rendering both sides ensures all faces are visible regardless of vertex order. The performance cost is negligible for the triangle counts in scope (up to 10K per mesh).
