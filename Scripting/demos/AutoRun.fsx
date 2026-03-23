// AutoRun.fsx — Non-interactive demo runner (reuses AllDemos definitions)
// Usage: dotnet fsi Scripting/demos/AutoRun.fsx [server-address]

#load "Prelude.fsx"
#load "AllDemos.fsx"

open Prelude
open AllDemos
open PhysicsClient.Session

let serverAddress =
    match fsi.CommandLineArgs |> Array.tryItem 1 with
    | Some a -> a
    | None -> "http://localhost:5180"

printfn "\n============================================"
printfn "  PhysicsSandbox Demo Runner — %d demos" demos.Length
printfn "============================================\n"
printfn "Connecting to %s..." serverAddress
let s = connect serverAddress |> ok
printfn "Connected!\n"

let mutable passed = 0
let mutable failed = 0
for i in 0 .. demos.Length - 1 do
    let d = demos.[i]
    printfn "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    printfn "  Demo %d/%d: %s" (i+1) demos.Length d.Name
    printfn "  %s" d.Description
    printfn "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n"
    try d.Run s; passed <- passed + 1; printfn "\n  ✓ Complete"
    with ex -> failed <- failed + 1; printfn "\n  ✗ FAILED: %s" ex.Message
    printfn ""; sleep 1000

printfn "============================================"
printfn "  Results: %d passed, %d failed" passed failed
printfn "============================================\n"
resetSimulation s; disconnect s; printfn "Done!"
