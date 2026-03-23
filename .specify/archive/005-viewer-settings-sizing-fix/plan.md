# Implementation Plan: Viewer Display Settings & Shape Sizing Fix

**Branch**: `005-viewer-settings-sizing-fix` | **Date**: 2026-03-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-viewer-settings-sizing-fix/spec.md`

## Summary

Fix the shape sizing bug where `ShapeGeometry.shapeSize` outputs diameter values for sphere/capsule/cylinder primitives, but Stride's `Create3DPrimitive` interprets Size.X as radius for these types — causing bodies to render 2x larger than their physics dimensions. Remove the artificial 1.02x debug wireframe scaling. Add viewer display settings (borderless windowed fullscreen via F11, resolution selection, basic quality controls) with JSON persistence and a text-based settings overlay (F2).

## Technical Context

**Language/Version**: F# on .NET 10.0 (PhysicsViewer project)
**Primary Dependencies**: Stride.CommunityToolkit 1.0.0-preview.62 (rendering), Stride.CommunityToolkit.Bepu 1.0.0-preview.62 (Bepu3DPhysicsOptions, Create3DPrimitive), Grpc.Net.Client 2.x (server communication), System.Text.Json (settings persistence)
**Storage**: JSON file at `~/.config/PhysicsSandbox/viewer-settings.json`
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x (unit + integration tests)
**Target Platform**: Linux with GPU (OpenGL via SDL)
**Project Type**: Desktop 3D viewer (Stride3D game application)
**Performance Goals**: ≥30 FPS with 100 bodies at minimum quality; settings changes <1s
**Constraints**: No new NuGet dependencies beyond System.Text.Json (built-in); all changes within PhysicsViewer project
**Scale/Scope**: Single project modification (PhysicsViewer + tests), ~3 new modules, ~1 modified module

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | Changes confined to PhysicsViewer — no cross-service state |
| II. Contract-First | PASS | No contract changes — viewer consumes existing proto messages |
| III. Shared Nothing | PASS | No new cross-service dependencies |
| IV. Spec-First Delivery | PASS | Spec and plan precede implementation |
| V. Compiler-Enforced Structural Contracts | PASS | New .fsi files required for new modules (ViewerSettings, SettingsOverlay, DisplayManager) |
| VI. Test Evidence | PASS | Unit tests for sizing fix, settings persistence, display manager; integration test for fullscreen toggle |
| VII. Observability by Default | PASS | Viewer already uses ServiceDefaults; settings changes will emit structured log events |

**Post-Phase 1 Re-check:**

| Principle | Status | Notes |
|-----------|--------|-------|
| V. .fsi files | REQUIRES | ViewerSettings.fsi, SettingsOverlay.fsi, DisplayManager.fsi must be created |
| V. Surface area baselines | REQUIRES | New baseline entries for 3 new modules + updated ShapeGeometry/DebugRenderer baselines |
| VI. Test coverage | REQUIRES | shapeSize fix must have unit tests proving correct values; settings persistence must have round-trip test |

## Project Structure

### Documentation (this feature)

```text
specs/005-viewer-settings-sizing-fix/
├── plan.md              # This file
├── research.md          # Phase 0 output — sizing root cause, Stride APIs
├── data-model.md        # Phase 1 output — ViewerSettings entity, size mapping table
├── quickstart.md        # Phase 1 output — build/run/verify instructions
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/PhysicsViewer/
├── Rendering/
│   ├── ShapeGeometry.fsi          # MODIFY — update shapeSize return contract
│   ├── ShapeGeometry.fs           # MODIFY — fix sizing for sphere/capsule/cylinder
│   ├── SceneManager.fsi           # unchanged
│   ├── SceneManager.fs            # unchanged (consumes corrected sizes)
│   ├── DebugRenderer.fsi          # MODIFY — add compound child rendering
│   ├── DebugRenderer.fs           # MODIFY — remove 1.02x scale, compound children
│   ├── FpsCounter.fsi             # unchanged
│   ├── FpsCounter.fs              # unchanged
│   ├── CameraController.fsi       # unchanged
│   └── CameraController.fs        # unchanged
├── Settings/                       # NEW directory
│   ├── ViewerSettings.fsi         # NEW — settings model + load/save signatures
│   ├── ViewerSettings.fs          # NEW — JSON persistence, defaults
│   ├── DisplayManager.fsi         # NEW — fullscreen/resolution/quality control
│   ├── DisplayManager.fs          # NEW — applies settings to Stride APIs
│   ├── SettingsOverlay.fsi        # NEW — text-based settings UI
│   └── SettingsOverlay.fs         # NEW — F2 toggle, keyboard nav, rendering
├── Streaming/
│   ├── ViewerClient.fsi           # unchanged
│   └── ViewerClient.fs            # unchanged
├── Program.fs                      # MODIFY — integrate settings, F11/F2 keys
└── PhysicsViewer.fsproj           # MODIFY — add new files to compile order

tests/PhysicsViewer.Tests/
├── SceneManagerTests.fs            # MODIFY — fix expected shapeSize values
├── ViewerSettingsTests.fs         # NEW — persistence round-trip tests
├── DisplayManagerTests.fs         # NEW — settings-to-Stride-API mapping tests
├── SurfaceAreaTests.fs            # MODIFY — add baselines for new modules
└── PhysicsViewer.Tests.fsproj     # MODIFY — add new test files
```

**Structure Decision**: All changes within existing PhysicsViewer project. New `Settings/` subdirectory groups the 3 new modules (ViewerSettings, DisplayManager, SettingsOverlay) for clear separation from rendering logic. No new projects required.

## Complexity Tracking

No constitution violations. All changes within a single existing project with no new external dependencies.
