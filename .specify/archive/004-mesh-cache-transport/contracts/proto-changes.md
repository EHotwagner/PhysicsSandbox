# Proto Contract Changes: Mesh Cache and On-Demand Transport

**File**: `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`

## New Messages

### CachedShapeRef

Added to `Shape` oneof as field 11. Replaces inline geometry for previously-seen complex shapes.

```proto
message CachedShapeRef {
  string mesh_id = 1;    // 32-char hex content hash
  Vec3 bbox_min = 2;     // AABB minimum corner
  Vec3 bbox_max = 3;     // AABB maximum corner
}
```

### MeshGeometry

Pairs a mesh ID with its full geometry definition. Used in `SimulationState.new_meshes` and `FetchMeshes` responses.

```proto
message MeshGeometry {
  string mesh_id = 1;    // 32-char hex content hash
  Shape shape = 2;       // Full shape (ConvexHull, MeshShape, or Compound only)
}
```

### MeshRequest / MeshResponse

Request/response pair for the `FetchMeshes` RPC.

```proto
message MeshRequest {
  repeated string mesh_ids = 1;  // IDs to fetch
}

message MeshResponse {
  repeated MeshGeometry meshes = 1;  // Found geometries (partial OK)
}
```

## Modified Messages

### Shape (oneof)

Add `CachedShapeRef` as field 11:

```proto
message Shape {
  oneof shape {
    Sphere sphere = 1;
    Box box = 2;
    Plane plane = 3;
    Capsule capsule = 4;
    Cylinder cylinder = 5;
    Triangle triangle = 6;
    ConvexHull convex_hull = 7;
    Compound compound = 8;
    MeshShape mesh = 9;
    ShapeReference shape_ref = 10;
    CachedShapeRef cached_ref = 11;   // NEW
  }
}
```

### SimulationState

Add `new_meshes` field (next available field number):

```proto
message SimulationState {
  repeated Body bodies = 1;
  double time = 2;
  bool running = 3;
  double tick_ms = 4;
  double serialize_ms = 5;
  repeated ConstraintState constraints = 6;
  repeated RegisteredShapeState registered_shapes = 7;
  repeated QueryResponse query_responses = 8;
  repeated MeshGeometry new_meshes = 9;   // NEW: geometries first seen this tick
}
```

## New RPC

### FetchMeshes (on PhysicsHub service)

```proto
service PhysicsHub {
  // ... existing RPCs ...
  rpc FetchMeshes (MeshRequest) returns (MeshResponse);   // NEW
}
```

**Behavior**:
- Returns geometries for requested mesh_ids that exist in the server cache
- Unknown mesh_ids are silently omitted from the response (partial results are valid)
- Empty request returns empty response

## Wire Compatibility

This is a **coordinated upgrade** — all components deploy together. No backward compatibility required. However, the proto changes are additive (new field numbers, new oneof variant, new RPC) so the wire format is naturally forward-compatible if needed.

## Affected Code Paths

Every code path that switches on `Shape.ShapeCase` or `shape.ShapeOneofCase` must handle the new `CachedShapeRef` case:

| Component | File | Current switch | New case behavior |
|-----------|------|---------------|-------------------|
| Simulation | SimulationWorld.fs | `convertShape` | N/A — simulation never receives CachedShapeRef |
| Server | MessageRouter.fs | passes through | Extract new_meshes → MeshCache |
| Viewer | ShapeGeometry.fs | `primitiveType`, `shapeSize` | Return Cube + bbox dimensions |
| Viewer | SceneManager.fs | `createEntity` | Render placeholder, queue fetch |
| Viewer | DebugRenderer.fs | wireframe creation | Render bbox wireframe |
| Client | StateDisplay.fs | `shapeDescription` | Return "Cached(id)" or resolved type |
| Client | LiveWatch.fs | `matchesShape` | Resolve from cache or match "cached" |
| MCP | SimulationTools.fs | tool responses | Resolve from cache for display |
| MCP | RecordingEngine.fs | log entries | Record as-is + log MeshDefinition |
