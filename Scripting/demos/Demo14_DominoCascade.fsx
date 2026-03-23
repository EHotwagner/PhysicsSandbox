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
open PhysicsSandbox.Shared.Contracts

module Demo14 =
    let name = "Domino Cascade"
    let description = "120 dominoes in a semicircular path — chain reaction at scale."

    let run (s: Session) =
        resetSimulation s
        // Elevated angle showing the full semicircle
        setCamera s (0.0, 10.0, -12.0) (0.0, 0.0, 3.0) |> ignore
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
                        // Rotate each domino so thin axis (local X) aligns with tangent
                        // Tangent at angle θ is (-sin θ, 0, cos θ)
                        // Y-rotation by -(π/2 + θ) maps (1,0,0) → (-sin θ, 0, cos θ)
                        let halfA = -(System.Math.PI / 4.0 + angle / 2.0)
                        let qy = sin halfA
                        let qw = cos halfA
                        let b = PhysicsSandbox.Shared.Contracts.Box()
                        b.HalfExtents <- toVec3 (0.05, 0.3, 0.15)
                        let shape = PhysicsSandbox.Shared.Contracts.Shape()
                        shape.Box <- b
                        let orient = Vec4()
                        orient.X <- 0.0; orient.Y <- qy; orient.Z <- 0.0; orient.W <- qw
                        let body = AddBody()
                        body.Id <- dominoIds.[i]
                        body.Position <- toVec3 (x, 0.3, z)
                        body.Mass <- 1.0
                        body.Shape <- shape
                        body.Orientation <- orient
                        let cmd = SimulationCommand()
                        cmd.AddBody <- body
                        cmd ]
                batchAdd s cmds
                dominoIds)
        printfn "  %d dominoes in semicircle (radius %.0fm)" count radius
        runFor s 1.0

        // Overhead view to show the full semicircle layout
        setCamera s (0.0, 16.0, 0.1) (0.0, 0.0, 4.0) |> ignore
        printfn "  Overhead view — full semicircle"
        sleep 1500

        // Pull back to a wide view of the full arc
        setCamera s (0.0, 12.0, -14.0) (0.0, 0.0, 3.0) |> ignore
        printfn "  Pushing first domino..."
        pushVec s ids.[0] (0.0, 0.0, 4.0) |> ignore
        timed "Cascade propagation" (fun () ->
            runFor s 20.0)
        printfn "  Cascade complete"
        status s

runStandalone Demo14.name Demo14.run
