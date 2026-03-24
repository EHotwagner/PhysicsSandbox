// Demo 07: Spinning Tops
// Six spinning objects in a ring — pushed inward for chaotic collisions.
// Usage: dotnet fsi Scripting/demos/Demo07_SpinningTops.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Steering
open PhysicsClient.StateDisplay

module Demo07 =
    let name = "Spinning Tops"
    let description = "Six spinning objects collide in the center — angular momentum chaos."

    let run (s: Session) =
        resetSimulation s

        // Camera: top-down angled view
        setCamera s (0.0, 10.0, 8.0) (0.0, 0.5, 0.0) |> ignore
        setDemoInfo s "Demo 07: Spinning Tops" "Spinning top physics — angular momentum and precession."

        // Place 6 objects in a ring (radius 2m), alternating spheres and boxes
        let radius = 2.0
        let ids =
            [ for i in 0 .. 5 do
                let angle = float i * System.Math.PI / 3.0
                let x = radius * cos angle
                let z = radius * sin angle
                let id = if i % 2 = 0 then nextId "sphere" else nextId "box"
                id ]

        let bodyCmds =
            [ for i in 0 .. 5 do
                let angle = float i * System.Math.PI / 3.0
                let x = radius * cos angle
                let z = radius * sin angle
                if i % 2 = 0 then
                    makeSphereCmd ids.[i] (x, 0.3, z) 0.25 2.0
                else
                    makeBoxCmd ids.[i] (x, 0.4, z) (0.3, 0.3, 0.3) 5.0 ]
        batchAdd s bodyCmds
        printfn "  6 objects placed in a ring"

        runFor s 0.5

        // Spin them all — strong torques for visible rotation
        let torqueCmds =
            [ makeTorqueCmd ids.[0] (0.0, 500.0, 0.0)
              makeTorqueCmd ids.[1] (0.0, -400.0, 200.0)
              makeTorqueCmd ids.[2] (0.0, 450.0, 0.0)
              makeTorqueCmd ids.[3] (300.0, 0.0, -350.0)
              makeTorqueCmd ids.[4] (0.0, -600.0, 0.0)
              makeTorqueCmd ids.[5] (-200.0, 400.0, 0.0) ]
        batchAdd s torqueCmds
        printfn "  All spinning..."
        runFor s 2.0

        // Wireframe on to see rotation clearly
        wireframe s true |> ignore
        printfn "  Wireframe on — watch the collisions!"

        // Push all objects inward toward center
        let impulseCmds =
            [ for i in 0 .. 5 do
                let angle = float i * System.Math.PI / 3.0
                let ix = -cos angle * 8.0   // inward force
                let iz = -sin angle * 8.0
                makeImpulseCmd ids.[i] (ix, 0.5, iz) ]
        batchAdd s impulseCmds
        printfn "  Pushed inward — COLLISION!"

        // Camera drops to side view for dramatic impact
        setCamera s (5.0, 3.0, 5.0) (0.0, 0.5, 0.0) |> ignore
        runFor s 3.0

        // Let chaos settle
        wireframe s false |> ignore
        setCamera s (4.0, 2.0, 4.0) (0.0, 0.3, 0.0) |> ignore
        printfn "  Settling..."
        runFor s 2.0

        listBodies s

runStandalone Demo07.name Demo07.run
