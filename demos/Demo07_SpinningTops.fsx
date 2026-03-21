// Demo 07: Spinning Tops
// Several bodies with torque applied — spinning in place.

module Demo07 =
    let name = "Spinning Tops"
    let description = "Beach balls and crates spinning with applied torques."

    let run (s: PhysicsClient.Session.Session) =
        resetSimulation s

        // Camera: top-down angled view
        setCamera s (0.0, 8.0, 6.0) (0.0, 0.5, 0.0) |> ignore

        // Batch-create 4 bodies: 2 beach balls (r=0.2, m=0.1) + 2 crates (half=0.5, m=20)
        let b1id = nextId "sphere"
        let b2id = nextId "sphere"
        let b3id = nextId "box"
        let b4id = nextId "box"
        let bodyCmds = [
            makeSphereCmd b1id (-2.0, 0.25, 0.0) 0.2 0.1
            makeSphereCmd b2id (2.0, 0.25, 0.0) 0.2 0.1
            makeBoxCmd b3id (0.0, 0.55, -2.0) (0.5, 0.5, 0.5) 20.0
            makeBoxCmd b4id (0.0, 0.55, 2.0) (0.5, 0.5, 0.5) 20.0 ]
        batchAdd s bodyCmds
        printfn "  Placed 4 bodies in a circle"

        runFor s 0.5

        // Batch-apply torques: Up=Y+, North=Z-, East=X+
        let torqueCmds = [
            makeTorqueCmd b1id (0.0, 50.0, 0.0)
            makeTorqueCmd b2id (0.0, 0.0, -30.0)
            makeTorqueCmd b3id (0.0, 80.0, 0.0)
            makeTorqueCmd b4id (40.0, 0.0, 0.0) ]
        batchAdd s torqueCmds
        printfn "  Applied torques — spinning..."

        runFor s 4.0

        // Wireframe mode for visual effect
        wireframe s true |> ignore
        printfn "  Wireframe view"
        sleep 2000
        wireframe s false |> ignore
        listBodies s
