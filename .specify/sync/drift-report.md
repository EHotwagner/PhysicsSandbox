# Spec Drift Report

Generated: 2026-03-20
Project: PhysicsSandbox — 004-client-repl

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 14 |
| Aligned | 12 (86%) |
| Drifted | 2 (14%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 0 |

## Detailed Findings

### Spec: 004-client-repl — Client REPL Library

#### Aligned

- FR-001: Connect function returning session handle → `Session.connect` in `Connection/Session.fs`
- FR-002: All simulation commands → `SimulationCommands` module with 13 functions
- FR-002a: Clear-all function → `SimulationCommands.clearAll` iterates bodyRegistry, returns count
- FR-003: Ready-made body presets (≥5, auto-IDs) → 7 presets in `Presets` module with optional position and mass overrides
- FR-004: Randomized body generation → `Generators.randomSpheres/randomBoxes/randomBodies` with optional seed
- FR-005: Scene-builder functions (≥3 patterns) → `Generators.stack/row/grid/pyramid` (4 patterns)
- FR-006: Steering functions → `Steering` module with push/pushVec/launch/spin/stop + Direction DU
- FR-007: State query functions → `StateDisplay.listBodies/inspect/status/snapshot`
- FR-008: Cancellable live-watch with filtering → `LiveWatch.watch` with Ctrl+C cancellation, bodyIds/shapeFilter/minVelocity filters
- FR-009: Viewer control → `ViewCommands.setCamera/setZoom/wireframe`
- FR-011: Result-based error handling → All functions return `Result<'T, string>`
- FR-012: Formatted text output → Spectre.Console tables, staleness timestamps

#### Drifted ⚠️

- **FR-003**: Spec says "Body IDs can be optionally overridden by the user." Preset functions accept `?position` and `?mass` overrides but do not expose an `?id` parameter — they always auto-generate IDs. Users must use `SimulationCommands.addSphere` directly for custom IDs.
  - Location: `src/PhysicsClient/Bodies/Presets.fs`
  - Severity: minor

- **FR-010**: Spec says "MUST be loadable in FSI without requiring a full application build." The compiled DLL is FSI-loadable, but no convenience `.fsx` script was created as planned in research.md (R2). The `OutputType=Exe` for Aspire does not prevent FSI loading.
  - Location: `src/PhysicsClient/PhysicsClient.fsproj`
  - Severity: minor

#### Not Implemented ✗

None — all 14 functional requirements have corresponding implementations.

### Success Criteria

| Criterion | Status | Notes |
|-----------|--------|-------|
| SC-001: ≤5 calls to running sim | ✓ Aligned | connect → addSphere → play = 3 calls |
| SC-002: All presets valid | ✓ Aligned | 7 presets, correct shape/mass per data-model |
| SC-003: Varied random bodies | ✓ Aligned | Seedable RNG, varied position/size/mass |
| SC-004: Accurate state queries | ✓ Aligned | Reads cached SimulationState |
| SC-005: All commands accessible | ✓ Aligned | 9 proto sim commands + 3 view commands |
| SC-006: Loads in FSI | ⚠️ Drifted | DLL loadable, no .fsx convenience script |
| SC-007: Steering produces motion | ✓ Aligned | Direction DU maps correctly |

### Edge Case Coverage

| Edge Case | Status | Implementation |
|-----------|--------|----------------|
| Commands after disconnect | ✓ | `sendCommand` checks `IsConnected` |
| Invalid body ID | ✓ | Server CommandAck error propagated |
| Zero/negative generator count | ✓ | Validates count > 0, returns Error |
| Stale state timestamp | ✓ | `stalenessInfo` shows age > 5s |
| Multiple REPL sessions | ✓ | Independent Session per connect |

## Inter-Spec Conflicts

None — uses existing proto contracts without modification.

## Recommendations

1. **Minor — Add `?id` to Presets** (FR-003): Add optional `id: string option` parameter to each preset function. Low effort.
2. **Minor — Create `.fsx` script** (FR-010): Add `PhysicsClient.fsx` with `#r` directives for FSI convenience.
