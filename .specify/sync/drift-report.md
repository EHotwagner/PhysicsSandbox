# Spec Drift Report

Generated: 2026-03-23
Project: PhysicsSandbox (004-mesh-cache-transport)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 20 (15 FR + 5 SC) |
| Aligned | 18 (90%) |
| Drifted | 2 (10%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 1 |

## Detailed Findings

### Spec: 004-mesh-cache-transport - Mesh Cache and On-Demand Transport

#### Aligned

- **FR-001**: Unique stable mesh identifiers via SHA-256 content hash (32 hex chars). ConvexHull, MeshShape, Compound handled. Triangle excluded as primitive. -> `src/PhysicsSimulation/World/MeshIdGenerator.fs`
- **FR-002**: State updates use CachedShapeRef (mesh_id + bbox) instead of inline geometry for complex shapes after first appearance. -> `src/PhysicsSimulation/World/SimulationWorld.fs:82-95`
- **FR-003**: Full geometry included in `SimulationState.new_meshes` on first appearance, tracked via `EmittedMeshIds` set. -> `src/PhysicsSimulation/World/SimulationWorld.fs:128-157`
- **FR-004**: Server maintains ConcurrentDictionary mesh cache, populated from state updates, cleared on reset/disconnect. -> `src/PhysicsServer/Hub/MeshCache.fs`, `src/PhysicsServer/Hub/MessageRouter.fs:173-178`
- **FR-005**: `FetchMeshes` unary RPC implemented, returns partial hits (unknown IDs silently skipped). -> `src/PhysicsServer/Services/PhysicsHubService.fs:75-85`
- **FR-006**: All three subscribers (viewer, client, MCP) maintain local ConcurrentDictionary caches with processNewMeshes/resolve/fetchMissing. -> `src/PhysicsViewer/Streaming/MeshResolver.fs`, `src/PhysicsClient/Connection/MeshResolver.fs`, `src/PhysicsSandbox.Mcp/MeshResolver.fs`
- **FR-007**: FetchMeshes is a separate unary RPC from StreamState. Viewer calls it asynchronously via `Async.Start`. -> `physics_hub.proto:37`, `src/PhysicsViewer/Program.fs:112`
- **FR-008**: Viewer renders CachedShapeRef as Cube placeholder with semi-transparent magenta color, sized from precomputed bbox. -> `src/PhysicsViewer/Rendering/ShapeGeometry.fs:24,138-146,163`
- **FR-009**: SceneManager tracks placeholder entities in `Placeholders: Set<string>`, recreates entity with real shape when mesh resolves. -> `src/PhysicsViewer/Rendering/SceneManager.fs:108-123`
- **FR-012**: Content-addressed SHA-256 hashing guarantees identical geometry produces identical mesh ID. Two bodies with same shape share one cache entry. -> `src/PhysicsSimulation/World/MeshIdGenerator.fs:29-39`
- **FR-013**: Primitives (Sphere, Box, Capsule, Cylinder, Plane, Triangle) always inline. Only ConvexHull, MeshShape, Compound compute mesh IDs. -> `src/PhysicsSimulation/World/MeshIdGenerator.fs:8-15`
- **FR-014**: Bounding box precomputed at AddBody time via `computeBoundingBox`, stored in `BodyRecord.BoundingBox`. -> `src/PhysicsSimulation/World/SimulationWorld.fs:365-367`
- **FR-015**: CachedShapeRef proto message carries `bbox_min` and `bbox_max` alongside `mesh_id`. -> `physics_hub.proto:289-293`, `SimulationWorld.fs:86-93`
- **SC-001**: Structural 80%+ reduction: CachedShapeRef ~56 bytes vs ConvexHull ~1200+ bytes per body per tick.
- **SC-002**: Integration test validates late-joiner resolves meshes within 5s timeout. -> `tests/PhysicsSandbox.Integration.Tests/MeshCacheIntegrationTests.cs:111-155`
- **SC-003**: Viewer uses `Async.Start` for mesh fetch; state processing loop unblocked. -> `src/PhysicsViewer/Program.fs:112`
- **SC-004**: Deduplication via content hash + `newMeshIds` Set in buildState ensures one cache entry per unique geometry.
- **SC-005**: Bounding box embedded in CachedShapeRef; ShapeGeometry renders placeholder immediately (no fetch needed).

#### Drifted

- **FR-010**: Spec says "System MUST invalidate mesh caches when the simulation is reset **or bodies are removed**." Reset and disconnect correctly clear server MeshCache and simulation EmittedMeshIds. However, `removeBody` does NOT clear the mesh entry for that body's geometry from the server cache. Orphaned mesh IDs persist until next reset/disconnect.
  - Location: `src/PhysicsSimulation/World/SimulationWorld.fs:482-501`, `src/PhysicsServer/Hub/MessageRouter.fs`
  - Severity: minor
  - Impact: Memory growth in server cache for long-running simulations with frequent body removal. Not a correctness issue since content-addressed IDs prevent stale data conflicts. Spec assumption (line 143) notes "Cache eviction on the server is not needed for typical sandbox usage."

- **FR-011**: Spec says "System MUST handle compound shapes by **independently caching each child** shape's geometry." Implementation caches the entire Compound as one atomic unit (single SHA-256 hash of `Compound.ToByteArray()`). Children are NOT individually identified or cached.
  - Location: `src/PhysicsSimulation/World/MeshIdGenerator.fs:24-25`
  - Severity: minor
  - Impact: Two different compounds sharing some identical children cannot deduplicate those children. Works correctly for deduplicating identical whole compounds. Simplifies the caching layer significantly. Acceptable tradeoff for sandbox-scale usage.

#### Not Implemented

(none)

### Unspecced Code

| Feature | Location | Lines | Notes |
|---------|----------|-------|-------|
| MeshDefinition recording | `src/PhysicsSandbox.Mcp/Recording/` | ~20 | New `LogEntry.MeshDefinition` case writes mesh geometry to recording sessions for self-contained replay. Not in spec but follows existing recording pattern. |

## Inter-Spec Conflicts

None detected.

## Recommendations

1. **FR-010 (body removal)**: Consider adding reference counting to MeshCache — when the last body using a mesh ID is removed, evict the entry. Low priority since reset-based eviction suffices for typical sandbox usage.
2. **FR-011 (compound children)**: The atomic-compound approach is a reasonable simplification. If per-child deduplication becomes valuable (e.g., many compounds sharing identical sub-meshes), the MeshIdGenerator could be extended to emit multiple MeshGeometry entries per compound body. This would be significant scope.
3. **Spec update**: Consider updating FR-011 to reflect the implemented design ("compound shapes are cached as atomic units") since this is a deliberate design choice documented in research.md.
