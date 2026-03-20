# Error Report — PhysicsSandbox Demo Execution

**Date**: 2026-03-20
**Context**: Running 10 demo scripts via `demos/AutoRun.fsx` against a live Aspire-orchestrated PhysicsSandbox instance.

---

## Problem 1: Simulation Not Connected to Server

### Symptom
Server responds to all `SendCommand` RPCs with:
```
success=true message='Command accepted (no simulation connected — dropped)'
```
The `StreamState` subscription returns cached state with `time=0.000, bodies=0, running=false` — the server's initial empty cache.

### Evidence
- `SimulationLink.ConnectSimulation` from a test script returned `AlreadyExists` initially, indicating the Aspire-launched simulation had connected at some point.
- Later tests showed `ConnectSimulation` succeeded (no existing simulation), meaning the simulation's connection had dropped.
- The simulation process (PID visible, 45 threads, sleeping state) is alive but its gRPC stream to the server is dead.

### Root Cause Theory
The `PhysicsSimulation` service's `SimulationClient.fs` creates a plain `GrpcChannel.ForAddress(serverAddress)` without SSL certificate validation bypass:

```fsharp
// SimulationClient.fs line 21
let channel = GrpcChannel.ForAddress(serverAddress)
```

When Aspire injects `services__server__https__0=https://localhost:7180`, the simulation connects via HTTPS but fails the dev certificate validation. The `ConnectSimulation` bidirectional stream either:
1. Never establishes (SSL handshake fails), or
2. Establishes briefly then drops when the SSL validation kicks in on the response path.

The simulation's error handler catches `RpcException`, logs "Server disconnected", sets `running <- false`, and exits the loop — but the host process keeps running (it's a Worker service). No reconnect logic exists.

By contrast, the `PhysicsViewer` service uses a `SocketsHttpHandler` with:
```fsharp
handler.SslOptions.RemoteCertificateValidationCallback <-
    System.Net.Security.RemoteCertificateValidationCallback(fun _ _ _ _ -> true)
```

The simulation was written in spec 002 before this pattern was established in spec 003.

### Impact
- All commands are accepted by the server but silently dropped (never reach the simulation)
- State stream returns only the server's empty cached state
- Bodies are "created" (server acks) but never simulated
- The demos appear to pass (commands return `Ok`) but produce no physics data

### Fixes
**Option A (Recommended)**: Add SSL bypass to `SimulationClient.fs`, matching the Viewer pattern:
```fsharp
let private createChannel (address: string) =
    let handler = new SocketsHttpHandler(EnableMultipleHttp2Connections = true)
    handler.SslOptions.RemoteCertificateValidationCallback <-
        System.Net.Security.RemoteCertificateValidationCallback(fun _ _ _ _ -> true)
    GrpcChannel.ForAddress(address, GrpcChannelOptions(HttpHandler = handler))
```

**Option B**: Configure the simulation to prefer the HTTP endpoint (`services__server__http__0`) over HTTPS, since gRPC over plain HTTP/2 avoids the cert issue entirely — but this requires the `Http2UnencryptedSupport` AppContext switch or `HttpVersionPolicy.RequestVersionExact`.

**Option C**: Configure Aspire to trust the dev cert in child processes by setting `NODE_EXTRA_CA_CERTS` or the .NET equivalent — but this is fragile and environment-dependent.

---

## Problem 2: PhysicsClient State Stream Shows Empty State

### Symptom
`StateDisplay.listBodies` always prints "No bodies in simulation" or "No simulation state available". The `snapshot` function returns `Some` with 0 bodies.

### Root Cause
This is a direct consequence of Problem 1. The state stream works correctly — it subscribes to the server's `StreamState` and caches incoming messages. But the server only has its initial empty state (time=0, 0 bodies, paused) because no simulation is streaming real physics data to it.

When the PhysicsClient's background state stream receives this empty cached state, it correctly stores it. All subsequent `listBodies`/`status` calls display this empty state.

### Evidence
When a simulation IS connected (tested by embedding `PhysicsSimulation` directly in the FSI script), the state stream would receive real data. We confirmed this by manually connecting via `SimulationLink.ConnectSimulation` from a test script and sending state — the `StreamState` subscription received and forwarded it.

### Fix
Resolving Problem 1 (simulation SSL) automatically fixes this. No changes needed in PhysicsClient.

---

## Problem 3: PhysicsClient `IsConnected` Set to False by Background Stream

### Symptom
After Demo 2, the client reports "Not connected to server" for all subsequent demos. The `resetScene` call in Demo 3 fails because `clearAll` → `sendCommand` checks `IsConnected` which is `false`.

### Root Cause (Original)
The background state stream task originally set `session.IsConnected <- false` on any `RpcException`:
```fsharp
| :? RpcException ->
    session.IsConnected <- false
```

This meant any transient stream error (server restart, temporary network glitch, stream completion) would permanently mark the session as disconnected, preventing all subsequent commands.

### Fix Applied
Changed the state stream to use exponential backoff reconnection instead of marking disconnected:
```fsharp
| :? RpcException ->
    if not session.Cts.Token.IsCancellationRequested then
        do! Task.Delay(delay, session.Cts.Token) ...
        delay <- min (delay * 2) 10000
```

Only explicit `sendCommand` failures now set `IsConnected = false`. This fix is already committed.

### Remaining Issue
The fix prevents false disconnection from the stream, but the demos still fail because Problem 1 (no simulation) means `sendCommand` itself might still fail on certain edge cases. In practice, with the Aspire proxy, `sendCommand` succeeds (server accepts and drops), so `IsConnected` stays true. The 10/10 demo pass rate after this fix confirms it works.

