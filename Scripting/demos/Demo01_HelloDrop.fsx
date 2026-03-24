// Demo 01: Hello Drop — Six different shapes fall side by side with custom colors.
// Usage: dotnet fsi Scripting/demos/Demo01_HelloDrop.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Presets
open PhysicsClient.StateDisplay

let name = "Hello Drop"

let run s =
    resetSimulation s
    setDemoInfo s "Demo 01: Hello Drop" "Six different shapes fall side by side — spheres, boxes, capsule, cylinder with bouncy/sticky materials."
    setNarration s "Setting the stage — six shapes ready to drop"
    smoothCamera s (8.0, 6.0, 10.0) (0.0, 3.0, 0.0) 1.5
    sleep 1700
    let dropHeight = 10.0

    // Original shapes
    bowlingBall s (Some (-3.0, dropHeight, 0.0)) None None |> ignore
    let beachId = nextId "sphere"
    batchAdd s [ makeSphereCmd beachId (-1.0, dropHeight, 0.0) 0.2 0.1 ]
    let crateId = nextId "box"
    batchAdd s [ makeBoxCmd crateId (1.0, dropHeight, 0.0) (0.25, 0.25, 0.25) 20.0
                 |> withColorAndMaterial (Some (makeColor 1.0 0.5 0.0 1.0)) None ]

    // New shapes with colors
    let capsuleId = nextId "capsule"
    batchAdd s [ makeCapsuleCmd capsuleId (3.0, dropHeight, 0.0) 0.2 0.6 3.0
                 |> withColorAndMaterial (Some (makeColor 0.2 0.8 0.2 1.0)) None ]
    let cylinderId = nextId "cylinder"
    batchAdd s [ makeCylinderCmd cylinderId (5.0, dropHeight, 0.0) 0.25 0.4 5.0
                 |> withColorAndMaterial (Some (makeColor 0.8 0.2 0.8 1.0)) (Some bouncyMaterial) ]

    setNarration s "Shapes dropping from height — watch them fall"
    printfn "  Dropping 5 shapes from %.0fm: bowling ball, beach ball, orange crate, green capsule, purple bouncy cylinder" dropHeight
    runFor s 2.5
    setNarration s "Ground-level closeup — different resting positions"
    smoothCamera s (5.0, 1.0, 6.0) (1.0, 0.2, 0.0) 1.5
    sleep 1700
    printfn "  Ground-level view — notice different resting positions and colors"
    runFor s 1.5
    let impulseCmds = [ for id in [beachId; crateId; capsuleId; cylinderId] do makeImpulseCmd id (0.0, 5.0, 0.0) ]
    batchAdd s impulseCmds
    setNarration s "Impulse applied — light shapes fly, bouncy cylinder rebounds"
    smoothCamera s (8.0, 6.0, 10.0) (0.0, 3.0, 0.0) 1.5
    sleep 1700
    printfn "  Upward impulse applied — watch the light ones fly! The purple cylinder is bouncy!"
    runFor s 3.0
    clearNarration s
    listBodies s

runStandalone name run
