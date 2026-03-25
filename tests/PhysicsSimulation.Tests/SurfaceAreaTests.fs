module PhysicsSimulation.Tests.SurfaceAreaTests

open Xunit
open TestHelpers

[<Fact>]
let ``SimulationWorld public API matches baseline`` () =
    assertModuleSurface
        typeof<PhysicsSimulation.SimulationWorld.World>
        "PhysicsSimulation.SimulationWorld"
        [ "addBody"; "addConstraint"; "applyForce"; "applyImpulse"; "applyTorque"
          "clearForces"; "create"; "currentState"; "destroy"
          "isRunning"; "registerShape"; "removeBody"; "removeConstraint"
          "resetSimulation"; "setBodyPose"; "setCollisionFilter"
          "setGravity"; "setRunning"; "step"; "time"
          "unregisterShape" ]

[<Fact>]
let ``CommandHandler public API matches baseline`` () =
    assertModuleSurface
        typeof<PhysicsSimulation.SimulationWorld.World>
        "PhysicsSimulation.CommandHandler"
        [ "handle" ]

[<Fact>]
let ``SimulationClient public API matches baseline`` () =
    assertModuleSurface
        typeof<PhysicsSimulation.SimulationWorld.World>
        "PhysicsSimulation.SimulationClient"
        [ "run" ]

[<Fact>]
let ``MeshIdGenerator public API matches baseline`` () =
    assertModuleSurface
        typeof<PhysicsSimulation.SimulationWorld.World>
        "PhysicsSimulation.MeshIdGenerator"
        [ "computeMeshId"; "computeBoundingBox" ]
