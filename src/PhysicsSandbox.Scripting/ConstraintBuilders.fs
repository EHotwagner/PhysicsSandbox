/// <summary>Factory functions for constructing constraint-related <c>SimulationCommand</c> messages from simple F# values.</summary>
/// <remarks>
/// These builders create the nested proto message hierarchy (ConstraintType → AddConstraint → SimulationCommand)
/// that the gRPC API requires. Use <see cref="M:PhysicsSandbox.Scripting.BatchOperations.batchAdd"/>
/// to send the resulting commands efficiently.
/// </remarks>
module PhysicsSandbox.Scripting.ConstraintBuilders

open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Scripting.Vec3Builders

/// Default spring settings for constraints: stiff but not infinitely rigid.
let private defaultSpring () =
    let s = SpringSettings()
    s.Frequency <- 30.0
    s.DampingRatio <- 1.0
    s

/// <summary>Builds an <c>AddConstraint</c> command for a ball-socket joint — allows free rotation around the anchor point.</summary>
let makeBallSocketCmd (id: string) (bodyA: string) (bodyB: string) (offsetA: float * float * float) (offsetB: float * float * float) =
    let bs = BallSocketConstraint()
    bs.LocalOffsetA <- toVec3 offsetA
    bs.LocalOffsetB <- toVec3 offsetB
    bs.Spring <- defaultSpring ()
    let ct = ConstraintType()
    ct.BallSocket <- bs
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

/// <summary>Builds an <c>AddConstraint</c> command for a hinge joint — allows rotation around a single axis.</summary>
let makeHingeCmd (id: string) (bodyA: string) (bodyB: string) (axis: float * float * float) (offsetA: float * float * float) (offsetB: float * float * float) =
    let h = HingeConstraint()
    h.LocalHingeAxisA <- toVec3 axis
    h.LocalHingeAxisB <- toVec3 axis
    h.LocalOffsetA <- toVec3 offsetA
    h.LocalOffsetB <- toVec3 offsetB
    h.Spring <- defaultSpring ()
    let ct = ConstraintType()
    ct.Hinge <- h
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

/// <summary>Builds an <c>AddConstraint</c> command for a weld joint — rigidly fixes two bodies together.</summary>
let makeWeldCmd (id: string) (bodyA: string) (bodyB: string) =
    let w = WeldConstraint()
    w.LocalOffset <- toVec3 (0.0, 0.0, 0.0)
    let orient = Vec4()
    orient.X <- 0.0
    orient.Y <- 0.0
    orient.Z <- 0.0
    orient.W <- 1.0
    w.LocalOrientation <- orient
    w.Spring <- defaultSpring ()
    let ct = ConstraintType()
    ct.Weld <- w
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

/// <summary>Builds an <c>AddConstraint</c> command for a distance limit — keeps two bodies within a distance range.</summary>
let makeDistanceLimitCmd (id: string) (bodyA: string) (bodyB: string) (minDist: float) (maxDist: float) =
    let dl = DistanceLimitConstraint()
    dl.LocalOffsetA <- toVec3 (0.0, 0.0, 0.0)
    dl.LocalOffsetB <- toVec3 (0.0, 0.0, 0.0)
    dl.MinDistance <- minDist
    dl.MaxDistance <- maxDist
    dl.Spring <- defaultSpring ()
    let ct = ConstraintType()
    ct.DistanceLimit <- dl
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

/// Default motor settings for motor constraints.
let private defaultMotor () =
    let m = MotorConfig()
    m.MaxForce <- 1000.0
    m.Damping <- 1.0
    m

/// <summary>Builds an <c>AddConstraint</c> command for a distance spring — pulls two bodies toward a target distance.</summary>
let makeDistanceSpringCmd (id: string) (bodyA: string) (bodyB: string) (offsetA: float * float * float) (offsetB: float * float * float) (targetDistance: float) =
    let ds = DistanceSpringConstraint()
    ds.LocalOffsetA <- toVec3 offsetA
    ds.LocalOffsetB <- toVec3 offsetB
    ds.TargetDistance <- targetDistance
    ds.Spring <- defaultSpring ()
    let ct = ConstraintType()
    ct.DistanceSpring <- ds
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

