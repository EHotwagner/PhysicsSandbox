// Demo 11: Body Scaling
// Progressive body count: 50 → 100 → 200 → 500.
// Finds the degradation point where simulation performance drops.
//
// Usage: dotnet fsi Scripting/demos/Demo11_BodyScaling.fsx [server-address]

#load "Prelude.fsx"
open Prelude.DemoHelpers

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay

let serverAddress =
    match fsi.CommandLineArgs |> Array.tryItem 1 with
    | Some addr -> addr
    | None -> "http://localhost:5180"

printfn "Connecting to %s..." serverAddress
let s = connect serverAddress |> ok
printfn "Connected!\n"
printfn "Demo 11: Body Scaling\n"

resetSimulation s
let tiers = [50; 100; 200; 500]
for tier in tiers do
    printfn "  === Tier: %d bodies ===" tier
    resetSimulation s
    let dist = if tier <= 100 then 15.0 elif tier <= 200 then 25.0 else 40.0
    setCamera s (dist, dist * 0.6, dist) (0.0, 2.0, 0.0) |> ignore
    timed (sprintf "Tier %d setup" tier) (fun () ->
        let cmds =
            [ for i in 0 .. tier - 1 do
                let x = float (i % 10) * 1.2 - 6.0
                let y = 2.0 + float (i / 100) * 3.0
                let z = float ((i / 10) % 10) * 1.2 - 6.0
                makeSphereCmd (nextId "sphere") (x, y, z) 0.3 1.0 ]
        batchAdd s cmds)
    timed (sprintf "Tier %d simulation (3s)" tier) (fun () ->
        runFor s 3.0)
    printfn "  Tier %d complete" tier
printfn "  All tiers complete — check [TIME] markers for degradation"
status s

printfn "\nDone."
resetSimulation s
disconnect s
