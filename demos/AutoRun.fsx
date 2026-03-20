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

let ok r = r |> Result.defaultWith (fun e -> failwith e)
let sleep (ms: int) = System.Threading.Thread.Sleep(ms)
let runFor (s: Session) (sec: float) = play s |> ignore; sleep (int (sec * 1000.0)); pause s |> ignore
let resetScene (s: Session) =
    pause s |> ignore; clearAll s |> ignore; PhysicsClient.IdGenerator.reset ()
    addPlane s None None |> ignore; setGravity s (0.0, -9.81, 0.0) |> ignore; sleep 100

type Demo = { Name: string; Desc: string; Run: Session -> unit }

let demos = [|
  { Name = "Hello Drop"; Desc = "A bowling ball falls from 10m."
    Run = fun s ->
      resetScene s; setCamera s (5.0, 3.0, 5.0) (0.0, 1.0, 0.0) |> ignore
      bowlingBall s (Some (0.0, 10.0, 0.0)) None None |> ignore
      printfn "  Dropping bowling ball..."; runFor s 3.0; listBodies s }

  { Name = "Bouncing Marbles"; Desc = "Five marbles from different heights."
    Run = fun s ->
      resetScene s; setCamera s (4.0, 5.0, 4.0) (0.0, 0.5, 0.0) |> ignore
      for i in 0..4 do marble s (Some (float i * 0.3 - 0.6, 3.0 + float i * 2.0, 0.0)) None None |> ignore
      printfn "  Dropping 5 marbles..."; runFor s 4.0; listBodies s }

  { Name = "Crate Stack"; Desc = "Tower of 8 crates — push the top one."
    Run = fun s ->
      resetScene s; setCamera s (6.0, 5.0, 0.0) (0.0, 4.0, 0.0) |> ignore
      let ids = stack s 8 (Some (0.0, 0.0, 0.0)) |> ok
      printfn "  Stack of %d" ids.Length; runFor s 2.0
      printfn "  Pushing top crate east..."; push s (List.last ids) East 15.0 |> ignore
      runFor s 3.0; listBodies s }

  { Name = "Bowling Alley"; Desc = "Bowling ball vs pyramid of bricks."
    Run = fun s ->
      resetScene s; setCamera s (-5.0, 3.0, 3.0) (3.0, 1.0, 0.0) |> ignore
      pyramid s 4 (Some (5.0, 0.0, 0.0)) |> ok |> ignore
      let ball = bowlingBall s (Some (-3.0, 0.15, 0.0)) None None |> ok
      printfn "  Pyramid + ball ready"; runFor s 1.0
      printfn "  STRIKE!"; launch s ball (5.0, 0.5, 0.0) 25.0 |> ignore
      runFor s 4.0; listBodies s }

  { Name = "Marble Rain"; Desc = "20 random spheres rain down."
    Run = fun s ->
      resetScene s; setCamera s (8.0, 12.0, 8.0) (0.0, 0.0, 0.0) |> ignore
      let ids = randomSpheres s 20 (Some 42) |> ok
      printfn "  %d spheres generated" ids.Length; runFor s 5.0
      setCamera s (3.0, 1.0, 3.0) (0.0, 0.5, 0.0) |> ignore
      printfn "  Ground-level view"; sleep 1000; listBodies s }

  { Name = "Domino Row"; Desc = "12 dominoes toppled by a push."
    Run = fun s ->
      resetScene s; setCamera s (-2.0, 3.0, 6.0) (3.0, 0.5, 0.0) |> ignore
      let mutable firstId = ""
      for i in 0..11 do
        let id = addBox s (float i * 0.5, 0.3, 0.0) (0.05, 0.3, 0.15) 1.0 None |> ok
        if i = 0 then firstId <- id
      printfn "  12 dominoes placed"; runFor s 1.0
      printfn "  Toppling..."; push s firstId East 3.0 |> ignore; runFor s 5.0
      setCamera s (8.0, 2.0, 4.0) (5.0, 0.0, 0.0) |> ignore; sleep 500; listBodies s }

  { Name = "Spinning Tops"; Desc = "Bodies spinning with torques + wireframe."
    Run = fun s ->
      resetScene s; setCamera s (0.0, 8.0, 6.0) (0.0, 0.5, 0.0) |> ignore
      let b1 = beachBall s (Some (-2.0, 0.25, 0.0)) None None |> ok
      let b2 = beachBall s (Some (2.0, 0.25, 0.0)) None None |> ok
      let b3 = crate s (Some (0.0, 0.55, -2.0)) None None |> ok
      let b4 = crate s (Some (0.0, 0.55, 2.0)) None None |> ok
      runFor s 0.5
      spin s b1 Up 50.0 |> ignore; spin s b2 North 30.0 |> ignore
      spin s b3 Up 80.0 |> ignore; spin s b4 East 40.0 |> ignore
      printfn "  Spinning..."; runFor s 4.0
      wireframe s true |> ignore; printfn "  Wireframe on"; sleep 2000
      wireframe s false |> ignore; listBodies s }

  { Name = "Gravity Flip"; Desc = "Normal gravity, then reversed, then sideways."
    Run = fun s ->
      resetScene s; setCamera s (6.0, 4.0, 6.0) (0.0, 2.0, 0.0) |> ignore
      grid s 3 3 (Some (-2.0, 0.0, -2.0)) |> ok |> ignore
      for i in 0..4 do beachBall s (Some (float i * 0.8 - 1.6, 5.0 + float i, 0.3)) None None |> ignore
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
      resetScene s; setCamera s (0.0, 10.0, 0.1) (0.0, 0.0, 0.0) |> ignore
      let r = 0.1
      for row in 0..4 do
        for col in 0..row do
          let x = float row * 0.22 * 0.866 + 1.0
          let z = (float col - float row / 2.0) * 0.22
          addSphere s (x, r, z) r 0.17 None |> ignore
      printfn "  15 balls placed"
      let cue = addSphere s (-2.0, r, 0.0) 0.11 0.17 (Some "cue") |> ok
      setCamera s (-3.0, 1.5, 2.0) (1.0, 0.0, 0.0) |> ignore; runFor s 0.5
      printfn "  BREAK!"; launch s cue (1.5, 0.0, 0.0) 15.0 |> ignore; runFor s 4.0
      setCamera s (0.0, 8.0, 0.1) (0.0, 0.0, 0.0) |> ignore; sleep 1000; listBodies s }

  { Name = "Chaos Scene"; Desc = "Everything: presets, generators, steering, gravity, camera sweeps."
    Run = fun s ->
      resetScene s; setCamera s (12.0, 8.0, 12.0) (0.0, 2.0, 0.0) |> ignore
      printfn "  Act 1: Building stage..."
      pyramid s 5 (Some (-4.0, 0.0, 0.0)) |> ok |> ignore
      stack s 6 (Some (4.0, 0.0, 0.0)) |> ok |> ignore
      row s 8 (Some (-3.0, 0.0, 3.0)) |> ok |> ignore; runFor s 1.5
      printfn "  Act 2: Bombardment!"
      setCamera s (0.0, 15.0, 10.0) (0.0, 2.0, 0.0) |> ignore
      let proj = randomSpheres s 10 (Some 99) |> ok
      for id in proj do pushVec s id (0.0, -20.0, 0.0) |> ignore
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
resetScene s; disconnect s; printfn "Done!"
