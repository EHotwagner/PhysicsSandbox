# Research: Mesh Cache and On-Demand Transport

**Branch**: `004-mesh-cache-transport` | **Date**: 2026-03-23

## R1: Content-Addressed Mesh Identifier Strategy

**Decision**: Use SHA-256 hash of the canonical protobuf-serialized bytes of the shape's geometry data, truncated to 16 bytes, encoded as a 32-character hex string.

**Rationale**: Content-addressing ensures identical geometry always maps to the same ID regardless of which body or command created it. Protobuf serialization is deterministic for the same message content (repeated field ordering is preserved, no map fields in shape messages). SHA-256 is widely available in .NET (`System.Security.Cryptography.SHA256`), fast for small inputs, and collision-resistant. 16 bytes (128 bits) provides negligible collision probability for sandbox-scale usage (< 10^6 shapes).

**Alternatives considered**:
- Sequential integer IDs: Simple but not content-addressed — duplicate geometry would get separate IDs, defeating deduplication (FR-012).
- Full SHA-256 (32 bytes / 64 hex chars): Unnecessarily long for this scale. 128 bits is sufficient.
- GUID/UUID: Not content-derived — same geometry, different ID. Fails deduplication.

**Implementation detail**: Hash only the geometry-relevant fields:
- ConvexHull: serialize `repeated Vec3 points`
- MeshShape: serialize `repeated MeshTriangle triangles`
- Compound: recursively serialize each child's shape + local pose (position + orientation)

## R2: Proto Message Design for CachedShapeRef

**Decision**: Add a `CachedShapeRef` message to the `Shape` oneof (field number 11) carrying `mesh_id`, `bbox_min`, `bbox_max`. Add a `repeated MeshGeometry new_meshes` field to `SimulationState` for proactive push of newly-seen geometries.

**Rationale**: Adding to the existing Shape oneof means all existing code paths that switch on `ShapeCase` naturally handle the new variant. The `new_meshes` field in SimulationState allows the simulation to push geometry proactively on the same tick a new shape appears, so subscribers that are already connected receive it without a separate fetch. Late joiners use `FetchMeshes` RPC for anything they missed.

**Alternatives considered**:
- Separate `Body.cached_shape` field alongside `Body.shape`: Breaks the Shape oneof abstraction. Every consumer would need to check two fields.
- Embedding mesh_id inside existing shape messages: Would require modifying all 3 complex shape messages and adding optional fields, complicating the invariant that a shape either has geometry OR an ID, never both.
- Server-only ID assignment (simulation sends full shapes always): Doubles simulation→server bandwidth. The simulation is the natural place to compute IDs since it owns the geometry.

## R3: Mesh Fetch RPC Design

**Decision**: Add `FetchMeshes` as a unary RPC on the existing `PhysicsHub` service: `rpc FetchMeshes (MeshRequest) returns (MeshResponse)`. MeshRequest carries `repeated string mesh_ids`. MeshResponse carries `repeated MeshGeometry meshes` (each with `mesh_id` + full `Shape`).

**Rationale**: A unary RPC is simplest and sufficient for this use case. Mesh fetches happen infrequently (on connect or when new shapes appear that the subscriber missed via `new_meshes`). The payload size per fetch is bounded by the number of unique complex shapes in the scene (typically < 20). Adding to the existing `PhysicsHub` service avoids a new service definition and keeps the gRPC channel count low.

The "separate channel" requirement (FR-007/US4) is satisfied because `FetchMeshes` is a distinct RPC call that does not share the `StreamState` server-streaming connection. gRPC multiplexes RPCs over HTTP/2 streams, so a `FetchMeshes` call cannot block or delay `StreamState` delivery.

**Alternatives considered**:
- Bidirectional streaming (`MeshExchange`): More complex, no clear benefit. Subscribers typically need meshes in a burst (on connect) then rarely again. Unary handles bursts fine.
- New `MeshCacheService` definition: Adds ceremony (new service, new client, new DI registration) for a single RPC. Unnecessary when PhysicsHub already exists.
- REST/HTTP endpoint: Breaks the gRPC-only communication pattern (Constitution Principle II).

## R4: Simulation State Building Changes

**Decision**: Modify `SimulationWorld.buildBodyProto` to emit `CachedShapeRef` for complex shapes after their first appearance. Track `emittedMeshIds: Set<string>` in the World state. On first occurrence of a new mesh_id, add the full `MeshGeometry` to `SimulationState.new_meshes` and the `CachedShapeRef` to the body. On subsequent ticks, only emit `CachedShapeRef`.

