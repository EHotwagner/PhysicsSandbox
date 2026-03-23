# Feature Specification: MCP Data Logging for Analysis

**Feature Branch**: `005-mcp-data-logging`
**Created**: 2026-03-23
**Status**: Completed
**Input**: User description: "improve the mcp tools. they are very slow in this context and cant really act on physics data received in realtime. lets log all received data for lets say an hour, so the mcp tools can use that to analyze. are there better ways to limit storage than time, maybe storage size?"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Continuous Physics Data Recording (Priority: P1)

An AI assistant user interacting with the physics sandbox through MCP tools wants to capture all incoming physics simulation data (body states, events, metrics) into a persistent log so they can later query and analyze what happened during a session without needing real-time responsiveness.

**Why this priority**: This is the core capability that addresses the fundamental limitation — MCP tools cannot process data in real-time, so recording it for deferred analysis is the primary value proposition.

**Independent Test**: Can be fully tested by starting a recording session, running a simulation for a period, stopping it, and verifying that all simulation states and events were captured in the log with accurate timestamps.

**Acceptance Scenarios**:

1. **Given** the MCP server connects to a running simulation, **When** the connection is established, **Then** recording begins automatically and all incoming simulation state updates and command events are written to persistent storage with timestamps.
2. **Given** a recording session is active, **When** the simulation produces rapid state updates (e.g., 60 updates/second), **Then** all updates are captured without dropping data or degrading MCP tool responsiveness.
3. **Given** a recording session is active, **When** the user stops recording, **Then** the log is finalized and available for querying immediately.

---

### User Story 2 - Dual-Limit Storage Management (Priority: P1)

A user wants recording sessions to automatically manage storage using both a time-based retention window and a size-based cap, so that logs stay bounded regardless of simulation complexity. Whichever limit is reached first triggers pruning.

**Why this priority**: Storage management is essential for the recording feature to be usable in practice — without it, users risk filling disk space during long sessions. Dual limits provide predictable behavior: the time limit (default 10 minutes) keeps the window focused, while the size limit (default 500 MB) acts as a safety cap for high-body-count simulations that produce large frames.

**Independent Test**: Can be fully tested by configuring time and size limits, running a recording that exceeds each limit independently, and verifying that the oldest data is pruned while the most recent data is preserved, and both constraints are respected.

**Acceptance Scenarios**:

1. **Given** a recording session with default settings, **When** recorded data spans more than 10 minutes, **Then** the oldest data beyond the 10-minute window is automatically pruned.
2. **Given** a recording session with a configured maximum storage size (e.g., 1 GB), **When** the log reaches the size limit before the time limit, **Then** the oldest recorded data is pruned to stay within the size cap.
3. **Given** a recording session with default settings, **When** no explicit limits are configured, **Then** the system uses defaults of 10 minutes and 500 MB.
4. **Given** a recording session is active, **When** a user queries the current storage usage, **Then** the system reports the current log size, configured size limit, configured time limit, and actual time window covered by the stored data.

---

### User Story 3 - Querying Recorded Data via MCP Tools (Priority: P1)

An AI assistant user wants to query the recorded physics data through new MCP tools to analyze simulation behavior — such as retrieving body trajectories over time, finding events of interest, and computing aggregate statistics — without needing to process live data in real-time.

**Why this priority**: This completes the value loop — recording data is only useful if it can be queried and analyzed. These query tools are what make the recorded data actionable for AI assistants.

**Independent Test**: Can be fully tested by recording a known simulation scenario, then using query tools to retrieve specific data slices, body histories, and event sequences, and verifying the results match what was recorded.

**Acceptance Scenarios**:

1. **Given** recorded data from a simulation session, **When** a user requests the trajectory of a specific body over a time range, **Then** the system returns a series of timestamped position/velocity snapshots for that body.
2. **Given** recorded data, **When** a user requests a summary of recorded data (time range, body count, event count, storage used), **Then** the system returns an accurate overview without reading the entire log.
3. **Given** recorded data, **When** a user requests all state snapshots within a time window, **Then** the system returns the matching snapshots ordered by timestamp.
4. **Given** recorded data, **When** a user requests all command events of a specific type (e.g., "ApplyForce"), **Then** the system returns the matching events with their timestamps.

---

### User Story 4 - Session Management (Priority: P2)

A user wants to manage multiple recording sessions — starting, stopping, listing, and deleting sessions — so they can organize their analysis work and clean up old data.

**Why this priority**: While not strictly required for the core recording+query flow, session management makes the feature practical for ongoing use across multiple simulation experiments.

**Independent Test**: Can be fully tested by creating multiple recording sessions, listing them, switching between them for queries, and deleting old ones.

**Acceptance Scenarios**:

