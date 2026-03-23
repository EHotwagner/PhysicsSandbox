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
