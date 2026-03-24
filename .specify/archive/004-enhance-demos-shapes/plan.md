# Implementation Plan: Enhance Demos with New Shapes and Viewer Labels

**Branch**: `004-enhance-demos-shapes` | **Date**: 2026-03-24 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-enhance-demos-shapes/spec.md`

## Summary

Add a demo metadata transport mechanism (via ViewCommand proto extension) so the 3D viewer displays each demo's name and description in a top-left overlay. Set a static viewer window title. Enhance 8+ existing demos with Triangle, ConvexHull, Mesh, and Compound shapes. Create 3 new demos (19, 20, 21) that showcase all new shape types with variation. Add `makeMeshCmd` builder to preludes and `setDemoInfo` helper to all demo infrastructure.

## Technical Context

**Language/Version**: F# on .NET 10.0 (services, viewer, client, scripting), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts), Python 3.10+ (demo scripts)
**Primary Dependencies**: Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, Stride.CommunityToolkit 1.0.0-preview.62, Spectre.Console, grpcio (Python)
**Storage**: N/A (in-memory only, no persistence changes)
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x, dotnet fsi (script validation)
**Target Platform**: Linux with GPU passthrough (container)
**Project Type**: Desktop app (viewer) + gRPC microservices + scripting
**Performance Goals**: Demo label renders at 60 fps with no measurable overhead. Metadata sent once per demo (not per tick).
**Constraints**: No changes to TickState bandwidth. ViewCommand extension must be backward-compatible (additive oneof field).
**Scale/Scope**: 21 demos total (18 existing + 3 new), ~40 files touched across F#/Python/proto

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | No new shared mutable state. Metadata flows via existing gRPC streams. |
| II. Contract-First | PASS | Proto contract change (SetDemoMetadata) defined before implementation. See [contracts/proto-changes.md](contracts/proto-changes.md). |
| III. Shared Nothing | PASS | Only PhysicsSandbox.Shared.Contracts touched for proto. No cross-project references added. |
| IV. Spec-First Delivery | PASS | Spec and plan completed before coding. |
| V. Compiler-Enforced Structural Contracts | PASS | New public F# functions (ViewCommands.setDemoMetadata, SceneManager metadata fields) require .fsi updates. |
| VI. Test Evidence | PASS | Integration tests for metadata transport. Unit tests for new helpers. Demo scripts serve as functional tests. |
| VII. Observability by Default | PASS | No new services. Existing ServiceDefaults covers all participants. |

**Post-Phase 1 Re-check**: All gates still pass. No new projects, no new services, no breaking contract changes.

## Project Structure

### Documentation (this feature)

```text
specs/004-enhance-demos-shapes/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research decisions
├── data-model.md        # Phase 1 data model
├── quickstart.md        # Phase 1 quickstart guide
├── contracts/           # Phase 1 proto contract changes
│   └── proto-changes.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (from /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── PhysicsSandbox.Shared.Contracts/
│   └── Protos/physics_hub.proto          # Add SetDemoMetadata message + ViewCommand field 4
├── PhysicsViewer/
│   ├── Program.fs                        # Window title, demo label rendering, ViewCommand handling
│   ├── Rendering/SceneManager.fs         # Add DemoName/DemoDescription to SceneState
│   └── Rendering/SceneManager.fsi        # Update signature
├── PhysicsClient/
│   ├── Commands/ViewCommands.fs          # Add setDemoMetadata function
│   └── Commands/ViewCommands.fsi         # Update signature
Scripting/
├── demos/
│   ├── Prelude.fsx                       # Add makeMeshCmd, setDemoInfo helpers
│   ├── AllDemos.fsx                      # Add Demo19-21, add setDemoInfo to all demos
│   ├── RunAll.fsx                        # Update demo count
│   ├── AutoRun.fsx                       # Update demo count
│   ├── Demo01_HelloDrop.fsx              # Enhance with new shapes + metadata
│   ├── ... (other enhanced demos)
│   ├── 19_ShapeGallery.fsx              # NEW: all shape types
│   ├── 20_CompoundConstructions.fsx     # NEW: compound shapes
│   └── 21_MeshHullPlayground.fsx        # NEW: mesh + convex hull
├── demos_py/
│   ├── prelude.py                        # Add make_mesh_cmd, set_demo_info helpers
│   ├── all_demos.py                      # Add demo19-21
│   ├── auto_run.py                       # Update demo count
│   ├── demo01_hello_drop.py              # Enhance + metadata
│   ├── ... (other enhanced demos)
│   ├── demo19_shape_gallery.py          # NEW
│   ├── demo20_compound_constructions.py # NEW
│   ├── demo21_mesh_hull_playground.py   # NEW
│   └── generated/                        # Regenerated proto stubs
tests/
├── PhysicsClient.Tests/                  # Test setDemoMetadata helper
├── PhysicsViewer.Tests/                  # Test SceneState metadata fields
└── PhysicsSandbox.Integration.Tests/     # Test metadata end-to-end transport
```

**Structure Decision**: No new projects. All changes fit within existing project boundaries. Proto contract change is additive (field 4 in ViewCommand oneof). Viewer and client modules extended with new functions.

## Complexity Tracking

No constitution violations. No complexity justification needed.
