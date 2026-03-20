# Quickstart: MCP Server in Aspire Orchestration

## What changed

The MCP server (`PhysicsSandbox.Mcp`) is now managed by the Aspire AppHost alongside the other services. You no longer need to start it manually.

## Running

```bash
# Start everything (includes MCP server)
dotnet run --project src/PhysicsSandbox.AppHost

# MCP server will appear in the Aspire dashboard as "mcp"
```

## Standalone mode (still supported)

```bash
# Run MCP server independently (e.g., for development)
dotnet run --project src/PhysicsSandbox.Mcp

# With custom server address
dotnet run --project src/PhysicsSandbox.Mcp -- https://localhost:7180
```

## Address resolution priority

When launched via Aspire, the MCP server resolves the PhysicsServer address in this order:

1. Command-line argument (if provided)
2. `services__server__https__0` environment variable (set by Aspire)
3. `services__server__http__0` environment variable (set by Aspire)
4. Hardcoded fallback: `https://localhost:7180`

## Connecting an AI assistant

The MCP server uses stdio transport. Configure your AI assistant's MCP client to run:

```json
{
  "command": "dotnet",
  "args": ["run", "--project", "src/PhysicsSandbox.Mcp"]
}
```

When running under Aspire, the server address is injected automatically — no manual configuration needed.
