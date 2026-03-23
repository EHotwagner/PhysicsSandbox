module PhysicsClient.Tests.MeshResolverTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsClient.MeshResolver

let private makeResolver () =
    // Create a resolver without a real gRPC client — only tests cache operations
    create (Unchecked.defaultof<PhysicsHub.PhysicsHubClient>)

[<Fact>]
let ``processNewMeshes populates cache`` () =
    let resolver = makeResolver ()
    let mg = MeshGeometry(MeshId = "mesh-001", Shape = Shape(Sphere = Sphere(Radius = 1.0)))
    processNewMeshes [mg] resolver
    let result = resolve "mesh-001" resolver
    Assert.True(result.IsSome)
    Assert.Equal(Shape.ShapeOneofCase.Sphere, result.Value.ShapeCase)

[<Fact>]
let ``resolve returns None for unknown`` () =
    let resolver = makeResolver ()
    let result = resolve "unknown" resolver
    Assert.True(result.IsNone)

[<Fact>]
let ``processNewMeshes with multiple entries`` () =
    let resolver = makeResolver ()
    let mg1 = MeshGeometry(MeshId = "a", Shape = Shape(Sphere = Sphere(Radius = 1.0)))
    let mg2 = MeshGeometry(MeshId = "b", Shape = Shape(Sphere = Sphere(Radius = 2.0)))
    processNewMeshes [mg1; mg2] resolver
    Assert.True((resolve "a" resolver).IsSome)
    Assert.True((resolve "b" resolver).IsSome)
    Assert.True((resolve "c" resolver).IsNone)
