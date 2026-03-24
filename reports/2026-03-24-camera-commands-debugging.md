# Camera Commands Debugging Report — 2026-03-24

## Summary

During implementation of smooth camera controls (004-camera-smooth-demos), several critical issues were discovered with ViewCommand delivery and the demo showcase. This report documents the investigation, root causes, and fixes.

---

## Issue 1: ViewCommand Single-Slot Drop

### Symptoms
- SmoothCamera, CameraLookAt, CameraFollow, CameraOrbit commands never reached the viewer
- SetNarration, CameraStop, CameraChase commands worked intermittently
- Aspire structured logs confirmed only ~30% of sent ViewCommands were received

### Root Cause
The viewer used a single-slot `Volatile.Write`/`Interlocked.Exchange` pattern for ViewCommands:

```fsharp
// Writer (background stream thread):
Volatile.Write(&latestViewCmd, stream.Current)

// Reader (game update loop):
let viewCmd = Interlocked.Exchange(&latestViewCmd, null)
```

When the demo script sent commands rapidly (e.g., `setNarration` then `smoothCamera` 100ms apart), the second command overwrote the first before the viewer's 15 FPS update loop could read it.

### Fix
Replaced with `ConcurrentQueue<ViewCommand>` and a `while TryDequeue` drain loop in the update function:

```fsharp
let viewCmdQueue = ConcurrentQueue<ViewCommand>()

// Writer:
viewCmdQueue.Enqueue(stream.Current)

// Reader:
while viewCmdQueue.TryDequeue(&viewCmd) do
    match viewCmd.CommandCase with ...
```

### Status: FIXED

---

## Issue 2: Duplicate Viewer Processes Competing for ViewCommands

### Symptoms
- Even after the queue fix, SmoothCamera/CameraOrbit/CameraFollow commands were still not arriving at the viewer
- Aspire RECV logs showed only SetNarration and a few other commands
- The pattern of missing commands was random/non-deterministic

### Root Cause
The server's `ViewCommandChannel` is a single-consumer `Channel<ViewCommand>` with capacity 1024. The `StreamViewCommands` gRPC RPC reads from this channel. When multiple viewers subscribe (e.g., stale viewer from a previous Aspire stack + new viewer from current stack), `Channel.Reader.ReadAsync` distributes commands round-robin between subscribers. Each command goes to only ONE subscriber.

Investigation via `ps aux` revealed two PhysicsViewer processes running simultaneously (PIDs from different Aspire startups at 12:52 and 12:53).

### Fix
- Fixed `kill.sh` to use `.dll` suffix patterns (e.g., `PhysicsViewer.dll` instead of `PhysicsViewer`) to avoid matching shell sessions and build processes
- The old `pkill -f "PhysicsViewer"` pattern matched any process with "PhysicsViewer" anywhere in its command line, including the Claude Code bash session whose cwd contained the path
- Killing stale viewer processes before running demos resolves the command competition

### Status: FIXED (kill.sh), ROOT CAUSE CONFIRMED
### Broader Note: The server's single-consumer ViewCommand channel is a design limitation — only one viewer can receive commands at a time. If multi-viewer support is ever needed, the channel should be replaced with a pub/sub broadcast pattern.

---

## Issue 3: kill.sh Self-Kill

### Symptoms
- Running `./kill.sh && dotnet build ...` would exit with code 144 (SIGKILL)
- Any bash command chaining `kill.sh` with other PhysicsSandbox-related commands would fail

### Root Cause
`pkill -f "PhysicsViewer"` matches against the FULL command line of all processes. When bash runs `./kill.sh && dotnet build src/PhysicsViewer`, the shell's command line contains "PhysicsViewer", so `pkill -9 -f "PhysicsViewer"` kills the parent shell.

Similarly, `pkill -f "PhysicsSandbox.AppHost"` would match any shell whose arguments or cwd contained that string.

### Fix
Changed kill patterns from bare names to `.dll` suffix:
- `"PhysicsServer"` → `"PhysicsServer.dll"`
- `"PhysicsViewer"` → `"PhysicsViewer.dll"`
- etc.

