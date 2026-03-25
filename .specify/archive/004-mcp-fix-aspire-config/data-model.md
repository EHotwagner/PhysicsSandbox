# Data Model: MCP Tool Schema Fix & Aspire MCP Configuration

**Branch**: `004-mcp-fix-aspire-config` | **Date**: 2026-03-25

## Entities

This feature does not introduce new persistent entities. The changes affect in-memory schema generation and configuration files.

### MCP Tool Parameter (modified)

Represents a parameter in an MCP tool's JSON schema.

| Attribute | Current State | Target State |
|-----------|--------------|--------------|
| Name | Tool param name | Unchanged |
| Type | F# type (`float`, `int`, `string`, `bool`) | Unchanged |
| Required | Always `true` (framework bug) | `true` for mandatory params, `false` for optional |
| Description | Basic description | Enhanced with defaults, applicability, grouping |
| Default | Not generated | Generated for optional params with known defaults |

### Tool Parameter Categories

| Category | Example Params | Current | Target |
|----------|---------------|---------|--------|
| Always required | `shape`, `body_a`, `count` | Required | Required |
| Shape-conditional | `radius`, `half_extents_x` | Required (wrong) | Optional |
| Mode-conditional | `offset_ax`, `axis_x` | Required (wrong) | Optional |
| Has sensible default | `page_size`, `seed`, `cursor` | Required (wrong) | Optional with default |
| Velocity/optional modifier | `vx`, `vy`, `vz` | Required (wrong) | Optional |

### MCP Configuration Entry (new)

Added to `.mcp.json` alongside existing PhysicsSandbox MCP entry.

| Field | Value |
|-------|-------|
| Server Name | `aspire-dashboard` |
| Transport | stdio |
| Command | `aspire` |
| Args | `["agent", "mcp", "--nologo", "--non-interactive"]` |

## State Transitions

None — no persistent state changes. Tool schemas are generated at MCP server startup and remain fixed for the server's lifetime.

## Validation Rules

- A parameter marked optional in the schema MUST accept both: (a) omission from the payload, and (b) explicit `null` value.
- A parameter marked required MUST reject payloads that omit it, returning a clear validation error.
- Default values documented in descriptions MUST match the actual defaults applied in tool implementation code.
