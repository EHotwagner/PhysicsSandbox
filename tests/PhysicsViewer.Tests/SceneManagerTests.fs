module PhysicsViewer.Tests.SceneManagerTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsViewer.SceneManager

type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere
type ProtoBox = PhysicsSandbox.Shared.Contracts.Box

// ---------------------------------------------------------------------------
// classifyShape
// ---------------------------------------------------------------------------

[<Fact>]
let ``classifyShape returns Sphere for proto Sphere`` () =
    let shape = Shape(Sphere = ProtoSphere(Radius = 1.0))
    let result = classifyShape shape
    Assert.Equal(ShapeKind.Sphere, result)

[<Fact>]
let ``classifyShape returns Box for proto Box`` () =
    let shape = Shape(Box = ProtoBox(HalfExtents = Vec3(X = 1.0, Y = 1.0, Z = 1.0)))
    let result = classifyShape shape
    Assert.Equal(ShapeKind.Box, result)

[<Fact>]
let ``classifyShape returns Unknown for null shape`` () =
    let result = classifyShape (null)
    Assert.Equal(ShapeKind.Unknown, result)

[<Fact>]
let ``classifyShape returns Unknown for unset shape`` () =
    let shape = Shape()
    let result = classifyShape shape
    Assert.Equal(ShapeKind.Unknown, result)

// ---------------------------------------------------------------------------
// SceneState initial values
// ---------------------------------------------------------------------------

[<Fact>]
let ``create returns state with zero simulation time`` () =
    let state = create ()
    Assert.Equal(0.0, simulationTime state)

[<Fact>]
let ``create returns state with running false`` () =
    let state = create ()
    Assert.False(isRunning state)

[<Fact>]
let ``create returns state with wireframe false`` () =
    let state = create ()
    Assert.False(isWireframe state)