/// <summary>Builds an <c>AddConstraint</c> command for a swing limit — limits the angle between two axes.</summary>
let makeSwingLimitCmd (id: string) (bodyA: string) (bodyB: string) (axisA: float * float * float) (axisB: float * float * float) (maxAngle: float) =
    let sl = SwingLimitConstraint()
    sl.AxisLocalA <- toVec3 axisA
    sl.AxisLocalB <- toVec3 axisB
    sl.MaxSwingAngle <- maxAngle
    sl.Spring <- defaultSpring ()
    let ct = ConstraintType()
    ct.SwingLimit <- sl
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

/// <summary>Builds an <c>AddConstraint</c> command for a twist limit — limits rotation around an axis to an angle range.</summary>
let makeTwistLimitCmd (id: string) (bodyA: string) (bodyB: string) (axisA: float * float * float) (axisB: float * float * float) (minAngle: float) (maxAngle: float) =
    let tl = TwistLimitConstraint()
    tl.LocalAxisA <- toVec3 axisA
    tl.LocalAxisB <- toVec3 axisB
    tl.MinAngle <- minAngle
    tl.MaxAngle <- maxAngle
    tl.Spring <- defaultSpring ()
    let ct = ConstraintType()
    ct.TwistLimit <- tl
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

/// <summary>Builds an <c>AddConstraint</c> command for a linear axis motor — drives linear motion along an axis.</summary>
let makeLinearAxisMotorCmd (id: string) (bodyA: string) (bodyB: string) (offsetA: float * float * float) (offsetB: float * float * float) (axis: float * float * float) (targetVelocity: float) (maxForce: float) =
    let lam = LinearAxisMotorConstraint()
    lam.LocalOffsetA <- toVec3 offsetA
    lam.LocalOffsetB <- toVec3 offsetB
    lam.LocalAxis <- toVec3 axis
    lam.TargetVelocity <- targetVelocity
    let motor = MotorConfig()
    motor.MaxForce <- maxForce
    motor.Damping <- 1.0
    lam.Motor <- motor
    let ct = ConstraintType()
    ct.LinearAxisMotor <- lam
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

/// <summary>Builds an <c>AddConstraint</c> command for an angular motor — drives rotation around axes.</summary>
let makeAngularMotorCmd (id: string) (bodyA: string) (bodyB: string) (targetVelocity: float * float * float) (maxForce: float) =
    let am = AngularMotorConstraint()
    am.TargetVelocity <- toVec3 targetVelocity
    let motor = MotorConfig()
    motor.MaxForce <- maxForce
    motor.Damping <- 1.0
    am.Motor <- motor
    let ct = ConstraintType()
    ct.AngularMotor <- am
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

/// <summary>Builds an <c>AddConstraint</c> command for a point-on-line constraint — constrains a point to slide along a line.</summary>
let makePointOnLineCmd (id: string) (bodyA: string) (bodyB: string) (origin: float * float * float) (direction: float * float * float) (offset: float * float * float) =
    let pol = PointOnLineConstraint()
    pol.LocalOrigin <- toVec3 origin
    pol.LocalDirection <- toVec3 direction
    pol.LocalOffset <- toVec3 offset
    pol.Spring <- defaultSpring ()
    let ct = ConstraintType()
    ct.PointOnLine <- pol
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

/// <summary>Builds a <c>RemoveConstraint</c> command to delete an existing constraint.</summary>
let makeRemoveConstraintCmd (constraintId: string) =
    let rc = RemoveConstraint()
    rc.ConstraintId <- constraintId
    let cmd = SimulationCommand()
    cmd.RemoveConstraint <- rc
    cmd
