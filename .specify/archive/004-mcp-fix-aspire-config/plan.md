# Implementation Plan: MCP Tool Schema Fix & Aspire MCP Configuration

**Branch**: `004-mcp-fix-aspire-config` | **Date**: 2026-03-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-mcp-fix-aspire-config/spec.md`

## Summary

Fix 17 MCP tool deserialization failures caused by ModelContextProtocol.AspNetCore marking all F# parameters as required in auto-generated JSON schemas. The fix converts F# optional parameters (`?param: Type`) to `Nullable<T>` which the framework recognizes as optional. Additionally, tools with logically-optional parameters currently declared as required will be converted to optional with sensible defaults. Tool descriptions will be enhanced for AI assistant clarity. An automated regression test covering all 59 tools will be added. Aspire Dashboard MCP will be configured in `.mcp.json` via stdio transport.

## Technical Context

**Language/Version**: F# on .NET 10.0 (MCP server, tool modules), C# on .NET 10.0 (integration tests)
**Primary Dependencies**: ModelContextProtocol.AspNetCore 1.1.* (MCP framework), Grpc.Net.Client 2.x, xUnit 2.x, Aspire.Hosting.Testing 10.x
**Storage**: N/A (no storage changes)
**Testing**: xUnit 2.x + Aspire DistributedApplicationTestingBuilder (integration), existing Python test runner (validation)
**Target Platform**: Linux container with GPU passthrough
**Project Type**: MCP server (web service) + configuration
**Performance Goals**: No performance changes — schema generation is startup-only
**Constraints**: Must not break existing 42 working tools; must maintain .fsi signature files and surface-area baselines
**Scale/Scope**: 59 MCP tools across 6 tool modules, 1 configuration file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | Changes confined to MCP server; no cross-service coupling |
| II. Contract-First | PASS | MCP tool schemas are the contract; this fix corrects the schema to match intended behavior |
| III. Shared Nothing | PASS | No new cross-service dependencies |
| IV. Spec-First Delivery | PASS | Spec and plan completed before implementation |
| V. Compiler-Enforced Structural Contracts | PASS | All modified tool modules have .fsi files; surface-area baselines will be updated |
| VI. Test Evidence | PASS | Automated regression test added to integration suite (FR-008) |
| VII. Observability by Default | PASS | No observability changes needed — tool errors already surface in MCP responses |

**Post-Phase 1 Re-check**: All gates still pass. The `Nullable<T>` approach modifies parameter types in .fsi files, which will be caught by surface-area baseline tests and explicitly updated.

## Project Structure

### Documentation (this feature)

```text
specs/004-mcp-fix-aspire-config/
├── plan.md              # This file
├── research.md          # Phase 0 output — technical decisions
├── data-model.md        # Phase 1 output — parameter model changes
├── quickstart.md        # Phase 1 output — build/test/verify guide
├── contracts/           # Phase 1 output — MCP schema contract
│   └── mcp-tool-schema.md
├── checklists/
│   └── requirements.md  # Specification quality checklist
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/PhysicsSandbox.Mcp/
├── SimulationTools.fs       # 8 tools: ?param → Nullable<T>, improved descriptions
├── SimulationTools.fsi      # Updated signatures
├── GeneratorTools.fs        # 2 tools: make seed/spacing optional
├── GeneratorTools.fsi       # Updated signatures
├── RecordingTools.fs        # 1 tool: convert optional params
├── RecordingTools.fsi       # Updated signatures
├── RecordingQueryTools.fs   # 4 tools: make pagination/time params optional
├── RecordingQueryTools.fsi  # Updated signatures
├── MeshFetchQueryTools.fs   # 1 tool: make filter/pagination optional
├── MeshFetchQueryTools.fsi  # Updated signatures
├── StressTestTools.fs       # 1 tool: convert optional params
└── StressTestTools.fsi      # Updated signatures

tests/PhysicsSandbox.Integration.Tests/
└── McpToolRegressionTests.cs  # NEW: 59-tool regression test

.mcp.json                      # Add aspire-dashboard stdio entry
```

**Structure Decision**: No new projects. Changes are confined to the existing PhysicsSandbox.Mcp project (tool signatures + descriptions) and the existing integration test project (new test class). Configuration change to `.mcp.json` at project root.

## Implementation Approach

### Phase 1: Fix Tool Signatures (FR-001, FR-002, FR-003, FR-004)

For each of the 17 failing tools across 6 modules:

1. **Convert F# optional params to Nullable<T>**:
   - `?radius: float` → `radius: Nullable<float>` (value types)
   - `?label: string` → `label: string` with null check (reference types are already nullable in .NET)
   - Inside the method body, replace `defaultArg radius 1.0` with `if radius.HasValue then radius.Value else 1.0`

2. **Convert logically-optional required params** (query tools, generators):
   - `session_id: string` → `session_id: string` (keep as-is, empty string = active session)
   - `page_size: int` → `page_size: Nullable<int>` with default 100
   - `cursor: string` → `cursor: string` (keep, empty = first page)
   - `seed: int` → `seed: Nullable<int>` with default 0

3. **Update .fsi signature files** to match new parameter types

4. **Update surface-area baseline tests** to reflect new signatures

### Phase 2: Improve Tool Descriptions (FR-009)

For all 59 tools (not just the 17 failing):

1. **Tool-level descriptions**: Add parameter group summaries (e.g., "Sphere params: radius. Box params: half_extents_x/y/z.")
2. **Parameter-level descriptions**: Add applicability ("Required when shape='sphere'"), defaults ("Default: 100"), and constraints ("Max: 500")
3. **Update .fsi files** if description attributes appear in signatures

### Phase 3: Automated Regression Test (FR-008)

Add `McpToolRegressionTests.cs` to integration test project:

1. Start AppHost with MCP service
2. Wait for MCP resource healthy
3. Connect to MCP via HTTP (SSE endpoint at allocated port)
4. For each of 59 tools: call with minimal relevant params, assert no RPC error
5. Validate that tools with optional params work when params are omitted
6. Validate that tools with required params fail gracefully when params are missing

### Phase 4: Aspire Dashboard MCP Configuration (FR-005, FR-006)

1. Add `aspire-dashboard` entry to `.mcp.json` with stdio transport
2. Verify tools appear in Claude Code when stack is running
3. Document in quickstart.md

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `Nullable<T>` not recognized by MCP framework as optional | Low | High | Test with single tool first; fallback to wrapper DTOs if needed |
| Existing 42 tools regress | Low | High | Regression test runs all 59 tools; existing unit tests cover logic |
| .fsi signature changes break consumers | Low | Medium | Surface-area baseline tests catch unintended changes |
| Aspire CLI not available in all environments | Medium | Low | Config is optional; PhysicsSandbox MCP works independently |
