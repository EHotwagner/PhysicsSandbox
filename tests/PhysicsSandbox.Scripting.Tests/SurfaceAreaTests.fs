module PhysicsSandbox.Scripting.Tests.SurfaceAreaTests

open System.IO
open System.Reflection
open Xunit
open TestHelpers

let private getAssembly () =
    Assembly.Load("PhysicsSandbox.Scripting")

let private anchorType =
    let asm = getAssembly ()
    asm.GetType("PhysicsSandbox.Scripting.Helpers")

[<Fact>]
let ``Helpers public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsSandbox.Scripting.Helpers"
        [ "ok"; "sleep"; "timed" ]

[<Fact>]
let ``Vec3Builders public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsSandbox.Scripting.Vec3Builders"
        [ "toVec3"; "toTuple" ]

[<Fact>]
let ``CommandBuilders public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsSandbox.Scripting.CommandBuilders"
        [ "makeSphereCmd"; "makeBoxCmd"; "makeImpulseCmd"; "makeTorqueCmd" ]

[<Fact>]
let ``BatchOperations public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsSandbox.Scripting.BatchOperations"
        [ "batchAdd" ]

[<Fact>]
let ``SimulationLifecycle public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsSandbox.Scripting.SimulationLifecycle"
        [ "resetSimulation"; "runFor"; "nextId" ]

[<Fact>]
let ``QueryBuilders public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsSandbox.Scripting.QueryBuilders"
        [ "raycast"; "raycastAll"; "sweepSphere"; "overlapSphere" ]

[<Fact>]
let ``Prelude public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsSandbox.Scripting.Prelude"
        [ "ok"; "sleep"; "timed"; "toVec3"; "toTuple"
          "makeSphereCmd"; "makeBoxCmd"; "makeImpulseCmd"; "makeTorqueCmd"
          "raycast"; "raycastAll"; "sweepSphere"; "overlapSphere"
          "batchAdd"; "resetSimulation"; "runFor"; "nextId" ]

[<Fact>]
let ``Surface area matches baseline file`` () =
    let assembly = getAssembly ()
    let modules =
        [| "PhysicsSandbox.Scripting.Helpers"
           "PhysicsSandbox.Scripting.Vec3Builders"
           "PhysicsSandbox.Scripting.CommandBuilders"
           "PhysicsSandbox.Scripting.ConstraintBuilders"
           "PhysicsSandbox.Scripting.QueryBuilders"
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
