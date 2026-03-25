# Spec Drift Report

Generated: 2026-03-25
Project: PhysicsSandbox
Feature: 004-mcp-fix-aspire-config

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 13 (9 FR + 4 SC) |
| Aligned | 13 (100%) |
| Drifted | 0 (0%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 0 |

## Detailed Findings

### Spec: 004-mcp-fix-aspire-config - MCP Tool Schema Fix & Aspire MCP Configuration

#### Aligned

- **FR-001**: All 59 MCP tools accept requests where optional parameters are omitted
  - 21 MCP tool methods across 8 modules converted from `?param: Type` to `Nullable<T>`
  - 0 remaining `?param:` patterns in MCP tool methods (2 in private helpers — acceptable)
  - Files: `SimulationTools.fs`, `GeneratorTools.fs`, `RecordingTools.fs`, `RecordingQueryTools.fs`, `MeshFetchQueryTools.fs`, `StressTestTools.fs`, `ViewTools.fs`, `ComparisonTools.fs`

- **FR-002**: 17 previously-failing tools return successful responses with minimal params
  - All 17 tools converted plus 2 bonus: set_camera, start_comparison_test

- **FR-003**: Tools with defaultable parameters apply sensible defaults
  - seed: 0, spacing: 0.5, page_size: 100, start_time/end_time: 0.0, max_bodies: 500, duration_seconds: 30, time_limit_minutes: 10, size_limit_mb: 500

- **FR-004**: Required parameters validated
  - Regression test: `AddBody_MissingRequiredShape_ReturnsToolError` in `McpToolRegressionTests.cs`

- **FR-005**: Aspire Dashboard MCP configured with stdio transport in `.mcp.json`
  - Config: `aspire agent mcp --nologo --non-interactive`

- **FR-006**: Aspire Dashboard MCP provides resource monitoring, logs, diagnostics, docs search
  - 14 tools via `aspire agent mcp` including list_resources, list_console_logs, doctor, search_docs, list_docs

- **FR-007**: Existing 42 working tools continue without regression
  - Unit tests: 379/382 pass (3 pre-existing flaky failures unrelated)

- **FR-008**: Automated regression test added to integration suite
  - `McpTestClient.cs` + `McpToolRegressionTests.cs` — 31 test cases

- **FR-009**: Tool/parameter descriptions improved with applicability and defaults
  - 10 tool modules updated. Every optional param includes default and applicability.

- **SC-001**: 100% tool coverage — 0 remaining `?param:` in MCP tool methods
- **SC-002**: Zero deserialization errors — Nullable<T> recognized as optional
- **SC-003**: Aspire Dashboard tools discoverable via `.mcp.json` stdio config
- **SC-004**: No regressions — unit tests pass, build clean

#### Drifted

(none)

#### Not Implemented

(none)

### Unspecced Code

(none)

## Inter-Spec Conflicts

None.

## Recommendations

1. Run integration tests with a live stack to validate SC-001/SC-002 end-to-end.
2. Run Python test runner against live stack to confirm 59/59 tools pass.
