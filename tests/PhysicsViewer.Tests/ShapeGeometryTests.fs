module PhysicsViewer.Tests.ShapeGeometryTests

open Xunit
open Stride.CommunityToolkit.Rendering.ProceduralModels
open PhysicsSandbox.Shared.Contracts
open PhysicsViewer.ShapeGeometry

[<Fact>]
let ``CachedShapeRef returns Cube primitiveType`` () =
    let cached = CachedShapeRef(MeshId = "abc123", BboxMin = Vec3(X = 0.0, Y = 0.0, Z = 0.0), BboxMax = Vec3(X = 1.0, Y = 1.0, Z = 1.0))
    let shape = Shape(CachedRef = cached)
    Assert.Equal(PrimitiveModelType.Cube, primitiveType shape)

[<Fact>]
let ``CachedShapeRef shapeSize returns bbox dimensions`` () =
    let cached = CachedShapeRef(MeshId = "abc123", BboxMin = Vec3(X = -1.0, Y = 0.0, Z = -0.5), BboxMax = Vec3(X = 1.0, Y = 2.0, Z = 0.5))
    let shape = Shape(CachedRef = cached)
    let size = shapeSize shape
    Assert.True(size.HasValue)
    Assert.Equal(2.0f, size.Value.X, 0.01f)
    Assert.Equal(2.0f, size.Value.Y, 0.01f)
    Assert.Equal(1.0f, size.Value.Z, 0.01f)

[<Fact>]
let ``CachedShapeRef defaultColor returns placeholder color`` () =
    let cached = CachedShapeRef(MeshId = "abc123")
    let shape = Shape(CachedRef = cached)
    let color = defaultColor shape
    // Semi-transparent magenta: (255, 0, 255, 128)
    Assert.Equal(255uy, color.R)
    Assert.Equal(0uy, color.G)
    Assert.Equal(255uy, color.B)
    Assert.Equal(128uy, color.A)
