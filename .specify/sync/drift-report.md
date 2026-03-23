# Spec Drift Report

Generated: 2026-03-23
Project: PhysicsSandbox (005-viewer-settings-sizing-fix)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 9 |
| ✓ Aligned | 7 (78%) |
| ⚠️ Drifted | 2 (22%) |
| ✗ Not Implemented | 0 (0%) |
| 🆕 Unspecced Code | 0 |

## Detailed Findings

### Spec: 005-viewer-settings-sizing-fix - Viewer Display Settings & Shape Sizing Fix

#### Aligned ✓

- **FR-001**: Viewer renders physics body shapes at exact physics dimensions → `ShapeGeometry.shapeSize` passes radius (not diameter) for sphere/capsule/cylinder. Box uses full extents. Unit tests verify correct values.
  - Location: `src/PhysicsViewer/Rendering/ShapeGeometry.fs:30-137`

- **FR-003**: Fullscreen toggle via F11 with borderless windowed mode → `DisplayManager.toggleFullscreen` + F11/Escape handling in `Program.fs:190-210`. Window uses `IsBorderLess` + `IsFullscreen`.
  - Location: `src/PhysicsViewer/Settings/DisplayManager.fs:44-67`, `src/PhysicsViewer/Program.fs:190-210`

- **FR-004**: Resolution selection from supported resolutions → `SettingsOverlay` presents 1280x720, 1920x1080, 2560x1440 options. `DisplayManager.applySettings` sets `PreferredBackBufferWidth/Height` + `ApplyChanges()`.
  - Location: `src/PhysicsViewer/Settings/SettingsOverlay.fs:22`, `src/PhysicsViewer/Settings/DisplayManager.fs:36-42`

- **FR-006**: Settings persist across restarts → `ViewerSettings.save` writes JSON to `~/.config/PhysicsSandbox/viewer-settings.json`. `ViewerSettings.load` reads on startup. `Program.fs:start` calls `ViewerSettings.load` and applies.
  - Location: `src/PhysicsViewer/Settings/ViewerSettings.fs:41-84`

- **FR-008**: On-screen settings interface via keyboard shortcut → F2 toggles `SettingsOverlay` with Display/Quality tabs, arrow keys navigate, Enter/Left/Right change values.
  - Location: `src/PhysicsViewer/Settings/SettingsOverlay.fs`, `src/PhysicsViewer/Program.fs:212-232`

- **FR-009**: Debug wireframe renders at same dimensions as solid body (no artificial scaling) → 1.02x scale removed from `createPrimitiveWireframe`. Comment on line 42 documents FR-009 compliance.
  - Location: `src/PhysicsViewer/Rendering/DebugRenderer.fs:34-46`

- **FR-002**: Debug wireframe covers all 10 shape types. Compound shapes render per-child wireframes. Convex hull/mesh/triangle use correctly-dimensioned bounding boxes (per clarified spec).
  - Location: `src/PhysicsViewer/Rendering/DebugRenderer.fs:48-70`

#### Drifted ⚠️

- **FR-005**: Spec says "Viewer MUST provide basic quality settings: anti-aliasing level, shadow quality, and texture filtering quality." Code provides the UI (`SettingsOverlay.fs` Quality tab with AA/Shadows/TextureFiltering/VSync options) but `DisplayManager.applySettings` only applies resolution and VSync via `GraphicsDeviceManager`. MSAA, shadow cascade count, and texture sampler changes are not yet wired to Stride's rendering pipeline — the settings save/load correctly but the visual effect is not applied at runtime.
  - Location: `src/PhysicsViewer/Settings/DisplayManager.fs:36-42`
  - Severity: moderate
  - Impact: Quality settings persist and display correctly in the UI, but changing AA/shadow/texture filtering has no visual effect.

- **FR-007**: Spec says "All settings changes MUST apply immediately without requiring a viewer restart." Resolution and fullscreen apply immediately. Quality settings (AA, shadows, texture filtering) do not apply at runtime — same root cause as FR-005 drift.
  - Location: `src/PhysicsViewer/Settings/DisplayManager.fs:36-42`
  - Severity: moderate
  - Impact: Resolution and fullscreen work immediately. Quality changes require restart to take effect.

### Unspecced Code 🆕

No unspecced code detected. All new modules map directly to spec requirements.

## Inter-Spec Conflicts

None detected.

## Recommendations

1. **Wire quality settings to Stride rendering pipeline** (FR-005/FR-007 drift): `DisplayManager.applySettings` needs to apply MSAA via `GraphicsDeviceManager.PreferredMultisampleCount`, shadow quality via the directional light's `LightDirectionalShadowMap.CascadeCount`, and texture filtering via `GraphicsDevice.SamplerStates`.

2. **Native resolution detection** (FR-004 minor gap): The resolution list is hardcoded to 3 options. The spec mentions "display's native resolution" should also be available. Consider querying `game.Window` for native resolution.

3. **Resolution clamping** (edge case): The spec mentions clamping to max supported resolution — `DisplayManager` should validate before applying.
