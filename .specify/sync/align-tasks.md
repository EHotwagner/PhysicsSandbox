# Alignment Tasks

Generated: 2026-03-22
Based on: drift proposals from 2026-03-22

## Task: Validate Viewer Shape Sizing Fix

**Source**: Proposal 7 (NEW_SPEC — viewer rendering accuracy)
**Current Code**: `src/PhysicsViewer/Rendering/SceneManager.fs:76-105`
**Status**: Fix implemented (passing `Size` to `Bepu3DPhysicsOptions`), not yet validated

### Description
The viewer was rendering all physics bodies at unit size (1x1x1) instead of their actual dimensions. A fix was implemented to pass the `Size` property based on sphere radius / box half-extents. However, visual testing showed objects still appear to merge. Further investigation needed into how Stride's `Create3DPrimitive` interprets the `Size` parameter.

### Files to Investigate
- `src/PhysicsViewer/Rendering/SceneManager.fs` — the `shapeSize` and `createEntity` functions
- Stride.CommunityToolkit.Bepu source — how `Bepu3DPhysicsOptions.Size` maps to entity scale

### Acceptance Criteria
- [ ] Spheres render at diameter = 2 * radius
- [ ] Boxes render at dimensions = 2 * half-extents
- [ ] Two objects touching (e.g., box on box) appear visually distinct, not merged
- [ ] Ground plane does not clip into objects sitting on it

### Estimated Effort
Medium — may require reading Stride toolkit source to understand Size semantics
