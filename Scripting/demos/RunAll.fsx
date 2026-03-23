// RunAll.fsx — Interactive demo runner
// Press Space or Enter to advance to the next demo.
//
// Usage:
//   1. Start Aspire: dotnet run --project src/PhysicsSandbox.AppHost
//   2. Run demos:    dotnet fsi Scripting/demos/RunAll.fsx [server-address]

#load "Prelude.fsx"
#load "AllDemos.fsx"

open AllDemos
open PhysicsClient.Session

let serverAddress =
    match fsi.CommandLineArgs |> Array.tryItem 1 with
    | Some addr -> addr
    | None -> "http://localhost:5180"

printfn ""
printfn "╔══════════════════════════════════════════════╗"
printfn "║       PhysicsSandbox Demo Runner             ║"
printfn "║  %d demos • Press Space/Enter to advance      ║" demos.Length
printfn "╚══════════════════════════════════════════════╝"
printfn ""
printfn "Connecting to %s..." serverAddress
let s = connect serverAddress |> ok
printfn "Connected!"
printfn ""

for i in 0 .. demos.Length - 1 do
    let d = demos.[i]
    printfn "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    printfn "  Demo %d/%d: %s" (i + 1) demos.Length d.Name
    printfn "  %s" d.Description
    printfn "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    printfn ""
    printfn "  Press Space or Enter to start..."
    let mutable waiting = true
    while waiting do
        let key = System.Console.ReadKey(true)
        if key.Key = System.ConsoleKey.Spacebar
           || key.Key = System.ConsoleKey.Enter then
            waiting <- false
    printfn ""
    try
        d.Run s
    with ex ->
        printfn "  [ERROR] %s" ex.Message
    printfn ""
    if i < demos.Length - 1 then
        printfn "  Done. Press Space or Enter for next demo..."
    else
        printfn "  All demos complete!"

printfn ""
printfn "Cleaning up..."
resetSimulation s
disconnect s
printfn "Disconnected. Goodbye!"
