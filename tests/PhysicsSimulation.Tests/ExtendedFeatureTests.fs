/// Tests for extended shape types, shape registration, constraints, and kinematic bodies.
module PhysicsSimulation.Tests.ExtendedFeatureTests

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

let private makeDynamic id =
    makeBody id 1.0 (Shape(Sphere = ProtoSphere(Radius = 0.5)))

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

// ─── T041a: Constraint Tests ────────────────────────────────────────────────

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
