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
    setDemoInfo s "Demo 02: Bouncing Marbles" "Two waves of 75 marbles rain down and pile up with varied sizes and masses."
    setNarration s "Overhead view — marble rain incoming"
    smoothCamera s (5.0, 8.0, 5.0) (0.0, 1.0, 0.0) 1.5
    sleep 1700
    let rng = System.Random(42)
    let wave1 =
        [ for i in 0 .. 74 do
            let x = float (i % 8) * 0.5 - 1.75
            let z = float ((i / 8) % 8) * 0.5 - 1.75
            let y = 5.0 + float i * 0.25 + rng.NextDouble() * 2.0
            let radius = 0.05 + rng.NextDouble() * 0.12
            let mass = radius * radius * 10.0
            makeSphereCmd (nextId "sphere") (x, y, z) radius mass ]
    batchAdd s wave1
    setNarration s "Wave 1 — 75 marbles raining down"
    printfn "  Wave 1: 75 marbles raining down..."
    runFor s 8.0
    setNarration s "Closer view — wave 2 joining the pile"
    smoothCamera s (3.0, 4.0, 3.0) (0.0, 0.5, 0.0) 1.5
    sleep 1700
    let wave2 =
        [ for i in 0 .. 74 do
            let x = rng.NextDouble() * 4.0 - 2.0
            let z = rng.NextDouble() * 4.0 - 2.0
            let y = 10.0 + float i * 0.3
            let radius = 0.06 + rng.NextDouble() * 0.1
            let mass = radius * radius * 10.0
            makeSphereCmd (nextId "sphere") (x, y, z) radius mass ]
    batchAdd s wave2
    setNarration s "Wave 2 — 75 more marbles into the pile"
    printfn "  Wave 2: 75 more marbles into the pile!"
    runFor s 10.0
    setNarration s "All settled — 150 marbles at rest"
    smoothCamera s (2.0, 2.0, 2.0) (0.0, 0.3, 0.0) 1.5
    sleep 1700
    printfn "  Settled."
    sleep 1000
    clearNarration s
    listBodies s

runStandalone name run
