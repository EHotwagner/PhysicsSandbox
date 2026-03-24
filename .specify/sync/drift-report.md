# Spec Drift Report

Generated: 2026-03-24
Project: PhysicsSandbox
Feature: 005-robust-network-connectivity

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 13 FR + 6 SC = 19 |
| Aligned | 19 (100%) |
| Drifted | 0 (0%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 0 |

## Detailed Findings

### Spec: 005-robust-network-connectivity - Robust Network Connectivity

#### Aligned

| Requirement | Evidence |
|-------------|----------|
| FR-001: Broadcast to all subscribers | MessageRouter.fs:126-127 — iterates ViewCommandSubscribers, TryWrite to each |
| FR-002: Preserve command ordering | MessageRouter.fs:468 — Channel preserves FIFO |
| FR-003: Graceful subscriber disconnection | PhysicsHubService.fs:114-125 — try/finally with unsubscribeViewCommands |
| FR-004: Backpressure via newest-drop | MessageRouter.fs:127 — TryWrite ignore skips when full |
| FR-005: MCP SSE reachable via Aspire URL | AppHost.cs:22-23 — WithHttpEndpoint + IsProxied=false |
| FR-006: MCP supports HTTP/1.1 for SSE | AppHost.cs:22-23 — bypasses DCP HTTP/2 proxy |
| FR-007: kill.sh no self-kill | kill.sh:8-26 — /bin and --project patterns |
| FR-008: kill.sh specific patterns | kill.sh:8-26 — 16 patterns with /bin, --project, .dll suffixes |
| FR-009: Camera hold on body-not-found | CameraController.fs:177,193,198,217,229 — returns state on None |
| FR-010: All issues documented | NetworkProblems.md — 7 structured entries |
| FR-011: Container environment section | NetworkProblems.md:7-25 — port table + networking boundary |
| FR-012: Viewer drains all per frame | Program.fs:40,339 — ConcurrentQueue + while TryDequeue |
| FR-013: Zero subscribers silent discard | MessageRouter.fs:126 — empty for loop = no-op |
| SC-001: Demo22 zero drops | Verified via live run |
| SC-002: Two viewers broadcast | ServerHubTests.cs:177-209 — integration test |
| SC-003: MCP SSE accessible | AppHost.cs:23 — IsProxied=false |
| SC-004: kill.sh && echo alive | kill.sh — specific patterns prevent self-kill |
| SC-005: Camera tracks within 2s | CameraController.fs — hold + activate on next frame |
| SC-006: 7 entries + environment | NetworkProblems.md — 7 entries + Container Environment |

#### Drifted

None.

#### Not Implemented

None.

### Unspecced Code

None.

## Inter-Spec Conflicts

None.

## Recommendations

No action required. All 13 functional requirements and 6 success criteria are fully aligned with implementation.
