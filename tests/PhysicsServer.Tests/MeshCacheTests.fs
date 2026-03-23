module PhysicsServer.Tests.MeshCacheTests

open System
open System.Reflection
open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MeshCache

let private makeShape () =
    Shape(Sphere = Sphere(Radius = 1.0))

[<Fact>]
let ``add and tryGet roundtrip`` () =
    let cache = create ()
    let shape = makeShape ()
    add "mesh-001" shape cache
    let result = tryGet "mesh-001" cache
    Assert.True(result.IsSome)
    Assert.Equal(Shape.ShapeOneofCase.Sphere, result.Value.ShapeCase)

[<Fact>]
let ``tryGet returns None for unknown`` () =
    let cache = create ()
    let result = tryGet "unknown" cache
    Assert.True(result.IsNone)

[<Fact>]
let ``getMany returns partial hits`` () =
    let cache = create ()
    add "a" (makeShape ()) cache
    add "b" (makeShape ()) cache
    let results = getMany ["a"; "missing"; "b"] cache
    Assert.Equal(2, results.Length)
    Assert.Contains(results, fun mg -> mg.MeshId = "a")
    Assert.Contains(results, fun mg -> mg.MeshId = "b")

[<Fact>]
let ``getMany with empty ids returns empty`` () =
    let cache = create ()
    add "a" (makeShape ()) cache
    let results = getMany [] cache
    Assert.Empty(results)

[<Fact>]
let ``clear empties cache`` () =
    let cache = create ()
    add "a" (makeShape ()) cache
    add "b" (makeShape ()) cache
    Assert.Equal(2, count cache)
    clear cache
    Assert.Equal(0, count cache)
    Assert.True((tryGet "a" cache).IsNone)

[<Fact>]
let ``count tracks entries`` () =
    let cache = create ()
    Assert.Equal(0, count cache)
    add "a" (makeShape ()) cache
    Assert.Equal(1, count cache)
    add "b" (makeShape ()) cache
    Assert.Equal(2, count cache)

[<Fact>]
let ``duplicate add does not overwrite`` () =
    let cache = create ()
    let shape1 = Shape(Sphere = Sphere(Radius = 1.0))
    let shape2 = Shape(Sphere = Sphere(Radius = 2.0))
    add "a" shape1 cache
    add "a" shape2 cache // TryAdd won't overwrite
    let result = (tryGet "a" cache).Value
    Assert.Equal(1.0, result.Sphere.Radius)
    Assert.Equal(1, count cache)

// ─── Surface Area ────────────────────────────────────────────────────────────

let private getPublicMembers (moduleType: Type) =
    moduleType.GetMembers(BindingFlags.Public ||| BindingFlags.Static)
    |> Array.filter (fun m -> m.MemberType = MemberTypes.Method || m.MemberType = MemberTypes.Property)
    |> Array.map (fun m -> m.Name)
    |> Array.sort

[<Fact>]
let ``MeshCache public API matches baseline`` () =
    let t = typeof<MeshCacheState>.DeclaringType
    Assert.NotNull(t)
    let members = getPublicMembers t
    for name in [| "add"; "clear"; "count"; "create"; "getMany"; "tryGet" |] do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")
