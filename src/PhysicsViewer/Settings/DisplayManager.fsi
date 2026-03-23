module PhysicsViewer.Settings.DisplayManager

open Stride.Engine
open PhysicsViewer.Settings.ViewerSettings

/// Display manager state tracking window and graphics settings.
type DisplayState

/// Create initial display state from game and settings.
val create: Game -> ViewerSettings -> DisplayState

/// Apply settings changes (resolution, quality, etc.) to the game.
val applySettings: DisplayState -> ViewerSettings -> DisplayState

/// Toggle borderless windowed fullscreen.
val toggleFullscreen: DisplayState -> DisplayState

/// Get current settings from display state.
val currentSettings: DisplayState -> ViewerSettings
