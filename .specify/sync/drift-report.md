# Spec Drift Report

Generated: 2026-03-23
Project: PhysicsSandbox (005-mcp-data-logging)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 16 |
| ✓ Aligned | 15 (94%) |
| ⚠️ Drifted | 1 (6%) |
| ✗ Not Implemented | 0 (0%) |
| 🆕 Unspecced Code | 0 |

## Detailed Findings

### Spec: 005-mcp-data-logging - MCP Data Logging for Analysis

#### Aligned ✓

- **FR-001**: Capture every state update at full fidelity → `RecordingEngine.OnStateReceived` + `ChunkWriter.writeEntry` (protobuf binary, no sampling)
- **FR-002**: Capture all command events → `RecordingEngine.OnCommandReceived` + `GrpcConnection.OnCommandReceived` callback
- **FR-003**: High-resolution timestamps → Wire format `int64 timestampMs` (unix millis), assigned via `DateTimeOffset.UtcNow`
- **FR-004**: Dual retention limits (10 min / 500 MB), reconfigurable → `SessionStore.createSession` with validation clamping
- **FR-005**: Auto-prune in 1-minute chunks → `ChunkWriter.pruneOldChunks` with time-based then size-based pruning on rotation
- **FR-006**: MCP tools for start/stop/list/delete → `RecordingTools`: 4 tools + `recording_status`
- **FR-007**: Query body trajectory → `RecordingQueryTools.query_body_trajectory` with pagination
- **FR-008**: Query snapshots by time range → `RecordingQueryTools.query_snapshots` with pagination
- **FR-009**: Query events by type/time → `RecordingQueryTools.query_events` with type filtering
- **FR-010**: Session summary → `RecordingQueryTools.query_summary` (reads session.json only)
- **FR-011**: No MCP degradation (async) → Bounded `Channel<LogEntry>` with `DropOldest`, non-blocking `TryWrite`
- **FR-012**: Data survives restarts → On-disk persistence + restart recovery in `RecordingEngine.create()`
- **FR-013**: One active session → `RecordingEngine.Start` stops previous; `start_recording` warns
- **FR-014**: Report storage usage → `recording_status` tool + `query_summary`
- **FR-016**: Auto-start on connection → First state received triggers `Start()` with defaults

#### Drifted ⚠️

- **FR-015**: Spec says "configurable page size (default 100 entries)" but code defaults to 50 entries
  - Location: `src/PhysicsSandbox.Mcp/Recording/ChunkReader.fs`
  - Severity: minor

#### Not Implemented ✗

None.

#### Acceptance Scenarios

| Scenario | Status |
|----------|--------|
| US1-AS1: Auto-start recording on connection | PASS |
| US1-AS2: 60/sec capture without degradation | PASS (design: async channel) |
| US1-AS3: Stop finalizes and makes queryable | PASS |
| US2-AS1: Prune on 10-min boundary | PASS |
| US2-AS2: Prune on size limit | PASS |
| US2-AS3: Defaults 10 min / 500 MB | PASS |
| US2-AS4: Report storage usage | PASS |
| US3-AS1: Body trajectory query | PASS |
| US3-AS2: Session summary | PASS |
| US3-AS3: Snapshots by time window | PASS |
| US3-AS4: Events by type | PASS |
| US4-AS1: Session assigned ID and label | PASS |
| US4-AS2: List sessions with metadata | PASS |
| US4-AS3: Delete frees storage | PASS |
| US4-AS4: Restart recovery preserves data | PASS |

#### Success Criteria

| SC | Status | Notes |
|----|--------|-------|
| SC-001 | Design supports | Sequential scan architecture; not yet benchmarked |
| SC-002 | Not measured | No latency instrumentation; design is non-blocking |
| SC-003 | Design supports | Pruning bounded; 5% tolerance not explicitly asserted in tests |
| SC-004 | Design supports | Trajectory query with pagination; not yet benchmarked |
| SC-005 | Aligned | Millisecond-precision timestamps in wire format |
| SC-006 | Aligned | Restart recovery marks interrupted sessions as Completed |

#### Edge Cases

| Edge Case | Status | Evidence |
|-----------|--------|----------|
| Disk full | Partial | Consumer loop catches exceptions but doesn't set session to Failed |
| Pruned data gaps | Partial | Queries return available data; no explicit pruned-range indicator |
| Simultaneous sessions | PASS | Start stops previous with warning |
| Simulation disconnect | Partial | Recording pauses naturally; no gap metadata tracked |

### Unspecced Code 🆕

None — all implementation maps directly to spec requirements.

## Inter-Spec Conflicts

None.

## Recommendations

1. **Fix FR-015 drift**: Change default page size from 50 to 100 in `ChunkReader.fs` to match spec
2. **Add SC-002 measurement**: Implement performance comparison test (deferred T028) when Aspire available
3. **Improve disk-full handling**: Add explicit IOException catch in ChunkWriter → set session to Failed
4. **Add gap tracking**: Record disconnect timestamps in session metadata for query gap indicators
