// Demo 12: Collision Pit
// Staged waves of varied-size spheres dropped into a walled pit.
//
// Usage: dotnet fsi Scripting/demos/Demo12_CollisionPit.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay

module Demo12 =
    let name = "Collision Pit"
    let description = "Staged waves of varied-size spheres dropped into a walled pit."

    let run (s: Session) =
        resetSimulation s
        setCamera s (8.0, 10.0, 8.0) (0.0, 2.0, 0.0) |> ignore

        // Build the pit
        timed "Pit walls setup" (fun () ->
            let wallCmds = [
                makeBoxCmd (nextId "box") (0.0, 2.0, -2.1) (2.0, 2.0, 0.1) 0.0
                makeBoxCmd (nextId "box") (0.0, 2.0, 2.1)  (2.0, 2.0, 0.1) 0.0
                makeBoxCmd (nextId "box") (-2.1, 2.0, 0.0) (0.1, 2.0, 2.0) 0.0
                makeBoxCmd (nextId "box") (2.1, 2.0, 0.0)  (0.1, 2.0, 2.0) 0.0
            ]
            batchAdd s wallCmds)
        printfn "  Pit built (4x4m walled enclosure)"

        // Wave 1: 40 large spheres
        let rng = System.Random(55)
        timed "Wave 1 — 40 large spheres" (fun () ->
            let wave1 =
                [ for i in 0 .. 39 do
                    let x = float (i % 8) * 0.45 - 1.6
                    let z = float (i / 8) * 0.45 - 0.9
                    let y = 6.0 + rng.NextDouble() * 2.0
                    makeSphereCmd (nextId "sphere") (x, y, z) (0.15 + rng.NextDouble() * 0.1) (0.3 + rng.NextDouble() * 1.0) ]
            batchAdd s wave1)
        printfn "  Wave 1: 40 large spheres dropping..."
        runFor s 3.0

        // Wave 2: 60 small marbles from higher up
        setCamera s (5.0, 8.0, 5.0) (0.0, 3.0, 0.0) |> ignore
        timed "Wave 2 — 60 small marbles" (fun () ->
            let wave2 =
                [ for i in 0 .. 59 do
                    let x = rng.NextDouble() * 3.2 - 1.6
                    let z = rng.NextDouble() * 3.2 - 1.6
                    let y = 10.0 + rng.NextDouble() * 4.0
                    makeSphereCmd (nextId "sphere") (x, y, z) (0.06 + rng.NextDouble() * 0.06) (0.1 + rng.NextDouble() * 0.3) ]
            batchAdd s wave2)
        printfn "  Wave 2: 60 small marbles into the pile!"
        runFor s 4.0

        // Wave 3: 20 heavy boulders from above
        timed "Wave 3 — 20 heavy spheres" (fun () ->
            let wave3 =
                [ for i in 0 .. 19 do
                    let x = rng.NextDouble() * 2.4 - 1.2
                    let z = rng.NextDouble() * 2.4 - 1.2
                    let y = 12.0 + rng.NextDouble() * 3.0
                    makeSphereCmd (nextId "sphere") (x, y, z) 0.2 3.0 ]
            batchAdd s wave3)
        printfn "  Wave 3: 20 heavy spheres — IMPACT!"
        runFor s 4.0

        // Close-up view
        setCamera s (3.0, 2.0, 3.0) (0.0, 1.5, 0.0) |> ignore
        printfn "  Close-up of the overflowing pit"
        sleep 1500
        status s

runStandalone Demo12.name Demo12.run
