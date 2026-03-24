// Demo 08: Gravity Flip
// Light objects settle, then gravity flips through 4 directions — dramatic chaos.
// Usage: dotnet fsi Scripting/demos/Demo08_GravityFlip.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay

module Demo08 =
    let name = "Gravity Flip"
    let description = "Light objects under four gravity directions — up, sideways, diagonal, restored."

    let run (s: Session) =
        resetSimulation s

        setDemoInfo s "Demo 08: Gravity Flip" "Gravity direction reversal — objects fly up then fall back down."
        setNarration s "Overview — 25 objects settling under normal gravity"
        smoothCamera s (6.0, 5.0, 6.0) (0.0, 2.0, 0.0) 1.5
        sleep 1700

        // Mix of light objects: beach balls, dice, and small marbles
        let rng = System.Random(77)
        let bodyCmds =
            [ // 8 beach balls scattered at various heights
              for i in 0 .. 7 do
                  let x = float (i % 4) * 1.2 - 1.8
                  let z = float (i / 4) * 1.2 - 0.6
                  let y = 3.0 + rng.NextDouble() * 4.0
                  makeSphereCmd (nextId "sphere") (x, y, z) 0.2 0.1
              // 12 dice scattered higher
              for i in 0 .. 11 do
                  let x = float (i % 4) * 0.8 - 1.2
                  let z = float (i / 4) * 0.8 - 1.0
                  let y = 5.0 + rng.NextDouble() * 3.0
                  makeBoxCmd (nextId "box") (x, y, z) (0.05, 0.05, 0.05) 0.03
              // 5 slightly heavier spheres as anchors
              for i in 0 .. 4 do
                  let x = float i * 1.0 - 2.0
                  makeSphereCmd (nextId "sphere") (x, 1.5, 0.0) 0.15 1.0 ]
        batchAdd s bodyCmds
        printfn "  25 objects in a walled enclosure"

        // Smooth camera tracking via body centroid
        let camDist = 12.0
        let mutable camX = 6.0
        let mutable camY = 5.0
        let mutable camZ = 6.0
        let mutable lookX = 0.0
        let mutable lookY = 2.0
        let mutable lookZ = 0.0
        let smooth = 0.08

        let lerp (c: float) (t: float) (f: float) = c + (t - c) * f

        let trackBodies () =
            match snapshot s with
            | Some state when state.Bodies.Count > 0 ->
                let bodies = state.Bodies |> Seq.filter (fun b -> not b.IsStatic)
                let n = bodies |> Seq.length |> float
                if n > 0.0 then
                    let cx = bodies |> Seq.sumBy (fun b -> b.Position.X) |> fun v -> v / n
                    let cy = bodies |> Seq.sumBy (fun b -> b.Position.Y) |> fun v -> v / n
                    let cz = bodies |> Seq.sumBy (fun b -> b.Position.Z) |> fun v -> v / n
                    lookX <- lerp lookX cx smooth
                    lookY <- lerp lookY cy smooth
                    lookZ <- lerp lookZ cz smooth
                    camX <- lerp camX (lookX + camDist * 0.5) smooth
                    camY <- lerp camY (lookY + camDist * 0.4) smooth
                    camZ <- lerp camZ (lookZ + camDist * 0.5) smooth
                    setCamera s (camX, camY, camZ) (lookX, lookY, lookZ) |> ignore
            | _ -> ()

        let runTracking (seconds: float) =
            play s |> ignore
            let steps = int (seconds * 1000.0) / 33
            for _ in 1 .. steps do
                sleep 33
                trackBodies ()
            pause s |> ignore

        // Normal gravity — let things settle
        runFor s 2.5
        setNarration s "Settled — now the gravity fun begins"
        printfn "  Settled. Now the fun begins..."

        // Short gravity pulses — camera tracks the action
        setNarration s "GRAVITY UP — objects launched skyward!"
        printfn "  GRAVITY UP!"
        setGravity s (0.0, 9.81, 0.0) |> ignore
        runTracking 0.8
        setGravity s (0.0, -9.81, 0.0) |> ignore
        runTracking 1.5

        setNarration s "GRAVITY EAST — sideways pull!"
        printfn "  GRAVITY EAST!"
        setGravity s (9.81, -2.0, 0.0) |> ignore
        runTracking 0.8
        setGravity s (0.0, -9.81, 0.0) |> ignore
        runTracking 1.5

        setNarration s "GRAVITY DIAGONAL — chaotic multi-axis pull!"
        printfn "  GRAVITY DIAGONAL!"
        setGravity s (-6.0, 6.0, 6.0) |> ignore
        runTracking 0.8
        setGravity s (0.0, -9.81, 0.0) |> ignore
        runTracking 1.5

        setNarration s "GRAVITY SOUTH — final direction change!"
        printfn "  GRAVITY SOUTH!"
        setGravity s (0.0, -2.0, -9.81) |> ignore
        runTracking 0.8
        setNarration s "Gravity restored — everything falls back down"
        printfn "  Gravity restored — everything falls!"
        setGravity s (0.0, -9.81, 0.0) |> ignore
        runTracking 2.0

        clearNarration s
        listBodies s

runStandalone Demo08.name Demo08.run
