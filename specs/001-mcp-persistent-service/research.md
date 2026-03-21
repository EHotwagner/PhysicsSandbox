# Research: MCP Persistent Service

**Date**: 2026-03-21 | **Feature**: 001-mcp-persistent-service

## Decision 1: MCP Transport — HTTP/SSE via ModelContextProtocol.AspNetCore

**Decision**: Switch from `ModelContextProtocol` (stdio) to `ModelContextProtocol.AspNetCore` (HTTP/SSE transport).

**Rationale**: The `ModelContextProtocol.AspNetCore` NuGet package provides HTTP/SSE server transport that runs as an ASP.NET Core endpoint. This enables the MCP server to persist independently of client connections — clients connect/disconnect via HTTP without affecting the server process lifecycle. The package is the official companion to the base `ModelContextProtocol` package already in use.

**Alternatives considered**:
- `ModelContextProtocol` with stdio (current): Rejected — stdio ties server lifecycle to client process. Server exits when client disconnects.
- Custom WebSocket transport: Rejected — `ModelContextProtocol.AspNetCore` already provides the needed transport. No need to build custom.

**Implementation notes**:
- Add `ModelContextProtocol.AspNetCore` package reference (version 1.1.*)
- Replace `Host.CreateApplicationBuilder` with `WebApplication.CreateBuilder` (ASP.NET Core host)
- Replace `.WithStdioServerTransport()` with `.WithHttpTransport()` or equivalent ASP.NET Core middleware
- Map MCP endpoint route (e.g., `/mcp`)
- Kestrel will handle multiple concurrent connections natively

## Decision 2: Command Audit Stream — Subscriber-Based Broadcasting

**Decision**: Add a new `StreamCommands` RPC to the `PhysicsHub` gRPC service using a subscriber-based fan-out pattern (matching the existing `StreamState` pattern).

**Rationale**: The server already uses a subscriber-based pattern for state broadcasting — each `StreamState` subscriber registers a callback in a `ConcurrentDictionary`, and `publishState` fans out to all subscribers. Commands currently use a single-consumer `Channel<T>` (for the simulation). The audit stream needs to broadcast to N audit subscribers simultaneously, so the subscriber pattern is the right fit.

**Alternatives considered**:
- Channel-based (single consumer per channel): Rejected — would only support one audit subscriber at a time.
- Interceptor/middleware pattern: Rejected — adds unnecessary abstraction; the existing subscriber pattern is proven and simple.

**Implementation notes**:
- New proto message `CommandEvent` wrapping either `SimulationCommand` or `ViewCommand` in a oneof
- New RPC: `rpc StreamCommands(StateRequest) returns (stream CommandEvent)`
- New field on `MessageRouter`: `CommandSubscribers: ConcurrentDictionary<Guid, CommandEvent -> Task>`
- Modify `submitCommand` and `submitViewCommand` to also publish to command subscribers
- New functions: `subscribeCommands`, `unsubscribeCommands`, `publishCommandEvent`

## Decision 3: PhysicsClient Library Reference — Direct Project Reference

**Decision**: PhysicsSandbox.Mcp will directly reference the PhysicsClient project to reuse convenience functions (Presets, Generators, Steering).

**Rationale**: PhysicsClient is a library, not a service. Constitution Principle III ("Shared Nothing") restricts service-to-service references but does not prohibit service-to-library references. The AppHost already references PhysicsClient, establishing precedent. Duplicating 9 F# modules of client logic would violate DRY without any architectural benefit.

**Alternatives considered**:
- Extract shared modules into a new library project: Rejected — over-engineering. PhysicsClient IS the library.
- Duplicate/reimplement convenience functions in MCP: Rejected — maintenance burden, drift risk, no architectural benefit.
- Reference PhysicsClient as a NuGet package: Rejected — unnecessary packaging overhead for an in-solution library.

**Implementation notes**:
- Add `<ProjectReference Include="..\PhysicsClient\PhysicsClient.fsproj" />` to MCP .fsproj
- MCP tools can call PhysicsClient modules directly (Presets, Generators, Steering)
- MCP will NOT use PhysicsClient's Session module — it has its own GrpcConnection singleton
- Need adapter layer to bridge MCP's GrpcConnection with PhysicsClient functions that expect a Session

## Decision 4: GrpcConnection Refactoring — Add View Command and Audit Streams

**Decision**: Extend MCP's `GrpcConnection` singleton to subscribe to all three server streams: `StreamState` (existing), `StreamViewCommands` (new), and `StreamCommands` (new audit stream).

**Rationale**: The MCP server currently only subscribes to `StreamState`. Full message visibility (FR-004) requires subscribing to all available streams. The shared singleton pattern means all MCP client sessions see the same data.

**Implementation notes**:
- Add `LatestViewCommand` cache alongside `LatestState`
- Add `CommandLog` (bounded circular buffer) for recent commands
- Three background streaming tasks with independent exponential backoff
- Query tools expose all three data sources
