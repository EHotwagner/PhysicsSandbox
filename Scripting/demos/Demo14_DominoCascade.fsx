// Demo 14: Domino Cascade
// 120 dominoes in a semicircular path — chain reaction at scale.
//
// Usage: dotnet fsi Scripting/demos/Demo14_DominoCascade.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Steering
open PhysicsClient.StateDisplay

module Demo14 =
    let name = "Domino Cascade"
    let description = "120 dominoes in a semicircular path — chain reaction at scale."

    let run (s: Session) =
        resetSimulation s
        setCamera s (0.0, 12.0, 0.1) (0.0, 0.0, 0.0) |> ignore
        let count = 120
        let radius = 8.0
        let ids =
            timed (sprintf "Place %d dominoes" count) (fun () ->
                let dominoIds = [ for _ in 0 .. count - 1 -> nextId "box" ]
                let cmds =
                    [ for i in 0 .. count - 1 do
                        let angle = float i / float count * System.Math.PI
                        let x = radius * cos angle
                        let z = radius * sin angle
                        makeBoxCmd dominoIds.[i] (x, 0.3, z) (0.05, 0.3, 0.15) 1.0 ]
                batchAdd s cmds
                dominoIds)
        printfn "  %d dominoes in semicircle (radius %.0fm)" count radius
        runFor s 1.0

        // Brief overhead view to show the full semicircle layout
        setCamera s (0.0, 14.0, 0.1) (0.0, 0.0, 0.0) |> ignore
        printfn "  Overhead view — full semicircle"
        sleep 1000

        // Move to side view for the push
        setCamera s (radius + 2.0, 3.0, 0.0) (0.0, 0.5, 0.0) |> ignore
        printfn "  Pushing first domino..."
        push s ids.[0] East 4.0 |> ignore
        timed "Cascade propagation" (fun () ->
            runFor s 10.0)
        for i in 0..5 do
            let angle = float i / 5.0 * System.Math.PI
            let cx = (radius + 4.0) * cos angle
            let cz = (radius + 4.0) * sin angle
            setCamera s (cx, 3.0, cz) (0.0, 0.5, 0.0) |> ignore
            sleep 350
        printfn "  Cascade complete"
        status s

runStandalone Demo14.name Demo14.run
