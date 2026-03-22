// Demo 02: Bouncing Marbles — Two waves of varied marbles rain down and pile up.
// Usage: dotnet fsi Scripting/demos/Demo02_BouncingMarbles.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay

let name = "Bouncing Marbles"

let run s =
    resetSimulation s
    setCamera s (5.0, 8.0, 5.0) (0.0, 1.0, 0.0) |> ignore
    let rng = System.Random(42)
    let wave1 =
        [ for i in 0 .. 14 do
            let x = float (i % 5) * 0.8 - 1.6
            let z = float (i / 5) * 0.8 - 0.8
            let y = 5.0 + float i * 0.6 + rng.NextDouble() * 2.0
            let radius = 0.05 + rng.NextDouble() * 0.12
            let mass = radius * radius * 10.0
            makeSphereCmd (nextId "sphere") (x, y, z) radius mass ]
    batchAdd s wave1
    printfn "  Wave 1: 15 marbles raining down..."
    runFor s 3.0
    setCamera s (3.0, 4.0, 3.0) (0.0, 0.5, 0.0) |> ignore
    let wave2 =
        [ for i in 0 .. 9 do
            let x = rng.NextDouble() * 3.0 - 1.5
            let z = rng.NextDouble() * 3.0 - 1.5
            let y = 10.0 + float i * 0.8
            let radius = 0.06 + rng.NextDouble() * 0.1
            let mass = radius * radius * 10.0
            makeSphereCmd (nextId "sphere") (x, y, z) radius mass ]
    batchAdd s wave2
    printfn "  Wave 2: 10 more marbles into the pile!"
    runFor s 3.0
    printfn "  Settled."
    sleep 1000
    listBodies s

runStandalone name run
