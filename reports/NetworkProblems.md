# Network Problems Log

Structured log of Aspire, gRPC, port, certificate, and service connectivity issues encountered during development.

---

## Container Environment

**Runtime**: Podman (rootless)
**Networking**: All services communicate internally via localhost. Only the Aspire dashboard is exposed externally.

| Port  | Typical Use       | Notes |
|-------|-------------------|-------|
| 4173  | Vite preview      | |
| 5000  | .NET HTTP         | |
| 5001  | .NET HTTPS        | |
| 5137  | Vite dev          | |
| 5173  | Vite dev          | |
| 5199  | MCP SSE (HTTP)    | isProxied=false, bypasses DCP |
| 8080  | General HTTP      | |
| 8081  | General HTTP      | |
| 18888 | Aspire Dashboard  | Only externally-exposed port (pending container rebuild) |
| 50051 | gRPC              | |

**Networking boundary**: Server, Simulation, Viewer, Client, and MCP all run inside the container. Aspire DCP allocates dynamic internal ports behind the mapped ports above. MCP clients (including Claude Code) operate within the container.

---

### Aspire Dashboard MCP returns 403 Forbidden on HTTP/SSE connections — 2026-03-25

**Context**: Attempting to connect to the Aspire Dashboard MCP server at `http://localhost:18093/sse` to test its tools using a Python MCP client over HTTP/SSE transport.

**Error**: `HTTP/1.1 403 Forbidden` on all SSE connection attempts, regardless of auth header (no auth, empty Bearer, explicit API key, dashboard browser token).

**Root Cause**: In .NET Aspire 13.1.3, the Dashboard MCP endpoint is configured with `DASHBOARD__MCP__USECLIMCP=true`, which means it only supports CLI-based stdio pipe transport, not HTTP/SSE. The DCP proxy (dcpctrl) on port 18093 enforces authentication that cannot be satisfied via HTTP headers.

**Hypothesis**: The `AppHost__McpApiKey` setting (empty by default) is ignored when `UseCliMcp=true`. The MCP endpoint is designed exclusively for the `aspire mcp` CLI command which uses stdio transport, not for HTTP/SSE clients.

**Resolution**: Resolved in `004-mcp-fix-aspire-config`. Configured Aspire Dashboard MCP in `.mcp.json` using stdio transport (`aspire agent mcp --nologo --non-interactive`), bypassing the 403 auth requirement. Available to all developers alongside the existing PhysicsSandbox MCP SSE config.

**Prevention**: When integrating with Aspire Dashboard MCP, use the CLI/stdio transport path. Do not attempt HTTP/SSE connections to port 18093.

---

### MCP tool parameter deserialization failures (17 tools) — 2026-03-25

**Context**: Testing all 59 PhysicsSandbox MCP tools via HTTP/SSE JSON-RPC client.

**Error**: `"An error occurred invoking '<tool>'"` for 17 tools when sending JSON arguments with only the relevant parameters (omitting unused nullable parameters).

**Root Cause**: The ModelContextProtocol.AspNetCore framework auto-generates JSON schemas that mark ALL F# method parameters as `required`, even those typed as `Nullable<T>` or `Option<T>`. When a client omits unused parameters (e.g., `half_extents_x` when creating a sphere), the framework fails to deserialize the arguments.

**Resolution**: Resolved in `004-mcp-fix-aspire-config`. Converted F# optional parameters (`?param: Type`) to `Nullable<T>` which the MCP framework recognizes as optional. Also converted logically-optional required params (pagination, time ranges, seeds) to `Nullable<T>` with sensible defaults. All 59 tools now accept requests with only relevant parameters. Automated regression test added to integration suite.

**Prevention**: No longer needed — optional parameters are now correctly marked in the schema. Clients can omit irrelevant parameters.

---

### MCP SSE endpoint unreachable via DCP proxy — 2026-03-23

**Context**: Testing MCP recording tools by curling `http://localhost:5180/sse`
**Error**: `An HTTP/1.x request was sent to an HTTP/2 only endpoint.`
**Root Cause**: Port 5180 is the Aspire DCP reverse proxy, which enforces HTTP/2 for gRPC. The MCP SSE endpoint expects HTTP/1.1. The actual MCP server listens on a dynamic port (e.g., 35745) behind DCP.
**Hypothesis**: N/A — root cause confirmed
**Resolution**: Connect directly to the MCP server's dynamic port (found via `ss -tlnp | grep PhysicsSandbox.Mcp`) instead of the DCP proxy port.
**Prevention**: When testing MCP tools outside of Aspire dashboard, resolve the actual service port rather than the proxy port. Aspire's DCP proxy is HTTP/2-only for gRPC services.

---

### MCP server GrpcConnection never started — lazy DI — 2026-03-23

**Context**: Recording auto-start was not triggering. No recording sessions created despite simulation running.
**Error**: No error — silent failure. `~/.config/PhysicsSandbox/recordings/` remained empty.
**Root Cause**: `GrpcConnection` registered as singleton via `AddSingleton<GrpcConnection>(fun _ -> ...)` — this is a lazy factory. The connection (and its 3 background streams) only starts when first resolved from DI, which happens when an MCP tool is called. Without any tool call, no streams connect, no state received, no auto-start.
**Resolution**: Added `app.Services.GetRequiredService<GrpcConnection>() |> ignore` after `builder.Build()` to eagerly resolve the singleton at startup.
**Prevention**: Any service that needs to run background work on startup must be eagerly resolved. ASP.NET Core DI singletons are lazy by default.