This ensures only the actual .NET runtime processes are matched, not shell sessions, build commands, or editors.

### Status: FIXED

---

## Issue 4: Body-Not-Found Cancels Camera Mode Immediately

### Symptoms
- CameraOrbit around a freshly-created body did nothing — the orbit mode was immediately cancelled
- CameraFollow/CameraChase on a body that hadn't appeared in the simulation state yet was silently dropped

### Root Cause
The `updateCameraMode` function cancelled any body-relative mode (LookingAt, Following, Orbiting, Chasing, Framing) when the body ID was not found in the `bodyPositions` map:

```fsharp
| Following bodyId ->
    match Map.tryFind bodyId bodyPositions with
    | None -> { state with ActiveMode = None }  // CANCELLED
```

The body position map is built from `latestSimState.Bodies`. A newly-created body may not appear in the next tick for 1-2 frames (16-33ms at 60Hz). If the camera mode is set during this window, it's immediately cancelled on the first frame.

### Fix
Changed body-not-found behavior to hold position and wait:

```fsharp
| Following bodyId ->
    match Map.tryFind bodyId bodyPositions with
    | None -> state  // hold position, keep mode active
```

The mode stays active until the body appears in the sim state. Tests updated accordingly.

### Status: FIXED

---

## Issue 5: Demo22 Follow/Chase/Orbit Not Visually Effective

### Symptoms
- Follow mode: camera tracked a settled ball on the ground — no visible movement
- Chase mode: camera chased a capsule that wasn't moving — static view
- Orbit mode: orbit target was a freshly-created static anchor body that wasn't in the sim state yet

### Root Cause
Demo design issue — the demo script didn't create enough motion for body-relative modes to be visually interesting.

### Fix
Redesigned Demo22_CameraShowcase.fsx:
- **Follow**: Apply strong upward impulse `(8, 40, 5)` to the ball before following — camera visibly tracks a flying ball
- **Chase**: Apply strong sideways impulse `(-20, 15, 8)` to the crate before chasing — camera follows a moving crate with offset
- **Orbit**: Orbit the red ball (already in sim) instead of a freshly-created anchor body
- Added `sleep 100` gaps between setNarration and camera commands to reduce command batching

### Status: FIXED

---

## Issue 6: Viewer FPS Drops to 15 During Demos

### Observation
Viewer FPS drops from 120 to exactly 15 when the demo script is running.

### Root Cause
This is expected Stride3D behavior — when the viewer window loses focus (e.g., because the terminal running the demo script has focus), Stride throttles to 15 FPS to save GPU resources.

### Status: NOT A BUG — expected behavior

---

## Diagnostic Tools Used

1. **Aspire Dashboard MCP** (`mcp__aspire-dashboard__list_structured_logs`) — invaluable for viewing viewer-side structured logs without touching log files
2. **`ViewCmd RECV: {Case}` logging** — added to the viewer's stream reader to trace which commands actually arrive
3. **Per-handler `LogInformation`** — added to each ViewCommand handler for command-level tracing
4. **`ps aux` process inspection** — discovered duplicate viewer processes

---

## Recommendations

1. **ViewCommand channel should be broadcast, not single-consumer** — The current `Channel<ViewCommand>` with `Reader.ReadAsync` only delivers each command to one subscriber. If the MCP or any other service ever subscribes to `StreamViewCommands`, commands will be split between subscribers. Consider replacing with a pub/sub pattern (e.g., one channel per subscriber).

2. **`kill.sh` should always use `.dll` patterns** — Never use bare process names like `PhysicsViewer` in `pkill -f` patterns. The `.dll` suffix ensures only actual .NET runtime processes are matched.

3. **Demo scripts should add `sleep 100` between rapid ViewCommands** — Even with the queue fix, giving the gRPC transport a small window between commands improves reliability.

4. **Body-relative camera modes should be tolerant of delayed body appearance** — The "hold and wait" behavior is more robust than immediate cancellation for newly-created bodies.
