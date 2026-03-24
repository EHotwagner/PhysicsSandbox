// 17_QueryRange.fsx — Raycasts, overlap tests, and sweep casts
// Usage: dotnet fsi Scripting/demos/17_QueryRange.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands

let run (s: Session) =
    resetSimulation s
    setNarration s "Building the walled query arena"
    smoothCamera s (10.0, 8.0, 10.0) (0.0, 2.0, 0.0) 1.5
    sleep 1700
    setDemoInfo s "Demo 17: Query Range" "Raycasts, overlap tests, and sweep casts with varied geometry."

    // Build walled pit (4 static walls)
    let wallThickness = 0.25
    let wallHeight = 2.0
    let wallLength = 5.0
    batchAdd s [
        makeBoxCmd (nextId "wall") (0.0, wallHeight / 2.0, wallLength / 2.0 + wallThickness)
            (wallLength / 2.0, wallHeight / 2.0, wallThickness) 0.0
            |> withColorAndMaterial (Some structureColor) None
        makeBoxCmd (nextId "wall") (0.0, wallHeight / 2.0, -(wallLength / 2.0 + wallThickness))
            (wallLength / 2.0, wallHeight / 2.0, wallThickness) 0.0
            |> withColorAndMaterial (Some structureColor) None
        makeBoxCmd (nextId "wall") (wallLength / 2.0 + wallThickness, wallHeight / 2.0, 0.0)
            (wallThickness, wallHeight / 2.0, wallLength / 2.0 + wallThickness * 2.0) 0.0
            |> withColorAndMaterial (Some structureColor) None
        makeBoxCmd (nextId "wall") (-(wallLength / 2.0 + wallThickness), wallHeight / 2.0, 0.0)
            (wallThickness, wallHeight / 2.0, wallLength / 2.0 + wallThickness * 2.0) 0.0
            |> withColorAndMaterial (Some structureColor) None
    ]
    printfn "  Built walled pit"

    // Drop 20 random colored bodies (mix of spheres and boxes)
    let rng = System.Random(42)
    let bodyColors = [| accentYellow; accentGreen |]
    let bodyIds =
        [ for i in 0 .. 19 do
            let id = nextId "body"
            let x = rng.NextDouble() * 4.0 - 2.0
            let z = rng.NextDouble() * 4.0 - 2.0
            let y = 3.0 + float i * 0.5
            let color = bodyColors.[i % 2]
            let cmd =
                if i % 2 = 0 then
                    makeSphereCmd id (x, y, z) 0.3 1.0
                else
                    makeBoxCmd id (x, y, z) (0.25, 0.25, 0.25) 1.0
            batchAdd s [ cmd |> withColorAndMaterial (Some color) None ]
            id ]
    printfn "  Dropped %d bodies into the pit" bodyIds.Length

    // Let them settle for 3 seconds
    setNarration s "Dropping bodies into the pit — settling..."
    runFor s 3.0
    printfn "  Bodies settled\n"

    // Fire 5 downward raycasts from different X positions
    setNarration s "Firing 5 downward raycasts across the pit"
    smoothCamera s (6.0, 12.0, 6.0) (0.0, 1.0, 0.0) 1.5
    sleep 1700
    printfn "  --- Raycasts ---"
    for x in [ -2.0; -1.0; 0.0; 1.0; 2.0 ] do
        let hits = queryRaycast s (x, 10.0, 0.0) (0.0, -1.0, 0.0) 20.0
        match hits with
        | (bodyId, _, _, dist) :: _ ->
            printfn "  Raycast at X=%.0f: hit %s at distance %.2f" x bodyId dist
        | [] ->
            printfn "  Raycast at X=%.0f: no hit" x

    // Overlap sphere at pit center
    setNarration s "Overlap test — sphere query at pit center"
    smoothCamera s (4.0, 6.0, 4.0) (0.0, 1.0, 0.0) 1.0
    sleep 1200
    printfn ""
    printfn "  --- Overlap ---"
    let overlapped = queryOverlapSphere s 2.0 (0.0, 1.0, 0.0)
    printfn "  Overlap at center: %d bodies" overlapped.Length

    // Sweep cast across the pit
    setNarration s "Sweep cast — sphere sweeping across the pit"
    smoothCamera s (0.0, 4.0, 8.0) (0.0, 1.0, 0.0) 1.0
    sleep 1200
    printfn ""
    printfn "  --- Sweep ---"
    let sweepResult = querySweepSphere s 0.3 (-3.0, 1.0, 0.0) (1.0, 0.0, 0.0) 6.0
    match sweepResult with
    | Some (bodyId, _, _, dist) ->
        printfn "  Sweep: hit %s at distance %.2f" bodyId dist
    | None ->
        printfn "  Sweep: no hit"
    clearNarration s

runStandalone "Query Range" run
