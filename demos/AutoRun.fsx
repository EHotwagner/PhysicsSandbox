// AutoRun.fsx — Non-interactive demo runner (single self-contained file)
// Usage: dotnet fsi demos/AutoRun.fsx [server-address]

#r "../src/PhysicsClient/bin/Debug/net10.0/PhysicsClient.dll"
#r "../src/PhysicsSandbox.Shared.Contracts/bin/Debug/net10.0/PhysicsSandbox.Shared.Contracts.dll"
#r "../src/PhysicsSandbox.ServiceDefaults/bin/Debug/net10.0/PhysicsSandbox.ServiceDefaults.dll"
#r "nuget: Grpc.Net.Client"
#r "nuget: Google.Protobuf"
#r "nuget: Grpc.Core.Api"
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
let runFor (s: Session) (sec: float) = play s |> ignore; sleep (int (sec * 1000.0)); pause s |> ignore

let toVec3 (x: float, y: float, z: float) =
    let v = Vec3()
    v.X <- x; v.Y <- y; v.Z <- z
    v

let resetSimulation (s: Session) =
    pause s |> ignore
    try reset s |> ok
    with ex -> printfn "  [RESET ERROR] %s — falling back to manual clear" ex.Message; clearAll s |> ignore
    PhysicsClient.IdGenerator.reset ()
    addPlane s None None |> ignore; setGravity s (0.0, -9.81, 0.0) |> ignore; sleep 100

let nextId prefix = PhysicsClient.IdGenerator.nextId prefix

let makeSphereCmd (id: string) (pos: float * float * float) (radius: float) (mass: float) =
    let sphere = Sphere()
    sphere.Radius <- radius
    let shape = Shape()
    shape.Sphere <- sphere
    let body = AddBody()
    body.Id <- id; body.Position <- toVec3 pos; body.Mass <- mass; body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

let makeBoxCmd (id: string) (pos: float * float * float) (halfExtents: float * float * float) (mass: float) =
    let box = Box()
    box.HalfExtents <- toVec3 halfExtents
    let shape = Shape()
    shape.Box <- box
    let body = AddBody()
    body.Id <- id; body.Position <- toVec3 pos; body.Mass <- mass; body.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- body
    cmd

let makeImpulseCmd (bodyId: string) (impulse: float * float * float) =
    let ai = ApplyImpulse()
    ai.BodyId <- bodyId; ai.Impulse <- toVec3 impulse
    let cmd = SimulationCommand()
    cmd.ApplyImpulse <- ai
    cmd

let makeTorqueCmd (bodyId: string) (torque: float * float * float) =
    let at = ApplyTorque()
    at.BodyId <- bodyId; at.Torque <- toVec3 torque
    let cmd = SimulationCommand()
    cmd.ApplyTorque <- at
    cmd

let batchAdd (s: Session) (commands: SimulationCommand list) =
    for chunk in commands |> List.chunkBySize 100 do
        let response = batchCommands s chunk |> ok
        for f in response.Results |> Seq.filter (fun r -> not r.Success) do
            printfn "  [BATCH FAIL] command %d: %s" f.Index f.Message

