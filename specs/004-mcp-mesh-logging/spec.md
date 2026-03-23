# Feature Specification: MCP Mesh Fetch Logging

**Feature Branch**: `004-mcp-mesh-logging`
**Created**: 2026-03-23
**Status**: Draft
**Input**: User description: "add mcp logging for the new mesh grpc channel"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record Mesh Fetch Activity (Priority: P1)

As an AI assistant analyzing a physics simulation recording, I want the recording to capture all mesh fetch requests and responses so that I can understand the mesh resolution lifecycle — which meshes were fetched, when, and whether any fetches failed or returned partial results.

Currently, the MCP recording captures state snapshots (including CachedShapeRef bodies), command events, and mesh geometry definitions (MeshDefinition entries from new_meshes). However, the separate FetchMeshes RPC channel is invisible to the recording — there's no way to know after the fact which subscribers fetched which meshes, or whether late-joiner mesh resolution succeeded.

**Why this priority**: This is the core value. Without logging FetchMeshes activity, mesh resolution behavior is a black box in recordings. Debugging late-joiner issues or mesh cache misses requires this visibility.

**Independent Test**: Can be tested by starting a recording, creating mesh bodies, calling FetchMeshes via a late-joining subscriber, then querying the recording for mesh fetch events. The recording should contain entries showing the mesh IDs requested and the hit/miss results.

**Acceptance Scenarios**:

1. **Given** a recording session is active, **When** the server handles a FetchMeshes request, **Then** a MeshFetchEvent entry is recorded with the requested mesh IDs, the number of hits, the number of misses, and a timestamp.
2. **Given** a FetchMeshes call returns zero results (all misses), **When** the recording is queried, **Then** the recorded event clearly shows 0 hits and the list of missed IDs.
3. **Given** multiple FetchMeshes calls occur during a recording, **When** the recording is queried for mesh fetch events, **Then** all calls appear in chronological order with their individual request/response details.

---

### User Story 2 - Query Mesh Fetch History via MCP Tools (Priority: P2)

As an AI assistant debugging mesh resolution issues, I want MCP query tools to retrieve mesh fetch events from recorded sessions so that I can analyze fetch patterns, identify cache miss rates, and diagnose late-joiner resolution timing.

**Why this priority**: Recording without queryability has limited value. Purpose-built query tools make the mesh fetch data actionable for analysis workflows.

**Independent Test**: Can be tested by recording a session with mesh fetch activity, then using an MCP query tool to retrieve mesh fetch events filtered by time range or mesh ID. Results should show fetch details with pagination support.

**Acceptance Scenarios**:

1. **Given** a recorded session with mesh fetch events, **When** an AI assistant calls a query tool for mesh fetch events, **Then** it receives a paginated list of fetch events with mesh IDs, hit/miss counts, and timestamps.
2. **Given** a query for mesh fetch events filtered by a specific mesh ID, **When** the query executes, **Then** only events involving that mesh ID are returned.

---

### Edge Cases

- What happens when FetchMeshes is called but no recording session is active? The fetch proceeds normally; no recording entry is created. No errors.
- What happens when a FetchMeshes request contains zero mesh IDs? An empty request should still be logged (if recording is active) as a valid event with 0 hits, 0 misses.
- What happens when the recording storage limit is reached while writing a mesh fetch event? The existing pruning mechanism handles this — oldest chunks are evicted as with any other entry type.
- What happens when multiple FetchMeshes calls arrive concurrently? Each call is logged independently; the async recording pipeline handles concurrent writes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST record a MeshFetchEvent log entry each time the server handles a FetchMeshes RPC, capturing: timestamp, requested mesh IDs, number of hits, number of misses, and the list of unresolved (missed) mesh IDs.
- **FR-002**: MeshFetchEvent entries MUST be written to the same recording session and chunk files as existing StateSnapshot, CommandEvent, and MeshDefinition entries, using the established binary wire format with a new entry type byte.
- **FR-003**: Recording of mesh fetch events MUST NOT degrade FetchMeshes RPC response time — the recording write must be non-blocking, using the existing async Channel pipeline.
- **FR-004**: An MCP query tool MUST be provided to retrieve mesh fetch events from recorded sessions, supporting time-range filtering and optional mesh ID filtering, with cursor-based pagination consistent with existing query tools.
- **FR-005**: The server MUST publish FetchMeshes observations through the existing command audit stream so that the MCP recording engine can capture mesh fetch activity without requiring a cross-service callback mechanism.
- **FR-006**: The recording session summary (query_summary tool) MUST include a count of recorded mesh fetch events alongside existing snapshot and command event counts.

### Key Entities

- **MeshFetchEvent**: A recorded observation of a FetchMeshes RPC call — timestamp, list of requested mesh IDs, hit count, miss count, list of missed mesh IDs.
- **LogEntry.MeshFetchEvent**: New case in the recording system's LogEntry discriminated union, carrying the MeshFetchEvent data and serialized as a new EntryType discriminator value.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All FetchMeshes RPC calls during an active recording session are captured with zero data loss under normal operation (non-overload conditions).
- **SC-002**: Querying mesh fetch events from a 10-minute recording session returns results within 2 seconds.
- **SC-003**: Recording mesh fetch events adds less than 1ms of overhead to FetchMeshes RPC response time.
- **SC-004**: Recording session summary accurately reports the count of mesh fetch events alongside existing entry type counts.

## Assumptions

- The FetchMeshes RPC is already implemented and functional (from 004-mesh-cache-transport feature).
- The MCP recording pipeline (ChunkWriter, async Channel, pruning) is already operational and does not need structural modification — only a new entry type case.
- The existing binary wire format can accommodate a new entry type byte value (currently 0=StateSnapshot, 1=CommandEvent, 2=MeshDefinition; next available: 3=MeshFetchEvent).
- Mesh fetch events are relatively infrequent compared to state snapshots (at most a few per second vs 60/sec), so storage impact is minimal.
- The server already tracks FetchMeshes hit/miss counts in MetricsCounter; the MCP needs a similar observation point for recording.
- The GrpcConnection callback pattern (OnStateReceived, OnCommandReceived) established by 005-mcp-data-logging is the model for adding a new OnMeshFetchObserved callback.
