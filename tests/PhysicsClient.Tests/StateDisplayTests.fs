module PhysicsClient.Tests.StateDisplayTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsClient.StateDisplay

[<Fact>]
let ``formatVec3 formats with 2 decimal places`` () =
    let v = Vec3()
    v.X <- 1.234
    v.Y <- -5.678
    v.Z <- 0.0
    let result = formatVec3 v
    Assert.Equal("(1.23, -5.68, 0.00)", result)

[<Fact>]
let ``formatVec3 handles null`` () =
    let result = formatVec3 (null :> obj :?> Vec3)
    Assert.Equal("(0.00, 0.00, 0.00)", result)

[<Fact>]
let ``velocityMagnitude computes correctly`` () =
    let v = Vec3()
    v.X <- 3.0
    v.Y <- 4.0
    v.Z <- 0.0
    let mag = velocityMagnitude v
    Assert.Equal(5.0, mag, 3)

[<Fact>]
let ``velocityMagnitude handles null`` () =
    let mag = velocityMagnitude (null :> obj :?> Vec3)
    Assert.Equal(0.0, mag)

[<Fact>]
let ``shapeDescription for sphere`` () =
    let body = Body()
    let shape = Shape()
    let sphere = Sphere()
    sphere.Radius <- 0.5
    shape.Sphere <- sphere
    body.Shape <- shape
    let desc = shapeDescription body
    Assert.Contains("Sphere", desc)
    Assert.Contains("0.50", desc)

[<Fact>]
let ``shapeDescription for box`` () =
    let body = Body()
    let shape = Shape()
    let box = Box()
    let he = Vec3()
    he.X <- 1.0; he.Y <- 2.0; he.Z <- 3.0
    box.HalfExtents <- he
    shape.Box <- box
    body.Shape <- shape
    let desc = shapeDescription body
    Assert.Contains("Box", desc)
