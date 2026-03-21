# Contract Changes: MCP Persistent Service

**Date**: 2026-03-21 | **Feature**: 001-mcp-persistent-service

## Proto Contract: physics_hub.proto

### New Message: CommandEvent

```protobuf
// Wraps any command for the audit stream
message CommandEvent {
  oneof command {
    SimulationCommand simulation_command = 1;
    ViewCommand view_command = 2;
  }
}
```

### New RPC: StreamCommands

Added to the `PhysicsHub` service:

```protobuf
service PhysicsHub {
  // ... existing RPCs unchanged ...

  // Audit stream: broadcasts every incoming command to subscribers
  rpc StreamCommands(StateRequest) returns (stream CommandEvent);
}
```

**Behavior**:
- Server streaming RPC (same pattern as `StreamState` and `StreamViewCommands`)
- Broadcasts every `SimulationCommand` and `ViewCommand` received by the server
- Late joiners receive no historical backfill (commands are ephemeral)
- Subscriber-based fan-out (N concurrent listeners supported)

### No Breaking Changes

- All existing messages and RPCs remain unchanged
- `CommandEvent` is additive
- `StreamCommands` is additive
- Existing clients are unaffected

## F# Module Contracts (.fsi Changes)

### MessageRouter.fsi (PhysicsServer) — Extended

New functions to add:

```fsharp
val subscribeCommands: MessageRouter -> (CommandEvent -> Task) -> Guid
val unsubscribeCommands: MessageRouter -> Guid -> unit
```

### GrpcConnection.fsi (PhysicsSandbox.Mcp) — Extended

New members to add:

```fsharp
val CommandLog: CommandEvent list  // bounded recent history
```

### New Tool Modules (.fsi files required)

- `PresetTools.fsi` — body preset tools (marble, bowlingBall, beachBall, crate, brick, boulder, die)
- `GeneratorTools.fsi` — scene generator tools (randomBodies, stack, row, grid, pyramid)
- `SteeringTools.fsi` — steering tools (push, launch, spin, stop)
- `AuditTools.fsi` — command audit stream query tools (recent commands, command log)
