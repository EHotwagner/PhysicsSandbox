# Feature Specification: Add MCP Server to Aspire AppHost Orchestration

**Feature Branch**: `006-mcp-aspire-orchestration`
**Created**: 2026-03-20
**Status**: Draft
**Input**: User description: "add the mcp server to the aspire apphost orchestration"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - MCP Server Starts with Aspire (Priority: P1)

As a developer using the PhysicsSandbox, I want the MCP server to start automatically as part of the Aspire orchestration so that I don't need to manually launch it in a separate terminal. When I run `dotnet run --project src/PhysicsSandbox.AppHost`, all services — including the MCP server — start together and are visible in the Aspire dashboard.

**Why this priority**: This is the core ask. Without this, the MCP server remains a standalone process that must be manually started and has no lifecycle management or observability through Aspire.

**Independent Test**: Can be fully tested by starting the AppHost and verifying the MCP server appears as a resource in the Aspire dashboard with a "Running" state.

**Acceptance Scenarios**:

1. **Given** the AppHost is started, **When** all services have initialized, **Then** the MCP server appears as a running resource in the Aspire dashboard alongside the existing server, simulation, viewer, and client resources.
2. **Given** the AppHost is stopped (Ctrl+C), **When** shutdown completes, **Then** the MCP server process is also terminated gracefully.
3. **Given** the PhysicsServer has not yet started, **When** the MCP server is scheduled to start, **Then** it waits for the PhysicsServer to be ready before starting (since it depends on gRPC connectivity to the server).

---

### User Story 2 - MCP Server Connects to PhysicsServer via Service Discovery (Priority: P1)

As a developer, I want the MCP server to automatically discover and connect to the PhysicsServer's address through Aspire service references rather than hardcoding `https://localhost:7180`. This ensures the MCP server connects to the correct PhysicsServer instance regardless of which port Aspire assigns.

**Why this priority**: Without service discovery, the MCP server would fail to connect when Aspire assigns dynamic ports to the PhysicsServer. This is equally critical to Story 1 — starting the MCP server is useless if it can't find the PhysicsServer.

**Independent Test**: Can be tested by starting the AppHost and verifying the MCP server successfully establishes a gRPC connection to the PhysicsServer without any manually specified address.

**Acceptance Scenarios**:

1. **Given** the AppHost is running with dynamic port assignment, **When** the MCP server starts, **Then** it resolves the PhysicsServer address through Aspire service discovery and connects successfully.
2. **Given** the PhysicsServer restarts during an active session, **When** the MCP server detects the disconnection, **Then** it reconnects using the service reference (existing reconnection logic applies).

---

### User Story 3 - MCP Server Logs Visible in Aspire Dashboard (Priority: P2)

As a developer debugging physics simulation issues through an AI assistant, I want the MCP server's logs to appear in the Aspire dashboard's structured logging view so I can correlate MCP tool invocations with server and simulation activity.

**Why this priority**: Observability is a key benefit of Aspire orchestration. Without log integration, the MCP server is orchestrated but opaque — reducing the value of adding it to Aspire.

**Independent Test**: Can be tested by invoking an MCP tool (e.g., list-bodies) and verifying the corresponding log entries appear in the Aspire dashboard's structured logs view for the MCP resource.

**Acceptance Scenarios**:

1. **Given** the MCP server is running under Aspire, **When** an MCP tool is invoked, **Then** the tool execution logs appear in the Aspire dashboard under the MCP server resource.
2. **Given** the MCP server encounters a connection error, **When** the error is logged, **Then** it appears in the Aspire dashboard with appropriate severity level.

---

### Edge Cases

- What happens when the MCP server crashes? Aspire should report it as failed in the dashboard, consistent with how other project resources behave.
- What happens when the MCP server is started but no AI assistant connects to its stdio transport? It should idle gracefully without errors, same as the client resource.
- What happens in headless/CI builds? The MCP server should build successfully with `StrideCompilerSkipBuild=true` (it has no Stride dependency, so this should be a non-issue).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The AppHost MUST register the MCP server as a project resource in the Aspire orchestration.
- **FR-002**: The MCP server resource MUST have a service reference to the PhysicsServer so it can resolve the server's address at runtime.
- **FR-003**: The MCP server MUST wait for the PhysicsServer to be ready before starting.
- **FR-004**: The MCP server MUST use the service-discovered PhysicsServer address instead of a hardcoded default.
- **FR-005**: The MCP server MUST appear in the Aspire dashboard with its resource name, state, and logs.
- **FR-006**: The MCP server MUST shut down gracefully when the AppHost is stopped.
- **FR-007**: Existing MCP server functionality (15 tools, stdio transport, gRPC connection management) MUST remain unchanged.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Starting the AppHost results in all 5 project resources (server, simulation, viewer, client, mcp) appearing in the Aspire dashboard within the normal startup time.
- **SC-002**: The MCP server successfully connects to the PhysicsServer without any manually specified address when launched through Aspire.
- **SC-003**: All existing MCP integration tests continue to pass after the orchestration change.
- **SC-004**: The MCP server's structured logs are visible in the Aspire dashboard during tool invocations.