---

### ViewCommand single-slot Volatile.Write drops rapid commands — 2026-03-24

**Context**: Implementing smooth camera controls. Demo scripts send setNarration + smoothCamera commands ~100ms apart. Viewer receives only ~30% of ViewCommands.
**Error**: No error — silent command loss. SmoothCamera, CameraOrbit, CameraFollow commands never arrive at the viewer. SetNarration and CameraStop arrive sporadically.
**Root Cause**: Viewer used `Volatile.Write(&latestViewCmd, stream.Current)` (single-slot overwrite) in the background gRPC stream reader, and `Interlocked.Exchange(&latestViewCmd, null)` in the 15 FPS game update loop. Rapid commands overwrote each other before the update loop consumed them.
**Resolution**: Replaced with `ConcurrentQueue<ViewCommand>` + `while TryDequeue` drain loop. All queued commands are now processed each frame.
**Prevention**: Never use single-slot volatile write for command streams where multiple commands may arrive between consumer reads. Use a concurrent queue or channel.

---

### Duplicate StreamViewCommands subscribers steal commands — 2026-03-24

**Context**: After fixing the single-slot issue, commands were still missing. Aspire RECV logs showed random subsets of commands arriving.
**Error**: Non-deterministic command delivery — some commands go to the viewer, others vanish.
**Root Cause**: The server's `ViewCommandChannel` is a single-consumer `Channel<ViewCommand>`. `StreamViewCommands` gRPC RPC reads from `Channel.Reader.ReadAsync`. When two viewer processes exist (stale viewer from previous Aspire stack + new viewer), they compete for the single channel — each `ReadAsync` dequeues one command, distributing round-robin. Half the commands go to the stale viewer.
**Hypothesis**: N/A — root cause confirmed via `ps aux` showing two PhysicsViewer processes.
**Resolution**: Fixed `kill.sh` patterns to use `.dll` suffix (e.g., `PhysicsViewer.dll`) to reliably kill stale processes without matching shell sessions. Killed stale viewer before running demos.
**Prevention**: (1) Always run `kill.sh` before starting a new Aspire stack. (2) The ViewCommand channel architecture only supports a single viewer subscriber — if multi-viewer is ever needed, replace with pub/sub broadcast.

---

### kill.sh pkill -f self-kill via command line matching — 2026-03-24

**Context**: Running `./kill.sh && dotnet build src/PhysicsViewer` fails with exit code 144 (SIGKILL).
**Error**: `Exit code 144` — the bash process running the chained command is killed.
**Root Cause**: `pkill -f "PhysicsViewer"` matches the FULL command line of all processes. The bash shell running `./kill.sh && dotnet build src/PhysicsViewer` has "PhysicsViewer" in its arguments, so `pkill -9 -f` kills it. Same issue with any pattern like `PhysicsSandbox.AppHost`, `PhysicsServer`, etc. — the cwd path `/home/developer/projects/PhysicsSandbox` or chained commands contain these strings.
**Resolution**: Changed kill patterns from bare names (`PhysicsViewer`) to `/bin` path suffixes (e.g., `PhysicsViewer/bin`) and `--project` patterns. Only actual .NET runtime host processes and `dotnet run` invocations match these patterns.
**Prevention**: Always use specific executable patterns in `pkill -f`. Bare substrings match too broadly (editors, build tools, shell sessions, cwd paths).

---

### Body-not-found cancels camera mode immediately — 2026-03-24

**Context**: Implementing body-relative camera modes (Follow, Orbit, Chase). Demo scripts send CameraFollow on a freshly-created body.
**Error**: No error — silent behavior. Camera mode is immediately cancelled; the follow/orbit/chase never activates.
**Root Cause**: `updateCameraMode` cancelled any body-relative mode (Following, Orbiting, Chasing, Framing) when the body ID was not found in the `bodyPositions` map: `| Following bodyId -> match Map.tryFind bodyId bodyPositions with | None -> { state with ActiveMode = None }`. A newly-created body may not appear in the simulation state for 1-2 frames (16-33ms at 60Hz).
**Resolution**: Changed body-not-found behavior to hold position and keep mode active: `| None -> state`. The mode stays active until the body appears.
**Prevention**: Body-relative modes must tolerate delayed body appearance. Never cancel a mode on body-not-found — hold position and wait.

---

### ViewCommand single-consumer channel replaced with per-subscriber broadcast — 2026-03-24

**Context**: 005-robust-network-connectivity feature. The single-consumer `Channel<ViewCommand>` caused round-robin distribution when multiple viewers subscribed (only one viewer received each command).
**Error**: Architecture limitation — not a runtime error. With N viewers, each viewer received ~1/N of the commands.
**Root Cause**: `Channel.Reader.ReadAsync` dequeues one item per call. When two `StreamViewCommands` RPCs read from the same channel, commands are distributed round-robin instead of broadcast.
**Resolution**: Replaced `ViewCommandChannel: Channel<ViewCommand>` with `ViewCommandSubscribers: ConcurrentDictionary<Guid, Channel<ViewCommand>>`. Each `StreamViewCommands` RPC gets its own bounded channel (1024). `submitViewCommand` iterates all subscriber channels and calls `TryWrite` on each (newest-drop if full). Zero subscribers = silent discard.
**Prevention**: Never use a single-consumer channel for fan-out delivery. Use per-subscriber channels or callback registries for broadcast semantics.
