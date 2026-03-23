# MCP Tool Contract: query_mesh_fetches

## Tool: query_mesh_fetches

**Purpose**: Retrieve mesh fetch events from a recorded session.

### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| session_id | string | No | Active session | Recording session to query |
| minutes_ago | int | No | 5 | Time window (minutes from now) |
| mesh_id | string | No | None | Filter to events involving this mesh ID |
| page_size | int | No | 100 | Results per page (max 500) |
| cursor | string | No | None | Pagination cursor from previous result |

### Response Format

```text
Mesh Fetch Events (session: {id})
---
[{timestamp}] FetchMeshes: {n} requested, {hits} hits, {misses} misses
  Requested: {id1}, {id2}, ...
  Missed: {missed_id1}, ...

[{timestamp}] FetchMeshes: ...

---
Page {n}, showing {count} events
Next cursor: {cursor} (if more pages)
```

### Error Cases

- Session not found: "Session '{id}' not found"
- No mesh fetch events in range: "No mesh fetch events found in the specified time range"
