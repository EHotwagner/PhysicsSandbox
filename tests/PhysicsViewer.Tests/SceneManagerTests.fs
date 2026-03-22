module PhysicsViewer.Tests.SceneManagerTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsViewer.SceneManager
open PhysicsViewer.ShapeGeometry
open Stride.CommunityToolkit.Rendering.ProceduralModels

type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere
type ProtoBox = PhysicsSandbox.Shared.Contracts.Box
type StrideColor = Stride.Core.Mathematics.Color

// ---------------------------------------------------------------------------
// ShapeGeometry.primitiveType
// ---------------------------------------------------------------------------

[<Fact>]
let ``primitiveType returns Sphere for proto Sphere`` () =
    let shape = Shape(Sphere = ProtoSphere(Radius = 1.0))
    Assert.Equal(PrimitiveModelType.Sphere, primitiveType shape)

[<Fact>]
let ``primitiveType returns Cube for proto Box`` () =
    let shape = Shape(Box = ProtoBox(HalfExtents = Vec3(X = 1.0, Y = 1.0, Z = 1.0)))
    Assert.Equal(PrimitiveModelType.Cube, primitiveType shape)

[<Fact>]
let ``primitiveType returns Sphere for null shape`` () =
    Assert.Equal(PrimitiveModelType.Sphere, primitiveType null)

[<Fact>]
let ``primitiveType returns Sphere for unset shape`` () =
    Assert.Equal(PrimitiveModelType.Sphere, primitiveType (Shape()))

[<Fact>]
let ``defaultColor returns blue for Sphere`` () =
    let shape = Shape(Sphere = ProtoSphere(Radius = 1.0))
    let c = defaultColor shape
    Assert.Equal(StrideColor(64uy, 128uy, 255uy, 255uy), c)

[<Fact>]
let ``defaultColor returns orange for Box`` () =
    let shape = Shape(Box = ProtoBox(HalfExtents = Vec3(X = 1.0, Y = 1.0, Z = 1.0)))
    Assert.Equal(StrideColor.Orange, defaultColor shape)

// ---------------------------------------------------------------------------
// FR-007: Shape type renders as correct primitive
// ---------------------------------------------------------------------------

[<Fact>]
let ``primitiveType returns Cube for Triangle`` () =
    let tri = Triangle()
    tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    tri.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    tri.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    Assert.Equal(PrimitiveModelType.Cube, primitiveType (Shape(Triangle = tri)))

[<Fact>]
let ``primitiveType returns Cube for Mesh`` () =
    let mesh = MeshShape()
    let mt = MeshTriangle()
    mt.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    mt.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    mt.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    mesh.Triangles.Add(mt)
    Assert.Equal(PrimitiveModelType.Cube, primitiveType (Shape(Mesh = mesh)))

[<Fact>]
let ``primitiveType returns Cube for Compound`` () =
    let compound = Compound()
    let child = CompoundChild()
    child.Shape <- Shape(Sphere = PhysicsSandbox.Shared.Contracts.Sphere(Radius = 0.5))
    compound.Children.Add(child)
    Assert.Equal(PrimitiveModelType.Cube, primitiveType (Shape(Compound = compound)))

[<Fact>]
let ``shapeSize returns bounding box for Triangle`` () =
    let tri = Triangle()
    tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    tri.B <- Vec3(X = 2.0, Y = 0.0, Z = 0.0)
    tri.C <- Vec3(X = 0.0, Y = 3.0, Z = 0.0)
    let size = shapeSize (Shape(Triangle = tri))
    Assert.True(size.HasValue)
    Assert.Equal(2.0f, size.Value.X)
    Assert.Equal(3.0f, size.Value.Y)

[<Fact>]
let ``shapeSize returns bounding box for Mesh`` () =
    let mesh = MeshShape()
    let mt = MeshTriangle()
    mt.A <- Vec3(X = -1.0, Y = 0.0, Z = -1.0)
    mt.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    mt.C <- Vec3(X = 0.0, Y = 2.0, Z = 1.0)
    mesh.Triangles.Add(mt)
    let size = shapeSize (Shape(Mesh = mesh))
    Assert.True(size.HasValue)
    Assert.Equal(2.0f, size.Value.X)
    Assert.Equal(2.0f, size.Value.Y)
    Assert.Equal(2.0f, size.Value.Z)

// ---------------------------------------------------------------------------
// T029b: Default Color Palette Tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``defaultColor returns green for Capsule`` () =
    let shape = Shape(Capsule = PhysicsSandbox.Shared.Contracts.Capsule(Radius = 0.5, Length = 1.0))
    Assert.Equal(StrideColor.Green, defaultColor shape)

[<Fact>]
let ``defaultColor returns yellow for Cylinder`` () =
    let shape = Shape(Cylinder = PhysicsSandbox.Shared.Contracts.Cylinder(Radius = 0.5, Length = 1.0))
    Assert.Equal(StrideColor.Yellow, defaultColor shape)

[<Fact>]
let ``defaultColor returns gray for Plane`` () =
    let shape = Shape(Plane = Plane(Normal = Vec3(X = 0.0, Y = 1.0, Z = 0.0)))
    Assert.Equal(StrideColor.Gray, defaultColor shape)

[<Fact>]
let ``defaultColor returns cyan for Triangle`` () =
    let tri = Triangle()
    tri.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    tri.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    tri.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    let shape = Shape(Triangle = tri)
    Assert.Equal(StrideColor(0uy, 255uy, 255uy, 255uy), defaultColor shape)

[<Fact>]
let ``defaultColor returns teal for MeshShape`` () =
    let mesh = MeshShape()
    let mt = MeshTriangle()
    mt.A <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    mt.B <- Vec3(X = 1.0, Y = 0.0, Z = 0.0)
    mt.C <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    mesh.Triangles.Add(mt)
    let shape = Shape(Mesh = mesh)
    Assert.Equal(StrideColor(0uy, 128uy, 128uy, 255uy), defaultColor shape)

// ---------------------------------------------------------------------------
// ShapeGeometry.shapeSize
// ---------------------------------------------------------------------------

[<Fact>]
let ``shapeSize returns diameter for sphere`` () =
    let shape = Shape(Sphere = ProtoSphere(Radius = 2.0))
    let size = shapeSize shape
    Assert.True(size.HasValue)
    Assert.Equal(4.0f, size.Value.X)

[<Fact>]
let ``shapeSize returns doubled extents for box`` () =
    let shape = Shape(Box = ProtoBox(HalfExtents = Vec3(X = 1.0, Y = 2.0, Z = 3.0)))
    let size = shapeSize shape
    Assert.True(size.HasValue)
    Assert.Equal(2.0f, size.Value.X)
    Assert.Equal(4.0f, size.Value.Y)
    Assert.Equal(6.0f, size.Value.Z)

// ---------------------------------------------------------------------------
// SceneState initial values
// ---------------------------------------------------------------------------

[<Fact>]
let ``create returns state with zero simulation time`` () =
    let state = create ()
    Assert.Equal(0.0, simulationTime state)

[<Fact>]
let ``create returns state with running false`` () =
    let state = create ()
    Assert.False(isRunning state)

[<Fact>]
let ``create returns state with wireframe false`` () =
    let state = create ()
    Assert.False(isWireframe state)
