# Feature Specification: MCP Tool Schema Fix & Aspire MCP Configuration

**Feature Branch**: `004-mcp-fix-aspire-config`
**Created**: 2026-03-25
**Status**: Completed
**Input**: User description: "Fix 17 MCP tool deserialization failures — the tools work but the auto-generated schemas mark nullable params as required. A targeted fix in the F# tool signatures would bring MCP coverage from 75% to ~100%. Configure Aspire MCP in Claude Code — add the aspire agent mcp stdio config so you get resource monitoring, logs, and docs search in your AI workflow."

## Clarifications

### Session 2026-03-25

- Q: Should SC-001 (100% tool coverage) be validated by an automated regression test in the integration test suite, or by one-time manual verification? → A: Add automated MCP tool regression test to the integration test suite.
- Q: Should the Aspire Dashboard MCP config be in shared `.mcp.json` or user-local settings? → A: Add to `.mcp.json` (shared, committed to repo alongside existing PhysicsSandbox MCP config).
- Q: Should improving tool/parameter descriptions be in scope alongside the schema fix? → A: Yes, in scope — improve tool/parameter descriptions alongside the schema fix.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - AI Assistant Calls Any MCP Tool Successfully (Priority: P1)

An AI assistant connected to the PhysicsSandbox MCP server invokes any of the 59 available tools with only the parameters relevant to the current operation. For example, when creating a sphere body, the assistant sends shape, radius, position, and mass — without including box, capsule, cylinder, or triangle parameters. The tool executes successfully and returns the expected result.

**Why this priority**: This is the core problem — 17 tools (29%) currently fail when clients omit irrelevant optional parameters. Fixing this removes the single largest barrier to full MCP tool coverage.

**Independent Test**: Can be fully tested by connecting an MCP client and calling each of the 17 currently-failing tools with minimal relevant parameters. Success means all 59 tools respond without deserialization errors.

**Acceptance Scenarios**:

1. **Given** an MCP client connected to the PhysicsSandbox MCP server, **When** the client calls a body creation tool with only sphere-relevant parameters (shape, radius, position, mass), **Then** the tool creates the body and returns a success response without error.
2. **Given** an MCP client connected to the server, **When** the client calls a constraint creation tool with only hinge-relevant parameters (omitting other constraint-type-specific parameters), **Then** the tool creates the constraint and returns a success response.
3. **Given** an MCP client connected to the server, **When** the client calls a query tool with only the required session identifier (omitting optional pagination and time-range parameters), **Then** the tool returns data using default values for omitted parameters.
4. **Given** an MCP client connected to the server, **When** the client calls a recording tool without providing an optional label, **Then** the tool starts recording and returns a session identifier.

---

### User Story 2 - Developer Uses Aspire Dashboard Tools in AI Workflow (Priority: P2)

A developer working in Claude Code has access to Aspire Dashboard tools (resource listing, console logs, diagnostics, documentation search) alongside the existing PhysicsSandbox tools. This enables the developer to monitor running services, search Aspire documentation, and diagnose issues without leaving the AI assistant workflow.

**Why this priority**: This adds significant developer productivity by bringing infrastructure observability into the AI workflow. It depends on the Aspire stack running but requires only configuration — no code changes.

**Independent Test**: Can be fully tested by starting the Aspire stack, launching Claude Code, and verifying that Aspire Dashboard tools appear in the tool list and return valid results.

**Acceptance Scenarios**:

1. **Given** the Aspire stack is running and Claude Code is launched, **When** the developer lists available tools, **Then** Aspire Dashboard tools (resource listing, console logs, diagnostics, documentation search) appear alongside PhysicsSandbox tools.
2. **Given** the Aspire stack is running with multiple services, **When** the developer asks to list running resources, **Then** the tool returns the current set of orchestrated services and their status.
3. **Given** the Aspire stack is running, **When** the developer searches Aspire documentation for a topic, **Then** the tool returns relevant documentation results.

---

### Edge Cases

- What happens when an MCP client sends `null` explicitly for an optional parameter vs. omitting the parameter entirely? Both must work identically.
- What happens when a required parameter (e.g., shape type for body creation) is omitted? The tool must return a clear validation error, not a deserialization crash.
- What happens when the Aspire stack is not running and Claude Code attempts to use Aspire Dashboard tools? The tools should fail gracefully with a connection error.
- What happens when all optional parameters are omitted from a tool that has many? The tool must still execute using defaults for every omitted parameter.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: All 59 MCP tools MUST accept requests where optional parameters are omitted from the payload (not just set to null).
- **FR-002**: The 17 currently-failing tools MUST return successful responses when called with only the parameters relevant to the specific operation (e.g., only sphere parameters for sphere creation).
- **FR-003**: Tools with defaultable parameters (e.g., random seed, page size, spacing) MUST apply sensible defaults when those parameters are omitted.
- **FR-004**: Required parameters MUST still be validated — omitting a truly required parameter MUST produce a clear error message, not a deserialization crash.
- **FR-005**: The Aspire Dashboard MCP server MUST be configured for Claude Code access using stdio transport (via the Aspire CLI) in the shared project MCP configuration file (`.mcp.json`).
- **FR-006**: The Aspire Dashboard MCP configuration MUST provide access to resource monitoring, console log viewing, diagnostics, and documentation search capabilities.
- **FR-007**: Existing MCP tools that currently work (42 tools) MUST continue to work without regression after the schema fix.
- **FR-008**: An automated regression test MUST be added to the integration test suite that validates all 59 MCP tools accept requests with only relevant parameters provided, ensuring SC-001 and SC-004 are continuously enforced.
- **FR-009**: Tool and parameter descriptions MUST be improved so that every optional parameter's description includes (a) when it applies (e.g., "Required when shape='sphere'") and (b) its default value when omitted (e.g., "Default: 100"). Tool-level descriptions MUST summarize available parameter groups for multi-variant tools.

### Key Entities

- **MCP Tool Schema**: The auto-generated description of each tool's parameters, types, and required/optional status that MCP clients use to construct tool calls.
- **Tool Parameter**: An input to an MCP tool that may be required (must always be provided) or optional (can be omitted, with a default applied).
- **Aspire Dashboard MCP**: The built-in MCP capability in .NET Aspire that exposes infrastructure management tools via stdio transport.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of MCP tools (59/59) accept requests with only relevant parameters provided — up from the current 75% (42/59).
- **SC-002**: Zero deserialization errors when an MCP client omits optional parameters from any tool call.
- **SC-003**: Aspire Dashboard tools are discoverable and callable from within the Claude Code AI workflow when the Aspire stack is running.
- **SC-004**: All 42 previously-working tools continue to function identically after the fix (no regressions).

## Assumptions

- The fix targets tool signatures and schema annotations rather than modifying the MCP framework library itself.
- The Aspire Dashboard MCP stdio transport is the correct approach, based on the confirmed finding that HTTP/SSE access returns 403 Forbidden.
- The Aspire CLI is available in the development environment for stdio-based MCP connections.
- Sending `null` explicitly for an optional parameter and omitting the parameter entirely are treated equivalently by the fixed tools.
