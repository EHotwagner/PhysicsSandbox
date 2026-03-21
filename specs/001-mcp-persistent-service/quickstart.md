# Quickstart: MCP Persistent Service

**Date**: 2026-03-21 | **Feature**: 001-mcp-persistent-service

## What This Feature Does

Transforms the MCP server from a stdio-based process (dies when client disconnects) into a persistent HTTP/SSE service that runs for the lifetime of the Aspire AppHost. AI assistants connect via network socket, can disconnect and reconnect freely, and have full visibility into all system messages plus convenience tools matching the REPL client library.

## Key Changes

1. **Transport**: stdio → HTTP/SSE (via `ModelContextProtocol.AspNetCore`)
2. **Lifecycle**: Client-bound → AppHost-bound (persistent)
3. **Visibility**: State-only → State + View Commands + Command Audit Stream
4. **Tools**: 15 existing tools + ~20 new convenience tools (presets, generators, steering, audit)
5. **Server**: New `StreamCommands` RPC on PhysicsServer for command audit broadcasting

## Affected Projects

| Project | Changes |
|---------|---------|
| PhysicsSandbox.Shared.Contracts | Add `CommandEvent` message, `StreamCommands` RPC to proto |
| PhysicsServer | Add command audit subscriber support to MessageRouter, implement `StreamCommands` |
| PhysicsSandbox.Mcp | Switch transport, add PhysicsClient reference, new tool modules, extend GrpcConnection |
| PhysicsSandbox.AppHost | Update MCP resource configuration for HTTP endpoint |

## Build & Test

```bash
# Build
dotnet build PhysicsSandbox.slnx

# Test
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run
dotnet run --project src/PhysicsSandbox.AppHost
```

## Connecting an AI Assistant

After starting the AppHost, configure your AI assistant's MCP client to connect via HTTP/SSE to the MCP server's endpoint (visible in the Aspire dashboard).
