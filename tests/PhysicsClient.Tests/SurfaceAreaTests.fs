module PhysicsClient.Tests.SurfaceAreaTests

open Xunit
open TestHelpers

let private anchorType = typeof<PhysicsClient.Session.Session>

[<Fact>]
let ``IdGenerator public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsClient.IdGenerator"
        [ "nextId"; "reset" ]

[<Fact>]
let ``Session public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsClient.Session"
        [ "connect"; "disconnect"; "reconnect"; "isConnected"
          "bodyRegistry"; "latestState"; "lastStateUpdate" ]

[<Fact>]
let ``SimulationCommands public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsClient.SimulationCommands"
        [ "addSphere"; "addBox"; "addCapsule"; "addCylinder"; "addPlane"
          "addConstraint"; "removeConstraint"
          "registerShape"; "unregisterShape"; "setCollisionFilter"
          "removeBody"; "clearAll"
          "applyForce"; "applyImpulse"; "applyTorque"; "clearForces"
          "setGravity"; "play"; "pause"; "step"; "reset"; "confirmedReset"
          "raycast"; "sweepCast"; "overlap"
          "batchCommands"; "batchViewCommands" ]

[<Fact>]
let ``ViewCommands public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsClient.ViewCommands"
        [ "setCamera"; "setZoom"; "wireframe"
          "smoothCamera"; "smoothCameraWithZoom"; "setNarration"
          "cameraLookAt"; "cameraFollow"; "cameraOrbit"; "cameraChase"
          "cameraFrameBodies"; "cameraShake"; "cameraStop" ]

[<Fact>]
let ``Presets public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsClient.Presets"
        [ "marble"; "bowlingBall"; "beachBall"; "crate"; "brick"; "boulder"; "die" ]

[<Fact>]
let ``Generators public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsClient.Generators"
        [ "randomSpheres"; "randomBoxes"; "randomBodies"; "stack"; "row"; "grid"; "pyramid" ]

[<Fact>]
let ``Steering public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsClient.Steering"
        [ "push"; "pushVec"; "launch"; "spin"; "stop" ]

[<Fact>]
let ``StateDisplay public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsClient.StateDisplay"
        [ "listBodies"; "inspect"; "status"; "snapshot" ]

[<Fact>]
let ``LiveWatch public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsClient.LiveWatch"
        [ "watch" ]

[<Fact>]
let ``MeshResolver public API matches baseline`` () =
    assertModuleSurface anchorType "PhysicsClient.MeshResolver"
        [ "create"; "fetchMissingSync"; "processNewMeshes"; "resolve" ]
