# Data Model: Proper Shape Rendering

**Date**: 2026-03-24 | **Branch**: `004-proper-shape-rendering`

## Entities

### CustomMeshData

Generated vertex/index data for rendering a non-primitive shape.

| Field | Type | Description |
|-------|------|-------------|
| Vertices | VertexPositionNormalColor[] | Position + face normal + color per vertex |
| Indices | int[] | Triangle indices (3 per face) |
| WireframeVertices | Vector3[] | Line endpoint positions for wireframe |
| WireframeIndices | int[] | Line segment indices (2 per edge) |

**Produced by**: `ShapeGeometry.buildCustomMesh` from proto Shape data.
**Consumed by**: `SceneManager.createEntity` and `DebugRenderer.createWireframeEntities`.

### CompoundChildRenderInfo

Per-child rendering data for compound shapes.

| Field | Type | Description |
|-------|------|-------------|
| Shape | Shape | Child's proto shape |
| LocalPosition | Vector3 | Offset from compound body origin |
| LocalRotation | Quaternion | Local orientation |

**Source**: `shape.Compound.Children` proto repeated field.

## State Changes

### SceneState (extended)

No new fields. Existing `Bodies: Map<string, Entity>` already supports multiple entities per body (compound children share the parent body ID via entity hierarchy).

### ShapeGeometry Module (new functions)

| Function | Input | Output | Purpose |
|----------|-------|--------|---------|
| `buildTriangleMesh` | Shape (Triangle) | CustomMeshData | Single triangle → 1 face, 3 edges |
| `buildMeshMesh` | Shape (Mesh) | CustomMeshData | N triangles → N faces, edges |
| `buildConvexHullMesh` | Shape (ConvexHull) | CustomMeshData | Point cloud → hull faces via MIConvexHull |
| `buildCustomMesh` | Shape | CustomMeshData option | Dispatcher: returns Some for custom shapes, None for primitives |

## Relationships

```
Body (proto)
 └─ Shape (proto oneof)
     ├─ Primitive (Sphere/Box/Capsule/Cylinder/Plane)
     │   └─ Create3DPrimitive path (unchanged)
     ├─ Custom (Triangle/Mesh/ConvexHull)
     │   └─ CustomMeshData → MeshDraw → Model → Entity
     ├─ Compound
     │   └─ Children[] → recursive per-child rendering
     ├─ CachedRef
     │   └─ MeshResolver.resolve → Shape → re-enter this dispatch
     └─ ShapeRef
         └─ RegisteredShapes lookup → Shape → re-enter this dispatch
```
