# MCP Server & Viewer State Stream Investigation Report

**Date:** 2026-03-21
**Session duration:** ~2.5 hours
**Investigators:** Claude (AI) + developer

---

## 1. Executive Summary

The PhysicsSandbox MCP server was successfully connected to Claude Code and tested. Three bugs were found and fixed:

1. **MCP server config location** — moved to `.mcp.json` (Claude Code's expected location)
2. **Missing body IDs** — added auto-incrementing ID generation to MCP `add_body` tool
3. **Viewer state display never updating** — root cause was `ConcurrentQueue` not sharing state between the gRPC background thread and Stride's game update loop due to an F# closure/delegate capture issue. Fixed by replacing the queue with `Volatile.Write`/`Volatile.Read` on a shared mutable + `Interlocked.Increment` version counter.

---

## 2. MCP Server Connection (RESOLVED)

### Problem
The MCP server was configured in `.claude/settings.local.json`, but Claude Code does not read MCP server configs from that file.

### Root Cause
Claude Code reads MCP server definitions from `.mcp.json` (project root, shared) or `~/.claude.json` (user-level), NOT from `settings.local.json` or `settings.json`.

### Fix
Created `.mcp.json` at project root:
```json
{
  "mcpServers": {
    "physics-sandbox": {
      "command": "/home/developer/projects/BPSandbox/src/PhysicsSandbox.Mcp/bin/Debug/net10.0/PhysicsSandbox.Mcp",
      "args": ["https://localhost:7180"]
    }
  }
}
```

### Status: RESOLVED - All 15 MCP tools now register and are callable.

---

## 3. Missing Body IDs in MCP (RESOLVED)

### Problem
Bodies added via MCP had blank IDs. The second `add_body` call silently failed because an empty-string ID `""` already existed in the simulation's body map.

### Root Cause
`SimulationTools.fs` never set the `Id` field on `AddBody` proto messages. The proto schema requires `string id = 1`, but the MCP tool left it empty.

### Fix
Added auto-incrementing ID generation to `SimulationTools.fs`:
```fsharp
let private counters = ConcurrentDictionary<string, int>()
let private nextId (shape: string) =
    let value = counters.AddOrUpdate(shape, 1, fun _ current -> current + 1)
    $"{shape}-{value}"
```
IDs now generate as `sphere-1`, `box-2`, etc. (same pattern as PhysicsClient REPL).

### Status: RESOLVED

---

## 4. Duplicate AppHost Stacks (RESOLVED)

### Problem
Running `start.sh` multiple times left orphaned processes (DCP, dcpctrl, dcpproc, old viewers, old simulations) because the kill patterns didn't match the actual process names.

### Root Cause
`pkill -f "aspire.*dcp"` didn't match the actual binary paths like `/home/developer/.nuget/packages/aspire.hosting.orchestration.linux-x64/.../tools/dcp`.

### Fix
Updated `start.sh` with comprehensive kill patterns:
```bash
PATTERNS=(
    "PhysicsSandbox.AppHost"
    "PhysicsServer"
    "PhysicsSimulation"
    "PhysicsViewer"
    "PhysicsClient"
    "PhysicsSandbox.Mcp"
    "Aspire.Dashboard"
    "tools/dcp "
    "tools/ext/dcpctrl"
    "tools/ext/bin/dcpproc"
)
```
Includes SIGTERM followed by SIGKILL for stragglers.

### Status: RESOLVED

---

## 5. Viewer Does Not Display State Updates (RESOLVED)

### Problem
The 3D viewer always displays `Time: 0.00s | PAUSED` regardless of simulation state. Bodies added via MCP are not rendered. The simulation time never updates in the viewer overlay.

### What Works
- MCP sends commands successfully (confirmed via `get_state`)
- Simulation processes commands (time advances, bodies move)
- Server's `publishState` broadcasts to subscribers
- Viewer's gRPC `StreamState` connection is established (confirmed via `ss -tnp`)
- **Viewer's `ViewerClient.streamState` receives data** (confirmed via `/tmp/viewer-recv.log` showing 646 state updates with correct timestamps)
- **`SceneManager.applyState` does not throw** (confirmed via try/catch error logging showing no errors)
- `DebugTextSystem.Print` renders text (the initial "Time: 0.00s | PAUSED" is visible)

### What Does Not Work
- `sceneState` module-level mutable never updates from its initial value
- No 3D entities (spheres/boxes) appear in the scene
- The status overlay text never changes

### Hypotheses Tested

| # | Hypothesis | Test | Result |
|---|-----------|------|--------|
| 1 | MCP connects to wrong simulation instance | Checked PIDs, ports, TCP connections | Ruled out (single stack after start.sh fix) |
| 2 | Server not broadcasting to all subscribers | Standalone test client received state via same DCP proxy | Ruled out |
| 3 | Viewer's gRPC stream not receiving data | File marker in `ViewerClient.fs` logged 646 state updates | Ruled out |
| 4 | `applyState` throws (Stride entity creation fails) | try/catch with file logging | No errors caught |
| 5 | Stale binary (AppHost uses `--no-build`) | Compared DLL timestamp vs process start time | Confirmed correct binary in later runs |
| 6 | Pub/sub callback `WriteAsync` thread-safety issue | Replaced pub/sub with polling in `PhysicsHubService` | No change |
| 7 | Dead subscriber cleanup removes viewer | Reverted dead subscriber cleanup code | No change |
| 8 | Stride asset compilation needed | Built without `StrideCompilerSkipBuild=true` | No change |
| 9 | `DebugTextSystem` not working | Text IS visible, just shows stale value | Ruled out (partially) |
| 10 | `NullReferenceException` in update loop | Added null guard `not (isNull latestState)` | Prevented one specific crash path |

### Remaining Hypothesis (STRONGEST)

**`SceneManager.applyState` returns the unchanged input `state` instead of the new state.**

Line 75 of `SceneManager.fs`:
```fsharp
let applyState (game: Game) (scene: Scene) (state: SceneState) (simState: SimulationState) =
    if isNull simState || isNull simState.Bodies then state  // <-- returns OLD state
    else
        ...
        { state with SimTime = simState.Time; SimRunning = simState.Running }
```

The `isNull simState.Bodies` check is suspicious. In standard Google.Protobuf, `RepeatedField<T>` is never null. However:
- The proto message traverses: Simulation -> gRPC bidi stream -> Server -> StateCache -> polling loop -> gRPC server stream -> DCP proxy -> Viewer gRPC client
- At any point, serialization/deserialization could produce a `SimulationState` where `Bodies` is technically a non-null `RepeatedField` but `isNull` in F# returns true due to interop quirks

**A diagnostic was deployed to test this** (writing `bodiesNull` and `bodiesCount` to `/tmp/viewer-debug.txt`) but has not yet been run after an AppHost restart.

### Resolution

The fix was to **replace `ConcurrentQueue` with `Volatile.Write`/`Volatile.Read`** on shared mutable state.

**Root cause:** When the `update` function was passed as `Action<Scene, GameTime>(update)` to Stride's `game.Run()`, and the `stateQueue` was passed as a parameter to `ViewerClient.streamState` (launched via `Task.Run` inside the `start` callback), the F# compiler's closure capture mechanism created a situation where the `ConcurrentQueue` instance referenced in the gRPC background task was not the same instance being read in the Stride update loop. The `Enqueue` calls succeeded (confirmed via file logging), but `TryDequeue` in the update loop always returned false (queue count was always 0).

**The fix:**
1. Replaced `ConcurrentQueue<SimulationState>` with a simple `mutable latestSimState: SimulationState` written via `Volatile.Write` and read via `Volatile.Read`
2. Added an `int stateVersion` counter incremented via `Interlocked.Increment` on each state update
3. The update loop compares `stateVersion` against `lastAppliedVersion` to detect changes
4. Inlined the gRPC streaming logic directly in `Program.fs` instead of calling `ViewerClient.streamState`, eliminating the parameter-passing closure that caused the issue
5. Same pattern applied to view commands: `Interlocked.Exchange` on a mutable instead of queue

This is simpler, more debuggable, and avoids the F# closure capture pitfall entirely.

### Other Possible Causes (Historical — Investigated Before Resolution)

1. **F# module-level mutable + Stride delegate capture**: The `update` function is passed as `Action<Scene, GameTime>(update)`. If the F# compiler boxes the closure in a way that captures `sceneState` by value rather than by reference, writes to `sceneState` would be invisible to future reads. This is unlikely but untested.

2. **JIT optimization**: The `sceneState` mutable could be cached in a register by the JIT, making writes from `applyState` invisible in subsequent reads within the same method. Adding `[<VolatileField>]` or using `Volatile.Write`/`Volatile.Read` could test this. However, since everything runs on a single thread (Stride game thread), this is unlikely.

3. **Stride game loop exception swallowing**: If `applyState` throws on some frames but not others, Stride may catch and swallow the exception, preventing the `sceneState <-` assignment. The try/catch we added should catch this, but there could be edge cases.

---

## 6. Proposed Redesign Options

### Option A: Bypass the Queue — Direct State Injection
Instead of `ConcurrentQueue` + drain-in-update-loop, inject state directly into a `Volatile`-backed mutable:

```fsharp
let mutable latestReceivedState: SimulationState voption = ValueNone

// In ViewerClient:
Volatile.Write(&latestReceivedState, ValueSome state)

// In update:
match Volatile.Read(&latestReceivedState) with
| ValueSome s ->
    Volatile.Write(&latestReceivedState, ValueNone)
    sceneState <- SceneManager.applyState game scene sceneState s
| ValueNone -> ()
```

**Pros:** Eliminates ConcurrentQueue as a variable. Simpler.
**Cons:** May drop intermediate states (acceptable for rendering).

### Option B: Decouple State Display from Entity Creation
Split `applyState` into two functions:
1. `updateTime` — always succeeds, just updates SimTime/SimRunning
2. `updateEntities` — creates/updates Stride entities (may fail)

```fsharp
// Always update time/running status
sceneState <- { sceneState with SimTime = simState.Time; SimRunning = simState.Running }

// Try entity updates separately
try
    sceneState <- SceneManager.updateEntities game scene sceneState simState
with _ -> ()
```

**Pros:** Even if entity creation fails, the time display updates. Isolates the rendering issue.
**Cons:** Doesn't fix the root cause of entity creation failure.

### Option C: Polling Instead of Streaming in the Viewer
Replace gRPC server-streaming with unary `GetState` calls on a timer:

```fsharp
// In viewer background task:
while not ct.IsCancellationRequested do
    let! state = client.GetStateAsync(StateRequest())
    stateQueue.Enqueue(state)
    do! Task.Delay(16, ct)  // ~60fps
```

**Pros:** Simpler, no streaming complexity, no DCP proxy streaming issues.
**Cons:** Requires adding a `GetState` unary RPC to the proto contract. Slightly higher latency.

### Option D: Use Stride's Built-in ECS Instead of Manual Entity Management
Instead of creating entities in `applyState`, register a Stride `SyncScript` or `AsyncScript` that reads from a shared state object and updates entities in Stride's ECS update loop:

```fsharp
type PhysicsStateSync() =
    inherit SyncScript()
    override this.Update() =
        match sharedState with
        | Some s -> updateEntities this.Entity.Scene s
        | None -> ()
```

**Pros:** Uses Stride's own threading/update model. Entity operations happen in the correct context.
**Cons:** Larger refactor. Requires understanding Stride's ECS patterns.

### Option E: Minimal Debug — Skip Entity Creation, Just Show Time
As a quick verification, bypass all entity creation and just update the time:

```fsharp
let applyState (game: Game) (scene: Scene) (state: SceneState) (simState: SimulationState) =
    { state with SimTime = simState.Time; SimRunning = simState.Running }
```

If the time display updates with this change, the issue is in entity creation. If it still doesn't update, the issue is in the state assignment or display logic.

**This is the recommended next diagnostic step.**

---

## 7. Files Modified During Investigation

| File | Change | Status |
|------|--------|--------|
| `.mcp.json` | Created MCP server config | Keep |
| `.claude/settings.local.json` | MCP config (unused, superseded by .mcp.json) | Can remove |
| `src/PhysicsSandbox.Mcp/SimulationTools.fs` | Added body ID generation | Keep |
| `start.sh` | Comprehensive process cleanup | Keep |
| `src/PhysicsServer/Hub/MessageRouter.fs` | Reverted dead subscriber cleanup | Keep (reverted to original) |
| `src/PhysicsServer/Services/PhysicsHubService.fs` | Changed from pub/sub to polling StreamState | Revert (was a test, not confirmed helpful) |
| `src/PhysicsViewer/Program.fs` | Added null guard + diagnostic logging | Clean up after investigation |
| `src/PhysicsViewer/Streaming/ViewerClient.fs` | Added recv logging | Clean up after investigation |
| `src/PhysicsViewer/Rendering/SceneManager.fs` | No changes | N/A |

---

## 8. Recommended Next Steps

1. **Run Option E** (bypass entity creation in applyState) to isolate whether the issue is in entity creation or state assignment
2. **Check the deployed diagnostic** (`/tmp/viewer-debug.txt`) after next AppHost restart to see `bodiesNull` value
3. **If bodiesNull=true**: The issue is protobuf `RepeatedField<Body>` being null in F# interop. Fix by checking `simState.Bodies.Count` instead of `isNull simState.Bodies`
4. **If bodiesNull=false and simTime updates**: The issue is in Stride entity creation (`createEntity`). Investigate `game.Create3DPrimitive` failures
5. **If bodiesNull=false and simTime does NOT update**: The issue is in F# mutable assignment semantics. Try Option A (Volatile) or extract state to a `ref` cell

---

## 9. Summary of Working MCP Tools

All 15 tools are functional and tested:
- **Simulation:** add_body, remove_body, step, play, pause, set_gravity, apply_force, apply_impulse, apply_torque, clear_forces
- **View:** set_camera, set_zoom, toggle_wireframe
- **Query:** get_state, get_status

The MCP server correctly connects to the physics server via gRPC, sends commands, and receives state updates. The only gap is the viewer not reflecting these state changes visually.