**Rationale**: The simulation already stores `BodyRecord.ShapeProto` (the original proto shape). Computing the mesh_id from this proto is a one-time cost per body addition. The `emittedMeshIds` set grows monotonically (bounded by unique complex shapes in the scene) and is cleared on simulation reset.

**Implementation flow**:
1. When `AddBody` command processed with a complex shape: compute mesh_id, compute AABB, store in body record
2. In `buildBodyProto`: if body has mesh_id → emit CachedShapeRef; if mesh_id not in emittedMeshIds → also add to new_meshes
3. In `buildState`: attach new_meshes, then update emittedMeshIds

## R5: Server Mesh Cache Design

**Decision**: `MeshCache` module in `PhysicsServer.Hub` with a `ConcurrentDictionary<string, Shape>` keyed by mesh_id. Populated from `SimulationState.new_meshes` during `publishState`. Queried by `FetchMeshes` RPC. Cleared on simulation disconnect or reset command.

**Rationale**: ConcurrentDictionary provides thread-safe reads without locking (important since `FetchMeshes` and `publishState` run concurrently). The server already processes every state update in `MessageRouter.publishState`, making it the natural point to intercept and cache `new_meshes`. The existing `StateCache` pattern (lock-based) is a precedent, but ConcurrentDictionary is better here since reads vastly outnumber writes.

**Cache lifecycle**:
- Populate: on each `publishState`, iterate `state.NewMeshes`, add to cache
- Query: `FetchMeshes` reads from cache, returns found meshes, omits unknown IDs (with warning log)
- Invalidate: clear cache when simulation disconnects (`disconnectSimulation`) or on `ResetSimulation` command

## R6: Subscriber MeshResolver Pattern

**Decision**: Each subscriber (viewer, client, MCP) gets a `MeshResolver` module with identical interface: local `ConcurrentDictionary<string, Shape>` cache, populated from `SimulationState.new_meshes` + `FetchMeshes` results. Exposes `resolve(meshId) -> Shape option` and `fetchMissing(meshIds) -> Async<unit>`.

**Rationale**: All three subscribers need the same capability: receive state with CachedShapeRef IDs, resolve to full shapes from local cache, fetch missing ones. The pattern is simple enough that a shared library would be over-engineering — each module is ~50 lines and can be tailored to its consumer's needs (viewer needs AABB for placeholder, client needs shape type for text, MCP needs full shape for recording).

**Fetch strategy**: On each state update, collect unknown mesh_ids, batch into a single `FetchMeshes` call. For the viewer, this runs asynchronously so it doesn't block rendering (placeholder shown meanwhile). For client/MCP, blocking fetch is acceptable since they don't render.

## R7: MCP Recording Impact

**Decision**: Recording stores the state as-received (with CachedShapeRef IDs). A separate `mesh_definitions` metadata entry is written to the session when new meshes arrive, enabling replay without the server.

**Rationale**: Recording the compact CachedShapeRef form preserves the bandwidth savings on disk. Storing mesh definitions alongside (in the same session) makes recordings self-contained. The existing `LogEntry` discriminated union gets a new case: `LogEntry.MeshDefinition(timestamp, meshId, shape)`.

**Alternatives considered**:
- Resolve all CachedShapeRefs back to full shapes before recording: Eliminates the bandwidth savings this feature provides. Disk usage would be unchanged from current approach.
- Store mesh definitions in a separate file: Complicates session management. Better to keep them in the same chunk stream.

## R8: Bounding Box Computation

**Decision**: Compute AABB (axis-aligned bounding box) as `Vec3 bbox_min` and `Vec3 bbox_max` when mesh geometry is first seen. For ConvexHull: iterate points. For MeshShape: iterate all triangle vertices. For Compound: recursively compute child AABBs, transform by local pose, union.

**Rationale**: AABB is the simplest bounding volume that provides meaningful placeholder sizing. The viewer already computes AABBs from shape geometry in `ShapeGeometry.shapeSize` — this moves that computation to the simulation (one-time) and transmits the result. The existing shapeSize logic for ConvexHull (iterate points, find min/max) and MeshShape (iterate triangle vertices) directly translates.

**Per-message overhead**: 2 × Vec3 = 6 doubles = 48 bytes per CachedShapeRef. Compared to a ConvexHull with 50 points (50 × 3 doubles × 8 bytes = 1200 bytes), this is a 96% reduction.
