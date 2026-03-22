module PhysicsSandbox.Scripting.Tests.Vec3BuildersTests

open Xunit
open PhysicsSandbox.Scripting.Vec3Builders

[<Fact>]
let ``toVec3 maps tuple components to Vec3 X Y Z`` () =
    let v = toVec3 (1.0, 2.0, 3.0)
    Assert.Equal(1.0, v.X)
    Assert.Equal(2.0, v.Y)
    Assert.Equal(3.0, v.Z)

[<Fact>]
let ``toVec3 handles zero values`` () =
    let v = toVec3 (0.0, 0.0, 0.0)
    Assert.Equal(0.0, v.X)
    Assert.Equal(0.0, v.Y)
    Assert.Equal(0.0, v.Z)

[<Fact>]
let ``toVec3 handles negative values`` () =
    let v = toVec3 (-1.5, -2.5, -3.5)
    Assert.Equal(-1.5, v.X)
    Assert.Equal(-2.5, v.Y)
    Assert.Equal(-3.5, v.Z)

[<Fact>]
let ``toTuple roundtrips with toVec3`` () =
    let original = (1.0, 2.0, 3.0)
    let result = toVec3 original |> toTuple
    Assert.Equal(original, result)
