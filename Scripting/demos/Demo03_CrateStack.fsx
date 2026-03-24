// Demo 03: Crate Stack — A tall tower hit by a bowling ball.
// Usage: dotnet fsi Scripting/demos/Demo03_CrateStack.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Presets
open PhysicsClient.Generators
open PhysicsClient.Steering
open PhysicsClient.StateDisplay

let name = "Crate Stack"

let run s =
    resetSimulation s
    setCamera s (8.0, 7.0, 4.0) (0.0, 5.0, 0.0) |> ignore
    setDemoInfo s "Demo 03: Crate Stack" "Stacking dynamics — crates piled high with careful placement."
    // Build tower from lighter boxes (2kg each instead of 20kg default crates)
    let ids =
        [ for i in 0 .. 11 do
            let id = nextId "box"
            batchAdd s [ makeBoxCmd id (0.0, 0.5 + float i * 1.0, 0.0) (0.25, 0.25, 0.25) 2.0 ]
            id ]
    printfn "  Built tower of %d light crates" ids.Length
    runFor s 2.0
    let ball = boulder s (Some (-6.0, 1.0, 0.0)) None None |> ok
    runFor s 0.5
    printfn "  Boulder ready — aiming at tower..."
    sleep 500
    // Move camera to impact angle before the hit
    setCamera s (5.0, 3.0, 5.0) (0.0, 2.0, 0.0) |> ignore
    printfn "  SMASH!"
    batchAdd s [ makeImpulseCmd ball (2000.0, 0.0, 0.0) ]
    runFor s 2.0
    printfn "  Watching debris settle..."
    runFor s 2.5
    setCamera s (0.0, 12.0, 0.1) (0.0, 0.0, 0.0) |> ignore
    printfn "  Overhead view of destruction"
    sleep 1500
    listBodies s

runStandalone name run
