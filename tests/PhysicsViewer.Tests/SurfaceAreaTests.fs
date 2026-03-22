module PhysicsViewer.Tests.SurfaceAreaTests

open System
open System.Reflection
open Xunit

let private getPublicMembers (moduleType: Type) =
    moduleType.GetMembers(BindingFlags.Public ||| BindingFlags.Static)
    |> Array.filter (fun m -> m.MemberType = MemberTypes.Method || m.MemberType = MemberTypes.Property)
    |> Array.map (fun m -> m.Name)
    |> Array.sort

let private viewerAssembly =
    typeof<PhysicsViewer.SceneManager.SceneState>.Assembly

[<Fact>]
let ``SceneManager public API matches baseline`` () =
    let t = viewerAssembly.GetType("PhysicsViewer.SceneManager")
    Assert.NotNull(t)
    let members = getPublicMembers t
    let expected = [|
        "applyState"; "applyWireframe"; "create"
        "isRunning"; "isWireframe"; "simulationTime"
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
        "defaultCamera"; "position"; "target"; "zoomLevel"
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
