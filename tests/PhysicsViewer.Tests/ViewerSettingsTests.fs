module PhysicsViewer.Tests.ViewerSettingsTests

open Xunit
open PhysicsViewer.Settings.ViewerSettings

[<Fact>]
let ``defaultSettings returns expected defaults`` () =
    let s = defaultSettings ()
    Assert.Equal(1280, s.ResolutionWidth)
    Assert.Equal(720, s.ResolutionHeight)
    Assert.False(s.IsFullscreen)
    Assert.Equal(AntiAliasingLevel.Off, s.AntiAliasing)
    Assert.Equal(ShadowQuality.Medium, s.ShadowQuality)
    Assert.Equal(TextureFilteringMode.Linear, s.TextureFiltering)
    Assert.True(s.VSync)

[<Fact>]
let ``load returns defaults when file does not exist`` () =
    let s = load ()
    Assert.Equal(1280, s.ResolutionWidth)

[<Fact>]
let ``save and load round-trip preserves settings`` () =
    let original =
        { ResolutionWidth = 1920
          ResolutionHeight = 1080
          IsFullscreen = true
          AntiAliasing = X4
          ShadowQuality = High
          TextureFiltering = Anisotropic
          VSync = false }
    save original
    let loaded = load ()
    Assert.Equal(original.ResolutionWidth, loaded.ResolutionWidth)
    Assert.Equal(original.ResolutionHeight, loaded.ResolutionHeight)
    Assert.Equal(original.IsFullscreen, loaded.IsFullscreen)
    Assert.True((original.AntiAliasing = loaded.AntiAliasing), "AntiAliasing mismatch")
    Assert.True((original.ShadowQuality = loaded.ShadowQuality), "ShadowQuality mismatch")
    Assert.True((original.TextureFiltering = loaded.TextureFiltering), "TextureFiltering mismatch")
    Assert.Equal(original.VSync, loaded.VSync)
    // Cleanup: restore defaults
    save (defaultSettings ())
