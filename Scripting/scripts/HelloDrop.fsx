// HelloDrop — minimal validation script proving the scripting library works
// Single NuGet reference — all dependencies resolve transitively

#r "nuget: PhysicsSandbox.Scripting"

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsSandbox.Scripting.Prelude

printfn "=== HelloDrop ==="
printfn "Connecting to sandbox..."

let s = connect "http://localhost:5180" |> ok

resetSimulation s
setCamera s (5.0, 3.0, 5.0) (0.0, 1.0, 0.0) |> ignore

let id = nextId "sphere"
printfn "  Creating sphere: %s" id
let cmd = makeSphereCmd id (0.0, 10.0, 0.0) 0.5 1.0
batchAdd s [cmd]

printfn "  Running simulation for 3 seconds..."
runFor s 3.0

printfn "  Done!"
disconnect s
