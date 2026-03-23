# Tasks: Viewer Display Settings & Shape Sizing Fix

**Input**: Design documents from `/specs/005-viewer-settings-sizing-fix/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

**Tests**: Included per constitution Principle VI (test evidence required for behavior-changing code).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Add new files to project structure and compilation order

- [x] T001 Create Settings directory at src/PhysicsViewer/Settings/
- [x] T002 Add new Settings module files (.fsi/.fs pairs) to compile order in src/PhysicsViewer/PhysicsViewer.fsproj: ViewerSettings.fsi/fs, DisplayManager.fsi/fs, SettingsOverlay.fsi/fs (before Program.fs)
- [x] T003 Add new test files to tests/PhysicsViewer.Tests/PhysicsViewer.Tests.fsproj: ViewerSettingsTests.fs, DisplayManagerTests.fs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: ViewerSettings module — shared persistence layer needed by US3, US4, and US5

**⚠️ CRITICAL**: Display settings stories (US3/US4/US5) cannot begin until this phase is complete

- [x] T004 [P] Create ViewerSettings signature in src/PhysicsViewer/Settings/ViewerSettings.fsi — define types: AntiAliasingLevel (Off|X2|X4|X8), ShadowQuality (Off|Low|Medium|High), TextureFilteringMode (Point|Linear|Anisotropic), ViewerSettings record (ResolutionWidth, ResolutionHeight, IsFullscreen, AntiAliasing, ShadowQuality, TextureFiltering, VSync), and functions: defaultSettings, load, save
- [x] T005 [P] Create ViewerSettings implementation in src/PhysicsViewer/Settings/ViewerSettings.fs — JSON serialization via System.Text.Json to ~/.config/PhysicsSandbox/viewer-settings.json, auto-create directory on save, return defaultSettings on missing/corrupt file
- [x] T006 [P] Write unit tests for ViewerSettings in tests/PhysicsViewer.Tests/ViewerSettingsTests.fs — test defaultSettings values, save/load round-trip (use temp file), load returns defaults on missing file, load returns defaults on corrupt JSON

**Checkpoint**: Settings model and persistence ready — display settings stories can now begin

---

## Phase 3: User Story 1 — Fix Shape Sizing (Priority: P1) 🎯 MVP

**Goal**: Fix the sizing bug where sphere, capsule, and cylinder primitives render 2x too large because shapeSize outputs diameter but Stride expects radius

**Independent Test**: Spawn spheres/capsules/cylinders with known dimensions, verify visual size matches physics size and no false overlaps

### Tests for User Story 1

- [x] T007 [P] [US1] Update shapeSize unit tests in tests/PhysicsViewer.Tests/SceneManagerTests.fs — change expected values: sphere(r=0.5) → Vector3(0.5, 0.5, 0.5) not (1,1,1); capsule(r=0.3, l=1.0) → Vector3(0.3, 1.0, 0.3) not (0.6, 1.6, 0.6); cylinder(r=0.5, l=2.0) → Vector3(0.5, 2.0, 0.5) not (1.0, 2.0, 1.0). Verify tests FAIL before implementation.

### Implementation for User Story 1

- [x] T008 [US1] Fix shapeSize in src/PhysicsViewer/Rendering/ShapeGeometry.fs — Sphere: change `radius * 2.0f` to `radius` for all 3 components; Capsule: change Size.X from `r * 2.0f` to `r`, change Size.Y from `l + r * 2.0f` to `l` (cylindrical section only); Cylinder: change Size.X from `r * 2.0f` to `r`, keep Size.Y as `l`, change Size.Z from `r * 2.0f` to `r`. Cross-reference Stride.BepuPhysics.Debug source for correctness.
- [x] T009 [US1] Update ShapeGeometry.fsi signature in src/PhysicsViewer/Rendering/ShapeGeometry.fsi if shapeSize signature changed (likely unchanged — return type is still Nullable<Vector3>)
- [x] T010 [US1] Verify all shapeSize tests pass after fix — run `dotnet test tests/PhysicsViewer.Tests -p:StrideCompilerSkipBuild=true --filter "ShapeGeometry"`

**Checkpoint**: Shape sizing is correct for all primitive types. Visual rendering matches physics dimensions.

---

## Phase 4: User Story 2 — Debug Wireframe Accuracy (Priority: P1)

**Goal**: Remove artificial 1.02x wireframe scaling, implement per-child compound wireframes, verify wireframe-to-solid alignment for all 10 shape types

**Independent Test**: Toggle F3, spawn each shape type, confirm wireframes exactly overlay solid bodies with no size mismatch

### Tests for User Story 2

- [x] T011 [P] [US2] Add DebugRenderer unit tests in tests/PhysicsViewer.Tests/SceneManagerTests.fs (or new DebugRendererTests.fs) — test that createWireframeEntity does not apply 1.02x scale to entity.Transform.Scale; test compound shape creates multiple child wireframe entities

### Implementation for User Story 2

- [x] T012 [US2] Remove 1.02x scale from debug wireframes in src/PhysicsViewer/Rendering/DebugRenderer.fs — delete lines setting `entity.Transform.Scale <- Vector3(1.02f, 1.02f, 1.02f)` in createWireframeEntity (line ~43-44), leave Scale at default Vector3.One
- [x] T013 [US2] Implement compound child wireframe rendering in src/PhysicsViewer/Rendering/DebugRenderer.fs — in createWireframeEntity, when body.Shape is Compound: iterate body.Shape.Compound.Children, for each child create a wireframe entity using primitiveType(child.Shape) and shapeSize(child.Shape), position at body.Position + child.LocalPosition (transformed by body.Orientation), apply child.LocalOrientation. Return list of entities (modify WireframeEntities to Map<string, Entity list> or use composite key like "bodyId_childN").
- [x] T014 [US2] Update DebugRenderer.fsi signature in src/PhysicsViewer/Rendering/DebugRenderer.fsi if DebugState type changes (e.g., WireframeEntities value type change)
- [x] T015 [US2] Update updateShapes in src/PhysicsViewer/Rendering/DebugRenderer.fs to handle multi-entity compounds when removing/updating stale entities
- [x] T016 [US2] Update setEnabled in src/PhysicsViewer/Rendering/DebugRenderer.fs to clean up all compound child entities when disabling debug view
- [x] T017 [US2] Verify debug wireframe tests pass — run `dotnet test tests/PhysicsViewer.Tests -p:StrideCompilerSkipBuild=true --filter "Debug"`

**Checkpoint**: Debug wireframes are pixel-accurate for all shapes including compound children. No artificial scaling.

---

## Phase 5: User Story 3 — Fullscreen Toggle (Priority: P2)

**Goal**: F11 toggles borderless windowed fullscreen; Escape returns to windowed; state persisted

**Independent Test**: Launch viewer, press F11, confirm borderless fullscreen. Press F11 again, confirm return to windowed. Press Escape while fullscreen, confirm return to windowed.

### Implementation for User Story 3

- [x] T018 [P] [US3] Create DisplayManager signature in src/PhysicsViewer/Settings/DisplayManager.fsi — define DisplayState record and functions: create (game: Game -> settings: ViewerSettings -> DisplayState), applySettings (DisplayState -> ViewerSettings -> DisplayState), toggleFullscreen (DisplayState -> DisplayState), currentSettings (DisplayState -> ViewerSettings)
- [x] T019 [P] [US3] Create DisplayManager implementation in src/PhysicsViewer/Settings/DisplayManager.fs — wrap Stride APIs: game.Window.FullscreenIsBorderlessWindow, game.Window.IsFullscreen, game.Window.IsBorderLess; store previous windowed size for restore; use GraphicsDeviceManager for resolution via game.GraphicsDeviceManager (or access through Game services)
- [x] T020 [US3] Integrate F11 fullscreen toggle in src/PhysicsViewer/Program.fs — in update function: check game.Input.IsKeyPressed(Keys.F11), call DisplayManager.toggleFullscreen, save settings via ViewerSettings.save. Also handle Escape key to exit fullscreen.
- [x] T021 [US3] Load and apply persisted fullscreen state on startup in src/PhysicsViewer/Program.fs — in start function: call ViewerSettings.load, then DisplayManager.create with loaded settings to apply saved fullscreen state
- [x] T022 [US3] Write DisplayManager unit tests in tests/PhysicsViewer.Tests/DisplayManagerTests.fs — test toggleFullscreen flips IsFullscreen, test currentSettings reflects state, test create applies initial settings

**Checkpoint**: Fullscreen toggle works via F11/Escape, persists across restarts.

---

## Phase 6: User Story 4 — Resolution Settings (Priority: P2)

**Goal**: User can select resolution from supported list via on-screen settings overlay; changes apply immediately and persist

**Independent Test**: Press F2, select a different resolution, confirm viewport resizes. Restart viewer, confirm resolution is restored.

### Implementation for User Story 4

- [x] T023 [P] [US4] Create SettingsOverlay signature in src/PhysicsViewer/Settings/SettingsOverlay.fsi — define OverlayState record, SettingsCategory enum (Display|Quality), and functions: create, isVisible, toggle, handleInput (game.Input -> OverlayState -> OverlayState * ViewerSettings option), render (DebugTextSystem -> OverlayState -> unit)
- [x] T024 [P] [US4] Create SettingsOverlay implementation in src/PhysicsViewer/Settings/SettingsOverlay.fs — text-based menu using DebugTextSystem.Print: list resolution options (1280×720, 1920×1080, 2560×1440, native), arrow keys navigate, Enter selects, Escape/F2 closes. Return updated ViewerSettings when a change is made.
- [x] T025 [US4] Add resolution change to DisplayManager in src/PhysicsViewer/Settings/DisplayManager.fs — implement applySettings to set GraphicsDeviceManager.PreferredBackBufferWidth/Height and call ApplyChanges()
- [x] T026 [US4] Integrate settings overlay in src/PhysicsViewer/Program.fs — in update function: check F2 for overlay toggle, pass input to SettingsOverlay.handleInput, on settings change call DisplayManager.applySettings and ViewerSettings.save. In update render: call SettingsOverlay.render when visible (suppress other input while overlay is open).
- [x] T027 [US4] Load and apply persisted resolution on startup in src/PhysicsViewer/Program.fs — in start function: apply loaded ViewerSettings resolution to DisplayManager
- [x] T027b [US4] Write SettingsOverlay and resolution integration tests in tests/PhysicsViewer.Tests/DisplayManagerTests.fs — test SettingsOverlay.handleInput cycles through resolution options, test DisplayManager.applySettings updates resolution values, test round-trip: change resolution → save → load → verify persisted

**Checkpoint**: Resolution selection works via F2 overlay, persists across restarts.

---

## Phase 7: User Story 5 — Quality Settings (Priority: P3)

**Goal**: User can adjust anti-aliasing, shadow quality, and texture filtering via the settings overlay; changes apply immediately and persist

**Independent Test**: Press F2, switch to quality settings, change anti-aliasing to X4, observe visual change. Restart, confirm setting persists.

### Implementation for User Story 5

- [x] T028 [US5] Add quality settings section to SettingsOverlay in src/PhysicsViewer/Settings/SettingsOverlay.fs — add Quality category with options: AntiAliasing (Off/X2/X4/X8), ShadowQuality (Off/Low/Medium/High), TextureFiltering (Point/Linear/Anisotropic), VSync (On/Off). Allow tab/category switching between Display and Quality.
- [x] T029 [US5] Implement quality settings application in DisplayManager in src/PhysicsViewer/Settings/DisplayManager.fs — MSAA: set GraphicsDeviceManager.PreferredMultisampleCount and ApplyChanges(); Shadows: find directional light entity, configure LightDirectionalShadowMap.Enabled and CascadeCount; TextureFiltering: configure sampler states on GraphicsDevice; VSync: set GraphicsDeviceManager.SynchronizeWithVerticalRetrace and ApplyChanges()
- [x] T030 [US5] Integrate quality settings persistence in src/PhysicsViewer/Program.fs — when SettingsOverlay returns updated quality settings, apply via DisplayManager and save via ViewerSettings
- [x] T031 [US5] Apply persisted quality settings on startup in src/PhysicsViewer/Program.fs — in start function: after scene setup (lights created), apply quality settings from loaded ViewerSettings
- [x] T031b [US5] Write quality settings tests in tests/PhysicsViewer.Tests/DisplayManagerTests.fs — test applySettings maps AntiAliasingLevel to correct MultisampleCount, test ShadowQuality maps to correct CascadeCount, test round-trip: change quality → save → load → verify persisted

**Checkpoint**: Quality settings adjustable in real-time via overlay, persisted across restarts.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Surface area baselines, cleanup, final validation

- [x] T032 Update surface area baselines in tests/PhysicsViewer.Tests/SurfaceAreaTests.fs — add baseline entries for ViewerSettings, DisplayManager, SettingsOverlay modules; update ShapeGeometry and DebugRenderer baselines if public API changed
- [x] T033 [P] Add structured logging for settings changes in src/PhysicsViewer/Program.fs — log resolution changes, fullscreen toggles, and quality setting modifications via ILogger
- [x] T034 [P] Handle edge case: near-zero shape dimensions in src/PhysicsViewer/Rendering/ShapeGeometry.fs — clamp minimum size to 0.01f to prevent invisible bodies
- [x] T034b [P] Handle edge case: resolution clamping in src/PhysicsViewer/Settings/DisplayManager.fs — if selected resolution exceeds display bounds, clamp to maximum supported and log a warning
- [x] T034c [P] Handle edge case: quality fallback in src/PhysicsViewer/Settings/DisplayManager.fs — if GPU does not support selected MSAA level, fall back to nearest supported level (query GraphicsDevice.Features)
- [x] T035 Build and run full test suite — `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`
- [x] T036 Run quickstart.md validation — start full stack, run a demo script, verify shape sizing visually, toggle F3/F2/F11

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS US3/US4/US5
- **US1 (Phase 3)**: Can start after Setup — **independent of Foundational phase**
- **US2 (Phase 4)**: Depends on US1 completion (needs corrected shapeSize values)
- **US3 (Phase 5)**: Depends on Foundational + Setup
- **US4 (Phase 6)**: Depends on Foundational + US3 (reuses DisplayManager)
- **US5 (Phase 7)**: Depends on US4 (extends SettingsOverlay and DisplayManager)
- **Polish (Phase 8)**: Depends on all story phases complete

### User Story Dependencies

- **US1 (P1)**: Independent — can start after Phase 1 Setup
- **US2 (P1)**: Depends on US1 (correct sizing needed for wireframe accuracy)
- **US3 (P2)**: Depends on Phase 2 Foundational (ViewerSettings)
- **US4 (P2)**: Depends on US3 (reuses DisplayManager, adds overlay)
- **US5 (P3)**: Depends on US4 (extends existing overlay + DisplayManager)

### Parallel Opportunities

- **Phase 1 + Phase 2**: Setup and Foundational can overlap (T001-T003 then T004-T006)
- **US1 ∥ Foundational**: US1 (sizing fix) has no dependency on ViewerSettings — can run in parallel with Phase 2
- **Within US2**: T011 (tests) can run parallel with nothing blocking
- **Within US3**: T018 and T019 (.fsi and .fs) can be written in parallel
- **Within US4**: T023 and T024 (.fsi and .fs) can be written in parallel
- **Phase 8**: T032 and T033 and T034 can all run in parallel

---

## Parallel Example: US1 + Foundational

```
# These can run simultaneously since US1 touches Rendering/ and Foundational touches Settings/:
Agent 1: T007 → T008 → T009 → T010 (US1: shape sizing fix)
Agent 2: T004 + T005 + T006 (Foundational: ViewerSettings)
```

## Parallel Example: Within US3

```
# .fsi and .fs can be authored in parallel:
Task: T018 "Create DisplayManager.fsi"
Task: T019 "Create DisplayManager.fs"
# Then sequential:
Task: T020 "Integrate F11 in Program.fs"
Task: T021 "Load settings on startup"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 3: US1 — Fix shape sizing
3. **STOP and VALIDATE**: Spawn shapes, verify correct visual dimensions
4. This alone resolves the primary reported bug

### Incremental Delivery

1. Setup → US1 (sizing fix) → US2 (wireframe fix) → **Core rendering is correct** (MVP)
2. Foundational (parallel with above) → US3 (fullscreen) → **Presentation mode ready**
3. US4 (resolution) → US5 (quality) → **Full settings suite**
4. Polish → **Release-ready**

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Cross-reference Stride.BepuPhysics.Debug source when implementing T008 (sizing fix)
- The shapeSize fix (T008) is the single highest-impact change — all other rendering improvements build on it
- Constitution Principle V: every new .fs module needs a .fsi file (handled in T004, T018, T023)
- Constitution Principle VI: test evidence required (handled in T007, T011, T022, T006)
