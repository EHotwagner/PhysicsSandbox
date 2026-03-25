/// Tests for kinematic bodies, mass=0 dynamics, and static body behavior.
module PhysicsSimulation.Tests.KinematicBodyTests

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

// ─── T057a: Kinematic Body Tests ────────────────────────────────────────────

[<Fact>]
let ``kinematic body creation succeeds`` () =
    let world = create ()
    try
        let cmd = AddBody(Id = "kin1", Mass = 0.0, MotionType = BodyMotionType.Kinematic)
        cmd.Position <- Vec3(X = 0.0, Y = 5.0, Z = 0.0)
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let ack = addBody world cmd
        Assert.True(ack.Success, ack.Message)
        Assert.Contains("Kinematic", ack.Message)
    finally
        destroy world

[<Fact>]
let ``static body creation succeeds`` () =
    let world = create ()
    try
        let cmd = AddBody(Id = "stat1", Mass = 0.0, MotionType = BodyMotionType.Static)
        cmd.Position <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 1.0))
        let ack = addBody world cmd
        Assert.True(ack.Success, ack.Message)
        Assert.Contains("Static", ack.Message)
    finally
        destroy world

[<Fact>]
let ``plane shape auto-becomes static`` () =
    let world = create ()
    try
        let cmd = AddBody(Id = "plane1", Mass = 0.0)
        cmd.Position <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
        cmd.Shape <- Shape(Plane = Plane(Normal = Vec3(X = 0.0, Y = 1.0, Z = 0.0)))
        let ack = addBody world cmd
        Assert.True(ack.Success, ack.Message)
        Assert.Contains("Static", ack.Message)
    finally
        destroy world

[<Fact>]
let ``kinematic body reports correct motion type in state`` () =
    let world = create ()
    try
        let cmd = AddBody(Id = "kin1", Mass = 0.0, MotionType = BodyMotionType.Kinematic)
        cmd.Position <- Vec3(X = 0.0, Y = 5.0, Z = 0.0)
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let _ = addBody world cmd
        let state = currentState world
        Assert.Single(state.Bodies) |> ignore
        Assert.Equal(BodyMotionType.Kinematic, state.Bodies.[0].MotionType)
    finally
        destroy world

[<Fact>]
let ``kinematic body not affected by gravity`` () =
    let world = create ()
    try
        let cmd = AddBody(Id = "kin1", Mass = 0.0, MotionType = BodyMotionType.Kinematic)
        cmd.Position <- Vec3(X = 0.0, Y = 5.0, Z = 0.0)
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let _ = addBody world cmd
        setGravity world (Vec3(X = 0.0, Y = -9.81, Z = 0.0))

        // Step multiple times
        for _ in 1..10 do
            let _ = step world
            ()

        let state = currentState world
        // Kinematic body velocity should not be affected by gravity
        Assert.True(abs(state.Bodies.[0].Velocity.Y) < 0.01)
    finally
        destroy world

[<Fact>]
let ``setBodyPose updates kinematic body position`` () =
    let world = create ()
    try
        let cmd = AddBody(Id = "kin1", Mass = 0.0, MotionType = BodyMotionType.Kinematic)
        cmd.Position <- Vec3(X = 0.0, Y = 5.0, Z = 0.0)
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let _ = addBody world cmd

        let pose = SetBodyPose(BodyId = "kin1")
        pose.Position <- Vec3(X = 10.0, Y = 20.0, Z = 30.0)
        let ack = setBodyPose world pose
        Assert.True(ack.Success, ack.Message)

        let state = currentState world
        let body = state.Bodies.[0]
        Assert.True(abs(body.Position.X - 10.0) < 0.1)
        Assert.True(abs(body.Position.Y - 20.0) < 0.1)
    finally
        destroy world

[<Fact>]
let ``setBodyPose rejects static body`` () =
    let world = create ()
    try
        let cmd = AddBody(Id = "stat1", Mass = 0.0, MotionType = BodyMotionType.Static)
        cmd.Position <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 1.0))
        let _ = addBody world cmd

        let pose = SetBodyPose(BodyId = "stat1")
        pose.Position <- Vec3(X = 10.0, Y = 0.0, Z = 0.0)
        let ack = setBodyPose world pose
        Assert.False(ack.Success)
        Assert.Contains("static", ack.Message.ToLower())
    finally
        destroy world

[<Fact>]
let ``setBodyPose updates dynamic body position`` () =
    let world = create ()
    try
        let _ = addBody world (makeDynamic "dyn1")

        let pose = SetBodyPose(BodyId = "dyn1")
        pose.Position <- Vec3(X = 99.0, Y = 99.0, Z = 99.0)
        let ack = setBodyPose world pose
        Assert.True(ack.Success, ack.Message)

        let state = currentState world
        Assert.True(abs(state.Bodies.[0].Position.X - 99.0) < 0.1)
    finally
        destroy world

[<Fact>]
let ``backward compat mass zero default becomes static`` () =
    let world = create ()
    try
        // Default motion type (DYNAMIC/0) with mass=0 should fail for non-plane
        let cmd = AddBody(Id = "bad", Mass = 0.0)
        cmd.Position <- Vec3(X = 0.0, Y = 5.0, Z = 0.0)
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let ack = addBody world cmd
        // Mass=0 with dynamic type should be rejected
        Assert.False(ack.Success)
        Assert.Contains("positive", ack.Message)
    finally
        destroy world
