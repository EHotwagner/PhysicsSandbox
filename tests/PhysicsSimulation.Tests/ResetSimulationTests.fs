module PhysicsSimulation.Tests.ResetSimulationTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld

type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere

let private makeSphereCmd id =
    let body = AddBody()
    body.Id <- id
    body.Position <- Vec3(X = 0.0, Y = 5.0, Z = 0.0)
    body.Mass <- 1.0
    body.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
    body

[<Fact>]
let ``resetSimulation clears all bodies`` () =
    let world = create ()
    try
        addBody world (makeSphereCmd "s1") |> ignore
        addBody world (makeSphereCmd "s2") |> ignore
        addBody world (makeSphereCmd "s3") |> ignore

        let stateBefore = currentState world
        Assert.Equal(3, stateBefore.Bodies.Count)

        let ack = resetSimulation world
        Assert.True(ack.Success)

        let stateAfter = currentState world
        Assert.Equal(0, stateAfter.Bodies.Count)
    finally
        destroy world

[<Fact>]
let ``resetSimulation resets time to zero`` () =
    let world = create ()
    try
        setRunning world true
        step world |> ignore
        step world |> ignore
        Assert.True(time world > 0.0)

        resetSimulation world |> ignore

        Assert.Equal(0.0, time world)
    finally
        destroy world

[<Fact>]
let ``resetSimulation sets running to false`` () =
    let world = create ()
    try
        setRunning world true
        Assert.True(isRunning world)

        resetSimulation world |> ignore

        Assert.False(isRunning world)
    finally
        destroy world

[<Fact>]
let ``resetSimulation clears active forces`` () =
    let world = create ()
    try
        addBody world (makeSphereCmd "s1") |> ignore
        applyForce world "s1" (Vec3(X = 10.0, Y = 0.0, Z = 0.0)) |> ignore

        resetSimulation world |> ignore

        // After reset, adding a new body with the same ID should succeed
        let ack = addBody world (makeSphereCmd "s1")
        Assert.True(ack.Success)
    finally
        destroy world

[<Fact>]
let ``resetSimulation returns success ack`` () =
    let world = create ()
    try
        let ack = resetSimulation world
        Assert.True(ack.Success)
        Assert.Contains("reset", ack.Message.ToLower())
    finally
        destroy world
