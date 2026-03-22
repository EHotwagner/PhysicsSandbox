// Demo 15: Overload
// Everything at once: 200+ bodies, forces, gravity shifts, camera sweep.
// The ultimate stress ceiling test.
//
// Usage: dotnet fsi Scripting/demos/Demo15_Overload.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Presets
open PhysicsClient.Generators
open PhysicsClient.StateDisplay

module Demo15 =
    let name = "Overload"
    let description = "Everything at once: 200+ bodies, forces, gravity shifts, camera sweep."

    let run (s: Session) =
        resetSimulation s
        let totalSw = System.Diagnostics.Stopwatch.StartNew()
        setCamera s (20.0, 12.0, 20.0) (0.0, 2.0, 0.0) |> ignore
        let pyramidIds =
            timed "Act 1 — pyramid + stack + row" (fun () ->
                let pIds = pyramid s 7 (Some (-5.0, 0.0, 0.0)) |> ok
                stack s 10 (Some (5.0, 0.0, 0.0)) |> ok |> ignore
                row s 12 (Some (-5.0, 0.0, 5.0)) |> ok |> ignore
                runFor s 2.0
                pIds)
        timed "Act 2 — 100 random spheres" (fun () ->
            let cmds =
                [ for i in 0 .. 99 do
                    let x = float (i % 10) * 1.5 - 7.0
                    let z = float (i / 10) * 1.5 - 7.0
                    let y = 8.0 + float (i / 20) * 2.0
                    makeSphereCmd (nextId "sphere") (x, y, z) 0.25 0.8 ]
            batchAdd s cmds
            runFor s 3.0)
        printfn "  200+ bodies active"
        status s
        timed "Act 3 — impulse storm" (fun () ->
            setCamera s (0.0, 20.0, 15.0) (0.0, 2.0, 0.0) |> ignore
            wireframe s true |> ignore
            let impCmds =
                [ for id in pyramidIds do makeImpulseCmd id (0.0, 10.0, 3.0) ]
            batchAdd s impCmds
            runFor s 3.0
            wireframe s false |> ignore)
        printfn "  Bodies after impulse storm:"
        status s
        timed "Act 4 — gravity chaos" (fun () ->
            setCamera s (12.0, 3.0, 12.0) (0.0, 4.0, 0.0) |> ignore
            setGravity s (0.0, 10.0, 0.0) |> ignore
            runFor s 2.0
            setGravity s (6.0, 0.0, 6.0) |> ignore
            setCamera s (-12.0, 5.0, -12.0) (0.0, 3.0, 0.0) |> ignore
            runFor s 2.0
            setGravity s (0.0, -9.81, 0.0) |> ignore)
        timed "Act 5 — camera sweep" (fun () ->
            wireframe s true |> ignore
            runFor s 1.0
            wireframe s false |> ignore
            for a in 0..7 do
                let angle = float a * 0.785
                setCamera s (18.0 * cos angle, 8.0, 18.0 * sin angle) (0.0, 2.0, 0.0) |> ignore
                sleep 400)
        totalSw.Stop()
        printfn "  [TIME] Total overload: %d ms" totalSw.ElapsedMilliseconds
        printfn "  Overload complete!"
        status s

runStandalone Demo15.name Demo15.run
