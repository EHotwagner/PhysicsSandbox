module PhysicsViewer.Tests.SurfaceAreaTests

open Xunit
open TestHelpers

let private viewerAssembly =
    typeof<PhysicsViewer.SceneManager.SceneState>.Assembly

[<Fact>]
let ``SceneManager public API matches baseline`` () =
    let t = viewerAssembly.GetType("PhysicsViewer.SceneManager")
    Assert.NotNull(t)
    let members = getPublicMembers t
    let expected = [|
        "applyNarration"; "applyState"; "applyWireframe"; "create"
        "isRunning"; "isWireframe"; "narrationText"; "simulationTime"
    |]
    for name in expected do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

[<Fact>]
let ``ShapeGeometry public API matches baseline`` () =
    let t = viewerAssembly.GetType("PhysicsViewer.ShapeGeometry")
    Assert.NotNull(t)
    let members = getPublicMembers t
    let expected = [| "defaultColor"; "primitiveType"; "shapeSize" |]
    for name in expected do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

[<Fact>]
let ``CameraController public API matches baseline`` () =
    let t = viewerAssembly.GetType("PhysicsViewer.CameraController")
    Assert.NotNull(t)
    let members = getPublicMembers t
    let expected = [|
        "applyInput"; "applySetCamera"; "applySetZoom"; "applyToCamera"
        "cancelMode"; "defaultCamera"; "isActive"; "position"
        "setMode"; "smoothstep"; "target"; "updateCameraMode"; "zoomLevel"
    |]
    for name in expected do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

[<Fact>]
let ``ViewerClient public API matches baseline`` () =
    let t = viewerAssembly.GetType("PhysicsViewer.ViewerClient")
    Assert.NotNull(t)
    let members = getPublicMembers t
    let expected = [| "streamState"; "streamViewCommands" |]
    for name in expected do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

[<Fact>]
let ``DebugRenderer public API matches baseline`` () =
    let t = viewerAssembly.GetType("PhysicsViewer.DebugRenderer")
    Assert.NotNull(t)
    let members = getPublicMembers t
    let expected = [| "create"; "isEnabled"; "setEnabled"; "updateConstraints"; "updateShapes" |]
    for name in expected do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

[<Fact>]
let ``ViewerSettings public API matches baseline`` () =
    let t = viewerAssembly.GetType("PhysicsViewer.Settings.ViewerSettings")
    Assert.NotNull(t)
    let members = getPublicMembers t
    let expected = [| "defaultSettings"; "load"; "save" |]
    for name in expected do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

[<Fact>]
let ``DisplayManager public API matches baseline`` () =
    let t = viewerAssembly.GetType("PhysicsViewer.Settings.DisplayManager")
    Assert.NotNull(t)
    let members = getPublicMembers t
    let expected = [| "applySettings"; "create"; "currentSettings"; "toggleFullscreen" |]
    for name in expected do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

[<Fact>]
let ``SettingsOverlay public API matches baseline`` () =
    let t = viewerAssembly.GetType("PhysicsViewer.Settings.SettingsOverlay")
    Assert.NotNull(t)
    let members = getPublicMembers t
    let expected = [| "create"; "handleInput"; "isVisible"; "render"; "toggle" |]
    for name in expected do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

[<Fact>]
let ``MeshResolver public API matches baseline`` () =
    let t = viewerAssembly.GetType("PhysicsViewer.Streaming.MeshResolver")
    Assert.NotNull(t)
    let members = getPublicMembers t
    for name in [| "create"; "fetchMissing"; "processNewMeshes"; "resolve" |] do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")
