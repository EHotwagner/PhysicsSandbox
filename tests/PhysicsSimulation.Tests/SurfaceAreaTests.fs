module PhysicsSimulation.Tests.SurfaceAreaTests

open System
open System.Reflection
open Xunit

let private getPublicMembers (moduleType: Type) =
    moduleType.GetMembers(BindingFlags.Public ||| BindingFlags.Static)
    |> Array.filter (fun m -> m.MemberType = MemberTypes.Method || m.MemberType = MemberTypes.Property)
    |> Array.map (fun m -> m.Name)
    |> Array.sort

[<Fact>]
let ``SimulationWorld public API matches baseline`` () =
    let t = typeof<PhysicsSimulation.SimulationWorld.World>.DeclaringType
    let members = getPublicMembers t
    let expected = [|
        "addBody"; "addConstraint"; "applyForce"; "applyImpulse"; "applyTorque"
        "clearForces"; "create"; "currentState"; "destroy"
        "isRunning"; "registerShape"; "removeBody"; "removeConstraint"
        "resetSimulation"; "setBodyPose"; "setCollisionFilter"
        "setGravity"; "setRunning"; "step"; "time"
        "unregisterShape"
    |]
    for name in expected do
        Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

[<Fact>]
let ``CommandHandler public API matches baseline`` () =
    let t = typeof<PhysicsSimulation.SimulationWorld.World>.Assembly.GetType("PhysicsSimulation.CommandHandler")
    Assert.NotNull(t)
    let members = getPublicMembers t
    Assert.Contains("handle", members)

[<Fact>]
let ``SimulationClient public API matches baseline`` () =
    let t = typeof<PhysicsSimulation.SimulationWorld.World>.Assembly.GetType("PhysicsSimulation.SimulationClient")
    Assert.NotNull(t)
    let members = getPublicMembers t
    Assert.Contains("run", members)

[<Fact>]
let ``MeshIdGenerator public API matches baseline`` () =
    let t = typeof<PhysicsSimulation.SimulationWorld.World>.Assembly.GetType("PhysicsSimulation.MeshIdGenerator")
    Assert.NotNull(t)
    let members = getPublicMembers t
    Assert.Contains("computeMeshId", members)
    Assert.Contains("computeBoundingBox", members)
