module PhysicsServer.Tests.MessageRouterTests

open System.Threading
open System.Threading.Tasks
open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MessageRouter

[<Fact>]
let ``SubmitCommand succeeds with no simulation connected`` () =
    let router = create ()
    let cmd = SimulationCommand()
    cmd.Step <- StepSimulation(DeltaTime = 0.016)
    let ack = submitCommand router cmd
    Assert.True(ack.Success)

[<Fact>]
let ``State fanout delivers to multiple subscribers`` () =
    task {
        let router = create ()
        let received1 = ResizeArray<SimulationState>()
        let received2 = ResizeArray<SimulationState>()
        use cts = new CancellationTokenSource()

        let sub1 = subscribe router (fun state -> received1.Add(state); Task.CompletedTask)
        let sub2 = subscribe router (fun state -> received2.Add(state); Task.CompletedTask)

        let state = SimulationState(Time = 1.0, Running = true)
        do! publishState router state

        Assert.Single(received1) |> ignore
        Assert.Single(received2) |> ignore
        Assert.Equal(1.0, received1.[0].Time)
        Assert.Equal(1.0, received2.[0].Time)

        unsubscribe router sub1
        unsubscribe router sub2
    }

[<Fact>]
let ``ConnectSimulation succeeds for first connection`` () =
    let router = create ()
    let result = tryConnectSimulation router
    Assert.True(result)

[<Fact>]
let ``ConnectSimulation rejects second connection`` () =
    let router = create ()
    let first = tryConnectSimulation router
    let second = tryConnectSimulation router
    Assert.True(first)
    Assert.False(second)

[<Fact>]
let ``DisconnectSimulation allows new connection`` () =
    let router = create ()
    tryConnectSimulation router |> ignore
    disconnectSimulation router
    let result = tryConnectSimulation router
    Assert.True(result)

[<Fact>]
let ``SubmitViewCommand succeeds with no viewer connected`` () =
    let router = create ()
    let cmd = ViewCommand()
    cmd.SetZoom <- SetZoom(Level = 2.0)
    let ack = submitViewCommand router cmd
    Assert.True(ack.Success)
