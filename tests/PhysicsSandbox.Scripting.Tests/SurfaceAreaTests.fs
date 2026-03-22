module PhysicsSandbox.Scripting.Tests.SurfaceAreaTests

open System
open System.IO
open System.Reflection
open Xunit

let private getPublicMembers (moduleType: Type) =
    moduleType.GetMembers(BindingFlags.Public ||| BindingFlags.Static)
    |> Array.filter (fun m -> m.MemberType = MemberTypes.Method || m.MemberType = MemberTypes.Property)
    |> Array.map (fun m -> m.Name)
    |> Array.sort

let private assertContains (members: string[]) (name: string) =
    Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

let private getAssembly () =
    Assembly.Load("PhysicsSandbox.Scripting")

let private getModule (name: string) =
    let asm = getAssembly ()
    let t = asm.GetType(name)
    Assert.NotNull(t)
    t

[<Fact>]
let ``Helpers public API matches baseline`` () =
    let members = getModule "PhysicsSandbox.Scripting.Helpers" |> getPublicMembers
    for name in [| "ok"; "sleep"; "timed" |] do
        assertContains members name

[<Fact>]
let ``Vec3Builders public API matches baseline`` () =
    let members = getModule "PhysicsSandbox.Scripting.Vec3Builders" |> getPublicMembers
    for name in [| "toVec3"; "toTuple" |] do
        assertContains members name

[<Fact>]
let ``CommandBuilders public API matches baseline`` () =
    let members = getModule "PhysicsSandbox.Scripting.CommandBuilders" |> getPublicMembers
    for name in [| "makeSphereCmd"; "makeBoxCmd"; "makeImpulseCmd"; "makeTorqueCmd" |] do
        assertContains members name

[<Fact>]
let ``BatchOperations public API matches baseline`` () =
    let members = getModule "PhysicsSandbox.Scripting.BatchOperations" |> getPublicMembers
    assertContains members "batchAdd"

[<Fact>]
let ``SimulationLifecycle public API matches baseline`` () =
    let members = getModule "PhysicsSandbox.Scripting.SimulationLifecycle" |> getPublicMembers
    for name in [| "resetSimulation"; "runFor"; "nextId" |] do
        assertContains members name

[<Fact>]
let ``Prelude public API matches baseline`` () =
    let members = getModule "PhysicsSandbox.Scripting.Prelude" |> getPublicMembers
    for name in [| "ok"; "sleep"; "timed"; "toVec3"; "toTuple"
                   "makeSphereCmd"; "makeBoxCmd"; "makeImpulseCmd"; "makeTorqueCmd"
                   "batchAdd"; "resetSimulation"; "runFor"; "nextId" |] do
        assertContains members name

[<Fact>]
let ``Surface area matches baseline file`` () =
    let assembly = getAssembly ()
    let modules =
        [| "PhysicsSandbox.Scripting.Helpers"
           "PhysicsSandbox.Scripting.Vec3Builders"
           "PhysicsSandbox.Scripting.CommandBuilders"
           "PhysicsSandbox.Scripting.BatchOperations"
           "PhysicsSandbox.Scripting.SimulationLifecycle"
           "PhysicsSandbox.Scripting.Prelude" |]

    let actual =
        modules
        |> Array.collect (fun moduleName ->
            let t = assembly.GetType(moduleName)
            if isNull t then [||]
            else
                getPublicMembers t
                |> Array.map (fun m -> $"{moduleName}:{m}"))
        |> Array.sort

    let baselinePath =
        Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "SurfaceAreaBaseline.txt")

    if File.Exists(baselinePath) then
        let expected =
            File.ReadAllLines(baselinePath)
            |> Array.filter (fun l -> l.Trim().Length > 0)
            |> Array.sort

        Assert.Equal<string>(expected, actual)
