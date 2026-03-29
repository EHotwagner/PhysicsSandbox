# Scripting API Contract Changes

## BatchOperations module

### Changed signature

```fsharp
// Before (005-fix-session-state-sync)
val batchAdd : Session -> SimulationCommand list -> unit

// After
val batchAdd : Session -> SimulationCommand list -> BatchResult
```

### New type

```fsharp
/// Aggregated result of a batch operation.
type BatchResult =
    { Succeeded: int
      Failed: (int * string) list }
```

Exposed in `BatchOperations.fsi` and re-exported via `Prelude.fsi`.

## SimulationLifecycle module

### No signature changes

`resetSimulation` remains `Session -> unit`. The internal implementation changes from fire-and-forget reset to confirmed reset, but callers see no difference.

## Session module (PhysicsClient)

### New internal function

```fsharp
val internal clearCaches : session: Session -> unit
```

Not exposed in the public scripting API. Used internally by `SimulationCommands.confirmedReset`.

## SimulationCommands module (PhysicsClient)

### New function

```fsharp
val confirmedReset : session: Session -> Result<ConfirmedResetResponse, string>
```

Sends a confirmed reset RPC and clears all client-side caches upon success.

## Breaking change assessment

- `batchAdd` return type changes from `unit` to `BatchResult` — callers that ignore the return value get a compiler warning but no error
- All 44 existing demo scripts ignore `batchAdd`'s return value → no breakage
- `Prelude.fsi` re-exports `batchAdd` and must be updated
- Surface area baselines must be updated
