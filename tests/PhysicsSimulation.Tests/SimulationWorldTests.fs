module PhysicsSimulation.Tests.SimulationWorldTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld

type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere
type ProtoBox = PhysicsSandbox.Shared.Contracts.Box

let private makeSphereBody id mass radius =
    let cmd = AddBody(Id = id, Mass = mass)
    cmd.Position <- Vec3(X = 0.0, Y = 5.0, Z = 0.0)
    cmd.Velocity <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = radius))
    cmd

let private makeBoxBody id mass =
    let cmd = AddBody(Id = id, Mass = mass)
    cmd.Position <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    cmd.Shape <- Shape(Box = ProtoBox(HalfExtents = Vec3(X = 0.5, Y = 0.5, Z = 0.5)))
    cmd

[<Fact>]
let ``create returns paused world with time zero`` () =
    let world = create ()
    try
        Assert.False(isRunning world)
        Assert.Equal(0.0, time world)
    finally
        destroy world

[<Fact>]
let ``step advances time by one timestep`` () =
    let world = create ()
    try
        let state = step world
        Assert.True(time world > 0.0)
        Assert.Equal(time world, state.Time)
    finally
        destroy world

[<Fact>]
let ``step returns SimulationState with running flag`` () =
    let world = create ()
    try
        let state1 = step world
        Assert.False(state1.Running)

        setRunning world true
        let state2 = step world
        Assert.True(state2.Running)
    finally
        destroy world

[<Fact>]
let ``isRunning returns false initially`` () =
    let world = create ()
    try
        Assert.False(isRunning world)
    finally
        destroy world

[<Fact>]
let ``currentState returns valid state without stepping`` () =
    let world = create ()
    try
        let state = currentState world
        Assert.Equal(0.0, state.Time)
        Assert.False(state.Running)
        Assert.Empty(state.Bodies)
    finally
        destroy world

[<Fact>]
let ``setRunning toggles play pause`` () =
    let world = create ()
    try
        setRunning world true
        Assert.True(isRunning world)
        setRunning world false
        Assert.False(isRunning world)
    finally
        destroy world

// ─── US2: Body Management ──────────────────────────────────────────────────

[<Fact>]
let ``addBody with sphere returns success and body in state`` () =
    let world = create ()
    try
        let ack = addBody world (makeSphereBody "ball1" 1.0 0.5)
        Assert.True(ack.Success)
        let state = currentState world
        Assert.Single(state.Bodies) |> ignore
        Assert.Equal("ball1", state.Bodies.[0].Id)
    finally
        destroy world

[<Fact>]
let ``addBody with box shape works`` () =
    let world = create ()
    try
        let ack = addBody world (makeBoxBody "box1" 2.0)
        Assert.True(ack.Success)
        let state = currentState world
        Assert.Single(state.Bodies) |> ignore
    finally
        destroy world

[<Fact>]
let ``addBody with duplicate ID returns error`` () =
    let world = create ()
    try
        let _ = addBody world (makeSphereBody "ball1" 1.0 0.5)
        let ack = addBody world (makeSphereBody "ball1" 2.0 0.3)
        Assert.False(ack.Success)
        Assert.Contains("already exists", ack.Message)
    finally
        destroy world

[<Fact>]
let ``addBody with zero mass returns error`` () =
    let world = create ()
    try
        let ack = addBody world (makeSphereBody "bad" 0.0 0.5)
        Assert.False(ack.Success)
        Assert.Contains("positive", ack.Message)
    finally
        destroy world

[<Fact>]
let ``addBody with negative mass returns error`` () =
    let world = create ()
    try
        let ack = addBody world (makeSphereBody "bad" -1.0 0.5)
        Assert.False(ack.Success)
    finally
        destroy world

[<Fact>]
let ``removeBody removes body from state`` () =
    let world = create ()
    try
        let _ = addBody world (makeSphereBody "ball1" 1.0 0.5)
        let ack = removeBody world "ball1"
        Assert.True(ack.Success)
        let state = currentState world
        Assert.Empty(state.Bodies)
    finally
        destroy world

[<Fact>]
let ``removeBody with non-existent ID returns success`` () =
    let world = create ()
    try
        let ack = removeBody world "nonexistent"
        Assert.True(ack.Success)
    finally
        destroy world

[<Fact>]
let ``step includes body positions in state`` () =
    let world = create ()
    try
        let _ = addBody world (makeSphereBody "ball1" 1.0 0.5)
        let state = step world
        Assert.Single(state.Bodies) |> ignore
        Assert.Equal("ball1", state.Bodies.[0].Id)
        Assert.True(state.Bodies.[0].Mass > 0.0)
    finally
        destroy world

// ─── US3: Force, Torque, Impulse ───────────────────────────────────────────

