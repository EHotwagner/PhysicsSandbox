# Drift Resolution Proposals

Generated: 2026-03-20
Based on: drift-report from 2026-03-20 (full project analysis)

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Backfill (Code → Spec) | 0 |
| Align (Spec → Code) | 0 |
| Human Decision | 0 |
| New Specs | 1 |
| Remove from Spec | 0 |
| No Change (informational) | 1 |

## Prior Proposals (all applied)

Proposals P1–P6 from the previous analysis have all been applied to specs. See previous `proposals.md` in git history for details.

## Current Proposals

### Proposal 7: New Spec for Demo Scripts

**Direction**: NEW_SPEC

**Feature**: Demo Scripts
**Location**: `demos/` (14 files, ~500 lines)

**Current State**:
The `demos/` directory contains a well-structured demo catalogue exercising the full PhysicsSandbox system end-to-end:

- **Prelude.fsx** — shared helpers (`ok`, `sleep`, `runFor`, `resetScene`)
- **10 demos** (`Demo01_HelloDrop` through `Demo10_Chaos`) — each a self-contained physics scenario
- **AutoRun.fsx** — standalone runner that executes all 10 demos sequentially with pass/fail tracking
- **AllDemos.fsx** / **RunAll.fsx** — alternative runner scripts

Demos exercise: presets, generators, steering, gravity changes, camera control, wireframe toggle, and state display. `AutoRun.fsx` serves as an end-to-end smoke test.

**Draft Spec**:

```markdown
# Feature Specification: Demo Scripts

**Feature Branch**: `006-demos`
**Status**: Draft (backfill from existing code)

## User Scenarios

### User Story 1 - Run Demo Catalogue (Priority: P1)
A developer runs the demo suite to verify the full system works end-to-end.
They execute `dotnet fsi demos/AutoRun.fsx` against a running Aspire stack
and see 10 demos execute sequentially, each exercising different physics
scenarios, with a pass/fail summary at the end.

### User Story 2 - Run Individual Demo (Priority: P2)
A developer loads a specific demo in FSI to explore or modify a single
physics scenario interactively.

## Requirements

- **FR-001**: System MUST provide at least 10 demo scripts covering distinct physics scenarios.
- **FR-002**: Each demo MUST be self-contained: reset the scene, configure camera, create bodies, run simulation, and display results.
- **FR-003**: System MUST provide an automated runner that executes all demos sequentially and reports pass/fail results.
- **FR-004**: Demos MUST use the PhysicsClient library API (presets, generators, steering, view commands, state display).
- **FR-005**: Demos MUST be runnable via `dotnet fsi` without compilation.
- **FR-006**: System MUST provide a shared prelude with common helpers to avoid duplication across demo scripts.
```

**Confidence**: HIGH — code is stable, tested (used to surface the 7 bugs in the error report), and well-structured.

**Action**:
- [ ] Approve and create spec
- [ ] Reject (demos are informal, no spec needed)
- [ ] Modify

---

### Proposal 8: Error Report — No Spec Needed

**Direction**: NO_CHANGE

**Feature**: Error Report
**Location**: `2026-03-20-Error-Report.md`

**Current State**:
Post-mortem document recording 7 problems found during demo execution. All actionable items (SSL fix, DISPLAY env, reconnection) were addressed in spec 005-mcp-server-testing. The document is a historical artifact.

**Proposed Resolution**:
No spec needed. Recommend archiving to `docs/post-mortems/` or project memory to keep repo root clean.

**Confidence**: HIGH

**Action**:
- [ ] Archive to docs/
- [ ] Leave as-is
- [ ] Delete
