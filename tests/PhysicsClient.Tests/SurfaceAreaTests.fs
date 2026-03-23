module PhysicsClient.Tests.SurfaceAreaTests

open System
open System.Reflection
open Xunit

let private getPublicMembers (moduleType: Type) =
    moduleType.GetMembers(BindingFlags.Public ||| BindingFlags.Static)
    |> Array.filter (fun m -> m.MemberType = MemberTypes.Method || m.MemberType = MemberTypes.Property)
    |> Array.map (fun m -> m.Name)
    |> Array.sort

let private assertContains (members: string[]) (name: string) =
    Assert.True(members |> Array.exists (fun m -> m = name), $"Missing public member: {name}")

[<Fact>]
let ``IdGenerator public API matches baseline`` () =
    let t = typeof<PhysicsClient.Session.Session>.Assembly.GetType("PhysicsClient.IdGenerator")
    Assert.NotNull(t)
    let members = getPublicMembers t
    assertContains members "nextId"
    assertContains members "reset"

[<Fact>]
let ``Session public API matches baseline`` () =
    let t = typeof<PhysicsClient.Session.Session>.DeclaringType
    let members = getPublicMembers t
    assertContains members "connect"
    assertContains members "disconnect"
    assertContains members "reconnect"
    assertContains members "isConnected"

[<Fact>]
let ``SimulationCommands public API matches baseline`` () =
    let t = typeof<PhysicsClient.Session.Session>.Assembly.GetType("PhysicsClient.SimulationCommands")
    Assert.NotNull(t)
    let members = getPublicMembers t
    for name in [| "addSphere"; "addBox"; "addCapsule"; "addCylinder"; "addPlane"
                   "addConstraint"; "removeConstraint"
                   "registerShape"; "unregisterShape"; "setCollisionFilter"
                   "removeBody"; "clearAll"
                   "applyForce"; "applyImpulse"; "applyTorque"; "clearForces"
                   "setGravity"; "play"; "pause"; "step"
                   "raycast"; "sweepCast"; "overlap"
                   "batchCommands"; "batchViewCommands" |] do
        assertContains members name

[<Fact>]
let ``ViewCommands public API matches baseline`` () =
    let t = typeof<PhysicsClient.Session.Session>.Assembly.GetType("PhysicsClient.ViewCommands")
    Assert.NotNull(t)
    let members = getPublicMembers t
    assertContains members "setCamera"
    assertContains members "setZoom"
    assertContains members "wireframe"

[<Fact>]
let ``Presets public API matches baseline`` () =
    let t = typeof<PhysicsClient.Session.Session>.Assembly.GetType("PhysicsClient.Presets")
    Assert.NotNull(t)
    let members = getPublicMembers t
    for name in [| "marble"; "bowlingBall"; "beachBall"; "crate"; "brick"; "boulder"; "die" |] do
        assertContains members name

[<Fact>]
let ``Generators public API matches baseline`` () =
    let t = typeof<PhysicsClient.Session.Session>.Assembly.GetType("PhysicsClient.Generators")
    Assert.NotNull(t)
    let members = getPublicMembers t
    for name in [| "randomSpheres"; "randomBoxes"; "randomBodies"; "stack"; "row"; "grid"; "pyramid" |] do
        assertContains members name

[<Fact>]
let ``Steering public API matches baseline`` () =
    let t = typeof<PhysicsClient.Session.Session>.Assembly.GetType("PhysicsClient.Steering")
    Assert.NotNull(t)
    let members = getPublicMembers t
    for name in [| "push"; "pushVec"; "launch"; "spin"; "stop" |] do
        assertContains members name

[<Fact>]
let ``StateDisplay public API matches baseline`` () =
    let t = typeof<PhysicsClient.Session.Session>.Assembly.GetType("PhysicsClient.StateDisplay")
    Assert.NotNull(t)
    let members = getPublicMembers t
    for name in [| "listBodies"; "inspect"; "status"; "snapshot" |] do
        assertContains members name

[<Fact>]
let ``LiveWatch public API matches baseline`` () =
    let t = typeof<PhysicsClient.Session.Session>.Assembly.GetType("PhysicsClient.LiveWatch")
    Assert.NotNull(t)
    let members = getPublicMembers t
    assertContains members "watch"

[<Fact>]
let ``MeshResolver public API matches baseline`` () =
    let t = typeof<PhysicsClient.Session.Session>.Assembly.GetType("PhysicsClient.MeshResolver")
    Assert.NotNull(t)
    let members = getPublicMembers t
    for name in [| "create"; "fetchMissingSync"; "processNewMeshes"; "resolve" |] do
        assertContains members name
