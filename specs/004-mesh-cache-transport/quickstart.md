# Quickstart: Mesh Cache and On-Demand Transport

**Branch**: `004-mesh-cache-transport` | **Date**: 2026-03-23

## Overview

This feature replaces repeated inline mesh geometry in gRPC state updates with content-addressed identifiers. Complex shapes (ConvexHull, MeshShape, Compound) are transmitted once, cached, and referenced by ID thereafter. Primitives are unaffected.

## Key Concepts

1. **MeshId**: SHA-256 content hash (32 hex chars) of a shape's geometry. Identical geometry always produces the same ID.
2. **CachedShapeRef**: Lightweight proto message (mesh_id + bounding box) that replaces inline geometry in `Body.Shape` after first transmission.
3. **new_meshes**: Field on `SimulationState` carrying full geometry for meshes first seen in that tick.
4. **FetchMeshes RPC**: On-demand unary RPC for subscribers to request geometry by mesh_id (late joiners, missed updates).

## Data Flow

```
Tick N (new ConvexHull body added):
  Simulation → Server:
    Body.Shape = CachedShapeRef(mesh_id="abc123...", bbox_min, bbox_max)
    SimulationState.new_meshes = [MeshGeometry(mesh_id="abc123...", shape=ConvexHull(...))]
  Server: caches geometry, forwards state to subscribers
  Subscriber: receives new_meshes, caches locally, renders full shape

Tick N+1 (same body, no change):
  Simulation → Server:
    Body.Shape = CachedShapeRef(mesh_id="abc123...", bbox_min, bbox_max)
    SimulationState.new_meshes = []  (nothing new)
  Subscriber: resolves mesh_id from local cache

Late joiner connects at Tick N+100:
  Subscriber: receives state with CachedShapeRef IDs
  Subscriber: unknown IDs → calls FetchMeshes(["abc123..."])
  Server: returns geometry from cache
  Subscriber: caches, replaces bounding box placeholders
```

## Changed Files (by component)

### Proto Contract
- `physics_hub.proto`: New `CachedShapeRef` message (Shape oneof field 11), new `MeshGeometry` message, new `SimulationState.new_meshes` field, new `FetchMeshes` RPC on PhysicsHub, new `MeshRequest`/`MeshResponse` messages.

### Simulation
- `MeshIdGenerator.fs/.fsi`: Computes content-hash mesh IDs from proto Shape geometry.
- `SimulationWorld.fs/.fsi`: Tracks emitted mesh IDs per session. `buildBodyProto` emits CachedShapeRef for complex shapes. `buildState` populates `new_meshes` for first-seen mesh IDs. Computes AABB on body addition.

### Server
- `MeshCache.fs/.fsi`: Thread-safe ConcurrentDictionary<mesh_id, Shape>. Populated from state updates. Queried by FetchMeshes.
- `MessageRouter.fs`: Intercepts `new_meshes` in `publishState`, populates MeshCache. Clears cache on reset/disconnect.
- `PhysicsHubService.fs`: Implements `FetchMeshes` RPC.

### Viewer
- `MeshResolver.fs/.fsi`: Local mesh cache + async FetchMeshes client. Populated from new_meshes and on-demand fetch.
- `ShapeGeometry.fs`: Handles `CachedShapeRef` → returns bounding box primitive type and size from bbox_min/bbox_max.
- `SceneManager.fs`: On state update, feeds unknown mesh_ids to MeshResolver. Renders placeholders for pending. Replaces with full shapes when resolved.

### Client
- `MeshResolver.fs/.fsi`: Local mesh cache + blocking FetchMeshes client.
- `StateDisplay.fs`: Resolves CachedShapeRef to get shape type description. Falls back to "Cached(mesh_id)" if unresolved.

### MCP
- `MeshResolver.fs/.fsi`: Local mesh cache + FetchMeshes client.
- `RecordingEngine.fs`: Records state as-is (with CachedShapeRef). Writes MeshDefinition log entries for new meshes.

## Testing Strategy

- **Unit**: MeshIdGenerator determinism, MeshCache CRUD + invalidation, MeshResolver cache hits/misses, ShapeGeometry CachedShapeRef handling
- **Integration**: End-to-end flow — add complex body, verify state messages shrink, late-join subscriber fetches meshes, placeholder→real transition
- **Surface area**: Update baselines for SimulationWorld, MeshCache, MeshResolver modules
