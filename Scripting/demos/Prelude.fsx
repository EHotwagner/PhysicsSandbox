// Shared preamble for all demo scripts
// Usage: #load "Prelude.fsx" at the top of any demo script

#r "nuget: PhysicsClient"
#r "nuget: Microsoft.Extensions.Logging.Abstractions"
#r "nuget: Spectre.Console"

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Presets
open PhysicsClient.Generators
open PhysicsClient.Steering
open PhysicsClient.StateDisplay
open PhysicsSandbox.Shared.Contracts

let ok r = r |> Result.defaultWith (fun e -> failwith e)
let sleep (ms: int) = System.Threading.Thread.Sleep(ms)

let runFor (s: Session) (seconds: float) =
    play s |> ignore
    sleep (int (seconds * 1000.0))
    pause s |> ignore

let toVec3 (x: float, y: float, z: float) =
    let v = Vec3()
    v.X <- x; v.Y <- y; v.Z <- z
    v

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

let nextId prefix = PhysicsClient.IdGenerator.nextId prefix

let makeSphereCmd (id: string) (pos: float * float * float) (radius: float) (mass: float) =
    let sphere = Sphere()
    sphere.Radius <- radius
    let shape = Shape()
    shape.Sphere <- sphere
    let body = AddBody()
    body.Id <- id
    body.Position <- toVec3 pos
    body.Mass <- mass
    body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

let makeBoxCmd (id: string) (pos: float * float * float) (halfExtents: float * float * float) (mass: float) =
    let box = Box()
    box.HalfExtents <- toVec3 halfExtents
    let shape = Shape()
    shape.Box <- box
    let body = AddBody()
    body.Id <- id
    body.Position <- toVec3 pos
    body.Mass <- mass
    body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

let makeImpulseCmd (bodyId: string) (impulse: float * float * float) =
    let ai = ApplyImpulse()
    ai.BodyId <- bodyId
    ai.Impulse <- toVec3 impulse
    let cmd = SimulationCommand()
    cmd.ApplyImpulse <- ai
    cmd

let makeTorqueCmd (bodyId: string) (torque: float * float * float) =
    let at = ApplyTorque()
    at.BodyId <- bodyId
    at.Torque <- toVec3 torque
    let cmd = SimulationCommand()
    cmd.ApplyTorque <- at
    cmd

let timed (label: string) (f: unit -> 'a) : 'a =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let result = f ()
    sw.Stop()
    printfn "  [TIME] %s: %d ms" label sw.ElapsedMilliseconds
    result

let batchAdd (s: Session) (commands: SimulationCommand list) =
    let chunks = commands |> List.chunkBySize 100
    for chunk in chunks do
        let response = batchCommands s chunk |> ok
        let failures = response.Results |> Seq.filter (fun r -> not r.Success) |> Seq.toList
        if failures.Length > 0 then
            for f in failures do
                printfn "  [BATCH FAIL] command %d: %s" f.Index f.Message

let runStandalone (name: string) (run: Session -> unit) =
    let addr =
        match fsi.CommandLineArgs |> Array.tryItem 1 with
        | Some a -> a
        | None -> "http://localhost:5180"
    printfn "\n  %s\n" name
    printfn "  Connecting to %s..." addr
    let s = connect addr |> ok
    printfn "  Connected!\n"
    run s
    printfn "\n  Done."
    resetSimulation s
    disconnect s
