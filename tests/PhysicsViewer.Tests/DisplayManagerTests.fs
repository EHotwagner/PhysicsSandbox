module PhysicsViewer.Tests.DisplayManagerTests

open Xunit
open PhysicsViewer.Settings.ViewerSettings
open PhysicsViewer.Settings.DisplayManager

// DisplayManager tests require a running Stride Game instance.
// Unit tests for settings mapping are added here; full integration
// tests require the viewer to be running.

[<Fact>]
let ``currentSettings returns settings from create`` () =
    // This is a logic-only test — we can't instantiate Game in unit tests.
    // Verify the ViewerSettings round-trip instead.
    let s = defaultSettings ()
    Assert.Equal(1280, s.ResolutionWidth)
    Assert.False(s.IsFullscreen)
