# Feature Specification: Viewer Display Settings & Shape Sizing Fix

**Feature Branch**: `005-viewer-settings-sizing-fix`
**Created**: 2026-03-23
**Status**: Planned
**Input**: User description: "Improve the viewer by enabling fullscreen, different resolutions, and basic quality settings. Fix bug where objects like balls are rendered with wrong size, causing them to merge visually. Use Stride.BepuPhysics.Debug source code as correctness reference for shape-to-rendering dimension mapping (cannot be used directly — it requires native BepuPhysics objects not available in the gRPC-based viewer)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fix Shape Sizing So Bodies Render at Correct Dimensions (Priority: P1)

A user runs the physics sandbox and observes bodies in the 3D viewer. Currently, some shapes (particularly spheres) appear to overlap or merge visually even when they are physically separate. The viewer must render each body at its true physics dimensions so that the visual representation faithfully matches the simulation state.

**Why this priority**: This is a correctness bug. If the viewer misrepresents body sizes, users cannot trust visual output for debugging, demos, or presentations. All other viewer improvements are diminished if the core rendering is inaccurate.

**Independent Test**: Spawn a row of spheres with known radii and spacing via a demo script, then visually confirm in the viewer that spheres appear correctly sized and do not overlap when they shouldn't.

**Acceptance Scenarios**:

1. **Given** a sphere with radius 0.5 is spawned at position (0, 5, 0), **When** the viewer renders it, **Then** its visual diameter matches the physics diameter (1.0 unit) and the debug wireframe overlay confirms the correct bounds.
2. **Given** two boxes of half-extents (1, 1, 1) are placed side by side with a 0.1-unit gap, **When** the viewer renders them, **Then** a visible gap is present between the boxes.
3. **Given** a capsule with radius 0.3 and length 1.0, **When** the viewer renders it, **Then** the visual capsule height equals length + 2×radius (1.6 units) and width equals 2×radius (0.6 units).
4. **Given** a debug wireframe view is toggled on (F3), **When** comparing wireframe outlines to solid body renders, **Then** wireframes tightly enclose the solid body with no visible size mismatch.

---

### User Story 2 - Debug Wireframe Visualization Accurately Represents Physics Bounds (Priority: P1)

A user toggles the debug view (F3) to inspect physics body boundaries. The debug wireframes must show the exact collision shapes used by the physics engine, providing a reliable ground-truth overlay for diagnosing visual-physics discrepancies.

**Why this priority**: Debug visualization is the primary diagnostic tool. If it is inaccurate, users have no way to verify whether visual rendering matches physics — making it impossible to diagnose the sizing bug or any future rendering issues.

**Independent Test**: Toggle F3 debug view, spawn various shape types, and confirm wireframes precisely match the physics collision geometry for each shape type.

**Acceptance Scenarios**:

1. **Given** the debug wireframe view is active, **When** bodies are spawned, **Then** each wireframe outline matches the physics collision shape dimensions exactly (not artificially scaled) for all 10 supported shape types.
2. **Given** a compound shape, **When** debug view is active, **Then** the wireframe renders each child shape individually at its correct position, orientation, and dimensions within the compound.
3. **Given** a convex hull or mesh shape, **When** debug view is active, **Then** the wireframe renders a correctly-dimensioned bounding box derived from the hull points or mesh vertices (procedural mesh wireframes deferred — significant scope).

---

### User Story 3 - Toggle Fullscreen Mode (Priority: P2)

A user wants to view the physics simulation on their full screen for presentations or better spatial awareness. They press a key (e.g., F11) to toggle between windowed and fullscreen mode.

**Why this priority**: Fullscreen is a standard viewer capability that significantly improves the visual experience, especially for demos and presentations.

**Independent Test**: Launch the viewer in windowed mode, press the fullscreen toggle key, confirm the window expands to fill the screen. Press again to return to windowed mode.

**Acceptance Scenarios**:

1. **Given** the viewer is running in windowed mode, **When** the user presses the fullscreen toggle key, **Then** the viewer switches to borderless windowed fullscreen at the display's native resolution.
2. **Given** the viewer is in fullscreen mode, **When** the user presses the fullscreen toggle key again, **Then** the viewer returns to its previous windowed size and position.
3. **Given** the viewer is in fullscreen mode, **When** the user presses Escape, **Then** the viewer returns to windowed mode.
4. **Given** the viewer is in borderless windowed fullscreen, **When** the user alt-tabs to another application, **Then** the transition is seamless with no resolution switching or display flicker.

---

### User Story 4 - Change Display Resolution (Priority: P2)

A user wants to adjust the viewer resolution — for example, to reduce GPU load on lower-end hardware or to increase visual clarity on high-resolution displays.

**Why this priority**: Resolution control is a basic display setting that enables the viewer to work well across different hardware configurations.

**Independent Test**: Open the settings interface, select a different resolution, confirm the viewport updates to the new resolution.

**Acceptance Scenarios**:

