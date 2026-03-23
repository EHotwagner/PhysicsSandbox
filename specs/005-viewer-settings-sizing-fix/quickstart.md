# Quickstart: Viewer Display Settings & Shape Sizing Fix

**Date**: 2026-03-23
**Feature**: 005-viewer-settings-sizing-fix

## Prerequisites

- .NET 10.0 SDK
- GPU with OpenGL support
- System packages: openal, freetype2, sdl2, ttf-liberation

## Build & Run

```bash
# Build everything
dotnet build PhysicsSandbox.slnx

# Run full stack (Aspire + all services including viewer)
dotnet run --project src/PhysicsSandbox.AppHost

# Run viewer only (requires server already running)
dotnet run --project src/PhysicsViewer
```

## Verify Shape Sizing Fix

1. Start the full stack
2. In the viewer, spawn test bodies via a demo script or client:
   ```
   # From Scripting/demos/ run a demo that creates various shapes
   dotnet fsi Scripting/demos/01_HelloSphere.fsx
   ```
3. Confirm spheres appear correctly sized (not overlapping when they shouldn't)
4. Press F3 to toggle debug wireframes — wireframes should align exactly with solid bodies

## Verify Display Settings

1. Press F2 to open settings overlay
2. Change resolution, anti-aliasing, or shadow quality
3. Changes apply immediately
4. Press F11 to toggle borderless windowed fullscreen
5. Close and reopen viewer — settings should persist

## Test

```bash
# Run all tests (headless)
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run viewer tests only
dotnet test tests/PhysicsViewer.Tests -p:StrideCompilerSkipBuild=true
```

## Key Files

| File | Purpose |
|------|---------|
| `src/PhysicsViewer/Rendering/ShapeGeometry.fs` | Shape-to-primitive size mapping (bug fix) |
| `src/PhysicsViewer/Rendering/DebugRenderer.fs` | Debug wireframe visualization (1.02x fix) |
| `src/PhysicsViewer/Rendering/SceneManager.fs` | Entity creation and update |
| `src/PhysicsViewer/Settings/ViewerSettings.fs` | Settings model and persistence (new) |
| `src/PhysicsViewer/Settings/SettingsOverlay.fs` | On-screen settings UI (new) |
| `src/PhysicsViewer/Settings/DisplayManager.fs` | Resolution/fullscreen/quality control (new) |
| `src/PhysicsViewer/Program.fs` | Main game loop integration |
