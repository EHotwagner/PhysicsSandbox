# Feature Specification: Mesh Cache and On-Demand Transport

**Feature Branch**: `004-mesh-cache-transport`
**Created**: 2026-03-23
**Status**: Draft
**Input**: User description: "minimize the amount of meshes transported over grpc. maybe have a separate bidirectional channel that sends meshes with id to server which caches it and distributes it further. the client/viewer send any id they dont have cached locally to the server which will send it to them. while they dont have the mesh show a bounding box placeholder."

## Clarifications

### Session 2026-03-23

- Q: Where do bounding box dimensions for unresolved mesh placeholders come from? → A: The state update must carry precomputed bounding box extents alongside the mesh identifier. The bounding box is calculated once when the mesh geometry is first registered in the cache.
- Q: Must old subscribers without cache support still receive full inline geometry? → A: No. Coordinated upgrade — all components adopt the new format together. No backward-compatibility code path needed.
- Q: Should single Triangle shapes be cached or sent inline? → A: Treat as primitive — send inline every tick like sphere/box. Only convex hulls, mesh shapes, and compound shapes use identifier-based caching.
- Q: Do primitives carry mesh data? → A: No. Primitives (sphere, box, capsule, cylinder, plane, triangle) carry no mesh geometry — they are fully described by their shape type and scalar parameters (radius, extents, etc.). Only complex shapes (convex hull, mesh shape, compound) carry vertex/triangle data that requires caching.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bandwidth-Efficient State Streaming (Priority: P1)

As a user running complex physics simulations with many mesh-based bodies (convex hulls, triangle meshes, compound shapes), I want the system to avoid resending unchanged mesh geometry every tick so that network bandwidth is drastically reduced and the system can scale to larger scenes.

Currently, every state update at 60 Hz includes the full vertex/triangle data for every body's shape — even when that geometry never changes between ticks. For a scene with several mesh shapes, this means megabytes of redundant data per second. This story eliminates that redundancy by replacing inline mesh data with lightweight identifiers after the first transmission.

**Why this priority**: This is the core value proposition. Without bandwidth reduction, all other stories are moot. Even a single convex hull with 50 vertices adds ~1.2 KB per body per tick, multiplied by 60 Hz and N bodies.

**Independent Test**: Can be tested by creating a simulation with mesh-based bodies and measuring the wire size of state update messages before and after. State updates should contain shape identifiers instead of full geometry for previously-seen shapes.

**Acceptance Scenarios**:

1. **Given** a simulation with 10 convex hull bodies running at 60 Hz, **When** state updates are streamed after the initial tick, **Then** subsequent state messages contain shape identifiers (not full vertex data) for those bodies, and message size is reduced by at least 80% compared to the current approach.
2. **Given** a new mesh body is added mid-simulation, **When** the next state update is sent, **Then** the full mesh geometry is included exactly once, and all subsequent updates use the identifier.
3. **Given** a body changes its shape (e.g., via a command), **When** the new shape is a mesh type, **Then** the new geometry is sent once and assigned a new identifier.

---

### User Story 2 - Server-Side Mesh Cache and Distribution (Priority: P1)

As a late-joining viewer or client, I want to request mesh geometry I don't have cached locally so that I can render the full scene without requiring all geometry to be present in every state update.

The server maintains a cache of all mesh geometries keyed by identifier. When a subscriber (viewer, client, MCP server) encounters an unknown shape identifier in a state update, it requests the geometry from the server. The server responds with the full mesh data for that identifier.

**Why this priority**: Equal to P1 because without server-side caching and on-demand retrieval, the identifier-based approach breaks for late joiners and reconnecting clients.

**Independent Test**: Can be tested by starting a simulation, adding mesh bodies, then connecting a new subscriber. The subscriber should receive identifiers in state updates, request unknown meshes from the server, and receive the full geometry.

**Acceptance Scenarios**:

1. **Given** a simulation with mesh bodies is running, **When** a new viewer connects, **Then** it receives state updates with shape identifiers and can request the full geometry for any identifier it does not recognize.
2. **Given** a subscriber has disconnected and reconnected, **When** it receives state updates, **Then** it only requests mesh data for identifiers not already in its local cache.
3. **Given** multiple subscribers request the same mesh identifier, **When** the server processes the requests, **Then** it serves the geometry from its cache without re-requesting from the simulation.

---

### User Story 3 - Bounding Box Placeholder While Mesh Loads (Priority: P2)

As a viewer user, I want to see a bounding box placeholder for any body whose mesh geometry has not yet been received so that the scene remains interactive and spatially coherent while mesh data loads on demand.

When a state update arrives with a shape identifier that the viewer hasn't resolved yet, the viewer displays a bounding box approximation at the correct position and orientation. Once the full geometry arrives, the placeholder is seamlessly replaced.

**Why this priority**: This is a UX enhancement. The system is functional without it (viewers already render complex shapes as bounding boxes today), but explicit placeholder-to-real transitions provide visual feedback that mesh loading is in progress.

**Independent Test**: Can be tested by connecting a viewer to a running simulation with mesh bodies and observing that unresolved shapes appear as bounding boxes, then transition to full representations once geometry is received.

**Acceptance Scenarios**:

1. **Given** a viewer receives a state update with an unknown mesh identifier, **When** the geometry has not yet been fetched, **Then** the body is rendered as a bounding box placeholder at the correct world position, rotation, and approximate size.
2. **Given** the viewer subsequently receives the mesh geometry, **When** the next frame renders, **Then** the placeholder is replaced with the actual shape representation.
3. **Given** the mesh request fails or times out, **When** the viewer cannot obtain the geometry, **Then** the bounding box placeholder persists and the viewer does not crash or stall.

---

### User Story 4 - Bidirectional Mesh Channel (Priority: P2)

As a system operator, I want mesh geometry exchange to happen on a dedicated channel separate from the main state stream so that mesh fetching does not block or slow down real-time state updates.

The main state stream remains lightweight and fast (positions, rotations, identifiers). Mesh geometry requests and responses flow over a separate bidirectional channel, decoupling bulk data transfer from the latency-sensitive simulation state.

**Why this priority**: Separating channels prevents large mesh transfers from introducing jitter in the 60 Hz state stream. This is important for interactive feel but not strictly required for correctness.

**Independent Test**: Can be tested by transferring large meshes while monitoring state update latency. State updates should maintain consistent timing regardless of concurrent mesh transfers.

**Acceptance Scenarios**:

1. **Given** a subscriber is receiving state updates at 60 Hz, **When** it simultaneously requests a large mesh geometry, **Then** state update delivery timing is not degraded by the mesh transfer.
2. **Given** the mesh channel is under load (many concurrent requests), **When** the state stream continues, **Then** state updates arrive without increased latency or dropped messages.

---

### Edge Cases

