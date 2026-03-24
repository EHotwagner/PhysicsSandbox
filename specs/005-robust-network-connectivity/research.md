# Research: 005-robust-network-connectivity

**Date**: 2026-03-24

## R1: ViewCommand Broadcast Pattern

**Decision**: Replace single-consumer `Channel<ViewCommand>` with `ConcurrentDictionary<Guid, Channel<ViewCommand>>` subscriber registry — one bounded channel per subscriber.

**Rationale**: The existing `MessageRouter` already uses `ConcurrentDictionary<Guid, TickState -> Task>` for state subscribers and `ConcurrentDictionary<Guid, CommandEvent -> Task>` for command audit subscribers. The ViewCommand broadcast should follow the same established pattern. Each `StreamViewCommands` RPC registers a per-subscriber bounded channel. The publisher writes to all active subscriber channels. This avoids adding new dependencies and is consistent with codebase conventions.

**Implementation**:
- Replace `ViewCommandChannel: Channel<ViewCommand>` with `ViewCommandSubscribers: ConcurrentDictionary<Guid, Channel<ViewCommand>>`
- Add `subscribeViewCommands` / `unsubscribeViewCommands` functions mirroring existing `subscribe`/`unsubscribe` for state
- `submitViewCommand` iterates all subscriber channels, calling `TryWrite` on each. If a subscriber's channel is full (backpressure), skip that subscriber's command (don't block the publisher)
- `StreamViewCommands` RPC creates a per-subscriber channel, registers it, reads from it in a loop, and unregisters on disconnection
- Zero subscribers = commands silently dropped (FR-013)

**Alternatives considered**:
- `mkmrk.Channels` BroadcastChannel NuGet: Rejected — adds external dependency; existing ConcurrentDictionary pattern is proven in this codebase.
- Single channel with `Reader.ReadAsync` + fan-out middleware: Rejected — still single-consumer at the channel level; doesn't solve the core problem.
- Callback-based `ConcurrentDictionary<Guid, ViewCommand -> Task>` (like state subscribers): Possible but channels provide built-in bounded backpressure and ordering, which callbacks don't. Channel-per-subscriber is a better fit for streaming RPCs.

## R2: MCP SSE Endpoint Through DCP Proxy

**Decision**: Add `isProxied: false` on the MCP HTTP endpoint in AppHost, OR configure the MCP endpoint with `Http1AndHttp2` protocol. Investigation needed to determine which approach Aspire 13.x supports for `AddProject` resources.

**Rationale**: The DCP proxy for gRPC services enforces HTTP/2, which rejects HTTP/1.1 SSE connections. The MCP server uses `ModelContextProtocol.AspNetCore` with HTTP/SSE transport, which requires HTTP/1.1. Setting `isProxied: false` tells DCP not to proxy the endpoint, allowing direct connections. Alternatively, ensuring the endpoint protocol is `Http1AndHttp2` would allow SSE through the proxy.

**Current configuration** (AppHost.cs):
```csharp
builder.AddProject<Projects.PhysicsSandbox_Mcp>("mcp")
    .WithReference(server)
    .WaitFor(server)
    .WithHttpEndpoint(port: 5199, name: "http");
```

**Investigation result**: `WithHttpEndpoint` already sets the transport to HTTP. The issue may be that the DCP proxy for HTTP endpoints still uses HTTP/2. The `isProxied` property on `EndpointAnnotation` can be set via `.WithEndpoint("http", e => e.IsProxied = false)` after the initial endpoint is created.

**Alternatives considered**:
- Connect to dynamic port via `ss -tlnp`: Current workaround, but fragile and undocumented. Rejected as long-term solution.
- Add separate named gRPC + HTTP endpoints on MCP: Overkill — MCP only needs HTTP/1.1.

## R3: Process Cleanup Patterns

**Decision**: Use `.dll` suffix patterns in `pkill -f` (e.g., `PhysicsViewer.dll` instead of `PhysicsViewer`) to match only actual .NET runtime processes.

**Rationale**: Bare process name patterns match too broadly — they match shell sessions, build commands, editor buffers, and the kill script's own parent shell when chained with other commands. Only actual .NET host processes have `.dll` in their command line arguments. This fix is already implemented in the 004-camera-smooth-demos merge on main.

**Implementation**: Already done on `main` branch. This branch needs rebasing to inherit the fix.

**Alternatives considered**:
- PID file tracking: Rejected — unreliable if processes crash.
- Process group (PGID) kill: More robust but requires recording PGID at startup, adding complexity to start.sh.

## R4: Body-Not-Found Camera Hold Behavior

**Decision**: When a body-relative camera mode references a body ID not found in the position map, hold the camera's current position and keep the mode active (don't cancel).

**Rationale**: Newly-created bodies may not appear in the simulation state for 1-2 frames (16-33ms at 60Hz). Immediately cancelling the camera mode on body-not-found causes a race condition where follow/orbit/chase commands on freshly-created bodies are silently dropped. Holding position allows the mode to activate once the body appears.

**Implementation**: Already done in 004-camera-smooth-demos merge on main (CameraController.fs `updateCameraMode` returns `state` instead of `{ state with ActiveMode = None }` on body-not-found). This branch needs rebasing.

## R5: Rebase Strategy

**Decision**: Rebase `005-robust-network-connectivity` on `main` before implementation to inherit all 004-camera-smooth-demos fixes.

**Rationale**: The 004 merge to main includes fixes for:
- FR-007/FR-008: kill.sh .dll patterns
- FR-009: Body-not-found camera hold
- FR-012: ConcurrentQueue drain loop in viewer
- Proto changes: 9 new ViewCommand message types
- CameraController: Full body-relative mode state machine

Without rebasing, the 005 implementation would need to re-implement these fixes and conflict with the main branch on merge.
