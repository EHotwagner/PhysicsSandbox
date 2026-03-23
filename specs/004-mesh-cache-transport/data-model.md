# Data Model: Mesh Cache and On-Demand Transport

**Branch**: `004-mesh-cache-transport` | **Date**: 2026-03-23

## Entities

### MeshId (Value Object)

A content-addressed identifier for a unique mesh geometry.

| Field | Type | Description |
|-------|------|-------------|
| value | string (32 hex chars) | SHA-256 hash (truncated to 128 bits) of canonical proto-serialized geometry bytes |

**Identity rule**: Two shapes with identical geometry MUST produce the same MeshId. Computed from geometry-only fields (no position, orientation, color, or material).

**Immutability**: Once a MeshId is assigned, the associated geometry never changes. A modified shape gets a new MeshId.

### CachedShapeRef (Proto Message)

Lightweight reference replacing inline geometry in state updates.

| Field | Type | Description |
|-------|------|-------------|
| mesh_id | string | Content-addressed mesh identifier |
| bbox_min | Vec3 | Axis-aligned bounding box minimum corner |
| bbox_max | Vec3 | Axis-aligned bounding box maximum corner |

**Wire size**: ~56 bytes (32 char string + 2 × 3 doubles). Replaces 100s–1000s of bytes of inline geometry.

**Relationship**: Added as field 11 in the `Shape` oneof. Only used for complex shapes (ConvexHull, MeshShape, Compound).

### MeshGeometry (Proto Message)

Full geometry definition paired with its content-addressed ID.

| Field | Type | Description |
|-------|------|-------------|
| mesh_id | string | Content-addressed mesh identifier |
| shape | Shape | The full shape definition (ConvexHull, MeshShape, or Compound) |

**Lifecycle**: Created once when a complex shape is first seen by the simulation. Stored in server cache indefinitely (until simulation reset/disconnect). Transmitted to subscribers via `SimulationState.new_meshes` or `FetchMeshes` RPC.

### MeshCache — Server (Runtime Entity)

Server-side authoritative cache of all known mesh geometries.

| Field | Type | Description |
|-------|------|-------------|
| entries | Map<string, Shape> | MeshId → full Shape proto |

**State transitions**:
- **Empty** → **Populated**: When simulation sends state with `new_meshes`
- **Populated** → **Empty**: On simulation disconnect or `ResetSimulation` command
- **Populated** → **Populated**: When new meshes arrive (additive, never removes individual entries during a session)

**Concurrency**: Read by `FetchMeshes` RPC handlers (multiple concurrent subscribers). Written by `publishState` (single simulation connection). Thread-safe via ConcurrentDictionary.

### MeshCache — Subscriber (Runtime Entity)

Local cache on each subscriber (viewer, client, MCP).

| Field | Type | Description |
|-------|------|-------------|
| entries | Map<string, Shape> | MeshId → full Shape proto |
| pending | Set<string> | MeshIds currently being fetched (prevents duplicate requests) |

**State transitions**:
- **Empty** → **Populated**: From `SimulationState.new_meshes` on state receipt, or from `FetchMeshes` response
- **Populated** → **Empty**: On simulation reset notification (all bodies cleared from state)
- **Miss** → **Pending** → **Resolved**: When state contains unknown CachedShapeRef, fetch initiated, response received

## Relationships

```text
SimulationState
  ├── repeated Body
  │     └── Shape (oneof)
  │           ├── [primitives: Sphere, Box, Capsule, Cylinder, Plane, Triangle] — inline
  │           ├── [complex: ConvexHull, MeshShape, Compound] — first tick only
  │           └── CachedShapeRef (mesh_id + bbox) — subsequent ticks
  └── repeated MeshGeometry (new_meshes) — geometry for newly-seen mesh_ids

Server MeshCache
  └── keyed by mesh_id → Shape (full geometry)
       ├── populated from SimulationState.new_meshes
       └── queried by FetchMeshes RPC

Subscriber MeshCache
  └── keyed by mesh_id → Shape (full geometry)
       ├── populated from SimulationState.new_meshes (proactive)
       └── populated from FetchMeshes response (on-demand, late joiners)
```

## Shape Classification

| Shape Type | Category | Transport | Caching |
|------------|----------|-----------|---------|
| Sphere | Primitive | Always inline | None needed |
| Box | Primitive | Always inline | None needed |
| Capsule | Primitive | Always inline | None needed |
| Cylinder | Primitive | Always inline | None needed |
| Plane | Primitive | Always inline | None needed |
| Triangle | Primitive | Always inline | None needed |
| ConvexHull | Complex | Inline once, then CachedShapeRef | Content-addressed |
| MeshShape | Complex | Inline once, then CachedShapeRef | Content-addressed |
| Compound | Complex | Inline once, then CachedShapeRef | Content-addressed |
| ShapeReference | Reference | Resolved to underlying shape | Inherits from resolved shape |

## Validation Rules

- MeshId MUST be exactly 32 hex characters (128 bits of SHA-256)
- CachedShapeRef.bbox_min MUST be component-wise ≤ bbox_max
- MeshGeometry.shape MUST be one of: ConvexHull, MeshShape, Compound (never a primitive or CachedShapeRef)
- FetchMeshes request with empty mesh_ids returns empty response (not an error)
- FetchMeshes request with unknown mesh_ids returns only the meshes found (partial response, not an error)