- What happens when a mesh identifier references geometry that was evicted from the server cache? The server should re-request it from the simulation or return an error with enough information for the subscriber to handle gracefully.
- What happens when the simulation is reset or all bodies are cleared? All mesh caches (server and subscriber) should be invalidated so stale identifiers don't map to wrong geometry.
- What happens when a compound shape contains children that are themselves mesh types? Each child mesh should get its own identifier and be independently cacheable.
- What happens when two bodies use the same registered shape? They should share the same mesh identifier and geometry — only one copy should be cached and transferred.
- How are mesh identifiers generated? They must be deterministic or content-addressed so that identical geometry always maps to the same identifier, enabling deduplication across bodies.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST assign a unique, stable identifier to each distinct mesh geometry (convex hull vertices, mesh shape triangles, compound child definitions). Single triangle shapes are excluded — they are treated as primitives.
- **FR-002**: State update messages MUST use shape identifiers instead of inline geometry for shapes that have been previously transmitted to the subscriber.
- **FR-003**: System MUST include full mesh geometry inline in the state update the first time a shape identifier appears for a given subscriber, OR provide it through the on-demand channel before the subscriber needs it.
- **FR-004**: Server MUST maintain a cache of all mesh geometries keyed by identifier, serving on-demand requests from any subscriber.
- **FR-005**: Subscribers MUST be able to request mesh geometry by identifier from the server at any time.
- **FR-006**: Subscribers MUST maintain a local cache of received mesh geometries to avoid redundant requests.
- **FR-007**: Mesh geometry exchange MUST occur on a channel separate from the main state update stream.
- **FR-008**: Viewer MUST display a bounding box placeholder for bodies whose mesh geometry has not yet been resolved locally.
- **FR-009**: Viewer MUST replace bounding box placeholders with actual shape representations once geometry is received.
- **FR-010**: System MUST invalidate mesh caches when the simulation is reset or bodies are removed.
- **FR-011**: System MUST handle compound shapes by independently caching each child shape's geometry.
- **FR-012**: Bodies sharing identical registered shapes MUST reuse the same mesh identifier and cached geometry.
- **FR-013**: Simple primitive shapes (sphere, box, capsule, cylinder, plane, triangle) MUST continue to be transported inline in state updates without identifiers. These shapes carry no mesh geometry — they are fully described by their shape type and scalar parameters (e.g., radius, extents, height). Mesh caching applies exclusively to convex hulls, mesh shapes, and compound shapes.
- **FR-014**: System MUST precompute and store bounding box extents (axis-aligned min/max or size) when a mesh geometry is first cached.
- **FR-015**: State update messages MUST include the precomputed bounding box extents alongside each mesh identifier, so subscribers can render placeholders without fetching the full geometry.

### Key Entities

- **Mesh Identifier**: A unique key representing a specific mesh geometry. Used to reference cached geometry without transmitting it.
- **Mesh Cache (Server)**: A server-side store of all known mesh geometries, keyed by mesh identifier. Serves on-demand requests.
- **Mesh Cache (Subscriber)**: A local store on each subscriber (viewer, client, MCP) that tracks which mesh identifiers have been resolved.
- **Mesh Channel**: A dedicated bidirectional communication path for mesh geometry requests and responses, separate from the state stream.
- **Shape Reference (Enhanced)**: An augmented shape representation in state updates that carries a mesh identifier and precomputed bounding box extents instead of full geometry for previously-seen complex shapes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: State update message size for scenes with 10+ mesh-based bodies is reduced by at least 80% compared to the current approach after initial geometry exchange.
- **SC-002**: Late-joining subscribers can fully resolve all mesh geometries and render the complete scene within 5 seconds of connecting.
- **SC-003**: State update delivery frequency (60 Hz) is maintained with less than 5% jitter during concurrent mesh geometry transfers.
- **SC-004**: Identical shapes used by multiple bodies result in only one cached copy — no duplicate mesh data is stored or transferred.
- **SC-005**: Viewers display placeholder bounding boxes for unresolved meshes within one frame of receiving the state update, with no visual gaps or missing bodies.

## Assumptions

- Primitive shapes (sphere, box, capsule, cylinder, plane, triangle) carry no mesh geometry and are fully described by their type discriminator and scalar parameters. They never participate in mesh caching.
- The mesh identifier scheme should be content-based (deterministic from geometry) so that identical shapes created independently still share identifiers.
- The server is the single authority for mesh cache — subscribers do not share meshes peer-to-peer.
- Mesh geometries are immutable once created; a changed shape gets a new identifier rather than updating the old one.
- The existing `ShapeReference` / registered shape mechanism is complementary but separate — registered shapes are a user-facing concept, while mesh identifiers are a transport optimization.
- Cache eviction on the server is not needed for typical sandbox usage (scenes are transient and reset frequently). If needed in the future, a simple LRU or reset-based eviction can be added.
- This is a coordinated upgrade: all components (simulation, server, viewer, client, MCP) adopt the new message format together. No backward-compatibility shim for old subscribers is needed.
