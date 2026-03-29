# Data Model: Fix Session State and Cache Synchronization

## Entities

### BatchResult (new — Scripting layer)

Aggregated outcome of a batch operation across all chunks.

| Field | Type | Description |
|-------|------|-------------|
| Succeeded | int | Count of commands that succeeded |
| Failed | (int * string) list | List of (command index, error message) for failures |

**Derived properties**:
- `Total`: `Succeeded + Failed.Length`
- `HasFailures`: `Failed.Length > 0`

**Relationships**: Produced by `batchAdd`, consumed by callers.

### Session (modified — PhysicsClient)

Existing opaque handle. Changes:
- Add `clearCaches` function that resets all mutable cache state to empty/default:
  - `BodyRegistry.Clear()`
  - `BodyPropertiesCache.Clear()`
  - `CachedConstraints <- []`
  - `CachedRegisteredShapes <- []`
  - `LatestState <- None`

### ConfirmedResetResponse (new — proto)

Server response confirming that reset has been fully processed by the simulation.

| Field | Type | Description |
|-------|------|-------------|
| success | bool | Whether reset completed successfully |
| message | string | Diagnostic message (e.g., "Reset complete: removed N bodies, M constraints") |
| bodies_removed | int32 | Count of bodies that were removed |
| constraints_removed | int32 | Count of constraints that were removed |

## State Transitions

### Reset Flow (current → proposed)

**Current**:
```
Client: resetSimulation()
  → pause (fire-and-forget)
  → reset (fire-and-forget, returns "forwarded")
  → IdGenerator.reset() ← happens immediately, before server processes reset
  → addPlane (fire-and-forget)
  → sleep 100ms
  → return ← server may still be processing reset
```

**Proposed**:
```
Client: resetSimulation()
  → pause (fire-and-forget)
  → confirmedReset (waits for server response via query infrastructure)
  → clearCaches() ← clears all client-side caches deterministically
  → IdGenerator.reset()
  → addPlane (fire-and-forget)
  → return ← server has confirmed reset is complete
```

### Batch Operation Flow (current → proposed)

**Current**:
```
Client: batchAdd(commands)
  → chunk into 100s
  → for each chunk: batchCommands → print failures → discard results
  → return unit
```

**Proposed**:
```
Client: batchAdd(commands)
  → chunk into 100s
  → for each chunk: batchCommands → print failures → accumulate results
  → return BatchResult { Succeeded; Failed }
```
