# Data Model: Viewer Display Settings & Shape Sizing Fix

**Date**: 2026-03-23
**Feature**: 005-viewer-settings-sizing-fix

## Entities

### ViewerSettings (new, persisted)

Persisted as JSON at `~/.config/PhysicsSandbox/viewer-settings.json`.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| ResolutionWidth | int | 1280 | Viewport width in pixels |
| ResolutionHeight | int | 720 | Viewport height in pixels |
| IsFullscreen | bool | false | Borderless windowed fullscreen mode |
| AntiAliasing | enum | Off | MSAA level: Off, X2, X4, X8 |
| ShadowQuality | enum | Medium | Off, Low (1 cascade), Medium (2), High (4) |
| TextureFiltering | enum | Linear | Point, Linear (bilinear), Anisotropic |
| VSync | bool | true | Vertical sync enabled |

### Shape Size Mapping (corrected, no persistence)

Maps physics proto shape dimensions to Stride `Create3DPrimitive` Size parameter:

| Shape Type | Size.X | Size.Y | Size.Z |
|------------|--------|--------|--------|
| Sphere | radius | radius | radius |
| Box | width (2×halfX) | height (2×halfY) | depth (2×halfZ) |
| Capsule | radius | cylindrical length | radius |
| Cylinder | radius | length | radius |
| Plane | 1000.0 | 0.1 | 1000.0 |
| Triangle | bbox width | bbox height | bbox depth |
| ConvexHull | bbox width | bbox height | bbox depth |
| Compound | (per-child rendering) | — | — |
| Mesh | bbox width | bbox height | bbox depth |

## State Transitions

### ViewerSettings Lifecycle

```
Load from disk → Apply to GraphicsDeviceManager → User modifies → Apply changes → Save to disk
                                                                              ↓
                                                                     Viewer restart → Load from disk
```

### Settings Overlay State

```
Closed (default) ─── F2 ──→ Open (Display tab)
                              │
                    Arrow keys + Enter to modify
                              │
                     F2 / Escape ──→ Closed (auto-save)
```
