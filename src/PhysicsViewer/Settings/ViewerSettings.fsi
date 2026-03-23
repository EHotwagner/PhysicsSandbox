module PhysicsViewer.Settings.ViewerSettings

/// MSAA anti-aliasing level.
type AntiAliasingLevel =
    | Off
    | X2
    | X4
    | X8

/// Shadow quality preset.
type ShadowQuality =
    | Off
    | Low
    | Medium
    | High

/// Texture filtering mode.
type TextureFilteringMode =
    | Point
    | Linear
    | Anisotropic

/// Persisted viewer display and quality settings.
type ViewerSettings =
    { ResolutionWidth: int
      ResolutionHeight: int
      IsFullscreen: bool
      AntiAliasing: AntiAliasingLevel
      ShadowQuality: ShadowQuality
      TextureFiltering: TextureFilteringMode
      VSync: bool }

/// Returns default settings (1280x720, windowed, AA off, medium shadows, linear filtering, vsync on).
val defaultSettings: unit -> ViewerSettings

/// Load settings from disk. Returns defaultSettings if file missing or corrupt.
val load: unit -> ViewerSettings

/// Save settings to disk (creates directory if needed).
val save: ViewerSettings -> unit