let timed (label: string) (f: unit -> 'a) : 'a =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let result = f ()
    sw.Stop()
    printfn "  [TIME] %s: %d ms" label sw.ElapsedMilliseconds
    result

type Demo = { Name: string; Desc: string; Run: Session -> unit }

let demos = [|
  { Name = "Hello Drop"; Desc = "A bowling ball falls from 10m."
    Run = fun s ->
      resetSimulation s; setCamera s (5.0, 3.0, 5.0) (0.0, 1.0, 0.0) |> ignore
      bowlingBall s (Some (0.0, 10.0, 0.0)) None None |> ignore
      printfn "  Dropping bowling ball..."; runFor s 3.0; listBodies s }

  { Name = "Bouncing Marbles"; Desc = "Five marbles from different heights."
    Run = fun s ->
      resetSimulation s; setCamera s (4.0, 5.0, 4.0) (0.0, 0.5, 0.0) |> ignore
      let cmds = [ for i in 0..4 do makeSphereCmd (nextId "sphere") (float i * 0.3 - 0.6, 3.0 + float i * 2.0, 0.0) 0.01 0.005 ]
      batchAdd s cmds
      printfn "  Dropping 5 marbles..."; runFor s 4.0; listBodies s }

  { Name = "Crate Stack"; Desc = "Tower of 8 crates — push the top one."
    Run = fun s ->
      resetSimulation s; setCamera s (6.0, 5.0, 0.0) (0.0, 4.0, 0.0) |> ignore
      let ids = stack s 8 (Some (0.0, 0.0, 0.0)) |> ok
      printfn "  Stack of %d" ids.Length; runFor s 2.0
      printfn "  Pushing top crate east..."; push s (List.last ids) East 15.0 |> ignore
      runFor s 3.0; listBodies s }

  { Name = "Bowling Alley"; Desc = "Bowling ball vs pyramid of bricks."
    Run = fun s ->
      resetSimulation s; setCamera s (-5.0, 3.0, 3.0) (3.0, 1.0, 0.0) |> ignore
      pyramid s 4 (Some (5.0, 0.0, 0.0)) |> ok |> ignore
      let ball = bowlingBall s (Some (-3.0, 0.15, 0.0)) None None |> ok
      printfn "  Pyramid + ball ready"; runFor s 1.0
      printfn "  STRIKE!"; launch s ball (5.0, 0.5, 0.0) 25.0 |> ignore
      runFor s 4.0; listBodies s }

  { Name = "Marble Rain"; Desc = "20 random spheres rain down."
    Run = fun s ->
      resetSimulation s; setCamera s (8.0, 12.0, 8.0) (0.0, 0.0, 0.0) |> ignore
      let ids = randomSpheres s 20 (Some 42) |> ok
      printfn "  %d spheres generated" ids.Length; runFor s 5.0
      setCamera s (3.0, 1.0, 3.0) (0.0, 0.5, 0.0) |> ignore
      printfn "  Ground-level view"; sleep 1000; listBodies s }

  { Name = "Domino Row"; Desc = "12 dominoes toppled by a push."
    Run = fun s ->
      resetSimulation s; setCamera s (-2.0, 3.0, 6.0) (3.0, 0.5, 0.0) |> ignore
      let ids = [ for _ in 0..11 -> nextId "box" ]
      let cmds = [ for i in 0..11 do makeBoxCmd ids.[i] (float i * 0.5, 0.3, 0.0) (0.05, 0.3, 0.15) 1.0 ]
      batchAdd s cmds
      printfn "  12 dominoes placed"; runFor s 1.0
      printfn "  Toppling..."; push s ids.[0] East 3.0 |> ignore; runFor s 5.0
      setCamera s (8.0, 2.0, 4.0) (5.0, 0.0, 0.0) |> ignore; sleep 500; listBodies s }

  { Name = "Spinning Tops"; Desc = "Bodies spinning with torques + wireframe."
    Run = fun s ->
      resetSimulation s; setCamera s (0.0, 8.0, 6.0) (0.0, 0.5, 0.0) |> ignore
      let b1id = nextId "sphere"
      let b2id = nextId "sphere"
      let b3id = nextId "box"
      let b4id = nextId "box"
      let bodyCmds = [
          makeSphereCmd b1id (-2.0, 0.25, 0.0) 0.2 0.1; makeSphereCmd b2id (2.0, 0.25, 0.0) 0.2 0.1
          makeBoxCmd b3id (0.0, 0.55, -2.0) (0.5, 0.5, 0.5) 20.0; makeBoxCmd b4id (0.0, 0.55, 2.0) (0.5, 0.5, 0.5) 20.0 ]
      batchAdd s bodyCmds
      runFor s 0.5
      let torqueCmds = [
          makeTorqueCmd b1id (0.0, 50.0, 0.0); makeTorqueCmd b2id (0.0, 0.0, -30.0)
          makeTorqueCmd b3id (0.0, 80.0, 0.0); makeTorqueCmd b4id (40.0, 0.0, 0.0) ]
      batchAdd s torqueCmds
      printfn "  Spinning..."; runFor s 4.0
      wireframe s true |> ignore; printfn "  Wireframe on"; sleep 2000
      wireframe s false |> ignore; listBodies s }

  { Name = "Gravity Flip"; Desc = "Normal gravity, then reversed, then sideways."
    Run = fun s ->
      resetSimulation s; setCamera s (6.0, 4.0, 6.0) (0.0, 2.0, 0.0) |> ignore
      grid s 3 3 (Some (-2.0, 0.0, -2.0)) |> ok |> ignore
      let ballCmds = [ for i in 0..4 do makeSphereCmd (nextId "sphere") (float i * 0.8 - 1.6, 5.0 + float i, 0.3) 0.2 0.1 ]
      batchAdd s ballCmds
      printfn "  Grid + 5 balls"; runFor s 3.0
      printfn "  GRAVITY REVERSED!"
      setCamera s (5.0, 1.0, 5.0) (0.0, 5.0, 0.0) |> ignore
      setGravity s (0.0, 15.0, 0.0) |> ignore; runFor s 3.0
      printfn "  Sideways gravity..."
      setGravity s (10.0, 0.0, 0.0) |> ignore
      setCamera s (-8.0, 3.0, 0.0) (0.0, 2.0, 0.0) |> ignore; runFor s 2.0
      setGravity s (0.0, -9.81, 0.0) |> ignore; printfn "  Restored"; runFor s 2.0; listBodies s }

  { Name = "Billiards"; Desc = "Cue ball breaks a triangle formation."
    Run = fun s ->
      resetSimulation s; setCamera s (0.0, 10.0, 0.1) (0.0, 0.0, 0.0) |> ignore
      let r = 0.1
      let spacing = 0.22
      let cueId = "cue"
      let cmds = [
          for row in 0..4 do
            for col in 0..row do
              let x = float row * spacing * 0.866 + 1.0
              let z = (float col - float row / 2.0) * spacing
              makeSphereCmd (nextId "sphere") (x, r, z) r 0.17
          makeSphereCmd cueId (-2.0, r, 0.0) (r * 1.1) 0.17 ]
      batchAdd s cmds
      printfn "  15 balls + cue placed"
      setCamera s (-3.0, 1.5, 2.0) (1.0, 0.0, 0.0) |> ignore; runFor s 0.5
      printfn "  BREAK!"; launch s cueId (1.5, 0.0, 0.0) 15.0 |> ignore; runFor s 4.0
      setCamera s (0.0, 8.0, 0.1) (0.0, 0.0, 0.0) |> ignore; sleep 1000; listBodies s }

  { Name = "Chaos Scene"; Desc = "Everything: presets, generators, steering, gravity, camera sweeps."
    Run = fun s ->
      resetSimulation s; setCamera s (12.0, 8.0, 12.0) (0.0, 2.0, 0.0) |> ignore
      printfn "  Act 1: Building stage..."
      pyramid s 5 (Some (-4.0, 0.0, 0.0)) |> ok |> ignore
      stack s 6 (Some (4.0, 0.0, 0.0)) |> ok |> ignore
      row s 8 (Some (-3.0, 0.0, 3.0)) |> ok |> ignore; runFor s 1.5
      printfn "  Act 2: Bombardment!"
      setCamera s (0.0, 15.0, 10.0) (0.0, 2.0, 0.0) |> ignore
      let proj = randomSpheres s 10 (Some 99) |> ok
      let impulseCmds = [ for id in proj do makeImpulseCmd id (0.0, -20.0, 0.0) ]
      batchAdd s impulseCmds
      runFor s 3.0
      printfn "  Act 3: Boulder attack!"
      setCamera s (-10.0, 3.0, 0.0) (0.0, 2.0, 0.0) |> ignore
      let rock = boulder s (Some (-8.0, 1.0, 0.0)) None None |> ok
      launch s rock (-4.0, 2.0, 0.0) 30.0 |> ignore; runFor s 3.0
      printfn "  Act 4: Gravity chaos!"
      setCamera s (8.0, 2.0, 8.0) (0.0, 3.0, 0.0) |> ignore
      setGravity s (0.0, 8.0, 0.0) |> ignore; runFor s 2.0
      setGravity s (5.0, 0.0, 5.0) |> ignore
      setCamera s (-6.0, 4.0, -6.0) (2.0, 2.0, 2.0) |> ignore; runFor s 2.0
      setGravity s (0.0, -9.81, 0.0) |> ignore
      printfn "  Act 5: Camera sweep"
      wireframe s true |> ignore; runFor s 2.0; wireframe s false |> ignore
      for a in 0..8 do
        let r = float a * 0.7
        setCamera s (10.0 * cos r, 5.0, 10.0 * sin r) (0.0, 1.0, 0.0) |> ignore; sleep 400
      printfn "  Chaos complete!"; status s }

  { Name = "Body Scaling"; Desc = "Progressive body count: 50 → 100 → 200 → 500. Finds degradation point."
    Run = fun s ->
      resetSimulation s
      let tiers = [50; 100; 200; 500]
      for tier in tiers do
        printfn "  === Tier: %d bodies ===" tier
        resetSimulation s
        let dist = if tier <= 100 then 15.0 elif tier <= 200 then 25.0 else 40.0
        setCamera s (dist, dist * 0.6, dist) (0.0, 2.0, 0.0) |> ignore
        timed (sprintf "Tier %d setup" tier) (fun () ->
            let cmds = [ for i in 0 .. tier - 1 do
                           let x = float (i % 10) * 1.2 - 6.0
                           let y = 2.0 + float (i / 100) * 3.0
                           let z = float ((i / 10) % 10) * 1.2 - 6.0
                           makeSphereCmd (nextId "sphere") (x, y, z) 0.3 1.0 ]
            batchAdd s cmds)
        timed (sprintf "Tier %d simulation (3s)" tier) (fun () -> runFor s 3.0)
        printfn "  Tier %d complete" tier
      printfn "  All tiers complete — check [TIME] markers for degradation"; status s }

  { Name = "Collision Pit"; Desc = "120 spheres dropped into a walled pit — maximum collision density."
    Run = fun s ->
      resetSimulation s; setCamera s (8.0, 10.0, 8.0) (0.0, 2.0, 0.0) |> ignore
      timed "Pit walls setup" (fun () ->
          let wallCmds = [
              makeBoxCmd (nextId "box") (0.0, 2.0, -2.1) (2.0, 2.0, 0.1) 0.0
              makeBoxCmd (nextId "box") (0.0, 2.0, 2.1)  (2.0, 2.0, 0.1) 0.0
              makeBoxCmd (nextId "box") (-2.1, 2.0, 0.0) (0.1, 2.0, 2.0) 0.0
              makeBoxCmd (nextId "box") (2.1, 2.0, 0.0)  (0.1, 2.0, 2.0) 0.0 ]
          batchAdd s wallCmds)
      printfn "  Pit built (4x4m walled enclosure)"
      timed "Drop 120 spheres" (fun () ->
          let sphereCmds = [ for i in 0 .. 119 do
                               let x = float (i % 10) * 0.35 - 1.6
                               let z = float ((i / 10) % 12) * 0.35 - 1.9
                               let y = 6.0 + float (i / 10) * 0.5
                               makeSphereCmd (nextId "sphere") (x, y, z) 0.15 0.5 ]
          batchAdd s sphereCmds)
      printfn "  120 spheres dropping into pit..."
      timed "Settling simulation (8s)" (fun () -> runFor s 8.0)
      setCamera s (4.0, 3.0, 4.0) (0.0, 1.5, 0.0) |> ignore
      printfn "  Close-up view of settled pit"; sleep 1000; status s }

  { Name = "Force Frenzy"; Desc = "100 bodies hit with 3 rounds of escalating impulses, torques, and gravity shifts."
    Run = fun s ->
      resetSimulation s; setCamera s (15.0, 10.0, 15.0) (0.0, 1.0, 0.0) |> ignore
      let ids = timed "Create 100 bodies" (fun () ->
          let bodyIds = [ for i in 0 .. 99 -> nextId "sphere" ]
          let cmds = [ for idx in 0 .. 99 do
                         let x = float (idx % 10) * 1.5 - 7.0
                         let z = float (idx / 10) * 1.5 - 7.0
                         makeSphereCmd bodyIds.[idx] (x, 0.5, z) 0.3 1.0 ]
          batchAdd s cmds; bodyIds)
      printfn "  100 spheres in 10x10 grid"
      timed "Settle (2s)" (fun () -> runFor s 2.0)
      timed "Round 1 — impulses (3s)" (fun () ->
          batchAdd s [ for id in ids do makeImpulseCmd id (0.0, 8.0, 0.0) ]; runFor s 3.0)
      timed "Round 2 — torques + sideways gravity (3s)" (fun () ->
          batchAdd s [ for id in ids do makeTorqueCmd id (0.0, 20.0, 10.0) ]
          setGravity s (8.0, -2.0, 0.0) |> ignore
          setCamera s (-15.0, 5.0, 10.0) (0.0, 2.0, 0.0) |> ignore; runFor s 3.0)
      timed "Round 3 — strong impulses + reversed gravity (3s)" (fun () ->
          batchAdd s [ for id in ids do makeImpulseCmd id (5.0, 15.0, -5.0) ]
          setGravity s (0.0, 12.0, 0.0) |> ignore
          setCamera s (10.0, 2.0, 15.0) (0.0, 5.0, 0.0) |> ignore; runFor s 3.0)
      setGravity s (0.0, -9.81, 0.0) |> ignore; printfn "  Gravity restored"; runFor s 2.0; status s }

  { Name = "Domino Cascade"; Desc = "120 dominoes in a semicircular path — chain reaction at scale."
    Run = fun s ->
      resetSimulation s; setCamera s (0.0, 12.0, 0.1) (0.0, 0.0, 0.0) |> ignore
      let count = 120
      let radius = 8.0
      let ids = timed (sprintf "Place %d dominoes" count) (fun () ->
          let dominoIds = [ for _ in 0 .. count - 1 -> nextId "box" ]
          let cmds = [ for i in 0 .. count - 1 do
                         let angle = float i / float count * System.Math.PI
                         let x = radius * cos angle
                         let z = radius * sin angle
                         makeBoxCmd dominoIds.[i] (x, 0.3, z) (0.05, 0.3, 0.15) 1.0 ]
          batchAdd s cmds; dominoIds)
      printfn "  %d dominoes in semicircle (radius %.0fm)" count radius; runFor s 1.0
      printfn "  Pushing first domino..."; push s ids.[0] East 4.0 |> ignore
      timed "Cascade propagation" (fun () -> runFor s 10.0)
      for i in 0..5 do
        let angle = float i / 5.0 * System.Math.PI
        let cx = (radius + 4.0) * cos angle
        let cz = (radius + 4.0) * sin angle
        setCamera s (cx, 3.0, cz) (0.0, 0.5, 0.0) |> ignore; sleep 500
      printfn "  Cascade complete"; status s }

  { Name = "Overload"; Desc = "Everything at once: 200+ bodies, forces, gravity shifts, camera sweep — stress ceiling test."
    Run = fun s ->
      resetSimulation s
      let totalSw = System.Diagnostics.Stopwatch.StartNew()
      setCamera s (20.0, 12.0, 20.0) (0.0, 2.0, 0.0) |> ignore
      let pyramidIds = timed "Act 1 — pyramid + stack + row" (fun () ->
          let pIds = pyramid s 7 (Some (-5.0, 0.0, 0.0)) |> ok
          stack s 10 (Some (5.0, 0.0, 0.0)) |> ok |> ignore
          row s 12 (Some (-5.0, 0.0, 5.0)) |> ok |> ignore; runFor s 2.0; pIds)
      timed "Act 2 — 100 random spheres" (fun () ->
          let cmds = [ for i in 0 .. 99 do
                         let x = float (i % 10) * 1.5 - 7.0
                         let z = float (i / 10) * 1.5 - 7.0
                         let y = 8.0 + float (i / 20) * 2.0
                         makeSphereCmd (nextId "sphere") (x, y, z) 0.25 0.8 ]
          batchAdd s cmds; runFor s 3.0)
      printfn "  200+ bodies active"
      timed "Act 3 — impulse storm" (fun () ->
          setCamera s (0.0, 20.0, 15.0) (0.0, 2.0, 0.0) |> ignore
          batchAdd s [ for id in pyramidIds do makeImpulseCmd id (0.0, 10.0, 3.0) ]; runFor s 3.0)
      timed "Act 4 — gravity chaos" (fun () ->
          setCamera s (12.0, 3.0, 12.0) (0.0, 4.0, 0.0) |> ignore
          setGravity s (0.0, 10.0, 0.0) |> ignore; runFor s 2.0
          setGravity s (6.0, 0.0, 6.0) |> ignore
          setCamera s (-12.0, 5.0, -12.0) (0.0, 3.0, 0.0) |> ignore; runFor s 2.0
          setGravity s (0.0, -9.81, 0.0) |> ignore)
      timed "Act 5 — camera sweep" (fun () ->
          wireframe s true |> ignore; runFor s 1.0; wireframe s false |> ignore
          for a in 0..7 do
            let angle = float a * 0.785
            setCamera s (18.0 * cos angle, 8.0, 18.0 * sin angle) (0.0, 2.0, 0.0) |> ignore; sleep 400)
      totalSw.Stop()
      printfn "  [TIME] Total overload: %d ms" totalSw.ElapsedMilliseconds
      printfn "  Overload complete!"; status s }
|]

// --- Runner ---
let serverAddress = match fsi.CommandLineArgs |> Array.tryItem 1 with Some a -> a | None -> "http://localhost:5000"

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
    printfn "  %s" d.Desc
    printfn "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n"
    try d.Run s; passed <- passed + 1; printfn "\n  ✓ Complete"
    with ex -> failed <- failed + 1; printfn "\n  ✗ FAILED: %s" ex.Message
    printfn ""; sleep 1000

printfn "============================================"
printfn "  Results: %d passed, %d failed" passed failed
printfn "============================================\n"
resetSimulation s; disconnect s; printfn "Done!"
