module PhysicsSandbox.Scripting.Tests.ConstraintBuilderTests

open Xunit
open PhysicsSandbox.Scripting.ConstraintBuilders

[<Fact>]
let ``makeDistanceSpringCmd produces correct proto with distance spring constraint`` () =
    let cmd = makeDistanceSpringCmd "ds1" "a" "b" (1.0, 0.0, 0.0) (0.0, 1.0, 0.0) 5.0
    Assert.NotNull(cmd.AddConstraint)
    Assert.Equal("ds1", cmd.AddConstraint.Id)
    Assert.Equal("a", cmd.AddConstraint.BodyA)
    Assert.Equal("b", cmd.AddConstraint.BodyB)
    Assert.NotNull(cmd.AddConstraint.Type.DistanceSpring)
    Assert.Equal(1.0, cmd.AddConstraint.Type.DistanceSpring.LocalOffsetA.X)
    Assert.Equal(1.0, cmd.AddConstraint.Type.DistanceSpring.LocalOffsetB.Y)
    Assert.Equal(5.0, cmd.AddConstraint.Type.DistanceSpring.TargetDistance)
    Assert.NotNull(cmd.AddConstraint.Type.DistanceSpring.Spring)

[<Fact>]
let ``makeSwingLimitCmd produces correct proto with swing limit constraint`` () =
    let cmd = makeSwingLimitCmd "sl1" "a" "b" (0.0, 1.0, 0.0) (0.0, 1.0, 0.0) 1.57
    Assert.NotNull(cmd.AddConstraint)
    Assert.Equal("sl1", cmd.AddConstraint.Id)
    Assert.Equal("a", cmd.AddConstraint.BodyA)
    Assert.Equal("b", cmd.AddConstraint.BodyB)
    Assert.NotNull(cmd.AddConstraint.Type.SwingLimit)
    Assert.Equal(1.0, cmd.AddConstraint.Type.SwingLimit.AxisLocalA.Y)
    Assert.Equal(1.0, cmd.AddConstraint.Type.SwingLimit.AxisLocalB.Y)
    Assert.Equal(1.57, cmd.AddConstraint.Type.SwingLimit.MaxSwingAngle)
    Assert.NotNull(cmd.AddConstraint.Type.SwingLimit.Spring)

[<Fact>]
let ``makeTwistLimitCmd produces correct proto with twist limit constraint`` () =
    let cmd = makeTwistLimitCmd "tl1" "a" "b" (1.0, 0.0, 0.0) (1.0, 0.0, 0.0) -0.5 0.5
    Assert.NotNull(cmd.AddConstraint)
    Assert.Equal("tl1", cmd.AddConstraint.Id)
    Assert.Equal("a", cmd.AddConstraint.BodyA)
    Assert.Equal("b", cmd.AddConstraint.BodyB)
    Assert.NotNull(cmd.AddConstraint.Type.TwistLimit)
    Assert.Equal(1.0, cmd.AddConstraint.Type.TwistLimit.LocalAxisA.X)
    Assert.Equal(1.0, cmd.AddConstraint.Type.TwistLimit.LocalAxisB.X)
    Assert.Equal(-0.5, cmd.AddConstraint.Type.TwistLimit.MinAngle)
    Assert.Equal(0.5, cmd.AddConstraint.Type.TwistLimit.MaxAngle)
    Assert.NotNull(cmd.AddConstraint.Type.TwistLimit.Spring)

[<Fact>]
let ``makeLinearAxisMotorCmd produces correct proto with linear axis motor constraint`` () =
    let cmd = makeLinearAxisMotorCmd "lam1" "a" "b" (0.0, 0.0, 0.0) (0.0, 1.0, 0.0) (0.0, 1.0, 0.0) 2.0 500.0
    Assert.NotNull(cmd.AddConstraint)
    Assert.Equal("lam1", cmd.AddConstraint.Id)
    Assert.Equal("a", cmd.AddConstraint.BodyA)
    Assert.Equal("b", cmd.AddConstraint.BodyB)
    Assert.NotNull(cmd.AddConstraint.Type.LinearAxisMotor)
    Assert.Equal(1.0, cmd.AddConstraint.Type.LinearAxisMotor.LocalOffsetB.Y)
    Assert.Equal(1.0, cmd.AddConstraint.Type.LinearAxisMotor.LocalAxis.Y)
    Assert.Equal(2.0, cmd.AddConstraint.Type.LinearAxisMotor.TargetVelocity)
    Assert.NotNull(cmd.AddConstraint.Type.LinearAxisMotor.Motor)
    Assert.Equal(500.0, cmd.AddConstraint.Type.LinearAxisMotor.Motor.MaxForce)

[<Fact>]
let ``makeAngularMotorCmd produces correct proto with angular motor constraint`` () =
    let cmd = makeAngularMotorCmd "am1" "a" "b" (0.0, 3.14, 0.0) 800.0
    Assert.NotNull(cmd.AddConstraint)
    Assert.Equal("am1", cmd.AddConstraint.Id)
    Assert.Equal("a", cmd.AddConstraint.BodyA)
    Assert.Equal("b", cmd.AddConstraint.BodyB)
    Assert.NotNull(cmd.AddConstraint.Type.AngularMotor)
    Assert.Equal(3.14, cmd.AddConstraint.Type.AngularMotor.TargetVelocity.Y)
    Assert.NotNull(cmd.AddConstraint.Type.AngularMotor.Motor)
    Assert.Equal(800.0, cmd.AddConstraint.Type.AngularMotor.Motor.MaxForce)

[<Fact>]
let ``makePointOnLineCmd produces correct proto with point on line constraint`` () =
    let cmd = makePointOnLineCmd "pol1" "a" "b" (0.0, 0.0, 0.0) (0.0, 1.0, 0.0) (0.0, 0.5, 0.0)
    Assert.NotNull(cmd.AddConstraint)
    Assert.Equal("pol1", cmd.AddConstraint.Id)
    Assert.Equal("a", cmd.AddConstraint.BodyA)
    Assert.Equal("b", cmd.AddConstraint.BodyB)
    Assert.NotNull(cmd.AddConstraint.Type.PointOnLine)
    Assert.Equal(1.0, cmd.AddConstraint.Type.PointOnLine.LocalDirection.Y)
    Assert.Equal(0.5, cmd.AddConstraint.Type.PointOnLine.LocalOffset.Y)
    Assert.NotNull(cmd.AddConstraint.Type.PointOnLine.Spring)
