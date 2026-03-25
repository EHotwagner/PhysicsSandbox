module PhysicsSimulation.Tests.SimulationWorldForcesTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld

type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere

let private makeSphereBody id mass radius =
    let cmd = AddBody(Id = id, Mass = mass)
    cmd.Position <- Vec3(X = 0.0, Y = 5.0, Z = 0.0)
    cmd.Velocity <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = radius))
    cmd

let private makeConvexHullBody id mass =
    let cmd = AddBody(Id = id, Mass = mass)
    cmd.Position <- Vec3(X = 0.0, Y = 5.0, Z = 0.0)
    let hull = ConvexHull()
    hull.Points.Add(Vec3(X = 0.0, Y = 0.0, Z = 0.0))
    hull.Points.Add(Vec3(X = 1.0, Y = 0.0, Z = 0.0))
    hull.Points.Add(Vec3(X = 0.0, Y = 1.0, Z = 0.0))
    hull.Points.Add(Vec3(X = 0.0, Y = 0.0, Z = 1.0))
    cmd.Shape <- Shape(ConvexHull = hull)
    cmd

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

// ─── Mesh Cache Transport (T018) ─────────────────────────────────────────

[<Fact>]
let ``ConvexHull body first state has CachedShapeRef and new_meshes`` () =
    let world = create ()
    try
        let ack = addBody world (makeConvexHullBody "hull1" 1.0)
        Assert.True(ack.Success)
        let state = step world
        // Body shape should be CachedShapeRef
        let body = state.Bodies.[0]
        Assert.Equal(Shape.ShapeOneofCase.CachedRef, body.Shape.ShapeCase)
        Assert.NotEmpty(body.Shape.CachedRef.MeshId)
        // new_meshes should contain exactly this mesh
        Assert.Single(state.NewMeshes) |> ignore
        Assert.Equal(body.Shape.CachedRef.MeshId, state.NewMeshes.[0].MeshId)
        Assert.Equal(Shape.ShapeOneofCase.ConvexHull, state.NewMeshes.[0].Shape.ShapeCase)
    finally
        destroy world

[<Fact>]
let ``ConvexHull body second state has CachedShapeRef but empty new_meshes`` () =
    let world = create ()
    try
        let _ = addBody world (makeConvexHullBody "hull1" 1.0)
        let _ = step world
        let state2 = step world
        let body = state2.Bodies.[0]
        Assert.Equal(Shape.ShapeOneofCase.CachedRef, body.Shape.ShapeCase)
        Assert.Empty(state2.NewMeshes)
    finally
        destroy world

[<Fact>]
let ``Sphere body always uses inline shape`` () =
    let world = create ()
    try
        let _ = addBody world (makeSphereBody "ball1" 1.0 0.5)
        let state1 = step world
        let body1 = state1.Bodies.[0]
        Assert.Equal(Shape.ShapeOneofCase.Sphere, body1.Shape.ShapeCase)
        Assert.Empty(state1.NewMeshes)
        let state2 = step world
        Assert.Equal(Shape.ShapeOneofCase.Sphere, state2.Bodies.[0].Shape.ShapeCase)
        Assert.Empty(state2.NewMeshes)
    finally
        destroy world

[<Fact>]
let ``resetSimulation clears EmittedMeshIds so new_meshes re-emits`` () =
    let world = create ()
    try
        let _ = addBody world (makeConvexHullBody "hull1" 1.0)
        let state1 = step world
        let meshId = state1.Bodies.[0].Shape.CachedRef.MeshId
        Assert.Single(state1.NewMeshes) |> ignore
        // Reset and re-add the same hull
        let _ = resetSimulation world
        let _ = addBody world (makeConvexHullBody "hull1" 1.0)
        let state2 = step world
        // Should re-emit mesh geometry since EmittedMeshIds was cleared
        Assert.Single(state2.NewMeshes) |> ignore
        Assert.Equal(meshId, state2.NewMeshes.[0].MeshId)
    finally
        destroy world

[<Fact>]
let ``CachedShapeRef has valid bounding box`` () =
    let world = create ()
    try
        let _ = addBody world (makeConvexHullBody "hull1" 1.0)
        let state = step world
        let cachedRef = state.Bodies.[0].Shape.CachedRef
        // bbox_min should be component-wise <= bbox_max
        Assert.True(cachedRef.BboxMin.X <= cachedRef.BboxMax.X)
        Assert.True(cachedRef.BboxMin.Y <= cachedRef.BboxMax.Y)
        Assert.True(cachedRef.BboxMin.Z <= cachedRef.BboxMax.Z)
    finally
        destroy world

[<Fact>]
let ``duplicate ConvexHull bodies share same mesh_id and only one new_meshes entry`` () =
    let world = create ()
    try
        let _ = addBody world (makeConvexHullBody "hull1" 1.0)
        let _ = addBody world (makeConvexHullBody "hull2" 2.0)
        let state = step world
        let id1 = state.Bodies |> Seq.find (fun b -> b.Id = "hull1") |> fun b -> b.Shape.CachedRef.MeshId
        let id2 = state.Bodies |> Seq.find (fun b -> b.Id = "hull2") |> fun b -> b.Shape.CachedRef.MeshId
        Assert.Equal(id1, id2)
        // Only one new_meshes entry despite two bodies with same geometry
        Assert.Single(state.NewMeshes) |> ignore
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
