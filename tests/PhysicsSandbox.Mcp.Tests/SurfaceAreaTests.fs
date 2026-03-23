module PhysicsSandbox.Mcp.Tests.SurfaceAreaTests

open System
open System.Reflection
open Xunit

let private getPublicMembers (moduleType: Type) =
    moduleType.GetMembers(BindingFlags.Public ||| BindingFlags.Static)
    |> Array.filter (fun m -> m.MemberType = MemberTypes.Method || m.MemberType = MemberTypes.Property)
    |> Array.map (fun m -> m.Name)
    |> Array.sort

[<Fact>]
let ``MeshResolver public API matches baseline`` () =
    let t = typeof<PhysicsSandbox.Mcp.MeshResolver.MeshResolverState>.DeclaringType
    Assert.NotNull(t)
    let members = getPublicMembers t
    for name in [| "create"; "fetchMissingSync"; "processNewMeshes"; "resolve" |] do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")
