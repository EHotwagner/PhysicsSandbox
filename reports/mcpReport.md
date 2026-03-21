# MCP Server Configuration Report

Date: 2026-03-21

## Summary

Two MCP servers are configured for Claude Code to interact with the PhysicsSandbox project:

1. **Physics Sandbox MCP** — Direct simulation control (SSE transport)
2. **Aspire Dashboard MCP** — Resource management, logs, traces (Streamable HTTP transport)

## Physics Sandbox MCP

| Property | Value |
|----------|-------|
| Transport | SSE |
| URL | `http://localhost:5000/sse` |
| Config | `.mcp.json` |
| Tools | 32 (simulation, presets, generators, steering, audit) |

### How It Works
The MCP server (`src/PhysicsSandbox.Mcp`) runs as an Aspire-orchestrated service. It connects to the PhysicsServer via gRPC using Aspire service discovery (`services__server__https__0` env var). It maintains 3 background gRPC streams:

- **State stream** — cached simulation state (bodies, time, running status)
- **View command stream** — camera/wireframe commands from the viewer
- **Command audit stream** — bounded log of all commands processed by the server

### Issue Fixed: gRPC Stream Disconnection
**Problem:** Streams would connect briefly then disconnect.

**Root cause:** `AppHost.cs` had `ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS` set to `Http2` instead of `Http1AndHttp2`. The Aspire DCP reverse proxy communicates with backend services over HTTP/1.1, so `Http2`-only mode prevented stable gRPC stream forwarding.

**Fix:** Changed to `Http1AndHttp2` in `src/PhysicsSandbox.AppHost/AppHost.cs`:
```csharp
var server = builder.AddProject<Projects.PhysicsServer>("server")
    .WithEnvironment("ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS", "Http1AndHttp2");
```

## Aspire Dashboard MCP

| Property | Value |
|----------|-------|
| Transport | Streamable HTTP |
| URL | `http://localhost:18093/mcp` (http profile) |
| Config | `.mcp.json` |
| Tools | 6 (list_resources, list_console_logs, list_structured_logs, list_traces, list_trace_structured_logs, execute_resource_command) |

### How It Works
The Aspire Dashboard (v13.1.3) includes a built-in MCP server that exposes resource management, log querying, distributed tracing, and resource commands. It runs on a dedicated Kestrel endpoint configured via `ASPIRE_DASHBOARD_MCP_ENDPOINT_URL`.

### Issue Fixed: 401/403 Authentication Errors
**Problem:** The MCP endpoint returned 403 (on `/sse`) or 401 (on `/mcp`) regardless of API key headers.

**Root cause:** Aspire 13.1.3 hardcodes `DASHBOARD__MCP__AUTHMODE=ApiKey` on the dashboard process with an auto-generated API key that changes every restart. Several attempted fixes failed:

| Attempt | Result |
|---------|--------|
| `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true` | Only affects frontend auth, not MCP |
| `Dashboard__Mcp__AuthMode=Unsecured` (launch profile) | Overridden by Aspire's uppercase `DASHBOARD__MCP__AUTHMODE=ApiKey` |
| `Dashboard:Mcp:AuthMode` in appsettings.json | Read by AppHost but overridden when constructing dashboard env vars |
| Fixed API key via `x-api-key` header | 401 — connection type auth scheme rejects before API key check |

**Fix:** Set `AppHost__McpApiKey` to an empty string. When the Aspire hosting layer sees an empty/null MCP API key, it sets `DASHBOARD__MCP__AUTHMODE=Unsecured` instead of `ApiKey`.

Applied in two places (both required):

`src/PhysicsSandbox.AppHost/Properties/launchSettings.json`:
```json
"AppHost__McpApiKey": ""
```

`src/PhysicsSandbox.AppHost/appsettings.json`:
```json
"AppHost": {
    "McpApiKey": ""
}
```

## Alternative: Aspire CLI (stdio)