1. **Given** the MCP server is running, **When** a user starts a new recording session, **Then** the session is assigned an identifier and a descriptive label.
2. **Given** multiple completed recording sessions exist, **When** a user lists sessions, **Then** all sessions are displayed with their identifier, label, time range, size, and status.
3. **Given** a completed recording session, **When** a user deletes it, **Then** all associated storage is freed and the session no longer appears in the session list.
4. **Given** a recording session is active, **When** the MCP server restarts, **Then** the previously active session's data up to the restart point is preserved and queryable.

---

### Edge Cases

- What happens when the disk is completely full during an active recording? The system should gracefully stop recording and notify the user rather than crashing.
- What happens when querying a time range that has been partially pruned due to time or size limits? The system should return available data and indicate that earlier data was pruned.
- What happens when two recording sessions are requested simultaneously? Only one recording session should be active at a time; starting a new one should stop the current one (with a warning).
- What happens when the simulation disconnects during a recording? The recording should pause and resume automatically when the simulation reconnects, with a gap noted in the log metadata.

## Clarifications

### Session 2026-03-23

- Q: Should the system record every state update or allow configurable sampling? → A: Record every state update (full fidelity). Users accept shorter retention windows in exchange for complete data — no sampling or downsampling.
- Q: How should large query results be handled? → A: Paginated results with configurable page size (default 100 entries) and cursor-based navigation.
- Q: Should recording start automatically or require manual activation? → A: Auto-start recording when MCP server connects to a simulation. Users can stop manually but data capture begins immediately so nothing is missed.
- Q: At what granularity should pruning operate when the storage limit is reached? → A: Time-based chunks — drop data in fixed-duration blocks (e.g., 1 minute at a time) for a balance of pruning efficiency and granularity.
- Q: Should recording have a time-based retention limit in addition to size? → A: Yes — default 10-minute rolling window, reconfigurable. Pruning triggers on whichever limit (time or size) is reached first.
- Q: What should the default storage size limit be? → A: 500 MB (changed from 256 MB).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST capture every simulation state update received from the physics server into a persistent, on-disk log during an active recording session at full fidelity (no sampling or downsampling).
- **FR-002**: System MUST capture all command events (simulation commands and view commands) into the recording log alongside state updates.
- **FR-003**: System MUST timestamp each recorded entry with high-resolution timing suitable for correlating events within a simulation frame.
- **FR-004**: System MUST enforce dual retention limits per recording session: a configurable time window (default 10 minutes) and a configurable maximum storage size (default 500 MB). Both limits are reconfigurable.
- **FR-005**: System MUST automatically prune the oldest recorded data in time-based chunks (e.g., 1 minute of data per prune cycle) when either the time limit or size limit is reached first, maintaining a rolling window of the most recent data.
- **FR-006**: System MUST provide MCP tools to start, stop, list, and delete recording sessions.
- **FR-007**: System MUST provide an MCP tool to query the trajectory (position, velocity over time) of a specific body from recorded data.
- **FR-008**: System MUST provide an MCP tool to query state snapshots within a specified time range from recorded data.
- **FR-009**: System MUST provide an MCP tool to query command events by type and/or time range from recorded data.
- **FR-010**: System MUST provide an MCP tool to retrieve a summary of a recording session (time range covered, body count, event count, storage used).
- **FR-011**: System MUST NOT degrade the responsiveness of existing MCP tools while recording is active — recording should happen asynchronously.
- **FR-012**: System MUST persist recording data in a format that survives MCP server restarts without corruption.
- **FR-013**: System MUST allow only one active recording session at a time.
- **FR-014**: System MUST report current storage usage and limit when queried.
- **FR-015**: All query tools returning multiple results (snapshots, events, trajectories) MUST support pagination with a configurable page size (default 100 entries) and cursor-based navigation for retrieving subsequent pages.
- **FR-016**: System MUST automatically begin recording when the MCP server establishes a connection to the physics simulation, without requiring an explicit user command.

### Key Entities

- **Recording Session**: A named, time-bounded capture of simulation data. Has an identifier, label, start/end time, storage size, and status (Recording, Completed, Failed).
- **State Snapshot**: A timestamped capture of the full simulation state (all body positions, velocities, properties) at a single point in time.
- **Event Entry**: A timestamped capture of a command event (simulation command or view command) that occurred during the session.
- **Storage Budget**: Dual retention constraints for a recording session — a maximum time window (default 10 minutes) and a maximum disk size (default 500 MB). Whichever limit is reached first triggers pruning.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can run a simulation with default settings (10-minute window) and query any 5-second window of recorded data within 2 seconds. With reconfigured limits, the system supports extended recording windows.
- **SC-002**: Recording does not increase MCP tool response times by more than 10% compared to baseline (no recording active).
- **SC-003**: Storage usage stays within 5% of the configured limit during active recording with pruning.
- **SC-004**: Users can retrieve a body's complete trajectory over a 10-minute window in a single query returning results within 3 seconds.
- **SC-005**: All recorded state snapshots and events are retrievable with timestamp accuracy sufficient to correlate events within the same simulation frame.
- **SC-006**: Recorded data survives an MCP server restart and is queryable after restart without data loss (except for the moment of restart itself).
