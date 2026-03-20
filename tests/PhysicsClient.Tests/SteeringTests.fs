module PhysicsClient.Tests.SteeringTests

open Xunit
open PhysicsClient.Steering

[<Fact>]
let ``directionToVec Up maps to +Y`` () =
    let (x, y, z) = directionToVec Up
    Assert.Equal(0.0, x)
    Assert.Equal(1.0, y)
    Assert.Equal(0.0, z)

[<Fact>]
let ``directionToVec Down maps to -Y`` () =
    let (x, y, z) = directionToVec Down
    Assert.Equal(0.0, x)
    Assert.Equal(-1.0, y)
    Assert.Equal(0.0, z)

[<Fact>]
let ``directionToVec North maps to -Z`` () =
    let (x, y, z) = directionToVec North
    Assert.Equal(0.0, x)
    Assert.Equal(0.0, y)
    Assert.Equal(-1.0, z)

[<Fact>]
let ``directionToVec South maps to +Z`` () =
    let (x, y, z) = directionToVec South
    Assert.Equal(0.0, x)
    Assert.Equal(0.0, y)
    Assert.Equal(1.0, z)

[<Fact>]
let ``directionToVec East maps to +X`` () =
    let (x, y, z) = directionToVec East
    Assert.Equal(1.0, x)
    Assert.Equal(0.0, y)
    Assert.Equal(0.0, z)

[<Fact>]
let ``directionToVec West maps to -X`` () =
    let (x, y, z) = directionToVec West
    Assert.Equal(-1.0, x)
    Assert.Equal(0.0, y)
    Assert.Equal(0.0, z)
