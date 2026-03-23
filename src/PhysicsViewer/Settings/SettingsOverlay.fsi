module PhysicsViewer.Settings.SettingsOverlay

open Stride.Core.Mathematics
open Stride.Input
open Stride.Profiling
open PhysicsViewer.Settings.ViewerSettings

/// Settings category tab.
type SettingsCategory =
    | Display
    | Quality

/// Overlay UI state.
type OverlayState

/// Create initial overlay state (hidden).
val create: ViewerSettings -> OverlayState

/// Whether the overlay is currently visible.
val isVisible: OverlayState -> bool

/// Toggle overlay visibility.
val toggle: OverlayState -> OverlayState

/// Process keyboard input. Returns updated state and optionally changed settings.
val handleInput: InputManager -> OverlayState -> OverlayState * ViewerSettings option

/// Render the overlay text using DebugTextSystem.
val render: DebugTextSystem -> OverlayState -> unit
