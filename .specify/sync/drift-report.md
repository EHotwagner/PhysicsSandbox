# Spec Drift Report

Generated: 2026-03-20T19:30:00Z
Project: PhysicsSandbox (006-mcp-aspire-orchestration)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 (006-mcp-aspire-orchestration) |
| Requirements Checked | 11 (7 FR + 4 SC) |
| Aligned | 11 (100%) |
| Drifted | 0 (0%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 0 |

## Detailed Findings

### Spec: 006-mcp-aspire-orchestration - Add MCP Server to Aspire AppHost Orchestration

#### Aligned

- FR-001: AppHost MUST register MCP server as project resource
  - `src/PhysicsSandbox.AppHost/AppHost.cs:19` — `builder.AddProject<Projects.PhysicsSandbox_Mcp>("mcp")`
- FR-002: MCP server resource MUST have service reference to PhysicsServer
  - `src/PhysicsSandbox.AppHost/AppHost.cs:20` — `.WithReference(server)`
- FR-003: MCP server MUST wait for PhysicsServer to be ready
  - `src/PhysicsSandbox.AppHost/AppHost.cs:21` — `.WaitFor(server)`
  - Test: `McpOrchestrationTests.McpResource_WaitsForServer`
- FR-004: MCP server MUST use service-discovered PhysicsServer address
  - `src/PhysicsSandbox.Mcp/Program.fs:13-18` — reads `services__server__https__0` / `services__server__http__0` env vars
- FR-005: MCP server MUST appear in Aspire dashboard with name, state, and logs
  - `src/PhysicsSandbox.AppHost/AppHost.cs:19` — registered as "mcp" resource
  - Test: `McpOrchestrationTests.McpResource_AppearsInAspireDashboard`
- FR-006: MCP server MUST shut down gracefully when AppHost stopped
  - Aspire handles graceful shutdown for `AddProject` resources automatically
  - Test: `McpOrchestrationTests.McpResource_ShutsDownGracefully`
- FR-007: Existing MCP server functionality MUST remain unchanged
  - Program.fs preserves CLI arg override (line 11), stdio transport (line 33), all tool assemblies (line 34)
  - Full test suite passes (149/149 excl. pre-existing flaky)
- SC-001: 5 project resources appear in Aspire dashboard
  - `AppHost.cs` registers: server, simulation, viewer, client, mcp
  - Test: `McpResource_AppearsInAspireDashboard` confirms "mcp" reaches Running
- SC-002: MCP server connects without manually specified address
  - `Program.fs:13-18` — env var lookup with fallback
- SC-003: All existing integration tests pass
  - 30/30 integration tests pass (includes 3 new MCP tests)
- SC-004: Structured logs visible in Aspire dashboard
  - Aspire captures stdout/stderr for all `AddProject` resources by design
  - MCP server logs to stderr via `LogToStandardErrorThreshold` (Program.fs:22-23)

#### Drifted

(none)

#### Not Implemented

(none)

### Unspecced Code

(none — all changes are within spec scope)

## Inter-Spec Conflicts

(none detected)

## Recommendations

No action needed. All requirements are fully aligned with implementation.
