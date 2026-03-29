# Quickstart: Expose Session State

**Feature**: 005-expose-session-state | **Date**: 2026-03-29

## What Changed

Three internal accessor functions in the `PhysicsClient.Session` module are now publicly accessible:

| Function | Returns | Description |
|----------|---------|-------------|
| `Session.latestState` | `SimulationState option` | Most recent simulation state from the background state stream |
| `Session.bodyRegistry` | `ConcurrentDictionary<string, string>` | Live mapping of body names to shape kinds |
| `Session.lastStateUpdate` | `DateTime` | UTC timestamp of the last state update received |

## Usage

```fsharp
open PhysicsClient.Connection.Session

// Connect to the physics server
let session = connect "http://localhost:5180" |> Result.defaultWith failwith

// Read current simulation state
match latestState session with
| Some state -> printfn "Bodies: %d" state.Bodies.Count
| None -> printfn "No state received yet"

// Look up body names
let registry = bodyRegistry session
for kvp in registry do
    printfn "Body: %s -> Shape: %s" kvp.Key kvp.Value

// Check state freshness
let lastUpdate = lastStateUpdate session
let staleness = System.DateTime.UtcNow - lastUpdate
printfn "State age: %.1f seconds" staleness.TotalSeconds
```

## Files Modified

1. `src/PhysicsClient/Connection/Session.fsi` — Remove `internal` keyword from 3 function signatures
2. `tests/PhysicsClient.Tests/SurfaceAreaTests.fs` — Add 3 entries to Session baseline
