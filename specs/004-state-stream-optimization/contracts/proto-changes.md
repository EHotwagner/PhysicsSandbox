# Proto Contract Changes: State Stream Bandwidth Optimization

**File**: `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`

## New Messages

```protobuf
// ─── Continuous Stream (60 Hz) ──────────────────────────────────────────────

// Per-body pose data for dynamic bodies only.
// Velocity fields are omitted for clients that opt out (proto3 default = not serialized).
message BodyPose {
  string id = 1;
  Vec3 position = 2;
  Vec4 orientation = 3;
  Vec3 velocity = 4;          // Omitted for viewer (include_velocity=false)
  Vec3 angular_velocity = 5;  // Omitted for viewer (include_velocity=false)
}

// Lean per-tick state replacing SimulationState on the 60 Hz stream.
// Contains only dynamic body poses and simulation metadata.
message TickState {
  repeated BodyPose bodies = 1;
  double time = 2;
  bool running = 3;
  double tick_ms = 4;
  double serialize_ms = 5;
  repeated QueryResponse query_responses = 6;
}

// ─── Property Event Stream (on-change) ──────────────────────────────────────

// Full semi-static properties for a body.
// Includes pose for static bodies and motion type transitions.
message BodyProperties {
  string id = 1;
  Shape shape = 2;
  Color color = 3;
  double mass = 4;
  bool is_static = 5;
  BodyMotionType motion_type = 6;
  uint32 collision_group = 7;
  uint32 collision_mask = 8;
  MaterialProperties material = 9;
  Vec3 position = 10;       // Included for static bodies and motion type transitions
  Vec4 orientation = 11;    // Included for static bodies and motion type transitions
}

// Wrapper for property/lifecycle events on the bidirectional channel.
message PropertyEvent {
  oneof event {
    BodyProperties body_created = 1;
    string body_removed = 2;
    BodyProperties body_updated = 3;
    PropertySnapshot snapshot = 4;  // Backfill for late joiners
  }
  repeated MeshGeometry new_meshes = 10;  // Piggyback mesh definitions
}

// Full snapshot for late-joiner backfill.
message PropertySnapshot {
  repeated BodyProperties bodies = 1;
  repeated ConstraintState constraints = 2;
  repeated RegisteredShapeState registered_shapes = 3;
}
```

## Modified Messages

```protobuf
// Extended with velocity opt-out field.
// Default false = include velocity (backward compat). Viewer sets true.
message StateRequest {
  bool exclude_velocity = 1;
}
```

## Modified RPCs

```protobuf
service PhysicsHub {
  // Changed: returns TickState instead of SimulationState
  rpc StreamState (StateRequest) returns (stream TickState);

  // New: property event stream for semi-static data and lifecycle events
  rpc StreamProperties (StateRequest) returns (stream PropertyEvent);

  // Existing RPCs unchanged:
  rpc SendCommand (SimulationCommand) returns (CommandAck);
  rpc StreamViewCommands (StateRequest) returns (stream ViewCommand);
  rpc StreamCommands (StateRequest) returns (stream CommandEvent);
  rpc Raycast (RaycastRequest) returns (RaycastResponse);
  rpc SendViewCommand (ViewCommand) returns (CommandAck);
  rpc FetchMeshes (MeshRequest) returns (MeshResponse);
}

// SimulationLink unchanged — simulation still sends SimulationState upstream.
// Server decomposes into TickState + PropertyEvents for downstream clients.
service SimulationLink {
  rpc ConnectSimulation (stream SimulationState) returns (stream SimulationCommand);
}
```

## Backward Compatibility Notes

- `SimulationState` message is **retained** and still used by `SimulationLink` (simulation → server upstream). Also used internally by `RecordingEngine` for recording (reconstructed from TickState + cached BodyProperties).
- `Body` message is **retained** for internal reconstruction and recording.
- `StateRequest.exclude_velocity` defaults to `false` (proto3 bool default) — existing clients that send an empty `StateRequest` get velocity by default. Only the viewer explicitly sets `exclude_velocity = true`.
