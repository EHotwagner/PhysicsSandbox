/// Tests for constraint types (ball-socket, hinge, weld, distance limit, etc.).
module PhysicsSimulation.Tests.ConstraintTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld

type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere

let private makeBody id mass shape =
    let cmd = AddBody(Id = id, Mass = mass)
    cmd.Position <- Vec3(X = 0.0, Y = 5.0, Z = 0.0)
    cmd.Shape <- shape
    cmd

let private makeDynamic id =
    makeBody id 1.0 (Shape(Sphere = ProtoSphere(Radius = 0.5)))

let private makeConstraintCmd id bodyA bodyB constraintType =
    let cmd = AddConstraint(Id = id, BodyA = bodyA, BodyB = bodyB)
    cmd.Type <- constraintType
    cmd

let private makeBallSocketType () =
    let ct = ConstraintType()
    ct.BallSocket <- BallSocketConstraint()
    ct.BallSocket.LocalOffsetA <- Vec3(X = 0.0, Y = 0.5, Z = 0.0)
    ct.BallSocket.LocalOffsetB <- Vec3(X = 0.0, Y = -0.5, Z = 0.0)
    ct

// ─── T041a: Constraint Tests ────────────────────────────────────────────────

[<Fact>]
let ``addConstraint ball socket works`` () =
    let world = create ()
    try
        let _ = addBody world (makeDynamic "a")
        let _ = addBody world (makeDynamic "b")
        let ack = addConstraint world (makeConstraintCmd "c1" "a" "b" (makeBallSocketType ()))
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``addConstraint hinge works`` () =
    let world = create ()
    try
        let _ = addBody world (makeDynamic "a")
        let _ = addBody world (makeDynamic "b")
        let ct = ConstraintType()
        ct.Hinge <- HingeConstraint()
        ct.Hinge.LocalHingeAxisA <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
        ct.Hinge.LocalHingeAxisB <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
        let ack = addConstraint world (makeConstraintCmd "c1" "a" "b" ct)
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``addConstraint weld works`` () =
    let world = create ()
    try
        let _ = addBody world (makeDynamic "a")
        let _ = addBody world (makeDynamic "b")
        let ct = ConstraintType()
        ct.Weld <- WeldConstraint()
        let ack = addConstraint world (makeConstraintCmd "c1" "a" "b" ct)
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``addConstraint distance limit works`` () =
    let world = create ()
    try
        let _ = addBody world (makeDynamic "a")
        let _ = addBody world (makeDynamic "b")
        let ct = ConstraintType()
        ct.DistanceLimit <- DistanceLimitConstraint(MinDistance = 0.5, MaxDistance = 2.0)
        let ack = addConstraint world (makeConstraintCmd "c1" "a" "b" ct)
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``removeConstraint removes from state`` () =
    let world = create ()
    try
        let _ = addBody world (makeDynamic "a")
        let _ = addBody world (makeDynamic "b")
        let _ = addConstraint world (makeConstraintCmd "c1" "a" "b" (makeBallSocketType ()))
        let state1 = currentState world
        Assert.Equal(1, state1.Constraints.Count)

        let ack = removeConstraint world "c1"
        Assert.True(ack.Success, ack.Message)
        let state2 = currentState world
        Assert.Equal(0, state2.Constraints.Count)
    finally
        destroy world

[<Fact>]
let ``auto-remove constraint on body removal`` () =
    let world = create ()
    try
        let _ = addBody world (makeDynamic "a")
        let _ = addBody world (makeDynamic "b")
        let _ = addConstraint world (makeConstraintCmd "c1" "a" "b" (makeBallSocketType ()))

        let _ = removeBody world "a"
        let state = currentState world
        Assert.Equal(0, state.Constraints.Count)
    finally
        destroy world

[<Fact>]
let ``addConstraint with unknown body rejected`` () =
    let world = create ()
    try
        let _ = addBody world (makeDynamic "a")
        let ack = addConstraint world (makeConstraintCmd "c1" "a" "nonexistent" (makeBallSocketType ()))
        Assert.False(ack.Success)
        Assert.Contains("not found", ack.Message)
    finally
        destroy world

[<Fact>]
let ``addConstraint duplicate ID rejected`` () =
    let world = create ()
    try
        let _ = addBody world (makeDynamic "a")
        let _ = addBody world (makeDynamic "b")
        let _ = addConstraint world (makeConstraintCmd "c1" "a" "b" (makeBallSocketType ()))
        let ack = addConstraint world (makeConstraintCmd "c1" "a" "b" (makeBallSocketType ()))
        Assert.False(ack.Success)
        Assert.Contains("already exists", ack.Message)
    finally
        destroy world

[<Fact>]
let ``resetSimulation clears constraints`` () =
    let world = create ()
    try
        let _ = addBody world (makeDynamic "a")
        let _ = addBody world (makeDynamic "b")
        let _ = addConstraint world (makeConstraintCmd "c1" "a" "b" (makeBallSocketType ()))

        let _ = resetSimulation world
        let state = currentState world
        Assert.Equal(0, state.Constraints.Count)
    finally
        destroy world

[<Fact>]
let ``constraints appear in state`` () =
    let world = create ()
    try
        let _ = addBody world (makeDynamic "a")
        let _ = addBody world (makeDynamic "b")
        let _ = addConstraint world (makeConstraintCmd "c1" "a" "b" (makeBallSocketType ()))

        let state = currentState world
        Assert.Equal(1, state.Constraints.Count)
        Assert.Equal("c1", state.Constraints.[0].Id)
        Assert.Equal("a", state.Constraints.[0].BodyA)
        Assert.Equal("b", state.Constraints.[0].BodyB)
    finally
        destroy world
