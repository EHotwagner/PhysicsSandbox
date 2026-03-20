module PhysicsSimulation.Tests.CommandHandlerTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld
open PhysicsSimulation.CommandHandler

type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere

[<Fact>]
let ``handle PlayPause sets running state`` () =
    let world = create ()
    try
        let cmd = SimulationCommand(PlayPause = PlayPause(Running = true))
        let ack = handle world cmd
        Assert.True(ack.Success)
        Assert.True(isRunning world)
    finally
        destroy world

[<Fact>]
let ``handle StepSimulation advances time`` () =
    let world = create ()
    try
        let cmd = SimulationCommand(Step = StepSimulation())
        let ack = handle world cmd
        Assert.True(ack.Success)
        Assert.True(time world > 0.0)
    finally
        destroy world

[<Fact>]
let ``handle unknown command returns success`` () =
    let world = create ()
    try
        let cmd = SimulationCommand()
        let ack = handle world cmd
        Assert.True(ack.Success)
    finally
        destroy world

// ─── US2: Body Management via CommandHandler ───────────────────────────────

[<Fact>]
let ``handle AddBody dispatches correctly`` () =
    let world = create ()
    try
        let body = AddBody(Id = "b1", Mass = 1.0)
        body.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let cmd = SimulationCommand(AddBody = body)
        let ack = handle world cmd
        Assert.True(ack.Success)
        Assert.Single(currentState(world).Bodies) |> ignore
    finally
        destroy world

[<Fact>]
let ``handle RemoveBody dispatches correctly`` () =
    let world = create ()
    try
        let body = AddBody(Id = "b1", Mass = 1.0)
        body.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let _ = handle world (SimulationCommand(AddBody = body))
        let cmd = SimulationCommand(RemoveBody = RemoveBody(BodyId = "b1"))
        let ack = handle world cmd
        Assert.True(ack.Success)
        Assert.Empty(currentState(world).Bodies)
    finally
        destroy world

[<Fact>]
let ``handle AddBody with invalid mass returns error`` () =
    let world = create ()
    try
        let body = AddBody(Id = "bad", Mass = 0.0)
        body.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let cmd = SimulationCommand(AddBody = body)
        let ack = handle world cmd
        Assert.False(ack.Success)
    finally
        destroy world

// ─── US3: Force/Impulse/Torque via CommandHandler ──────────────────────────

[<Fact>]
let ``handle ApplyForce on existing body`` () =
    let world = create ()
    try
        let body = AddBody(Id = "b1", Mass = 1.0)
        body.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let _ = handle world (SimulationCommand(AddBody = body))
        let cmd = SimulationCommand(ApplyForce = ApplyForce(BodyId = "b1", Force = Vec3(X = 10.0)))
        let ack = handle world cmd
        Assert.True(ack.Success)
    finally
        destroy world

[<Fact>]
let ``handle ClearForces on existing body`` () =
    let world = create ()
    try
        let body = AddBody(Id = "b1", Mass = 1.0)
        body.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let _ = handle world (SimulationCommand(AddBody = body))
        let _ = handle world (SimulationCommand(ApplyForce = ApplyForce(BodyId = "b1", Force = Vec3(X = 10.0))))
        let cmd = SimulationCommand(ClearForces = ClearForces(BodyId = "b1"))
        let ack = handle world cmd
        Assert.True(ack.Success)
    finally
        destroy world

[<Fact>]
let ``handle force on non-existent body returns success`` () =
    let world = create ()
    try
        let cmd = SimulationCommand(ApplyForce = ApplyForce(BodyId = "nope", Force = Vec3(X = 1.0)))
        let ack = handle world cmd
        Assert.True(ack.Success)
    finally
        destroy world

// ─── US4: Gravity via CommandHandler ───────────────────────────────────────

[<Fact>]
let ``handle SetGravity updates gravity`` () =
    let world = create ()
    try
        let cmd = SimulationCommand(SetGravity = SetGravity(Gravity = Vec3(Y = -9.81)))
        let ack = handle world cmd
        Assert.True(ack.Success)
    finally
        destroy world
