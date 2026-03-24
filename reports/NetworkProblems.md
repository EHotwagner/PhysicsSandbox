# Network Problems Log

Structured log of Aspire, gRPC, port, certificate, and service connectivity issues encountered during development.

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
**Resolution**: Changed kill patterns from bare names (`PhysicsViewer`) to `.dll` suffixes (`PhysicsViewer.dll`). Only actual .NET runtime host processes have `.dll` in their command line.
**Prevention**: Always use specific executable patterns in `pkill -f`. Bare substrings match too broadly (editors, build tools, shell sessions, cwd paths).
