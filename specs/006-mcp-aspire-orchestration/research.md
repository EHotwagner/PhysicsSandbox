# Research: Add MCP Server to Aspire AppHost Orchestration

## R1: How do existing services resolve the PhysicsServer address?

**Decision**: Use environment variables `services__server__https__0` / `services__server__http__0` with fallback to hardcoded default.

**Rationale**: All three existing client services (PhysicsSimulation, PhysicsViewer, PhysicsClient) use this exact pattern. Aspire's `WithReference(server)` automatically injects these environment variables. The MCP server should follow the identical convention for consistency.

**Alternatives considered**:
- Aspire service discovery client (`Microsoft.Extensions.ServiceDiscovery`): Requires adding ServiceDefaults and configuring HTTP client factory. The MCP server creates its gRPC channel directly, not via DI, so this would require significant restructuring for no benefit.
- Command-line argument passthrough from AppHost: Possible but non-standard. Environment variables are the established Aspire convention.

## R2: Does the MCP server's stdio transport conflict with Aspire orchestration?

**Decision**: No conflict. Aspire manages project resources as child processes regardless of their transport mechanism.

**Rationale**: The PhysicsClient also runs as a non-HTTP process (REPL with Spectre.Console TUI). It's already orchestrated via `AddProject` without issues. The MCP server's stdio transport is between the MCP server and the AI assistant, not between the MCP server and Aspire. Aspire captures stdout/stderr for dashboard logging regardless.

**Alternatives considered**: None — this was a validation question, not a design choice.

## R3: Does the MCP server need ServiceDefaults?

**Decision**: No. The MCP server does not need `AddServiceDefaults()`.

**Rationale**: ServiceDefaults configures OpenTelemetry, health checks, and HTTP service discovery for ASP.NET services. The MCP server is not an ASP.NET service — it uses `Host.CreateApplicationBuilder` with stdio transport. Adding ServiceDefaults would require adding ASP.NET dependencies for no benefit. The existing services that use ServiceDefaults (PhysicsServer, PhysicsSimulation, PhysicsViewer) are all ASP.NET/gRPC servers. PhysicsClient also does not use ServiceDefaults.

**Alternatives considered**:
- Add ServiceDefaults for OpenTelemetry: Would provide trace correlation but requires pulling in ASP.NET hosting dependencies into a non-web project. Out of scope for this feature — can be added later if needed.

## R4: What changes are needed in Program.fs?

**Decision**: Replace the hardcoded default address with environment variable lookup, following the PhysicsSimulation pattern.

**Rationale**: Current code: `if args.Length > 0 then args.[0] else "https://localhost:7180"`. New code should check `services__server__https__0`, then `services__server__http__0`, then fall back to `"https://localhost:7180"` for standalone use. Command-line args should still take precedence for manual overrides.

**Alternatives considered**:
- Remove CLI arg support entirely: Would break standalone `dotnet run --project src/PhysicsSandbox.Mcp -- https://localhost:7180` usage documented in CLAUDE.md.
- Use Aspire service discovery client: Over-engineered for a simple address lookup (see R1).
