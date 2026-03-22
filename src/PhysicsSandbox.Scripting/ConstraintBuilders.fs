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

/// <summary>Builds a <c>RemoveConstraint</c> command to delete an existing constraint.</summary>
let makeRemoveConstraintCmd (constraintId: string) =
    let rc = RemoveConstraint()
    rc.ConstraintId <- constraintId
    let cmd = SimulationCommand()
    cmd.RemoveConstraint <- rc
    cmd
