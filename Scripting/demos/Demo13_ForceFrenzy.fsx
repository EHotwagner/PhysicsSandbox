// Demo 13: Force Frenzy
// 80 tightly-packed bodies hit with 3 rounds of escalating forces — collisions everywhere.
//
// Usage: dotnet fsi Scripting/demos/Demo13_ForceFrenzy.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay

module Demo13 =
    let name = "Force Frenzy"
    let description = "80 tightly-packed bodies hit with 3 rounds of escalating forces — collisions everywhere."

    let run (s: Session) =
        resetSimulation s
        setCamera s (10.0, 8.0, 10.0) (0.0, 1.0, 0.0) |> ignore

        // Tight 8x10 grid (0.7m spacing) — bodies will collide when forces hit
        let ids =
            timed "Create 80 bodies" (fun () ->
                let bodyIds = [ for _ in 0 .. 79 -> nextId "sphere" ]
                let cmds =
                    [ for idx in 0 .. 79 do
                        let x = float (idx % 8) * 0.7 - 2.45
                        let z = float (idx / 8) * 0.7 - 3.15
                        // Alternate between lighter and heavier for variety
                        let mass = if idx % 3 = 0 then 0.5 else 1.5
                        let radius = if idx % 3 = 0 then 0.2 else 0.25
                        makeSphereCmd bodyIds.[idx] (x, 0.5, z) radius mass ]
                batchAdd s cmds
                bodyIds)
        printfn "  80 spheres in tight 8x10 grid"

        timed "Settle (1.5s)" (fun () -> runFor s 1.5)

        // Round 1: upward impulses — tightly packed so they collide on the way up
        timed "Round 1 — upward impulses (3s)" (fun () ->
            let impCmds = [ for id in ids do makeImpulseCmd id (0.0, 12.0, 0.0) ]
            batchAdd s impCmds
            printfn "  Launch! Bodies colliding on the way up..."
            setCamera s (8.0, 2.0, 8.0) (0.0, 5.0, 0.0) |> ignore
            runFor s 3.0)

        // Round 2: torques + sideways gravity — spinning pile slides
        timed "Round 2 — torques + sideways gravity (3s)" (fun () ->
            let torCmds = [ for id in ids do makeTorqueCmd id (0.0, 30.0, 15.0) ]
            batchAdd s torCmds
            setGravity s (10.0, -3.0, 0.0) |> ignore
            setCamera s (-12.0, 5.0, 8.0) (0.0, 2.0, 0.0) |> ignore
            printfn "  Spinning + sliding sideways..."
            runFor s 3.0)

        // Round 3: inward impulses + reduced gravity — bodies swarm toward center
        timed "Round 3 — inward impulses + low gravity (3s)" (fun () ->
            let inwardCmds =
                [ for idx in 0 .. 79 do
                    let x = float (idx % 8) * 0.7 - 2.45
                    let z = float (idx / 8) * 0.7 - 3.15
                    // Push toward center
                    makeImpulseCmd ids.[idx] (-x * 3.0, 8.0, -z * 3.0) ]
            batchAdd s inwardCmds
            setGravity s (0.0, -2.0, 0.0) |> ignore  // Low gravity = longer air time
            setCamera s (0.0, 12.0, 10.0) (0.0, 3.0, 0.0) |> ignore
            printfn "  Swarming inward under low gravity!"
            runFor s 3.0)

        setGravity s (0.0, -9.81, 0.0) |> ignore
        printfn "  Gravity restored — settling..."
        setCamera s (8.0, 5.0, 8.0) (0.0, 1.0, 0.0) |> ignore
        runFor s 2.0
        status s

runStandalone Demo13.name Demo13.run
