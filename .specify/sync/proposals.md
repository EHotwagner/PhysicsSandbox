# Drift Resolution Proposals

Generated: 2026-03-23
Based on: drift-report from 2026-03-23 (004-backlog-fix-test-progress)

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Backfill (Code → Spec) | 1 |
| Align (Spec → Code) | 0 |
| Human Decision | 0 |
| New Specs | 0 |
| Remove from Spec | 0 |

## Proposals

### Proposal 1: 004-backlog-fix-test-progress / FR-004a — clearAll registry warning

**Direction**: BACKFILL (Code → Spec)

**Current State**:
- Spec says: "System MUST return Result.Error with a descriptive message when PhysicsClient body registry operations (TryAdd/TryRemove in SimulationCommands) fail, covering all 7 identified instances"
- Code does: 6/7 return `Result.Error`. The 7th instance — `clearAll` (SimulationCommands.fs:307-308) — silently counts `TryRemove` failures into a local `registryWarnings` counter and returns `Ok count` regardless.

**Proposed Resolution**:

Update FR-004a to acknowledge that `clearAll` registry cleanup failures are **warnings, not errors**, and add a `Trace.TraceWarning` to make them observable (consistent with FR-004b's cache warning pattern).

**Updated spec text for FR-004a**:

> **FR-004a**: System MUST return `Result.Error` with a descriptive message when PhysicsClient body registry operations (`TryAdd`/`TryRemove` in SimulationCommands) fail, covering the **6 single-body** operations (addSphere, addBox, addCapsule, addCylinder, addPlane, removeBody). The `clearAll` bulk operation MUST emit a structured warning (`Trace.TraceWarning`) when individual registry cleanup entries fail, but still return `Ok` with the removal count, since the server-side removals succeeded.

**Updated SC-003**:

> **SC-003**: All 10 silent `TryAdd`/`TryRemove` failures in PhysicsClient are replaced with explicit reporting — 6 single-body registry operations return `Result.Error`, 1 bulk registry cleanup emits `Trace.TraceWarning`, 3 cache operations emit structured warnings.

**Code change** (small, to make the warning observable):

```fsharp
// SimulationCommands.fs ~line 307-308, replace:
if not (registry.TryRemove(key, &_removed)) then
    registryWarnings <- registryWarnings + 1

// with:
if not (registry.TryRemove(key, &_removed)) then
    System.Diagnostics.Trace.TraceWarning($"SimulationCommands.clearAll: body '{key}' not found in registry during cleanup")
    registryWarnings <- registryWarnings + 1
```

**Rationale**:

The current code behavior is correct for a bulk operation:
1. The **server-side removal succeeded** — `sendCommand` returned `Ok`. The body is gone from the physics world.
2. The local registry `TryRemove` failure means the registry was already out of sync (entry missing). This is a cache inconsistency, not a command failure.
3. Returning `Result.Error` for the entire `clearAll` because one local registry entry was missing would be surprising — the user asked to clear all bodies and they were cleared.
4. The pattern is consistent with **FR-004b**, which already uses `Trace.TraceWarning` for benign cache inconsistencies.
5. The only gap is that the warning is currently invisible (counted but not emitted). Adding `Trace.TraceWarning` makes it observable.

**Confidence**: HIGH

**Action**:
- [ ] Approve (update spec + add TraceWarning to code)
- [ ] Reject
- [ ] Modify