---

## Problem 4: Viewer Window Not Appearing

### Symptom
User reports "saw white screen then window vanished" and "no window".

### Root Cause
The Aspire-launched `PhysicsViewer` process does not have the `DISPLAY` environment variable set. Verified:
```bash
cat /proc/<viewer-pid>/environ | tr '\0' '\n' | grep DISPLAY
# (empty)
```

Stride3D requires `DISPLAY` (or Wayland equivalent) to create a window. Without it, the game loop starts, briefly creates a window context, fails to connect to the display server, and the window vanishes or never appears.

The user confirms this is a GPU-enabled system with passthrough — the hardware is capable, but the environment variable isn't propagated to Aspire child processes.

### Fixes
**Option A (Recommended)**: Add `DISPLAY` to the viewer resource in AppHost:
```csharp
builder.AddProject<Projects.PhysicsViewer>("viewer")
    .WithReference(server)
    .WaitFor(server)
    .WithEnvironment("DISPLAY", Environment.GetEnvironmentVariable("DISPLAY") ?? ":0");
```

**Option B**: Set `DISPLAY` globally in the AppHost's `launchSettings.json`:
```json
"environmentVariables": {
    "DISPLAY": ":0"
}
```

**Option C**: Run the viewer outside of Aspire as a standalone process that connects to the server directly.

---

## Problem 5: FSI Script Scoping Issues

### Symptom
Demo scripts defined as F# modules (`module Demo01 =`) inside separate `.fsx` files couldn't access helper functions (`resetScene`, `runFor`, `ok`, `sleep`) defined in `Prelude.fsx`.

### Root Cause
FSI's `#load` directive creates separate compilation units. Top-level bindings in one `#load`ed file are NOT visible inside modules defined in subsequently `#load`ed files. Even `[<AutoOpen>]` modules don't propagate across `#load` boundaries.

This is a documented FSI limitation: each `#load`ed file gets its own scope. Only `#r` (assembly references) and namespace `open` statements work across files.

### Fix Applied
Consolidated all 10 demos into a single `AllDemos.fsx` file with inline functions (no separate modules). The self-contained `AutoRun.fsx` defines all helpers and demos in one file, avoiding cross-file scoping entirely.

The individual `Demo01_HelloDrop.fsx` through `Demo10_Chaos.fsx` files are preserved as documentation but are not directly loadable — they require the consolidated runner.

---

## Problem 6: gRPC over Plain HTTP Requires HTTP/2 Configuration

### Symptom
Connecting to the server's HTTP port (e.g., `http://localhost:5180`) fails with:
```
An HTTP/2 connection could not be established because the server did not complete the HTTP/2 handshake.
```

### Root Cause
gRPC requires HTTP/2. When connecting over plain HTTP (no TLS), .NET's `HttpClient` defaults to HTTP/1.1 and relies on the `Upgrade` mechanism, which the server (configured with `Http1AndHttp2`) may not handle correctly for gRPC.

Setting `GrpcChannelOptions.HttpVersion = HttpVersion.Version20` and `HttpVersionPolicy = RequestVersionExact` should force HTTP/2, but in testing this still failed — likely because the server's `Http1AndHttp2` protocol setting requires ALPN negotiation which only works over TLS.

### Fix
Use HTTPS endpoints exclusively. The Aspire proxy at `https://localhost:7180` works correctly with the SSL validation bypass handler. The PhysicsClient's `Session.createChannel` already includes this bypass.

---

## Problem 7: F# Namespace Collisions in Test Scripts

### Symptom
When loading both `PhysicsSimulation.dll` and `PhysicsClient.dll` in the same FSI script (for embedded simulation testing), `step`, `play`, `pause` resolve to `SimulationWorld.step` instead of `SimulationCommands.step` due to `open` order.

### Root Cause
Both modules define functions with the same names (`step`, `play`, `pause`). F# resolves to the last `open`ed module. Since `SimulationWorld` was opened for the embedded simulation, its `step : World -> SimulationState` shadows `SimulationCommands.step : Session -> Result<unit, string>`.

### Fix
Qualify ambiguous calls: `PhysicsClient.SimulationCommands.step s`. Or restructure the test to avoid opening both namespaces — use the simulation module in a separate helper function with its own scope.

---

## Summary

| # | Problem | Severity | Status | Fix Effort |
|---|---------|----------|--------|------------|
| 1 | Simulation SSL connection failure | **Critical** | Open | Small — add SSL bypass to SimulationClient.fs |
| 2 | Empty state stream | Critical | Blocked by #1 | Automatic once #1 fixed |
| 3 | False disconnection from stream | Medium | **Fixed** | Committed — backoff retry in Session.fs |
| 4 | Viewer no display | Medium | Open | Small — add DISPLAY env to AppHost |
| 5 | FSI scoping | Low | **Fixed** | Consolidated into single-file runner |
| 6 | HTTP/2 plain HTTP | Low | Workaround | Use HTTPS endpoints |
| 7 | Namespace collisions | Low | Documented | Qualify names in mixed scripts |

## Recommended Next Steps

1. **Fix Problem 1** (highest priority): Add SSL bypass handler to `src/PhysicsSimulation/Client/SimulationClient.fs`. This unblocks all demo functionality — bodies will be simulated and state will stream back with real physics data.

2. **Fix Problem 4**: Add `DISPLAY` passthrough to the viewer's AppHost registration so the 3D window appears.

3. **Re-run demos** after fixes to verify plausible simulation data (falling bodies, correct positions, forces causing motion).
