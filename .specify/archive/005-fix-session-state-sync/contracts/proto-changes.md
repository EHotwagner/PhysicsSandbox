# Proto Contract Changes: Confirmed Reset

## New RPC

Add to `PhysicsHub` service in `physics_hub.proto`:

```protobuf
// Resets the simulation and waits for confirmation that all bodies/constraints
// are removed before returning. Unlike SendCommand with Reset, this blocks
// until the simulation has fully processed the reset.
rpc ConfirmedReset (ConfirmedResetRequest) returns (ConfirmedResetResponse);
```

## New Messages

```protobuf
message ConfirmedResetRequest {
  // Empty for now. Future: optional flags like preserve_registered_shapes.
}

message ConfirmedResetResponse {
  bool success = 1;
  string message = 2;
  int32 bodies_removed = 3;
  int32 constraints_removed = 4;
}
```

## Rationale

The existing `SendCommand` with `ResetSimulation` payload is fire-and-forget — the server acknowledges receipt but not completion. For scripting workflows that need deterministic reset-then-create cycles, a blocking RPC that waits for simulation processing is required.

## Implementation Approach

The `ConfirmedReset` handler in `PhysicsHubService` will:
1. Submit a `ResetSimulation` command to the simulation via the command channel
2. Submit a follow-up "noop query" (or dedicated reset-query) through the query infrastructure
3. The simulation processes reset first (commands are ordered in the channel), then the query
4. When the query response arrives (via `submitQuery` TCS), the reset is guaranteed complete
5. Return the `ConfirmedResetResponse` with body/constraint counts from the simulation state before reset

Alternative: Add a dedicated reset command type to the query request oneof, so the simulation can report exact removal counts in the response.
