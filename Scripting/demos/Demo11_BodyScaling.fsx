// Demo 11: Body Scaling
// Progressive body count with tight packing — collision-dense stress test.
//
// Usage: dotnet fsi Scripting/demos/Demo11_BodyScaling.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Steering
open PhysicsClient.StateDisplay

module Demo11 =
    let name = "Body Scaling"
    let description = "Progressive body count with tight packing — collision-dense stress test."

    let run (s: Session) =
        resetSimulation s
        setDemoInfo s "Demo 11: Body Scaling" "Bodies with variable mass and scale — watch the heavy ones dominate."
        let tiers = [50; 100; 200; 500]
        for tier in tiers do
            printfn "  === Tier: %d bodies ===" tier
            resetSimulation s
            let dist = if tier <= 100 then 10.0 elif tier <= 200 then 18.0 else 30.0
            setCamera s (dist, dist * 0.6, dist) (0.0, 2.0, 0.0) |> ignore
            let rng = System.Random(tier)
            let cols = int (sqrt (float tier))
            timed (sprintf "Tier %d setup" tier) (fun () ->
                let cmds =
                    [ for i in 0 .. tier - 1 do
                        let x = float (i % cols) * 0.7 - float cols * 0.35
                        let z = float ((i / cols) % cols) * 0.7 - float cols * 0.35
                        let y = 2.0 + float (i / (cols * cols)) * 1.5
                        // Alternate spheres and boxes for visual variety
                        if i % 3 = 0 then
                            makeBoxCmd (nextId "box") (x, y, z) (0.2, 0.2, 0.2) (0.5 + rng.NextDouble() * 2.0)
                        else
                            makeSphereCmd (nextId "sphere") (x, y, z) (0.15 + rng.NextDouble() * 0.1) (0.5 + rng.NextDouble() * 2.0) ]
                batchAdd s cmds)
            // Let bodies settle
            timed (sprintf "Tier %d settle (3s)" tier) (fun () ->
                runFor s 3.0)
            // Smash bowling spheres into the pile from different directions
            let bowlingCount = max 3 (tier / 20)
            printfn "  Launching %d bowling spheres!" bowlingCount
            let bowlingIds =
                [ for i in 0 .. bowlingCount - 1 do
                    let angle = float i / float bowlingCount * 2.0 * System.Math.PI
                    let spawnDist = float cols * 0.5 + 5.0
                    let sx = spawnDist * cos angle
                    let sz = spawnDist * sin angle
                    let bid = nextId "bowling"
                    addSphere s (sx, 1.5, sz) 0.5 10.0 (Some bid) None None None None None |> ignore
                    // Launch toward center
                    pushVec s bid (-sx * 5.0, 2.0, -sz * 5.0) |> ignore
                    bid ]
            timed (sprintf "Tier %d chaos (12s)" tier) (fun () ->
                runFor s 6.0
                // Second volley from above
                printfn "  Aerial bombardment!"
                for i in 0 .. bowlingCount / 2 do
                    let x = rng.NextDouble() * 4.0 - 2.0
                    let z = rng.NextDouble() * 4.0 - 2.0
                    let bid = nextId "bomb"
                    addSphere s (x, 15.0, z) 0.4 8.0 (Some bid) None None None None None |> ignore
                    pushVec s bid (0.0, -20.0, 0.0) |> ignore
                runFor s 6.0)
            printfn "  Tier %d complete" tier
        printfn "  All tiers complete — check [TIME] markers for degradation"
        status s

runStandalone Demo11.name Demo11.run
