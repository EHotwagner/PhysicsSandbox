---
name: MCP Server Configuration
description: How both MCP servers (physics-sandbox and aspire-dashboard) are configured, key auth fixes, and common pitfalls
type: project
---

Two MCP servers are configured for Claude Code in `.mcp.json` (project root). The `.claude/settings.local.json` is cleared.

## Physics Sandbox MCP (SSE)
- Transport: SSE at `http://localhost:5000/sse`
- Connects to PhysicsServer via Aspire service discovery (`services__server__https__0`)
- Requires `Http1AndHttp2` protocol on PhysicsServer Kestrel (not `Http2` only) — the Aspire DCP proxy needs HTTP/1.1 to forward gRPC streams
- 32 MCP tools for simulation control, presets, generators, steering

**Why:** `Http2`-only caused gRPC streams to connect briefly then drop because the DCP reverse proxy communicates with backends over HTTP/1.1.

**How to apply:** If physics MCP streams show "disconnected", check `AppHost.cs` has `Http1AndHttp2`, not `Http2`.

## Aspire Dashboard MCP (Streamable HTTP)
- Transport: HTTP at `http://localhost:18093/mcp` (http profile)
- Must use the **http** launch profile — Claude Code rejects self-signed TLS certs on the https profile endpoint
- Provides resource management, logs, traces, commands for Aspire-orchestrated services (6 tools)
- Auth is disabled via `AppHost__McpApiKey=""` env var in launchSettings.json AND `AppHost:McpApiKey: ""` in appsettings.json

**Why:** Aspire 13.1.3 hardcodes `DASHBOARD__MCP__AUTHMODE=ApiKey` on the dashboard process with an auto-generated key. The only way to get `Unsecured` mode is setting `AppHost__McpApiKey` to empty string — this causes the hosting layer to set `DASHBOARD__MCP__AUTHMODE=Unsecured` instead. The `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS` env var does NOT affect MCP auth (only frontend).

**How to apply:** If Aspire MCP returns 401/403, verify `AppHost__McpApiKey=""` is in the launch profile env vars. The appsettings.json alone is insufficient — the env var must also be present.

## Startup Order (Critical)
Claude Code does NOT retry failed MCP connections (github.com/anthropics/claude-code/issues/31198). The stack **must be running before** Claude Code starts, otherwise the Aspire Dashboard MCP tools won't be discovered.

**How to apply:** Always start the stack first, then Claude Code:
```bash
./start.sh && MCP_TIMEOUT=10000 claude
```
The `start.sh` script now defaults to the `http` profile (use `--https` to override). `MCP_TIMEOUT=10000` gives the server 10 seconds to respond during tool discovery.

## Key Pitfalls
- Claude Code does not retry failed MCP connections — start the stack before Claude Code
- Claude Code rejects self-signed TLS certs — use the http profile for the Aspire Dashboard MCP
- `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true` only unsecures the dashboard frontend, not MCP or OTLP
- `Dashboard__Mcp__AuthMode=Unsecured` in launch profile is overridden by Aspire hosting's uppercase `DASHBOARD__MCP__AUTHMODE=ApiKey`
- The Aspire CLI (`aspire mcp start`) uses stdio transport via Unix socket backchannel — requires `aspire run` (not `dotnet run`) and doesn't survive AppHost restart order issues
- Aspire CLI v13.1.3 installed globally (`dotnet tool install -g aspire.cli`)
- MCP config must be in `.mcp.json`, not `.claude/settings.local.json` — the Aspire Dashboard MCP (`type: "http"`) only worked from `.mcp.json`