The Aspire CLI (`aspire.cli` v13.1.3, installed globally) provides an `aspire mcp start` command that uses stdio transport. However, it requires:
- The AppHost to be started via `aspire run` (not `dotnet run`) to enable the Unix socket backchannel
- The AppHost to be running before the MCP client connects

This makes it unsuitable for Claude Code since the AppHost startup order can't be guaranteed. The HTTP transport approach is preferred.

## Issue: Claude Code Does Not Retry Failed MCP Connections

**Problem:** Claude Code discovers MCP tools at startup only. If the stack isn't running when Claude Code starts, the Aspire Dashboard MCP tools are never loaded for the entire session. No retry or reconnect mechanism exists.

**Tracked:** [github.com/anthropics/claude-code/issues/31198](https://github.com/anthropics/claude-code/issues/31198)

**Root cause:** The physics-sandbox MCP appears to work regardless because its process (port 5000) starts quickly and is available for tool discovery even when its backend gRPC streams haven't connected yet. The Aspire Dashboard MCP (port 18093) only becomes available once the full Aspire stack is running, which takes ~20 seconds.

**Fix:** Always start the stack before Claude Code:
```bash
./start.sh && MCP_TIMEOUT=10000 claude
```

The `start.sh` script now defaults to the `http` profile (use `--https` to override). `MCP_TIMEOUT=10000` gives 10 seconds for tool discovery.

## Issue: Claude Code Rejects Self-Signed TLS Certificates

**Problem:** The https profile serves the Aspire Dashboard MCP at `https://localhost:23197/mcp` with a self-signed dev certificate. Claude Code's HTTP client rejects untrusted certificates, so the MCP connection silently fails.

**Fix:** Use the http profile instead. The `start.sh` script now defaults to `http`. The MCP config points to `http://localhost:18093/mcp`.

## Issue: MCP Config Must Be in `.mcp.json`

**Problem:** MCP server config placed in `.claude/settings.local.json` was not picked up for the Aspire Dashboard MCP (`type: "http"`). The physics-sandbox MCP (`type: "sse"`) worked from either location.

**Fix:** Both MCP servers are configured in `.mcp.json` (project root). The `.claude/settings.local.json` is cleared to `{}`.

## Configuration Files

### `.mcp.json`
```json
{
  "mcpServers": {
    "physics-sandbox": {
      "type": "sse",
      "url": "http://localhost:5000/sse"
    },
    "aspire-dashboard": {
      "type": "http",
      "url": "http://localhost:18093/mcp"
    }
  }
}
```

### Files Modified
- `src/PhysicsSandbox.AppHost/AppHost.cs` — `Http1AndHttp2` protocol
- `src/PhysicsSandbox.AppHost/appsettings.json` — `AppHost:McpApiKey: ""`
- `src/PhysicsSandbox.AppHost/Properties/launchSettings.json` — `AppHost__McpApiKey: ""` in both profiles
- `start.sh` — defaults to `http` profile (was `https`)
- `.mcp.json` — added `aspire-dashboard` MCP server
- `.claude/settings.local.json` — cleared (MCP config moved to `.mcp.json`)

## Final Status (2026-03-21)

Both MCP servers verified working:

### Physics Sandbox MCP
- All 3 gRPC streams: connected
- 32 tools available

### Aspire Dashboard MCP
- 6 tools available
- All 5 resources running:

| Resource | State | Endpoint |
|----------|-------|----------|
| server | Running | `http://localhost:5180` |
| simulation | Running | — |
| viewer | Running | — |
| client | Running | — |
| mcp | Running | — |

## Verification

```bash
# 1. Start the stack first (defaults to http profile)
./start.sh

# 2. Wait ~20s for all services, then start Claude Code
MCP_TIMEOUT=10000 claude

# 3. Check physics MCP (via MCP tool)
# get_status should show all 3 streams "connected"

# 4. Check Aspire MCP (via curl)
curl -s http://localhost:18093/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
# Should return HTTP 200 with server capabilities
```
