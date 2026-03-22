// Demo 01: Hello Drop — Four different objects fall side by side.
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
    setCamera s (6.0, 5.0, 8.0) (0.0, 3.0, 0.0) |> ignore
    let dropHeight = 10.0
    bowlingBall s (Some (-2.0, dropHeight, 0.0)) None None |> ignore
    let beachId = nextId "sphere"
    batchAdd s [ makeSphereCmd beachId (0.0, dropHeight, 0.0) 0.2 0.1 ]
    let crateId = nextId "box"
    batchAdd s [ makeBoxCmd crateId (2.0, dropHeight, 0.0) (0.25, 0.25, 0.25) 20.0 ]
    let dieId = nextId "box"
    batchAdd s [ makeBoxCmd dieId (3.5, dropHeight, 0.0) (0.05, 0.05, 0.05) 0.03 ]
    printfn "  Dropping: bowling ball, beach ball, crate, die — all from %.0fm" dropHeight
    printfn "  Same gravity, different shapes and masses..."
    runFor s 2.5
    setCamera s (4.0, 1.0, 5.0) (0.5, 0.2, 0.0) |> ignore
    printfn "  Ground-level view — notice different resting positions"
    runFor s 1.5
    let impulseCmds = [ for id in [beachId; crateId; dieId] do makeImpulseCmd id (0.0, 5.0, 0.0) ]
    batchAdd s impulseCmds
    printfn "  Upward impulse applied — watch the light ones fly!"
    setCamera s (6.0, 5.0, 8.0) (0.0, 3.0, 0.0) |> ignore
    runFor s 3.0
    listBodies s

runStandalone name run
