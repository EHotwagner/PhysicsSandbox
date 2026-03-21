module PhysicsSimulation.Tests.StaticBodyTrackingTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld

type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere

let private makePlaneCmd id =
    let body = AddBody()
    body.Id <- id
    body.Position <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    body.Mass <- 0.0
    body.Shape <- Shape(Plane = Plane(Normal = Vec3(X = 0.0, Y = 1.0, Z = 0.0)))
    body

let private makeSphereCmd id y =
    let body = AddBody()
    body.Id <- id
    body.Position <- Vec3(X = 0.0, Y = y, Z = 0.0)
    body.Mass <- 1.0
    body.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
    body

[<Fact>]
let ``addBody for plane creates static body in state`` () =
    let world = create ()
    try
        let ack = addBody world (makePlaneCmd "plane-1")
        Assert.True(ack.Success)

        let state = currentState world
        Assert.Equal(1, state.Bodies.Count)
        Assert.True(state.Bodies.[0].IsStatic)
        Assert.Equal("plane-1", state.Bodies.[0].Id)
    finally
        destroy world

[<Fact>]
let ``static body has zero velocity in state`` () =
    let world = create ()
    try
        addBody world (makePlaneCmd "plane-1") |> ignore
        let state = currentState world
        let body = state.Bodies.[0]
        Assert.Equal(0.0, body.Velocity.X)
        Assert.Equal(0.0, body.Velocity.Y)
        Assert.Equal(0.0, body.Velocity.Z)
    finally
        destroy world

[<Fact>]
let ``static and dynamic bodies coexist in state`` () =
    let world = create ()
    try
        addBody world (makePlaneCmd "plane") |> ignore
        addBody world (makeSphereCmd "sphere" 5.0) |> ignore

        let state = currentState world
        Assert.Equal(2, state.Bodies.Count)

        let statics = state.Bodies |> Seq.filter (fun b -> b.IsStatic) |> Seq.toList
        let dynamics = state.Bodies |> Seq.filter (fun b -> not b.IsStatic) |> Seq.toList
        Assert.Single(statics) |> ignore
        Assert.Single(dynamics) |> ignore
    finally
        destroy world

[<Fact>]
let ``removeBody works for static bodies`` () =
    let world = create ()
    try
        addBody world (makePlaneCmd "plane") |> ignore
        Assert.Equal(1, (currentState world).Bodies.Count)

        let ack = removeBody world "plane"
        Assert.True(ack.Success)
        Assert.Equal(0, (currentState world).Bodies.Count)
    finally
        destroy world

[<Fact>]
let ``resetSimulation clears static bodies from state`` () =
    let world = create ()
    try
        addBody world (makePlaneCmd "plane") |> ignore
        addBody world (makeSphereCmd "sphere" 5.0) |> ignore
        Assert.Equal(2, (currentState world).Bodies.Count)

        resetSimulation world |> ignore
        Assert.Equal(0, (currentState world).Bodies.Count)
    finally
        destroy world

[<Fact>]
let ``static body is_static flag is true in proto`` () =
    let world = create ()
    try
        addBody world (makePlaneCmd "p1") |> ignore
        addBody world (makeSphereCmd "s1" 5.0) |> ignore

        let state = currentState world
        for body in state.Bodies do
            if body.Id = "p1" then
                Assert.True(body.IsStatic)
            elif body.Id = "s1" then
                Assert.False(body.IsStatic)
    finally
        destroy world

[<Fact>]
let ``duplicate static body ID is rejected`` () =
    let world = create ()
    try
        let ack1 = addBody world (makePlaneCmd "plane")
        Assert.True(ack1.Success)

        let ack2 = addBody world (makePlaneCmd "plane")
        Assert.False(ack2.Success)
        Assert.Contains("already exists", ack2.Message)
    finally
        destroy world
