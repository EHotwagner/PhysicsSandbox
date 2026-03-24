module PhysicsServer.Tests.MessageRouterTests

open System
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
        let received1 = ResizeArray<TickState>()
        let received2 = ResizeArray<TickState>()
        use cts = new CancellationTokenSource()

        let sub1 = subscribe router (fun state -> received1.Add(state); Task.CompletedTask)
        let sub2 = subscribe router (fun state -> received2.Add(state); Task.CompletedTask)

        // publishState takes SimulationState and decomposes to TickState + PropertyEvents
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

[<Fact>]
let ``subscribeViewCommands receives submitted ViewCommand`` () =
    task {
        let router = create ()
        use cts = new CancellationTokenSource()

        let (subId, reader) = subscribeViewCommands router

        let cmd = ViewCommand()
        cmd.SetZoom <- SetZoom(Level = 3.0)
        submitViewCommand router cmd |> ignore

        let! result = reader.ReadAsync(cts.Token).AsTask()
        Assert.Equal(3.0, result.SetZoom.Level)

        unsubscribeViewCommands router subId
    }

[<Fact>]
let ``subscribeViewCommands reader cancelled on cancellation`` () =
    task {
        let router = create ()
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let (_subId, reader) = subscribeViewCommands router

        let! ex = Assert.ThrowsAnyAsync<OperationCanceledException>(fun () ->
            reader.ReadAsync(cts.Token).AsTask() :> Task)
        Assert.NotNull(ex)
    }

[<Fact>]
let ``subscribeViewCommands reader blocks when no commands available`` () =
    task {
        let router = create ()
        use cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50.0))

        let (_subId, reader) = subscribeViewCommands router

        let! ex = Assert.ThrowsAnyAsync<OperationCanceledException>(fun () ->
            reader.ReadAsync(cts.Token).AsTask() :> Task)
        Assert.NotNull(ex)
    }

[<Fact>]
let ``ViewCommand broadcast delivers to multiple subscribers in order`` () =
    task {
        let router = create ()
        use cts = new CancellationTokenSource()

        let (sub1Id, reader1) = subscribeViewCommands router
        let (sub2Id, reader2) = subscribeViewCommands router

        let cmd1 = ViewCommand()
        cmd1.SetZoom <- SetZoom(Level = 1.0)
        let cmd2 = ViewCommand()
        cmd2.SetZoom <- SetZoom(Level = 2.0)
        let cmd3 = ViewCommand()
        cmd3.SetZoom <- SetZoom(Level = 3.0)

        submitViewCommand router cmd1 |> ignore
        submitViewCommand router cmd2 |> ignore
        submitViewCommand router cmd3 |> ignore

        // Both subscribers receive all 3 commands in order
        let! r1a = reader1.ReadAsync(cts.Token).AsTask()
        let! r1b = reader1.ReadAsync(cts.Token).AsTask()
        let! r1c = reader1.ReadAsync(cts.Token).AsTask()
        Assert.Equal(1.0, r1a.SetZoom.Level)
        Assert.Equal(2.0, r1b.SetZoom.Level)
        Assert.Equal(3.0, r1c.SetZoom.Level)

        let! r2a = reader2.ReadAsync(cts.Token).AsTask()
        let! r2b = reader2.ReadAsync(cts.Token).AsTask()
        let! r2c = reader2.ReadAsync(cts.Token).AsTask()
        Assert.Equal(1.0, r2a.SetZoom.Level)
        Assert.Equal(2.0, r2b.SetZoom.Level)
        Assert.Equal(3.0, r2c.SetZoom.Level)

        unsubscribeViewCommands router sub1Id
        unsubscribeViewCommands router sub2Id
    }

[<Fact>]
let ``ViewCommand broadcast with zero subscribers drops silently`` () =
    let router = create ()

    let cmd = ViewCommand()
    cmd.SetZoom <- SetZoom(Level = 5.0)
    let ack = submitViewCommand router cmd

    Assert.True(ack.Success)

[<Fact>]
let ``ViewCommand subscriber disconnect does not affect other subscribers`` () =
    task {
        let router = create ()
        use cts = new CancellationTokenSource()

        let (sub1Id, _reader1) = subscribeViewCommands router
        let (_sub2Id, reader2) = subscribeViewCommands router

        // Disconnect subscriber 1
        unsubscribeViewCommands router sub1Id

        // Subscriber 2 still receives commands
        let cmd = ViewCommand()
        cmd.SetZoom <- SetZoom(Level = 7.0)
        submitViewCommand router cmd |> ignore

        let! result = reader2.ReadAsync(cts.Token).AsTask()
        Assert.Equal(7.0, result.SetZoom.Level)
    }

[<Fact>]
let ``SubscribeCommands delivers command events to subscribers`` () =
    task {
        let router = create ()
        let received = ResizeArray<CommandEvent>()

        let subId = subscribeCommands router (fun evt -> received.Add(evt); Task.CompletedTask)

        let cmd = SimulationCommand()
        cmd.Step <- StepSimulation(DeltaTime = 0.016)
        let evt = CommandEvent()
        evt.SimulationCommand <- cmd
        do! publishCommandEvent router evt

        Assert.Equal(1, received.Count)
        Assert.NotNull(received.[0].SimulationCommand)

        unsubscribeCommands router subId
    }

[<Fact>]
let ``UnsubscribeCommands stops delivery`` () =
    task {
        let router = create ()
        let received = ResizeArray<CommandEvent>()

        let subId = subscribeCommands router (fun evt -> received.Add(evt); Task.CompletedTask)
        unsubscribeCommands router subId

        let cmd = SimulationCommand()
        cmd.Step <- StepSimulation(DeltaTime = 0.016)
        let evt = CommandEvent()
        evt.SimulationCommand <- cmd
        do! publishCommandEvent router evt

        Assert.Equal(0, received.Count)
    }

[<Fact>]
let ``PublishCommandEvent fans out to multiple command subscribers`` () =
    task {
        let router = create ()
        let received1 = ResizeArray<CommandEvent>()
        let received2 = ResizeArray<CommandEvent>()

        let _sub1 = subscribeCommands router (fun evt -> received1.Add(evt); Task.CompletedTask)
        let _sub2 = subscribeCommands router (fun evt -> received2.Add(evt); Task.CompletedTask)

        let viewCmd = ViewCommand()
        viewCmd.SetZoom <- SetZoom(Level = 2.0)
        let evt = CommandEvent()
        evt.ViewCommand <- viewCmd
        do! publishCommandEvent router evt

        Assert.Equal(1, received1.Count)
        Assert.Equal(1, received2.Count)
    }