1. **Given** the viewer is running, **When** the user opens display settings, **Then** a list of supported resolutions is shown (at minimum: 1280×720, 1920×1080, 2560×1440, and the display's native resolution).
2. **Given** the user selects a new resolution, **When** the change is applied, **Then** the viewport resizes to the selected resolution without requiring a restart.
3. **Given** the user selects a resolution, **When** the viewer is restarted, **Then** the previously selected resolution is restored.

---

### User Story 5 - Adjust Basic Quality Settings (Priority: P3)

A user wants to tune rendering quality to balance visual fidelity against performance. Basic settings such as shadow quality, anti-aliasing, and texture filtering should be adjustable.

**Why this priority**: Quality settings let users optimize the viewer for their hardware. Lower priority because the viewer is primarily a diagnostic/debugging tool, but still valuable for presentations and screenshots.

**Independent Test**: Open quality settings, change anti-aliasing level, observe the visual difference in the viewport.

**Acceptance Scenarios**:

1. **Given** the viewer is running, **When** the user opens quality settings, **Then** settings for anti-aliasing, shadow quality, and texture filtering are available.
2. **Given** the user changes a quality setting, **When** the change is applied, **Then** the visual output updates immediately without requiring a restart.
3. **Given** the user adjusts quality settings, **When** the viewer is restarted, **Then** the previously selected quality settings are restored.

---

### Edge Cases

- What happens when the user selects a resolution larger than the display supports? The viewer should clamp to the maximum supported resolution and inform the user.
- What happens when fullscreen is toggled while the viewer is mid-render update? The transition should complete gracefully without crashes or visual corruption.
- What happens when a body has zero or near-zero dimensions (e.g., a sphere with radius 0.001)? The viewer should still render a visible indicator, not an invisible dot.
- What happens when quality settings are set to minimum and many bodies are present? The viewer should maintain usable frame rates (above the existing 30 FPS warning threshold).
- What happens if the user's GPU does not support a selected quality level? The viewer should fall back to the nearest supported level.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Viewer MUST render all physics body shapes at their exact physics dimensions (no scaling errors between physics and visual representation).
- **FR-002**: Debug wireframe visualization MUST accurately represent the physics collision boundaries for all supported shape types (sphere, box, capsule, cylinder, triangle, convex hull, compound, mesh, plane).
- **FR-003**: Viewer MUST provide a fullscreen toggle via keyboard shortcut (F11) that switches between windowed and borderless windowed fullscreen modes (not exclusive fullscreen).
- **FR-004**: Viewer MUST allow the user to select from a set of supported display resolutions.
- **FR-005**: Viewer MUST provide basic quality settings: anti-aliasing level, shadow quality, and texture filtering quality.
- **FR-006**: Display and quality settings MUST persist across viewer restarts.
- **FR-007**: All settings changes (resolution, quality) MUST apply immediately without requiring a viewer restart.
- **FR-008**: Viewer MUST provide an on-screen settings interface accessible via a keyboard shortcut.
- **FR-009**: Debug wireframe overlay MUST render at the same dimensions as the solid body (no artificial scaling offset).

### Key Entities

- **Display Settings**: Resolution (width × height), fullscreen mode (on/off), persisted per user session.
- **Quality Settings**: Anti-aliasing level (Off/X2/X4/X8), shadow quality (Off/Low/Medium/High), texture filtering (Point/Linear/Anisotropic).
- **Shape Dimensions**: The physics-authoritative size of each body shape, used as the single source of truth for both solid rendering and debug wireframe overlay.

## Clarifications

### Session 2026-03-23

- Q: Should debug wireframe rendering cover all 10 shape types (including accurate compound child rendering and convex hull mesh wireframes), or defer complex shapes? → A: All 10 shape types. Compounds use per-child rendering. Convex hulls and meshes use correctly-dimensioned bounding boxes (procedural mesh wireframes deferred — significant scope).
- Q: Should fullscreen be exclusive (takes over display) or borderless windowed (seamless alt-tab)? → A: Borderless windowed fullscreen.

## Assumptions

- The settings interface will use a simple on-screen overlay (e.g., toggled via F2 or a designated key) rather than a separate window or external configuration file, since the viewer is primarily a diagnostic tool.
- Resolution options will be derived from the display's supported modes plus common standard resolutions.
- Settings will be persisted to a local file in the user's config directory.
- The shape sizing bug is in the translation layer between physics dimensions and the rendering primitives, not in the physics engine itself.
- Stride.BepuPhysics.Debug cannot be integrated directly because the viewer receives physics data via gRPC and does not have access to native BepuPhysics objects. Its open-source code serves as the correctness reference for how shape dimensions should map to Stride rendering primitives.
- The 1.02x scaling currently applied to debug wireframes is intentional for overlay visibility but should be revisited if it causes confusion about actual physics bounds.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 10 supported shape types render at visually correct dimensions — verified by spawning shapes with known sizes and confirming no visual overlap where physics bodies are separated.
- **SC-002**: Debug wireframe outlines align with solid body edges to within 1 pixel at default camera distance for all shape types.
- **SC-003**: Fullscreen toggle responds within 0.5 seconds of keypress with no visual corruption.
- **SC-004**: Resolution changes apply within 1 second without requiring a restart.
- **SC-005**: Quality setting changes produce a visible difference in rendering output and apply within 1 second.
- **SC-006**: Settings persist across viewer restarts — user does not need to reconfigure after closing and reopening.
- **SC-007**: Viewer maintains above 30 FPS at minimum quality settings with 100 active bodies on supported hardware.
