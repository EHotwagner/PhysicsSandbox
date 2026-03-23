# Data Model: Backlog Fixes and Test Progress Reporting

**Date**: 2026-03-23 | **Branch**: `004-backlog-fix-test-progress`

## Entities

### PendingQueryEntry (new — wraps existing TCS)

Replaces the bare `TaskCompletionSource<QueryResponse>` in the MessageRouter's `pendingQueries` dictionary.

| Field | Type | Description |
|-------|------|-------------|
| Tcs | TaskCompletionSource<QueryResponse> | The existing completion source |
| CreatedAt | DateTime | UTC timestamp when query was submitted |

**Lifecycle**: Created → Resolved (response arrives) | Expired (timeout sweep removes it) | Cancelled (caller cancellation)

**Identity**: Keyed by CorrelationId (string), same as current behavior.

### TestProjectResult (script-internal, not persisted)

Represents the outcome of running a single test project, used by the progress script.

| Field | Type | Description |
|-------|------|-------------|
| ProjectName | string | Test project name |
| Passed | int | Number of passing tests |
| Failed | int | Number of failing tests |
| Skipped | int | Number of skipped tests |
| Duration | float | Seconds elapsed for this project |
| ExitCode | int | Process exit code (0 = success) |

**Lifecycle**: Created when project test run completes. Aggregated into final summary.

### ConstraintBuilder Functions (no new entities)

Constraint builders are stateless functions — no new entities introduced. They construct existing proto message types (`SimulationCommand` → `AddConstraint` → `ConstraintType` → specific constraint message).

## Relationships

- `PendingQueryEntry` replaces `TaskCompletionSource<QueryResponse>` as the value type in `ConcurrentDictionary<string, PendingQueryEntry>`. The existing `submitQuery` and `processQueryResponses` functions access `.Tcs` on the wrapper.
- `TestProjectResult` instances are collected in a list by the progress script. No persistence or cross-entity relationships.

## State Transitions

### PendingQueryEntry

```
Created ──→ Resolved (processQueryResponses sets result)
       ──→ Expired (cleanup timer sets TimeoutException)
       ──→ Cancelled (CancellationToken fires)
```

All terminal states remove the entry from the dictionary.
