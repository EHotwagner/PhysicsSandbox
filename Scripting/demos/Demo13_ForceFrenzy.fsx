// Demo 13: Force Frenzy
// 100 bodies hit with 3 rounds of escalating impulses, torques, and gravity shifts.
//
// Usage: dotnet fsi Scripting/demos/Demo13_ForceFrenzy.fsx [server-address]

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
printfn "Demo 13: Force Frenzy\n"

resetSimulation s
setCamera s (15.0, 10.0, 15.0) (0.0, 1.0, 0.0) |> ignore
let ids =
    timed "Create 100 bodies" (fun () ->
        let bodyIds = [ for _ in 0 .. 99 -> nextId "sphere" ]
        let cmds =
            [ for idx in 0 .. 99 do
                let x = float (idx % 10) * 1.5 - 7.0
                let z = float (idx / 10) * 1.5 - 7.0
                makeSphereCmd bodyIds.[idx] (x, 0.5, z) 0.3 1.0 ]
        batchAdd s cmds
        bodyIds)
printfn "  100 spheres in 10x10 grid"
timed "Settle (2s)" (fun () -> runFor s 2.0)
timed "Round 1 — impulses (3s)" (fun () ->
    let impCmds = [ for id in ids do makeImpulseCmd id (0.0, 8.0, 0.0) ]
    batchAdd s impCmds
    runFor s 3.0)
timed "Round 2 — torques + sideways gravity (3s)" (fun () ->
    let torCmds = [ for id in ids do makeTorqueCmd id (0.0, 20.0, 10.0) ]
    batchAdd s torCmds
    setGravity s (8.0, -2.0, 0.0) |> ignore
    setCamera s (-15.0, 5.0, 10.0) (0.0, 2.0, 0.0) |> ignore
    runFor s 3.0)
timed "Round 3 — strong impulses + reversed gravity (3s)" (fun () ->
    let impCmds2 = [ for id in ids do makeImpulseCmd id (5.0, 15.0, -5.0) ]
    batchAdd s impCmds2
    setGravity s (0.0, 12.0, 0.0) |> ignore
    setCamera s (10.0, 2.0, 15.0) (0.0, 5.0, 0.0) |> ignore
    runFor s 3.0)
setGravity s (0.0, -9.81, 0.0) |> ignore
printfn "  Gravity restored"
runFor s 2.0
status s

printfn "\nDone."
resetSimulation s
disconnect s
