# Spec Drift Report

Generated: 2026-03-20
Project: BPSandbox (PhysicsViewer — 003-3d-viewer)

## Summary

| Category | Count |
|----------|-------|
| Specs Analyzed | 1 |
| Requirements Checked | 16 FR + 6 SC |
| Aligned | 13 (81%) |
| Drifted | 3 (19%) |
| Not Implemented | 0 (0%) |
| Unspecced Code | 1 |

## Detailed Findings

### Spec: 003-3d-viewer — 3D Viewer

#### Aligned

- FR-001: Connect to server and subscribe to state stream → `ViewerClient.streamState` + `Program.fs:57-65`
- FR-002: Render bodies as 3D shapes → `SceneManager.fs:54-68`
- FR-003: Position and orient from quaternion → `SceneManager.fs:46-52,70-72`
- FR-004: Update scene on each state → `Program.fs:80-88`
- FR-005: Apply SetCamera commands → `CameraController.fs:24-30` + `Program.fs:96`
- FR-007: Apply ToggleWireframe → `SceneManager.fs:103-109`
- FR-008: Display simulation time → `Program.fs:111-117`
- FR-009: Indicate running/paused → `Program.fs:113-117`
- FR-010: Default camera position → `CameraController.fs:14-18` + `Program.fs:46`
- FR-011: Handle late-join → `ViewerClient.fs:39-50` + server caches state
- FR-012: Differentiate by shape color → `SceneManager.fs:34-38`
- FR-014: Interactive mouse/keyboard camera → `CameraController.fs:42-90`
- FR-016: Ground reference grid at Y=0 → `Program.fs:37-43`

#### Drifted

- **FR-006**: Spec says "apply SetZoom by adjusting camera zoom" but code stores zoom level without applying it to camera entity. `applyToCamera` does not adjust FOV or distance based on zoom.
  - Location: `src/PhysicsViewer/Rendering/CameraController.fs:92-104`
  - Severity: moderate
  - Fix: Scale camera offset by `1.0 / zoomLevel` in `applyToCamera`.

- **FR-013**: Spec says "register with Aspire for health checks". AppHost registers viewer but Program.fs never calls `AddServiceDefaults()` — no health endpoints exposed.
  - Location: `src/PhysicsViewer/Program.fs` (missing)
  - Severity: moderate
  - Fix: Add background `WebApplication` host with `AddServiceDefaults()` + `MapDefaultEndpoints()`.

- **FR-015**: Spec says "REPL commands override current camera state". Code processes REPL commands BEFORE interactive input, so interactive input overwrites REPL in same frame.
  - Location: `src/PhysicsViewer/Program.fs:90-108`
  - Severity: minor
  - Fix: Move `applyInput` before view command queue drain so REPL is applied last.

#### Not Implemented

None.

### Success Criteria

- SC-001 through SC-006: All aligned except SC-002 (drifted due to FR-006 zoom gap).

### Unspecced Code

| Feature | Location | Description |
|---------|----------|-------------|
| Reconnection with exponential backoff | `ViewerClient.fs:20-37` | Auto-reconnect with 1s→30s backoff (covered by edge case, not a formal FR) |

## Recommendations

1. **Fix FR-015 (minor)**: Swap order — apply `applyInput` first, then drain REPL commands so they override.
2. **Fix FR-006 (moderate)**: Apply zoom level in `applyToCamera` by scaling camera distance.
3. **Fix FR-013 (moderate)**: Add `AddServiceDefaults()` via background web host.
