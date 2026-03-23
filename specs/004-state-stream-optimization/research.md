# Research: State Stream Bandwidth Optimization

**Feature**: 004-state-stream-optimization
**Date**: 2026-03-23

## R1: Protobuf 3 Default Value Wire Format Behavior

**Decision**: Leverage proto3 zero-default-value semantics for message-type fields; use explicit presence tracking for scalar fields.

**Rationale**: In proto3, fields set to their default value (0, false, empty string, null for messages) are NOT serialized on the wire. This means:
- **Message-type fields** (Vec3, Vec4, Shape, Color, MaterialProperties): not setting them = zero bytes on wire. This works perfectly for our semi-static optimization.
- **Scalar fields** (double `mass`, bool `is_static`, uint32 `collision_group/mask`, enum `motion_type`): their default IS zero/false, which could be a legitimate value. Cannot distinguish "not set" from "set to zero".

However, since we're splitting into two channels entirely (not omitting fields within the same message), this distinction becomes moot. The tick stream uses a new lean message type that simply doesn't have semi-static fields. The bidirectional channel sends full property messages. No ambiguity.

**Alternatives Considered**:
- Proto3 `optional` keyword (adds explicit has_field presence tracking) — unnecessary with the two-channel split
- Wrapper types (google.protobuf.DoubleValue etc.) — adds overhead, unnecessary
- Field masks (google.protobuf.FieldMask) — over-engineered for this use case

## R2: Bidirectional Channel Reuse for Semi-Static Data

**Decision**: Extend the existing `SimulationLink` bidirectional stream and `PhysicsHub` service to carry semi-static body property events alongside existing mesh data and commands.

**Rationale**: The existing architecture already has the patterns we need:
- **SimulationLink** (simulation ↔ server): bidirectional stream where simulation pushes state upstream and receives commands downstream. Currently pushes full `SimulationState` — will be split to push `TickState` (continuous) on the main stream and property/lifecycle events on a new server-published channel.
- **MeshCache + MeshResolver**: Proves the pattern of "send once, cache locally, fetch on demand". Semi-static body properties follow the same lifecycle.
- **StateCache + late-joiner backfill**: Already caches latest state for new subscribers. Will be extended to cache semi-static properties separately.
- **CommandEvent audit stream**: Already publishes lifecycle events. Property changes can be published similarly.

**Key implementation detail**: The `SimulationLink` proto (simulation ↔ server) is **unchanged** — the simulation continues sending full `SimulationState` upstream. The **server** decomposes each `SimulationState` into a `TickState` (for the 60 Hz `StreamState` RPC) and `PropertyEvent` messages (for the new `StreamProperties` RPC). This avoids changing the simulation-to-server protocol and keeps the split logic centralized in the server's `MessageRouter`.

**Alternatives Considered**:
- New separate gRPC service for semi-static data — unnecessary, adds service complexity
- Piggyback on the existing `SimulationState` with omitted fields — doesn't cleanly separate concerns, still serializes empty repeated fields
- WebSocket sidecar — violates constitution (gRPC-only for real-time)

## R3: Client-Side State Merging Strategy

**Decision**: Each client maintains a local `ConcurrentDictionary<string, BodyProperties>` cache populated from the property event stream. On each tick, clients merge the lean `TickState` (pose data) with cached semi-static properties to reconstruct full `Body` objects where needed.

**Rationale**: This mirrors the existing `MeshResolver` pattern (per-client `ConcurrentDictionary` cache, updated from stream, fetched on demand). The merge is simple: tick data provides the live pose, cached properties provide everything else. For display/rendering, each client already reads only the fields it needs.

**Key considerations**:
- **Viewer**: Caches shape + color (the only semi-static fields it reads). Merges with pose for rendering. Ignores mass, material, collision filters.
- **Client/MCP**: Caches all semi-static fields. Merges with pose + velocity for display/recording.
- **RecordingEngine**: Must reconstruct full `SimulationState` for recording compatibility. Merges tick + cached properties into complete snapshots before writing to disk.
- **Late joiners**: Receive full property backfill on connect, then tick stream. No partial state.

**Alternatives Considered**:
- Server-side per-client state reconstruction — adds server CPU/memory overhead per client, doesn't scale
- Periodic full-state snapshots (e.g., every 5 seconds) — adds bandwidth spikes, doesn't solve the per-tick problem

## R4: Static Body Handling

**Decision**: Static bodies are excluded from the 60 Hz tick stream entirely. Their pose (position + orientation) is delivered once via the property event stream alongside other semi-static data.

**Rationale**: Static bodies have fixed position and orientation by definition. Including them in the tick stream wastes ~56 bytes per body per tick for zero information gain. Since clients already cache semi-static data and static body poses never change, they're naturally handled by the same caching mechanism.

**Edge case**: If a static body's pose is changed via `SetBodyPose` command, the server detects this and pushes an updated property event. This is the same mechanism used for any semi-static property change.

## R5: Body Lifecycle Events on Bidirectional Channel

**Decision**: Three lifecycle event types on the property event stream:
1. **BodyCreated**: Full semi-static properties (+ pose for static bodies). Sent when a body is added to the simulation.
2. **BodyRemoved**: Body ID only. Sent when a body is removed.
3. **BodyPropertiesChanged**: Updated semi-static properties (+ pose for static bodies). Sent when any semi-static property changes (including motion type transitions).

**Rationale**: Explicit lifecycle events are unambiguous and simple to handle. The existing pattern of "detect removal by absence from body list" doesn't work when static bodies are absent from the tick stream. Explicit events also decouple lifecycle detection from tick stream parsing.

**Alternatives Considered**:
- Periodic full body ID lists for diff-based detection — adds periodic bandwidth overhead, more complex client logic
- Tombstone markers in tick stream — conflates continuous and lifecycle data

## R6: Client Field Profile for Velocity Opt-In

**Decision**: Add an optional field to `StateRequest` (the message clients send when subscribing to `StreamState`) that declares which continuous fields the client wants. Default includes velocity + angular velocity for backward compatibility. Viewer opts out.

**Rationale**: Simple, declarative, and backward-compatible. Existing clients that don't set the field get full continuous data (including velocity). Only the viewer explicitly opts out. The server uses the profile to decide whether to populate velocity fields in the `TickState` for each subscriber.

**Implementation note**: Since the server already has per-subscriber callbacks in `MessageRouter.Subscribers`, it can generate per-subscriber tick messages. However, for simplicity and to avoid per-subscriber serialization overhead, the initial implementation will generate at most 2 variants: with-velocity and without-velocity. Subscribers are bucketed into one of these two groups.

**Alternatives Considered**:
- Per-client custom field masks — over-engineered for 2 variants
- Always include velocity, let clients ignore — doesn't achieve the bandwidth goal for the viewer
