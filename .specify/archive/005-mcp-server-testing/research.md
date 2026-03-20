# Research: MCP Server and Integration Testing

**Date**: 2026-03-20 | **Feature**: 005-mcp-server-testing

## Decision 1: MCP SDK for .NET

**Decision**: Use `ModelContextProtocol` NuGet package (v1.1.0) with `Microsoft.Extensions.Hosting` for stdio-based MCP server.

**Rationale**: This is the official MCP SDK for .NET, released by the MCP organization. It provides attribute-based tool discovery (`[McpServerToolType]`, `[McpServerTool]`), stdio transport via `WithStdioServerTransport()`, and integrates with standard .NET DI/hosting. Version 1.1.0 is stable (released 2026-03-06), targets netstandard2.0+ through net10.0.

**Alternatives considered**:
- Custom JSON-RPC implementation over stdio: Rejected — unnecessary work when official SDK exists.
- `ModelContextProtocol.AspNetCore` (HTTP/SSE transport): Rejected — stdio is the standard for local tool servers invoked by AI assistants.
- `ModelContextProtocol.Core` (minimal): Rejected — the full `ModelContextProtocol` package adds hosting/DI which we need for gRPC channel lifecycle.

## Decision 2: F# vs C# for MCP Server

**Decision**: Write the MCP server in F# with `[<McpServerToolType>]` attributed types, matching the project's F# convention.

**Rationale**: The SDK's attribute-based tool discovery works on static methods. F# types with static members compile to classes that the SDK can discover. The `Host.CreateApplicationBuilder` pattern works identically in F#. This keeps the project consistent with the constitution's "F# on .NET is the exclusive stack within each service" constraint.

**Alternatives considered**:
- C# MCP server: Would work but violates the constitution's F# stack requirement for services. The AppHost is C# because it's Aspire boilerplate, but the MCP server has domain logic (tool definitions, state formatting).

## Decision 3: SSL Bypass Pattern

**Decision**: Copy the `createChannel` function from `PhysicsClient/Connection/Session.fs` (lines 22-31) into both the MCP server and the SimulationClient fix.

**Rationale**: This is the established, working pattern in the codebase. It handles both HTTPS (with dev cert bypass) and plain HTTP (with forced HTTP/2). The integration tests also use this pattern.

**Alternatives considered**:
- Use HTTP-only endpoints: Rejected — gRPC over plain HTTP requires `Http2UnencryptedSupport` AppContext switch and server protocol changes, which is fragile.
- Trust dev certs system-wide: Rejected — environment-dependent, doesn't work in all container/CI scenarios.

## Decision 4: Simulation Reconnection Strategy

**Decision**: Add exponential backoff reconnection loop (1s → 10s max) around the SimulationClient's main `run` function, matching the PhysicsClient's state stream pattern.

**Rationale**: The simulation is an Aspire-managed long-running worker. If its gRPC stream drops, it should reconnect automatically rather than staying dead. The PhysicsClient already implements this pattern for its state stream (Session.fs lines 33-59). The simulation preserves its BepuPhysics world across reconnections.

**Alternatives considered**:
- Let Aspire restart the process: Rejected — loses simulation world state (all bodies, forces, time). Reconnecting the stream preserves the physics world.
- Fixed interval retry: Rejected — exponential backoff is better for server recovery scenarios.

## Decision 5: MCP Server State Caching

**Decision**: MCP server opens a `StreamState` subscription on startup and caches the latest state. `get_state` returns the cached snapshot with a timestamp.

**Rationale**: This matches the PhysicsClient pattern (background state stream in Session.fs). Opening a new stream per `get_state` call would add 100-500ms latency per invocation, which defeats the "fast exploration" goal. The cached approach gives instant responses.

**Alternatives considered**:
- On-demand stream per call: Rejected — too slow for interactive exploration.
- Hybrid (background + fallback): Unnecessary complexity — the background stream already handles reconnection.

## Decision 6: Integration Test Infrastructure

**Decision**: Extend the existing `PhysicsSandbox.Integration.Tests` C# project with new test classes. Continue using `DistributedApplicationTestingBuilder` and the established SSL bypass handler pattern.

**Rationale**: The test project already exists with 5 tests and the correct Aspire testing setup. Adding new test classes is simpler than creating a new project. C# is already used here (Aspire testing templates are C#-only).

**Alternatives considered**:
- New F# integration test project: Rejected — Aspire testing builder generates `Projects.*` types via MSBuild targets that are untested with F# projects. The existing C# test project works.
- Separate test projects per concern: Rejected — over-engineering. One project with multiple test classes organized by concern is sufficient.

## Decision 7: MCP Server Address Configuration

**Decision**: The MCP server accepts the PhysicsServer gRPC address as a command-line argument (first positional arg). Defaults to `https://localhost:7180` (the standard Aspire HTTPS proxy port).

**Rationale**: Command-line arguments are the simplest configuration mechanism and work naturally with MCP client configurations (which specify the command + args to launch the server). Environment variables would also work but args are more explicit in MCP configs.
