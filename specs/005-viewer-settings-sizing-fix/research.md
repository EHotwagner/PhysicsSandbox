# Research: Viewer Display Settings & Shape Sizing Fix

**Date**: 2026-03-23
**Feature**: 005-viewer-settings-sizing-fix

## R-001: Shape Sizing Bug Root Cause

### Decision
The sizing bug originates in `ShapeGeometry.shapeSize` which computes `Bepu3DPhysicsOptions.Size` values using full-dimension semantics (diameter, total height), but Stride's `Create3DPrimitive` interprets Size values with mixed semantics depending on primitive type.

### Root Cause Analysis

Stride's `Create3DPrimitive` passes `Size` to the underlying `PrimitiveProceduralModelBase` subclass, which interprets the vector differently per type:

| Primitive | Size.X meaning | Size.Y meaning | Size.Z meaning |
|-----------|---------------|---------------|---------------|
| Sphere | **Radius** | (unused) | (unused) |
| Cube | Full width | Full height | Full depth |
| Capsule | **Radius** | Length (cylindrical section) | (unused) |
| Cylinder | **Radius** | (unused) | Height |

Current `shapeSize` output vs expected:

| Shape | Current Size.X | Expected Size.X | Error |
|-------|---------------|-----------------|-------|
| Sphere (r=0.5) | 1.0 (diameter) | 0.5 (radius) | **2x too large** |
| Box (he=1,1,1) | 2.0 (correct) | 2.0 | OK |
| Capsule (r=0.3, l=1.0) | 0.6 (diameter) | 0.3 (radius) | **2x too wide** |
| Cylinder (r=0.5, l=2.0) | 1.0 (diameter) | 0.5 (radius) | **2x too wide** |

Additionally, for capsules: Current Size.Y = l + r*2 (total height), but Stride expects the cylindrical section length only (excluding hemispherical caps). For cylinders: Current Size.Z = l, but should map height differently per Stride's convention.

### Evidence
- `ShapeGeometry.fs:32-33`: Sphere passes `radius * 2.0f` (diameter) as Size.X
- `ShapeGeometry.fs:42-45`: Capsule passes `radius * 2.0f` (diameter) as Size.X
- `ShapeGeometry.fs:47-49`: Cylinder passes `radius * 2.0f` (diameter) as Size.X
- Stride CommunityToolkit `EntityExtensions.cs`: Collider creation uses `size.Value.X` directly as `Radius` for sphere/capsule/cylinder
- `Primitive3DEntityOptions.Size` XML doc: "Interpretation depends on primitive type"

### Fix
Update `shapeSize` to output values matching Stride's per-type interpretation:
- Sphere: `Vector3(radius, radius, radius)` — not diameter
- Capsule: `Vector3(radius, cylindrical_length, radius)` — not diameter, not total height
- Cylinder: `Vector3(radius, length, radius)` — not diameter; verify Size.Z vs Size.Y mapping
- Box: unchanged (already correct)
- Triangle/ConvexHull/Compound/Mesh: these use Cube primitive with bounding-box dimensions — correct as-is

### Alternatives Considered
- **Entity transform scaling**: Could scale entities post-creation instead of fixing Size. Rejected — this would add a second scaling layer and complicate debug wireframes.
- **Custom mesh generation**: Build meshes manually instead of using Create3DPrimitive. Rejected — massive scope increase for no benefit.

---

## R-002: Debug Wireframe 1.02x Scale

### Decision
Remove the 1.02x artificial scaling from debug wireframes (DebugRenderer.fs:43-44). The wireframes should match physics bounds exactly. If slight z-fighting occurs, use a depth bias offset in the material instead.

### Rationale
The 1.02x scale causes debug wireframes to be visibly larger than the actual physics bounds, which defeats the purpose of debug visualization as a ground-truth reference.

### Alternatives Considered
- **Keep 1.02x**: Avoids z-fighting but misrepresents physics bounds. Rejected per spec FR-009.
- **Configurable scale**: Add a user option. Rejected — over-engineering for a debug tool.

---

## R-003: Stride Fullscreen & Resolution APIs

