module PhysicsViewer.Tests.SurfaceAreaTests

open Xunit
open TestHelpers

let private anchorType = typeof<PhysicsViewer.SceneManager.SceneState>

[<Fact>]
let ``SceneManager public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsViewer.SceneManager"
        [ "applyNarration"; "applyState"; "applyWireframe"; "create"
          "isRunning"; "isWireframe"; "narrationText"; "simulationTime" ]

[<Fact>]
let ``ShapeGeometry public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsViewer.ShapeGeometry"
        [ "defaultColor"; "primitiveType"; "shapeSize" ]

[<Fact>]
let ``CameraController public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsViewer.CameraController"
        [ "applyInput"; "applySetCamera"; "applySetZoom"; "applyToCamera"
          "cancelMode"; "defaultCamera"; "isActive"; "position"
          "setMode"; "smoothstep"; "target"; "updateCameraMode"; "zoomLevel" ]

[<Fact>]
let ``ViewerClient public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsViewer.ViewerClient"
        [ "streamState"; "streamViewCommands" ]

[<Fact>]
let ``DebugRenderer public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsViewer.DebugRenderer"
        [ "create"; "isEnabled"; "setEnabled"; "updateConstraints"; "updateShapes" ]

[<Fact>]
let ``ViewerSettings public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsViewer.Settings.ViewerSettings"
        [ "defaultSettings"; "load"; "save" ]

[<Fact>]
let ``DisplayManager public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsViewer.Settings.DisplayManager"
        [ "applySettings"; "create"; "currentSettings"; "toggleFullscreen" ]

[<Fact>]
let ``SettingsOverlay public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsViewer.Settings.SettingsOverlay"
        [ "create"; "handleInput"; "isVisible"; "render"; "toggle" ]

[<Fact>]
let ``MeshResolver public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsViewer.Streaming.MeshResolver"
        [ "create"; "fetchMissing"; "processNewMeshes"; "resolve" ]
