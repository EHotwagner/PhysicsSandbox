# Proto Contract Changes: Performance Diagnostics & Stress Testing

**Date**: 2026-03-21
**File**: `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`

## New Messages

### Body (modified)

```protobuf
message Body {
  string id = 1;
  Vec3 position = 2;
  Vec3 velocity = 3;
  double mass = 4;
  Shape shape = 5;
  Vec3 angular_velocity = 6;
  Vec4 orientation = 7;
  bool is_static = 8;          // NEW: true for static bodies (planes, static boxes)
}
```

### SimulationCommand (modified oneof)

```protobuf
message SimulationCommand {
  oneof command {
    AddBody add_body = 1;
    ApplyForce apply_force = 2;
    SetGravity set_gravity = 3;
    StepSimulation step = 4;
    PlayPause play_pause = 5;
    RemoveBody remove_body = 6;
    ApplyImpulse apply_impulse = 7;
    ApplyTorque apply_torque = 8;
    ClearForces clear_forces = 9;
    ResetSimulation reset = 10;    // NEW: clears all bodies, resets time
  }
}

message ResetSimulation {}         // NEW: empty message, no parameters needed
```

### Batch Messages (new)

```protobuf
message BatchSimulationRequest {
  repeated SimulationCommand commands = 1;
}

message BatchViewRequest {
  repeated ViewCommand commands = 1;
}

message CommandResult {
  bool success = 1;
  string message = 2;
  int32 index = 3;              // position in batch (0-based)
}

message BatchResponse {
  repeated CommandResult results = 1;
  double total_time_ms = 2;     // wall-clock time for entire batch
}
```

### Metrics Messages (new)

```protobuf
message MetricsRequest {}

message ServiceMetricsReport {
  string service_name = 1;
  int64 messages_sent = 2;
  int64 messages_received = 3;
  int64 bytes_sent = 4;
  int64 bytes_received = 5;
}

message PipelineTimings {
  double simulation_tick_ms = 1;
  double state_serialization_ms = 2;
  double grpc_transfer_ms = 3;
  double viewer_render_ms = 4;
  double total_pipeline_ms = 5;
}

message MetricsResponse {
  repeated ServiceMetricsReport services = 1;
  PipelineTimings pipeline = 2;
}
```

## New RPCs on PhysicsHub Service

```protobuf
service PhysicsHub {
  // Existing RPCs...
  rpc SendCommand (SimulationCommand) returns (CommandAck);
  rpc SendViewCommand (ViewCommand) returns (CommandAck);
  rpc StreamState (StateRequest) returns (stream SimulationState);
  rpc StreamViewCommands (StateRequest) returns (stream ViewCommand);
  rpc StreamCommands (StateRequest) returns (stream CommandEvent);

  // NEW: Batch RPCs
  rpc SendBatchCommand (BatchSimulationRequest) returns (BatchResponse);
  rpc SendBatchViewCommand (BatchViewRequest) returns (BatchResponse);

  // NEW: Metrics RPC
  rpc GetMetrics (MetricsRequest) returns (MetricsResponse);
}
```

## Migration Impact

- **Body.is_static** (field 8): Additive, backward-compatible. Old clients ignore the field; old servers send default `false`.
- **ResetSimulation** (oneof variant 10): Additive. Old simulations won't recognize the command — requires coordinated deploy of server + simulation.
- **Batch RPCs**: New RPCs, no impact on existing clients.
- **GetMetrics RPC**: New RPC, no impact on existing clients.
- **All changes are additive** — no breaking changes to existing proto contract.

## Affected Services

| Service | Changes Required |
|---------|-----------------|
| PhysicsServer | Implement `SendBatchCommand`, `SendBatchViewCommand`, `GetMetrics` RPCs. Add metrics counters. Route `ResetSimulation` command. |
| PhysicsSimulation | Handle `ResetSimulation` command. Track static bodies in state. Add timing instrumentation. Report metrics. |
| PhysicsViewer | Add FPS calculation/display. Report frame timing for pipeline diagnostics. |
| PhysicsSandbox.Mcp | Add batch, restart, metrics query, diagnostics, stress test, and comparison MCP tools. |
| PhysicsClient | Add `reset`, `batchCommands`, `batchViewCommands`, `getMetrics` functions. |
