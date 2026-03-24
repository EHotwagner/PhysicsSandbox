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
        setNarration s "Setting up 80-body force frenzy"
        smoothCamera s (0.0, 12.0, 20.0) (0.0, 3.0, 0.0) 1.5
        sleep 1700
        setDemoInfo s "Demo 13: Force Frenzy" "Force application stress — constant forces pushing bodies around."

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

        // Smoothly track camera to centroid of dynamic bodies
        let camDist = 22.0   // constant distance from centroid
        let camHeight = 10.0  // height offset above centroid
        let mutable camX = 0.0
        let mutable camY = camHeight
        let mutable camZ = camDist
        let mutable lookX = 0.0
        let mutable lookY = 0.0
        let mutable lookZ = 0.0
        let smooth = 0.15  // lerp factor per step (0=no move, 1=instant snap)

        let lerp (current: float) (target: float) (t: float) = current + (target - current) * t

        let trackBodies () =
            match snapshot s with
            | Some state when state.Bodies.Count > 0 ->
                let bodies = state.Bodies |> Seq.filter (fun b -> not b.IsStatic)
                let n = bodies |> Seq.length |> float
                if n > 0.0 then
                    let cx = bodies |> Seq.sumBy (fun b -> b.Position.X) |> fun s -> s / n
                    let cy = bodies |> Seq.sumBy (fun b -> b.Position.Y) |> fun s -> s / n
                    let cz = bodies |> Seq.sumBy (fun b -> b.Position.Z) |> fun s -> s / n
                    // Smooth the look-at target
                    lookX <- lerp lookX cx smooth
                    lookY <- lerp lookY cy smooth
                    lookZ <- lerp lookZ cz smooth
                    // Position camera at constant distance behind look-at
                    let targetCamX = lookX
                    let targetCamY = lookY + camHeight
                    let targetCamZ = lookZ + camDist
                    camX <- lerp camX targetCamX smooth
                    camY <- lerp camY targetCamY smooth
                    camZ <- lerp camZ targetCamZ smooth
                    setCamera s (camX, camY, camZ) (lookX, lookY, lookZ) |> ignore
            | _ -> ()

        // Run simulation in steps, tracking camera each step
        let runTracking (seconds: float) (stepMs: int) =
            play s |> ignore
            let steps = int (seconds * 1000.0) / stepMs
            for _ in 1 .. steps do
                sleep stepMs
                trackBodies ()
            pause s |> ignore

        // Round 1: upward impulses — tightly packed so they collide on the way up
        setNarration s "Round 1: Upward impulses — bodies colliding on the way up"
        timed "Round 1 — upward impulses (3s)" (fun () ->
            let impCmds = [ for id in ids do makeImpulseCmd id (0.0, 12.0, 0.0) ]
            batchAdd s impCmds
            printfn "  Launch! Bodies colliding on the way up..."
            runTracking 3.0 200)

        // Round 2: torques + mild sideways gravity — spinning pile drifts
        setNarration s "Round 2: Torques + sideways gravity — spinning pile drifts"
        timed "Round 2 — torques + sideways gravity (3s)" (fun () ->
            let torCmds = [ for id in ids do makeTorqueCmd id (0.0, 30.0, 15.0) ]
            batchAdd s torCmds
            setGravity s (4.0, -5.0, 0.0) |> ignore
            printfn "  Spinning + sliding sideways..."
            runTracking 3.0 200)

        // Round 3: strong inward impulses + reduced gravity — bodies swarm toward center
        setNarration s "Round 3: Inward impulses under low gravity — swarming!"
        setGravity s (0.0, -2.0, 0.0) |> ignore
        timed "Round 3 — inward impulses + low gravity (5s)" (fun () ->
            let inwardCmds =
                [ for idx in 0 .. 79 do
                    makeImpulseCmd ids.[idx] (0.0, 10.0, 0.0) ]
            batchAdd s inwardCmds
            printfn "  Swarming inward under low gravity!"
            runTracking 5.0 200)

        setNarration s "Gravity restored — settling down"
        setGravity s (0.0, -9.81, 0.0) |> ignore
        printfn "  Gravity restored — settling..."
        runTracking 3.0 200
        clearNarration s
        status s

runStandalone Demo13.name Demo13.run
