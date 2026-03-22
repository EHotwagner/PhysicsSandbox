module PhysicsSandbox.Scripting.SimulationLifecycle

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsSandbox.Scripting.Helpers

let resetSimulation (s: Session) =
    pause s |> ignore
    try
        reset s |> ok
    with ex ->
        printfn "  [RESET ERROR] %s — falling back to manual clear" ex.Message
        clearAll s |> ignore
    PhysicsClient.IdGenerator.reset ()
    addPlane s None None |> ignore
    setGravity s (0.0, -9.81, 0.0) |> ignore
    sleep 100

let runFor (s: Session) (seconds: float) =
    play s |> ignore
    sleep (int (seconds * 1000.0))
    pause s |> ignore

let nextId prefix = PhysicsClient.IdGenerator.nextId prefix
