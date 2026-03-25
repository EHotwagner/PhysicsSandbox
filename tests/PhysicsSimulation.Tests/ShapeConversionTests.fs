/// Tests for extended shape types and shape registration.
module PhysicsSimulation.Tests.ShapeConversionTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld

type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere
type ProtoCapsule = PhysicsSandbox.Shared.Contracts.Capsule
type ProtoCylinder = PhysicsSandbox.Shared.Contracts.Cylinder

let private makeBody id mass shape =
    let cmd = AddBody(Id = id, Mass = mass)
    cmd.Position <- Vec3(X = 0.0, Y = 5.0, Z = 0.0)
    cmd.Shape <- shape
    cmd

// ─── T029a: Shape Conversion Tests ──────────────────────────────────────────

[<Fact>]
let ``addBody with capsule shape works`` () =
    let world = create ()
    try
        let shape = Shape(Capsule = ProtoCapsule(Radius = 0.3, Length = 1.0))
        let ack = addBody world (makeBody "cap1" 1.0 shape)
        Assert.True(ack.Success, ack.Message)
        let state = currentState world
        Assert.Single(state.Bodies) |> ignore
    finally
        destroy world

[<Fact>]
let ``addBody with cylinder shape works`` () =
    let world = create ()
    try
        let shape = Shape(Cylinder = ProtoCylinder(Radius = 0.25, Length = 0.5))
        let ack = addBody world (makeBody "cyl1" 1.0 shape)
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``addBody with triangle shape works`` () =
    let world = create ()
    try
        let tri = Triangle()
        tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
        tri.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
        tri.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
        let shape = Shape(Triangle = tri)
        let ack = addBody world (makeBody "tri1" 1.0 shape)
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``addBody with convex hull works`` () =
    let world = create ()
    try
        let hull = ConvexHull()
        hull.Points.Add(Vec3(X = 0.0, Y = 0.0, Z = 0.0))
        hull.Points.Add(Vec3(X = 1.0, Y = 0.0, Z = 0.0))
        hull.Points.Add(Vec3(X = 0.0, Y = 1.0, Z = 0.0))
        hull.Points.Add(Vec3(X = 0.0, Y = 0.0, Z = 1.0))
        let shape = Shape(ConvexHull = hull)
        let ack = addBody world (makeBody "hull1" 1.0 shape)
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``addBody with compound shape works`` () =
    let world = create ()
    try
        let compound = Compound()
        let child = CompoundChild()
        child.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        child.LocalPosition <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
        compound.Children.Add(child)
        let shape = Shape(Compound = compound)
        let ack = addBody world (makeBody "comp1" 1.0 shape)
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``addBody with mesh shape works`` () =
    let world = create ()
    try
        let mesh = MeshShape()
        let tri = MeshTriangle()
        tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
        tri.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
        tri.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
        mesh.Triangles.Add(tri)
        let shape = Shape(Mesh = mesh)
        let ack = addBody world (makeBody "mesh1" 1.0 shape)
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``capsule with negative radius rejected`` () =
    let world = create ()
    try
        let shape = Shape(Capsule = ProtoCapsule(Radius = -0.5, Length = 1.0))
        let ack = addBody world (makeBody "bad" 1.0 shape)
        Assert.False(ack.Success)
    finally
        destroy world

[<Fact>]
let ``convex hull with fewer than 4 points rejected`` () =
    let world = create ()
    try
        let hull = ConvexHull()
        hull.Points.Add(Vec3(X = 0.0, Y = 0.0, Z = 0.0))
        hull.Points.Add(Vec3(X = 1.0, Y = 0.0, Z = 0.0))
        hull.Points.Add(Vec3(X = 0.0, Y = 1.0, Z = 0.0))
        let shape = Shape(ConvexHull = hull)
        let ack = addBody world (makeBody "bad" 1.0 shape)
        Assert.False(ack.Success)
        Assert.Contains("4 points", ack.Message)
    finally
        destroy world

[<Fact>]
let ``compound with zero children rejected`` () =
    let world = create ()
    try
        let compound = Compound()
        let shape = Shape(Compound = compound)
        let ack = addBody world (makeBody "bad" 1.0 shape)
        Assert.False(ack.Success)
        Assert.Contains("1 child", ack.Message)
    finally
        destroy world

[<Fact>]
let ``mesh with zero triangles rejected`` () =
    let world = create ()
    try
        let mesh = MeshShape()
        let shape = Shape(Mesh = mesh)
        let ack = addBody world (makeBody "bad" 1.0 shape)
        Assert.False(ack.Success)
        Assert.Contains("1 triangle", ack.Message)
    finally
        destroy world

// ─── T029c: Shape Registration Tests ────────────────────────────────────────

[<Fact>]
let ``registerShape stores handle`` () =
    let world = create ()
    try
        let cmd = RegisterShape(ShapeHandle = "myshape")
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 1.0))
        let ack = registerShape world cmd
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``registerShape duplicate handle rejected`` () =
    let world = create ()
    try
        let cmd = RegisterShape(ShapeHandle = "myshape")
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 1.0))
        let _ = registerShape world cmd
        let ack = registerShape world cmd
        Assert.False(ack.Success)
        Assert.Contains("already registered", ack.Message)
    finally
        destroy world

[<Fact>]
let ``unregisterShape removes handle`` () =
    let world = create ()
    try
        let cmd = RegisterShape(ShapeHandle = "myshape")
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 1.0))
        let _ = registerShape world cmd
        let ack = unregisterShape world "myshape"
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``shapeReference resolves registered handle`` () =
    let world = create ()
    try
        let regCmd = RegisterShape(ShapeHandle = "cached")
        regCmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let _ = registerShape world regCmd

        let shapeRef = ShapeReference(ShapeHandle = "cached")
        let body = makeBody "ref1" 1.0 (Shape(ShapeRef = shapeRef))
        let ack = addBody world body
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``shapeReference with unknown handle rejected`` () =
    let world = create ()
    try
        let shapeRef = ShapeReference(ShapeHandle = "nonexistent")
        let body = makeBody "bad" 1.0 (Shape(ShapeRef = shapeRef))
        let ack = addBody world body
        Assert.False(ack.Success)
        Assert.Contains("Unknown shape reference", ack.Message)
    finally
        destroy world

[<Fact>]
let ``resetSimulation clears registered shapes`` () =
    let world = create ()
    try
        let cmd = RegisterShape(ShapeHandle = "myshape")
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 1.0))
        let _ = registerShape world cmd

        let _ = resetSimulation world

        // Re-registering same handle should succeed (was cleared)
        let ack = registerShape world cmd
        Assert.True(ack.Success, ack.Message)
    finally
        destroy world

[<Fact>]
let ``registered shapes appear in state`` () =
    let world = create ()
    try
        let cmd = RegisterShape(ShapeHandle = "cached_sphere")
        cmd.Shape <- Shape(Sphere = ProtoSphere(Radius = 0.5))
        let _ = registerShape world cmd

        let state = currentState world
        Assert.True(state.RegisteredShapes.Count > 0)
        Assert.Equal("cached_sphere", state.RegisteredShapes.[0].ShapeHandle)
    finally
        destroy world
