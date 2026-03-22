# Sync Apply Report

Applied: 2026-03-22

## Changes Made

### Code Fixed (ALIGN)
| File | Change |
|------|--------|
| Scripting/demos/AllDemos.fsx | `open Prelude.DemoHelpers` → `open Prelude` |
| Scripting/demos/Demo06_DominoRow.fsx | `open Prelude.DemoHelpers` → `open Prelude` |
| Scripting/demos/Demo07_SpinningTops.fsx | `open Prelude.DemoHelpers` → `open Prelude` |
| Scripting/demos/Demo08_GravityFlip.fsx | `open Prelude.DemoHelpers` → `open Prelude` |
| Scripting/demos/Demo09_Billiards.fsx | `open Prelude.DemoHelpers` → `open Prelude` |
| Scripting/demos/Demo10_Chaos.fsx | `open Prelude.DemoHelpers` → `open Prelude` |
| Scripting/demos/Demo11_BodyScaling.fsx | `open Prelude.DemoHelpers` → `open Prelude` |
| Scripting/demos/Demo12_CollisionPit.fsx | `open Prelude.DemoHelpers` → `open Prelude` |
| Scripting/demos/Demo13_ForceFrenzy.fsx | `open Prelude.DemoHelpers` → `open Prelude` |
| Scripting/demos/Demo14_DominoCascade.fsx | `open Prelude.DemoHelpers` → `open Prelude` |
| Scripting/demos/Demo15_Overload.fsx | `open Prelude.DemoHelpers` → `open Prelude` |

### Specs Updated (BACKFILL)
| Spec | Requirement | Change |
|------|-------------|--------|
| 004-improve-demos | FR-001 | Updated to scope visual rendering to viewer, not demos |
| 004-improve-demos | FR-006 | Added "verified by code inspection" annotation |
| 004-improve-demos | FR-008 table | Demo 04 direction updated to wrecking ball + brick wall |
| 004-improve-demos | SC-002 | Marked as superseded by SC-003 |
| 004-improve-demos | SC-003 | Updated: demos 01-05 reviewed individually, 06-15 accepted |
| 004-improve-demos | SC-004 | Marked as verified by code inspection |
| 004-improve-demos | Assumptions | Added viewer merging note |

### Alignment Tasks Generated
- 1 task in `.specify/sync/align-tasks.md` (viewer shape sizing validation)

### Human Decisions Resolved
- SC-003: User chose "Accept current state" — demos 06-15 accepted without individual review

## Summary

| Action | Count |
|--------|-------|
| Proposals applied | 7/7 |
| Files modified (code) | 11 |
| Spec sections updated | 7 |
| Alignment tasks created | 1 |

## Next Steps

1. Verify the import fix works: `dotnet fsi Scripting/demos/Demo06_DominoRow.fsx`
2. Address viewer sizing task in `.specify/sync/align-tasks.md` (separate feature)
3. Commit changes
