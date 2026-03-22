// Demo 08: Gravity Flip
// Light objects settle, then gravity flips through 4 directions — dramatic chaos.
// Usage: dotnet fsi Scripting/demos/Demo08_GravityFlip.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.StateDisplay

module Demo08 =
    let name = "Gravity Flip"
    let description = "Light objects under four gravity directions — up, sideways, diagonal, restored."

    let run (s: Session) =
        resetSimulation s

        setCamera s (6.0, 5.0, 6.0) (0.0, 2.0, 0.0) |> ignore

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
        printfn "  25 objects: beach balls, dice, and spheres"

        // Normal gravity — let things settle
        runFor s 2.5
        printfn "  Settled. Now the fun begins..."

        // Phase 1: REVERSE — objects fly upward
        setCamera s (5.0, 1.0, 5.0) (0.0, 6.0, 0.0) |> ignore
        printfn "  GRAVITY UP!"
        setGravity s (0.0, 15.0, 0.0) |> ignore
        runFor s 2.0

        // Phase 2: SIDEWAYS — everything slides east
        printfn "  GRAVITY EAST!"
        setGravity s (12.0, 0.0, 0.0) |> ignore
        setCamera s (-8.0, 4.0, 4.0) (2.0, 2.0, 0.0) |> ignore
        runFor s 2.0

        // Phase 3: DIAGONAL — pulls to a corner
        printfn "  GRAVITY DIAGONAL!"
        setGravity s (-8.0, -5.0, 8.0) |> ignore
        setCamera s (4.0, 6.0, -6.0) (-2.0, 1.0, 2.0) |> ignore
        runFor s 2.0

        // Phase 4: RESTORED — everything falls back
        printfn "  Gravity restored — everything falls!"
        setGravity s (0.0, -9.81, 0.0) |> ignore
        setCamera s (6.0, 5.0, 6.0) (0.0, 1.0, 0.0) |> ignore
        runFor s 2.5

        listBodies s

runStandalone Demo08.name Demo08.run
