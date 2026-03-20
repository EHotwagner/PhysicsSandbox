// Shared preamble for all demo scripts

#r "../src/PhysicsClient/bin/Debug/net10.0/PhysicsClient.dll"
#r "../src/PhysicsSandbox.Shared.Contracts/bin/Debug/net10.0/PhysicsSandbox.Shared.Contracts.dll"
#r "../src/PhysicsSandbox.ServiceDefaults/bin/Debug/net10.0/PhysicsSandbox.ServiceDefaults.dll"
#r "nuget: Grpc.Net.Client"
#r "nuget: Google.Protobuf"
#r "nuget: Grpc.Core.Api"
#r "nuget: Spectre.Console"

[<AutoOpen>]
module DemoHelpers =
    open PhysicsClient.Session
    open PhysicsClient.SimulationCommands
    open PhysicsClient.ViewCommands

    let ok r = r |> Result.defaultWith (fun e -> failwith e)
    let sleep (ms: int) = System.Threading.Thread.Sleep(ms)

    let runFor (s: Session) (seconds: float) =
        play s |> ignore
        sleep (int (seconds * 1000.0))
        pause s |> ignore

    let resetScene (s: Session) =
        pause s |> ignore
        clearAll s |> ignore
        PhysicsClient.IdGenerator.reset ()
        addPlane s None None |> ignore
        setGravity s (0.0, -9.81, 0.0) |> ignore
        sleep 100
