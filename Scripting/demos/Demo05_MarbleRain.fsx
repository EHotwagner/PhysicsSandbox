// Demo 05: Marble Rain — Mixed shapes rain from the sky and pile up.
// Usage: dotnet fsi Scripting/demos/Demo05_MarbleRain.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Generators
open PhysicsClient.StateDisplay

let name = "Marble Rain"

let run s =
    resetSimulation s
    setDemoInfo s "Demo 05: Marble Rain" "Continuous marble rain — hundreds of marbles falling from the sky."
    setNarration s "High angle — watching the sky for incoming marbles"
    smoothCamera s (6.0, 10.0, 6.0) (0.0, 0.0, 0.0) 1.5
    sleep 1700
    let ids = randomSpheres s 20 (Some 42) |> ok
    setNarration s "Wave 1 — random spheres raining down"
    printfn "  Wave 1: %d random spheres raining down..." ids.Length
    runFor s 3.0
    let rng = System.Random(99)
    let colors = [| projectileColor; targetColor; accentYellow; accentGreen; accentPurple; accentOrange |]
    let wave2 =
        [ for i in 0 .. 49 do
              let x = rng.NextDouble() * 6.0 - 3.0
              let z = rng.NextDouble() * 6.0 - 3.0
              let y = 8.0 + float i * 0.5
              let half = 0.2 + rng.NextDouble() * 0.1
              makeBoxCmd (nextId "box") (x, y, z) (half, half, half) 2.0
              |> withColorAndMaterial (Some colors.[i % colors.Length]) None ]
    batchAdd s wave2
    setNarration s "Wave 2 — 50 colorful crates joining the pile"
    printfn "  Wave 2: 50 colorful crates joining the pile!"
    smoothCamera s (4.0, 6.0, 4.0) (0.0, 1.0, 0.0) 1.5
    sleep 1700
    runFor s 30.0
    setNarration s "Closing in on the pile"
    smoothCamera s (2.0, 3.0, 2.0) (0.0, 0.5, 0.0) 2.0
    sleep 2200
    runFor s 28.0
    clearNarration s
    printfn "  Settled."
    listBodies s

runStandalone name run