[<Fact>]
let ``applyForce stores persistent force`` () =
    let world = create ()
    try
        let _ = addBody world (makeSphereBody "ball1" 1.0 0.5)
        let force = Vec3(X = 10.0, Y = 0.0, Z = 0.0)
        let ack = applyForce world "ball1" force
        Assert.True(ack.Success)
        let state = step world
        Assert.True(state.Bodies.[0].Velocity.X > 0.0)
    finally
        destroy world

[<Fact>]
let ``applyImpulse is one-shot`` () =
    let world = create ()
    try
        let _ = addBody world (makeSphereBody "ball1" 1.0 0.5)
        let impulse = Vec3(X = 5.0, Y = 0.0, Z = 0.0)
        let ack = applyImpulse world "ball1" impulse
        Assert.True(ack.Success)
        let state1 = step world
        let v1 = state1.Bodies.[0].Velocity.X
        Assert.True(v1 > 0.0)
        let state2 = step world
        let v2 = state2.Bodies.[0].Velocity.X
        Assert.True(abs(v2 - v1) < 0.1)
    finally
        destroy world

[<Fact>]
let ``clearForces stops acceleration`` () =
    let world = create ()
    try
        let _ = addBody world (makeSphereBody "ball1" 1.0 0.5)
        let force = Vec3(X = 10.0, Y = 0.0, Z = 0.0)
        let _ = applyForce world "ball1" force
        let _ = step world
        let ack = clearForces world "ball1"
        Assert.True(ack.Success)
        let state1 = step world
        let v1 = state1.Bodies.[0].Velocity.X
        let state2 = step world
        let v2 = state2.Bodies.[0].Velocity.X
        Assert.True(abs(v2 - v1) < 0.1)
    finally
        destroy world

[<Fact>]
let ``force on non-existent body returns success`` () =
    let world = create ()
    try
        let ack = applyForce world "nonexistent" (Vec3(X = 1.0))
        Assert.True(ack.Success)
    finally
        destroy world

// ─── US4: Gravity ──────────────────────────────────────────────────────────

[<Fact>]
let ``setGravity causes bodies to accelerate downward`` () =
    let world = create ()
    try
        let _ = addBody world (makeSphereBody "ball1" 1.0 0.5)
        setGravity world (Vec3(X = 0.0, Y = -9.81, Z = 0.0))
        let state = step world
        Assert.True(state.Bodies.[0].Velocity.Y < 0.0)
    finally
        destroy world

[<Fact>]
let ``zero gravity means no acceleration`` () =
    let world = create ()
    try
        let _ = addBody world (makeSphereBody "ball1" 1.0 0.5)
        let state = step world
        Assert.True(abs(state.Bodies.[0].Velocity.Y) < 0.01)
    finally
        destroy world

[<Fact>]
let ``changing gravity mid-simulation takes effect`` () =
    let world = create ()
    try
        let _ = addBody world (makeSphereBody "ball1" 1.0 0.5)
        setGravity world (Vec3(X = 0.0, Y = -9.81, Z = 0.0))
        let _ = step world
        setGravity world (Vec3(X = 0.0, Y = 0.0, Z = 0.0))
        let state1 = step world
        let vy1 = state1.Bodies.[0].Velocity.Y
        let state2 = step world
        let vy2 = state2.Bodies.[0].Velocity.Y
        Assert.True(abs(vy2 - vy1) < 0.1)
    finally
        destroy world

// ─── Edge Cases & Stress Tests (T043) ──────────────────────────────────────

[<Fact>]
let ``empty world stepping streams valid empty state`` () =
    let world = create ()
    try
        setRunning world true
        let state = step world
        Assert.Empty(state.Bodies)
        Assert.True(state.Time > 0.0)
        Assert.True(state.Running)
    finally
        destroy world

[<Fact>]
let ``extremely large forces do not crash`` () =
    let world = create ()
    try
        let _ = addBody world (makeSphereBody "ball1" 1.0 0.5)
        let bigForce = Vec3(X = 1e15, Y = 1e15, Z = 1e15)
        let ack = applyForce world "ball1" bigForce
        Assert.True(ack.Success)
        // Multiple steps with huge force — should not crash
        for _ in 1..10 do
            let _ = step world
            ()
    finally
        destroy world

[<Fact>]
let ``100 bodies stable operation`` () =
    let world = create ()
    try
        // Add 100 sphere bodies at various positions
        for i in 0..99 do
            let y = float (i * 2)
            let cmd = AddBody(Id = $"body{i}", Mass = 1.0)
            cmd.Position <- Vec3(X = 0.0, Y = y, Z = 0.0)
            cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
            let ack = addBody world cmd
            Assert.True(ack.Success, $"Failed to add body{i}: {ack.Message}")

        // Step 60 times (1 second of simulation)
        for _ in 1..60 do
            let state = step world
            Assert.Equal(100, state.Bodies.Count)

        // Verify all bodies still present
        let finalState = currentState world
        Assert.Equal(100, finalState.Bodies.Count)
    finally
        destroy world
