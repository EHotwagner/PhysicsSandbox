# Contract: MCP Tool JSON Schema

**Type**: Auto-generated JSON Schema (MCP protocol)
**Producer**: PhysicsSandbox.Mcp server (ModelContextProtocol.AspNetCore)
**Consumer**: MCP clients (Claude Code, test runners, AI assistants)

## Schema Requirements

Each MCP tool exposes a JSON schema via the `tools/list` MCP method. The schema describes input parameters, their types, descriptions, and whether they are required.

### Required Parameter Contract

A parameter MUST be marked `"required": true` in the schema only if:
- It is always needed regardless of which operation variant is being performed
- Examples: `shape` (body creation), `body_a`/`body_b` (constraint creation), `count` (generators)

### Optional Parameter Contract

A parameter MUST be marked `"required": false` (or omitted from `required` array) if:
- It applies only to a specific variant (e.g., `radius` only for sphere shapes)
- It has a sensible default (e.g., `page_size` defaults to 100)
- It is a modifier that enhances but is not essential (e.g., `collision_mask`, `all_hits`)

### Description Contract

Parameter descriptions MUST include:
- When the parameter applies (e.g., "Required when shape='sphere'")
- Default value when omitted (e.g., "Default: 100")
- Value constraints (e.g., "Max: 500")

### Example: Correct `add_body` Schema

```json
{
  "name": "add_body",
  "inputSchema": {
    "type": "object",
    "properties": {
      "shape": {"type": "string", "description": "Body shape: sphere, box, capsule, cylinder, triangle, or plane"},
      "radius": {"type": "number", "description": "Sphere radius. Required when shape='sphere'."},
      "half_extents_x": {"type": "number", "description": "Box half-extent X. Required when shape='box'."},
      "x": {"type": "number", "description": "Position X. Default: 0."},
      "mass": {"type": "number", "description": "Body mass in kg. Default: 1. Use 0 for static bodies."}
    },
    "required": ["shape"]
  }
}
```

### Example: Correct `query_snapshots` Schema

```json
{
  "name": "query_snapshots",
  "inputSchema": {
    "type": "object",
    "properties": {
      "session_id": {"type": "string", "description": "Session ID. Empty string for active session."},
      "start_time": {"type": "number", "description": "Start time (Unix epoch seconds). Default: session start."},
      "end_time": {"type": "number", "description": "End time (Unix epoch seconds). Default: session end/now."},
      "page_size": {"type": "integer", "description": "Results per page. Default: 100. Max: 500."},
      "cursor": {"type": "string", "description": "Pagination cursor from previous query. Empty for first page."}
    },
    "required": []
  }
}
```
