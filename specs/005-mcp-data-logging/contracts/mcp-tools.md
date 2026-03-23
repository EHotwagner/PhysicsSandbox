# MCP Tool Contracts: Recording & Query Tools

**Branch**: `005-mcp-data-logging` | **Date**: 2026-03-23

These tools are exposed via the MCP server's tool registry (`[<McpServerTool>]` attribute pattern).

## Session Management Tools

### start_recording

Start a new recording session (or restart recording if already active).

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| label | string | No | Auto-generated (timestamp) | Descriptive label for the session |
| time_limit_minutes | int | No | 10 | Rolling time window in minutes |
| size_limit_mb | int | No | 500 | Maximum storage in MB |

**Returns**: Session ID, label, configured limits. Warning if a previous active session was stopped.

### stop_recording

Stop the currently active recording session.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| (none) | | | | |

**Returns**: Session summary (ID, label, duration, size, snapshot count, event count). Error if no active session.

### list_sessions

List all recording sessions.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| (none) | | | | |

**Returns**: Table of sessions with: ID, label, status, time range, size, snapshot count, event count.

### delete_session

Delete a completed recording session and free its storage.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| session_id | string | Yes | | Session ID to delete |

**Returns**: Confirmation with freed storage size. Error if session is active (must stop first) or not found.

### recording_status

Get status of the current or most recent recording session.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| (none) | | | | |

**Returns**: Current session status, storage usage, configured limits, time window covered, whether pruning has occurred.

## Query Tools

### query_body_trajectory

Get the position and velocity history of a specific body over a time range.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| body_id | int | Yes | | Body ID to track |
| session_id | string | No | Current/latest session | Which session to query |
| start_time | double | No | Session start | Start of time range (simulation time) |
| end_time | double | No | Session end | End of time range (simulation time) |
| page_size | int | No | 100 | Results per page |
| cursor | string | No | (none) | Pagination cursor from previous query |

**Returns**: List of timestamped `{time, position {x,y,z}, velocity {x,y,z}, rotation {x,y,z,w}}` entries. Pagination cursor if more results available. Note if data was pruned in requested range.

### query_snapshots

Get full simulation state snapshots within a time window.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| session_id | string | No | Current/latest session | Which session to query |
| start_time | double | No | Session start | Start of time range (simulation time) |
| end_time | double | No | Session end | End of time range (simulation time) |
| page_size | int | No | 100 | Results per page |
| cursor | string | No | (none) | Pagination cursor |

**Returns**: List of timestamped state summaries (body count, simulation time, tick_ms). Full body details available per snapshot. Pagination cursor if more results.

### query_events

Get command events filtered by type and/or time range.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| session_id | string | No | Current/latest session | Which session to query |
| event_type | string | No | All types | Filter by command type (e.g., "AddBody", "ApplyForce") |
| start_time | double | No | Session start | Start of time range |
| end_time | double | No | Session end | End of time range |
| page_size | int | No | 100 | Results per page |
| cursor | string | No | (none) | Pagination cursor |

**Returns**: List of timestamped command events with human-readable descriptions. Pagination cursor if more results.

### query_summary

Get an overview of a recording session without reading the full log.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| session_id | string | No | Current/latest session | Which session to summarize |

**Returns**: Session label, status, time range, total duration, storage used, snapshot count, event count, unique body IDs seen, whether pruning has occurred, configured limits.
