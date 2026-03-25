# Research: MCP Tool Schema Fix & Aspire MCP Configuration

**Branch**: `004-mcp-fix-aspire-config` | **Date**: 2026-03-25

## R1: How does ModelContextProtocol.AspNetCore generate schemas from F# tool methods?

**Decision**: The framework uses `WithToolsFromAssembly()` to reflect on static methods with `[<McpServerTool>]` attributes. It discovers parameters, their types, and `[<Description>]` attributes. DI-registered types (e.g., `GrpcConnection`, `RecordingEngine`) are injected automatically and excluded from the schema. All remaining parameters are added to the JSON schema as `required`.

**Rationale**: The framework was designed for C# where `Nullable<T>` signals optionality. F#'s `?param: Type` compiles to `FSharpOption<T>`, which the framework does not recognize as optional.

**Alternatives considered**:
- Framework patching — rejected (not maintainable, violates spec assumption)
- Custom JSON schema endpoint — rejected (framework doesn't expose this extension point)

## R2: Why do the 17 tools fail and how should they be fixed?

**Decision**: Two distinct failure categories require different fixes:

1. **Tools with F# optional params (`?param`)** — 10 tools (add_body, add_constraint, register_shape, set_body_pose, raycast, sweep_cast, overlap, start_recording, start_stress_test, generate_row). The framework marks `FSharpOption<T>` params as required. Fix: change `?param: float` to `param: Nullable<float>` (value types) or `param: string` with null default (reference types), which the framework recognizes as optional.

2. **Tools with required params that should be optional** — 5 tools (query_snapshots, query_events, query_body_trajectory, query_mesh_fetches, generate_random_bodies). These have logically-optional params (session_id defaults to active, page_size defaults to 100, cursor defaults to empty) declared as required. Fix: change signatures to use `Nullable<T>` with `defaultArg` for defaults.

3. **set_collision_filter** — Has all required params but test fails because the referenced body may not exist after simulation restart. Fix: ensure test creates a body first, or handle the tool error gracefully in the regression test.

**Rationale**: Using `Nullable<T>` instead of F# `Option<T>` is the least-invasive change that works with the MCP framework's schema generation. The framework already recognizes `Nullable<T>` as optional in C# tools.

**Alternatives considered**:
- Wrapper DTO classes (single params object per tool) — rejected (massive signature rewrite, breaks existing callers, .fsi files need full rewrite)
- Custom `ISchemaProvider` — rejected (not a supported extension point in ModelContextProtocol.AspNetCore 1.1.x)
- Keep `?param` + manually annotate schema — rejected (no annotation mechanism available)

## R3: What is the correct Aspire MCP stdio configuration for Claude Code?

**Decision**: Add stdio transport entry to `.mcp.json` using `aspire agent mcp` CLI command:
```json
{
  "aspire-dashboard": {
    "type": "stdio",
    "command": "aspire",
    "args": ["agent", "mcp", "--nologo", "--non-interactive"]
  }
}
```

**Rationale**: The Aspire Dashboard MCP endpoint at port 18093 returns 403 Forbidden for HTTP/SSE (documented in NetworkProblems.md). The Aspire CLI 13.2.0 (confirmed installed via `dotnet tool list -g`) provides stdio transport that bypasses the auth requirement. This provides 14 tools including list_resources, list_console_logs, doctor, list_docs, search_docs.

**Alternatives considered**:
- HTTP/SSE direct connection — rejected (403 Forbidden, documented blocker)
- Disabling dashboard auth — rejected (requires modifying Aspire internals, not portable)

## R4: How should the MCP regression test be structured?

**Decision**: C# integration test using Aspire's `DistributedApplicationTestingBuilder` pattern, consistent with existing integration tests. The test will:
1. Start AppHost (server + simulation + MCP)
2. Wait for MCP resource healthy
3. Connect to MCP via HTTP/SSE (reusing the pattern from `mcp_test_runner.py` but in C#)
4. Call each of the 59 tools with minimal relevant parameters
5. Assert no RPC_ERROR responses (TOOL_ERROR is acceptable for tools that fail due to missing state)

**Rationale**: C# integration tests already use `DistributedApplicationTestingBuilder` and have established patterns for AppHost lifecycle, gRPC channels, and HTTPS dev cert handling. Adding MCP tests in the same project keeps the test infrastructure unified.

**Alternatives considered**:
- Python test script in CI — rejected (adds Python dependency to build pipeline)
- F# unit tests with mocked MCP — rejected (doesn't test actual schema generation)
- Separate test project — rejected (unnecessary when existing integration test project suffices)

## R5: What `.fsi` signature changes are needed?

**Decision**: Every tool module `.fsi` file must be updated to match the new parameter types. Changing `?radius: float` to `radius: Nullable<float>` changes the public API surface. Corresponding `.fsi` files and surface-area baselines must be updated.

**Rationale**: Constitution Principle V requires `.fsi` files for all public modules. Surface-area baselines must be updated to reflect the new parameter types.

**Affected files**:
- `SimulationTools.fsi` (8 tools)
- `GeneratorTools.fsi` (2 tools)
- `RecordingTools.fsi` (1 tool)
- `RecordingQueryTools.fsi` (4 tools)
- `MeshFetchQueryTools.fsi` (1 tool)
- `StressTestTools.fsi` (1 tool)

## R6: Tool description improvement strategy

**Decision**: Improve `[<Description>]` attributes on parameters to clarify:
- Which params are shape-specific (e.g., "Required when shape='sphere'. Ignored for other shapes.")
- What defaults are used when params are omitted (e.g., "Default: 100. Max: 500.")
- Parameter grouping hints (e.g., "Sphere parameters: radius. Box parameters: half_extents_x/y/z.")
- Tool-level descriptions should summarize the parameter groups available.

**Rationale**: AI assistants use tool descriptions to decide which parameters to include. Clear descriptions reduce the chance of sending irrelevant params and improve tool call accuracy.