### Decision
Use Stride's built-in `GameWindow` and `GraphicsDeviceManager` APIs for display settings.

### API Summary

**Borderless Windowed Fullscreen:**
```
game.Window.FullscreenIsBorderlessWindow <- true  // SDL mode
game.Window.IsFullscreen <- true/false             // Toggle
game.Window.IsBorderLess <- true/false             // Borderless
```

**Resolution:**
```
GraphicsDeviceManager.PreferredBackBufferWidth <- width
GraphicsDeviceManager.PreferredBackBufferHeight <- height
GraphicsDeviceManager.ApplyChanges()
```

**Anti-Aliasing (MSAA):**
```
GraphicsDeviceManager.PreferredMultisampleCount <- MultisampleCount.X4
GraphicsDeviceManager.ApplyChanges()
```
Available: None, X2, X4, X8

**Shadows:**
Via directional light's `LightDirectionalShadowMap`:
- `Enabled` (bool)
- `CascadeCount` (int: 1, 2, 4)

**Texture Filtering:**
Via `GraphicsDevice.SamplerStates`:
- `PointClamp` / `LinearClamp` / `AnisotropicClamp`

**VSync:**
```
GraphicsDeviceManager.SynchronizeWithVerticalRetrace <- true/false
GraphicsDeviceManager.ApplyChanges()
```

### Rationale
Native Stride APIs are the most reliable approach. They handle platform-specific details (SDL on Linux, WinForms on Windows) internally.

---

## R-004: Stride.BepuPhysics.Debug as Reference

### Decision
Use Stride.BepuPhysics.Debug source code as a correctness reference only. Do not integrate the package directly.

### Rationale
The package requires native `CollidableComponent` entities with BepuPhysics simulation access. The PhysicsViewer receives data via gRPC proto messages and has no native BepuPhysics objects. However, the debug renderer's shape-to-Stride-primitive mapping code is authoritative for correct dimension translation.

### How to Use
- Reference the Stride.BepuPhysics.Debug GitHub source to verify the correct `Size` parameter interpretation for each primitive type
- Cross-check against the fix applied in R-001

---

## R-005: Complex Shape Debug Wireframes

### Decision
For compound shapes, render each child shape as an individual wireframe entity at its local transform within the compound. For convex hulls and meshes, continue using bounding-box approximation for the initial fix, but use correct bounding-box dimensions.

### Rationale
- Compound child data is available via proto `CompoundChild` messages (each has shape + localPosition + localOrientation)
- Convex hull vertex data is available via proto but building custom mesh wireframes from vertices requires procedural mesh generation — significant scope
- Mesh triangle data is similarly complex

### Implementation Approach
1. Compound: iterate children, create a wireframe entity per child using `primitiveType(child.Shape)` and `shapeSize(child.Shape)`, offset by child local position/orientation relative to the parent body
2. ConvexHull: use corrected bounding-box dimensions from hull points — already implemented, just needs sizing fix verification
3. Mesh: use corrected bounding-box dimensions from triangle vertices — already implemented

---

## R-006: Settings Persistence

### Decision
Store viewer settings as a JSON file at `~/.config/PhysicsSandbox/viewer-settings.json`.

### Rationale
Standard XDG convention for Linux. Simple JSON format is human-readable and easy to edit manually if needed. No external dependencies required — `System.Text.Json` is built-in.

### Alternatives Considered
- **In-memory only**: Settings lost on restart. Rejected per FR-006.
- **AppHost configuration**: Would couple viewer settings to orchestrator. Rejected — viewer may run standalone.

---

## R-007: Settings UI Approach

### Decision
Use Stride's `DebugTextSystem` for a simple text-based settings overlay, toggled via F2. Use keyboard navigation (arrow keys + Enter) for option selection.

### Rationale
The viewer already uses `DebugTextSystem` for the status overlay (FPS, sim time). Extending it for settings avoids adding UI framework dependencies. This matches the diagnostic tool nature of the viewer.

### Alternatives Considered
- **ImGui integration**: Rich UI but heavyweight dependency, not available in Stride out-of-box. Rejected.
- **Stride UI system**: Full widget framework but requires assets and significant setup. Rejected — overkill for a settings menu.
