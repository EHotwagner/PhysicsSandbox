// Demo 11: Body Scaling
// Progressive body count with tight packing — collision-dense stress test.
//
// Usage: dotnet fsi Scripting/demos/Demo11_BodyScaling.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay

module Demo11 =
    let name = "Body Scaling"
    let description = "Progressive body count with tight packing — collision-dense stress test."

    let run (s: Session) =
        resetSimulation s
        let tiers = [50; 100; 200; 500]
        for tier in tiers do
            printfn "  === Tier: %d bodies ===" tier
            resetSimulation s
            let dist = if tier <= 100 then 10.0 elif tier <= 200 then 18.0 else 30.0
            setCamera s (dist, dist * 0.6, dist) (0.0, 2.0, 0.0) |> ignore
            let rng = System.Random(tier)
            timed (sprintf "Tier %d setup" tier) (fun () ->
                let cols = int (sqrt (float tier))
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
            timed (sprintf "Tier %d simulation (3s)" tier) (fun () ->
                runFor s 3.0)
            printfn "  Tier %d complete" tier
        printfn "  All tiers complete — check [TIME] markers for degradation"
        status s

runStandalone Demo11.name Demo11.run
