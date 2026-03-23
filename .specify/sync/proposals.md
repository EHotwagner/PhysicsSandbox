# Drift Resolution Proposals

Generated: 2026-03-23
Based on: drift-report from 2026-03-23 (004-mesh-cache-transport)

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Backfill (Code -> Spec) | 3 |
| Align (Spec -> Code) | 0 |
| Human Decision | 0 |
| New Specs | 0 |
| Remove from Spec | 0 |

## Proposals

### Proposal 1: 004-mesh-cache-transport/FR-010

**Direction**: BACKFILL

**Current State**:
- Spec says: "System MUST invalidate mesh caches when the simulation is reset or bodies are removed."
- Code does: "Reset and disconnect clear server MeshCache and simulation EmittedMeshIds. Body removal does NOT clear orphaned mesh entries from the server cache."

**Proposed Resolution**:

Update FR-010 to:

> **FR-010**: System MUST invalidate all mesh caches when the simulation is reset or the simulation disconnects. Individual body removal does not evict mesh entries — shared mesh IDs may still be referenced by other bodies, and orphaned entries are reclaimed on the next reset or disconnect. This reset-based eviction strategy is sufficient for typical sandbox usage where scenes are transient.

**Rationale**: The spec's own Assumptions section (line 143) states "Cache eviction on the server is not needed for typical sandbox usage (scenes are transient and reset frequently)." Per-body eviction would require reference counting (tracking which bodies share a mesh ID), adding complexity with no practical benefit at sandbox scale. The implementation correctly clears all caches on reset and disconnect, which are the natural session boundaries. Content-addressed IDs prevent any correctness issues from orphaned entries.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 2: 004-mesh-cache-transport/FR-011

**Direction**: BACKFILL

**Current State**:
- Spec says: "System MUST handle compound shapes by independently caching each child shape's geometry."
- Code does: "Compound shapes are cached as a single atomic unit (one SHA-256 hash of entire Compound proto). Children are not individually identified or cached."

**Proposed Resolution**:

Update FR-011 to:

> **FR-011**: System MUST handle compound shapes as cacheable units. A compound shape receives a single content-addressed identifier derived from the serialized representation of all its children (shapes and local poses). Identical compound structures produce the same identifier. Individual children within a compound are not independently cached — the compound is treated as an atomic geometry unit for caching purposes.

**Rationale**: The research.md (R1) explicitly documents the design: "Compound: recursively serialize each child's shape + local pose (position + orientation)" — the hash includes all children but produces one ID for the whole compound. Per-child caching would require multiple MeshIds per BodyRecord, hierarchical cache lookup, and complex partial-resolution logic in the viewer. This adds significant complexity for a scenario (shared sub-meshes across different compounds) that doesn't occur in typical sandbox usage. The atomic approach correctly deduplicates identical whole compounds and is tested.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 3: Backfill MeshDefinition Recording

**Direction**: BACKFILL (unspecced code into existing spec)

**Current State**:
- No spec coverage for MCP recording of mesh definitions
- Code writes `LogEntry.MeshDefinition` entries to recording sessions when `state.NewMeshes` arrives

**Proposed Resolution**:

Add to spec under FR section:

> **FR-016**: The MCP recording system MUST persist mesh geometry definitions alongside state snapshots so that recording sessions are self-contained and can be replayed without requiring a live server to resolve mesh identifiers.

**Rationale**: Without mesh definitions in the recording, replayed sessions containing CachedShapeRef bodies would be unresolvable — viewers would show only bounding box placeholders. Writing mesh definitions makes sessions self-contained, following the existing StateSnapshot and CommandEvent recording pattern. The implementation adds ~20 lines across Types.fs, ChunkWriter.fs, ChunkReader.fs, and RecordingEngine.fs.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify
