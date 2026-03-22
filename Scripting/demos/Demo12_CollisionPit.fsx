// Demo 12: Collision Pit
// 120 spheres dropped into a walled pit — maximum collision density.
//
// Usage: dotnet fsi Scripting/demos/Demo12_CollisionPit.fsx [server-address]

#load "Prelude.fsx"
open Prelude.DemoHelpers

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay

let serverAddress =
    match fsi.CommandLineArgs |> Array.tryItem 1 with
    | Some addr -> addr
    | None -> "http://localhost:5000"

printfn "Connecting to %s..." serverAddress
let s = connect serverAddress |> ok
printfn "Connected!\n"
printfn "Demo 12: Collision Pit\n"

resetSimulation s
setCamera s (8.0, 10.0, 8.0) (0.0, 2.0, 0.0) |> ignore
timed "Pit walls setup" (fun () ->
    let wallCmds = [
        makeBoxCmd (nextId "box") (0.0, 2.0, -2.1) (2.0, 2.0, 0.1) 0.0
        makeBoxCmd (nextId "box") (0.0, 2.0, 2.1)  (2.0, 2.0, 0.1) 0.0
        makeBoxCmd (nextId "box") (-2.1, 2.0, 0.0) (0.1, 2.0, 2.0) 0.0
        makeBoxCmd (nextId "box") (2.1, 2.0, 0.0)  (0.1, 2.0, 2.0) 0.0
    ]
    batchAdd s wallCmds)
printfn "  Pit built (4x4m walled enclosure)"
timed "Drop 120 spheres" (fun () ->
    let sphereCmds =
        [ for i in 0 .. 119 do
            let x = float (i % 10) * 0.35 - 1.6
            let z = float ((i / 10) % 12) * 0.35 - 1.9
            let y = 6.0 + float (i / 10) * 0.5
            makeSphereCmd (nextId "sphere") (x, y, z) 0.15 0.5 ]
    batchAdd s sphereCmds)
printfn "  120 spheres dropping into pit..."
timed "Settling simulation (8s)" (fun () ->
    runFor s 8.0)
setCamera s (4.0, 3.0, 4.0) (0.0, 1.5, 0.0) |> ignore
printfn "  Close-up view of settled pit"
sleep 1000
status s

printfn "\nDone."
resetSimulation s
disconnect s
