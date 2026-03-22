module PhysicsSandbox.Scripting.Tests.CommandBuildersTests

open Xunit
open PhysicsSandbox.Scripting.CommandBuilders

[<Fact>]
let ``makeSphereCmd produces correct proto with sphere shape and position`` () =
    let cmd = makeSphereCmd "s1" (1.0, 2.0, 3.0) 0.5 1.0
    Assert.NotNull(cmd.AddBody)
    Assert.Equal("s1", cmd.AddBody.Id)
    Assert.Equal(1.0, cmd.AddBody.Position.X)
    Assert.Equal(2.0, cmd.AddBody.Position.Y)
    Assert.Equal(3.0, cmd.AddBody.Position.Z)
    Assert.NotNull(cmd.AddBody.Shape.Sphere)
    Assert.Equal(0.5, cmd.AddBody.Shape.Sphere.Radius)
    Assert.Equal(1.0, cmd.AddBody.Mass)

[<Fact>]
let ``makeBoxCmd produces correct proto with box halfExtents`` () =
    let cmd = makeBoxCmd "b1" (0.0, 5.0, 0.0) (1.0, 2.0, 3.0) 2.0
    Assert.NotNull(cmd.AddBody)
    Assert.Equal("b1", cmd.AddBody.Id)
    Assert.Equal(0.0, cmd.AddBody.Position.X)
    Assert.Equal(5.0, cmd.AddBody.Position.Y)
    Assert.Equal(0.0, cmd.AddBody.Position.Z)
    Assert.NotNull(cmd.AddBody.Shape.Box)
    Assert.Equal(1.0, cmd.AddBody.Shape.Box.HalfExtents.X)
    Assert.Equal(2.0, cmd.AddBody.Shape.Box.HalfExtents.Y)
    Assert.Equal(3.0, cmd.AddBody.Shape.Box.HalfExtents.Z)
    Assert.Equal(2.0, cmd.AddBody.Mass)

[<Fact>]
let ``makeImpulseCmd sets bodyId and impulse vector`` () =
    let cmd = makeImpulseCmd "body1" (10.0, 0.0, -5.0)
    Assert.NotNull(cmd.ApplyImpulse)
    Assert.Equal("body1", cmd.ApplyImpulse.BodyId)
    Assert.Equal(10.0, cmd.ApplyImpulse.Impulse.X)
    Assert.Equal(0.0, cmd.ApplyImpulse.Impulse.Y)
    Assert.Equal(-5.0, cmd.ApplyImpulse.Impulse.Z)

[<Fact>]
let ``makeTorqueCmd sets bodyId and torque vector`` () =
    let cmd = makeTorqueCmd "body2" (0.0, 1.0, 0.0)
    Assert.NotNull(cmd.ApplyTorque)
    Assert.Equal("body2", cmd.ApplyTorque.BodyId)
    Assert.Equal(0.0, cmd.ApplyTorque.Torque.X)
    Assert.Equal(1.0, cmd.ApplyTorque.Torque.Y)
    Assert.Equal(0.0, cmd.ApplyTorque.Torque.Z)
