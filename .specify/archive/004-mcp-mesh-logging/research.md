# Research: MCP Mesh Fetch Logging

**Branch**: `004-mcp-mesh-logging` | **Date**: 2026-03-23

## R1: Observation Mechanism for FetchMeshes Activity

**Decision**: Publish mesh fetch observations through the existing `CommandEvent` audit stream. After handling a FetchMeshes RPC, `PhysicsHubService` publishes a `CommandEvent` containing a mesh fetch observation to the audit stream via `publishCommandEvent`. The MCP already subscribes to this stream via `GrpcConnection.StreamCommands` and forwards events to `RecordingEngine.OnCommandReceived`.

**Rationale**: The MCP and PhysicsServer are separate Aspire services — the MCP cannot directly access the server's DI container or MessageRouter. A callback on MessageRouter would require cross-service wiring that doesn't exist. The command audit stream (`StreamCommands`) is already the established bridge: the MCP subscribes to it for recording command events. Extending this to include mesh fetch observations requires no new streaming infrastructure and no cross-service callback mechanism.

**Alternatives considered**:
- Direct callback on MessageRouter: Rejected — MCP cannot access server-side DI. Would require a new cross-service mechanism.
- MCP intercepts FetchMeshes via its own gRPC client: Rejected — MCP is a subscriber, not the server. Observation must happen server-side.
- New dedicated gRPC stream for fetch events: Over-engineered for a low-frequency event. The audit stream already exists.

**Implementation**: In `PhysicsHubService.FetchMeshes`, after computing hits/misses, publish a `CommandEvent` with a recognizable pattern (e.g., a `SimulationCommand` wrapper or a direct marker) to the audit stream. The `RecordingEngine.OnCommandReceived` detects this pattern and enqueues a `LogEntry.MeshFetchEvent`.

## R2: MeshFetchEvent Proto Serialization

**Decision**: Serialize MeshFetchEvent as a new proto message `MeshFetchLog` with fields: `repeated string requested_ids`, `int32 hits`, `int32 misses`, `repeated string missed_ids`. Use EntryType byte value `3` (next after MeshDefinition=2).

**Rationale**: Using a proto message for the payload maintains consistency with existing entry types (StateSnapshot serializes SimulationState, CommandEvent serializes CommandEvent, MeshDefinition serializes MeshGeometry). A dedicated `MeshFetchLog` proto message is cleaner than ad-hoc binary encoding.

**Alternative considered**: JSON encoding of the fetch event. Rejected — all other entry types use protobuf binary; consistency matters for the reader/writer pipeline.

## R3: Query Tool Design

**Decision**: Add a single `query_mesh_fetches` MCP tool that accepts: `session_id` (optional, defaults to active), `minutes_ago` (time window), `mesh_id` (optional filter), `page_size`, `cursor`. Returns paginated results matching existing query tool patterns.

**Rationale**: Follows the exact pattern of `query_events` and `query_snapshots`. Filtering by mesh_id requires scanning the deserialized payload — acceptable for the expected low volume of mesh fetch events.
