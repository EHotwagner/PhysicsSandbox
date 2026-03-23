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
    setCamera s (6.0, 10.0, 6.0) (0.0, 0.0, 0.0) |> ignore
    let ids = randomSpheres s 20 (Some 42) |> ok
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
    printfn "  Wave 2: 50 colorful crates joining the pile!"
    setCamera s (4.0, 6.0, 4.0) (0.0, 1.0, 0.0) |> ignore
    runFor s 60.0
    printfn "  Settled."
    listBodies s

runStandalone name run
