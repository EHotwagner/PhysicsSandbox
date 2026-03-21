module PhysicsServer.Hub.MessageRouter

open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.StateCache

type SubscriptionId = SubscriptionId of Guid

type MessageRouter =
    { StateCache: StateCache.StateCache
      Subscribers: ConcurrentDictionary<Guid, SimulationState -> Task>
      CommandSubscribers: ConcurrentDictionary<Guid, CommandEvent -> Task>
      CommandChannel: Channel<SimulationCommand>
      ViewCommandChannel: Channel<ViewCommand>
      mutable SimulationConnected: bool
      SimulationLock: obj
      Metrics: MetricsCounter.MetricsState }

let create () =
    { StateCache = StateCache.create ()
      Subscribers = ConcurrentDictionary<Guid, SimulationState -> Task>()
      CommandSubscribers = ConcurrentDictionary<Guid, CommandEvent -> Task>()
      CommandChannel = Channel.CreateBounded<SimulationCommand>(1024)
      ViewCommandChannel = Channel.CreateBounded<ViewCommand>(1024)
      SimulationConnected = false
      SimulationLock = obj ()
      Metrics = MetricsCounter.create "PhysicsServer" }

let subscribeCommands (router: MessageRouter) (callback: CommandEvent -> Task) =
    let id = Guid.NewGuid()
    router.CommandSubscribers.TryAdd(id, callback) |> ignore
    id

let unsubscribeCommands (router: MessageRouter) (id: Guid) =
    router.CommandSubscribers.TryRemove(id) |> ignore

let publishCommandEvent (router: MessageRouter) (evt: CommandEvent) =
    task {
        for kvp in router.CommandSubscribers do
            try
                do! kvp.Value evt
            with
            | _ -> ()
    }

let submitCommand (router: MessageRouter) (cmd: SimulationCommand) =
    // Track incoming command
    MetricsCounter.incrementReceived 1 (int64 (cmd.CalculateSize())) router.Metrics

    // Try to write to the command channel; drop if full or no simulation connected
    let written =
        lock router.SimulationLock (fun () ->
            if router.SimulationConnected then
                router.CommandChannel.Writer.TryWrite(cmd)
            else
                false)

    if written then
        MetricsCounter.incrementSent 1 (int64 (cmd.CalculateSize())) router.Metrics

    // Publish to command audit subscribers
    let evt = CommandEvent()
    evt.SimulationCommand <- cmd
    publishCommandEvent router evt |> ignore

    CommandAck(
        Success = true,
        Message =
            if written then
                "Command forwarded to simulation"
            else
                "Command accepted (no simulation connected — dropped)"
    )

let submitViewCommand (router: MessageRouter) (cmd: ViewCommand) =
    MetricsCounter.incrementReceived 1 (int64 (cmd.CalculateSize())) router.Metrics
    // Try to write to the view command channel; drop if full or no viewer connected
    let _written = router.ViewCommandChannel.Writer.TryWrite(cmd)

    // Publish to command audit subscribers
    let evt = CommandEvent()
    evt.ViewCommand <- cmd
    publishCommandEvent router evt |> ignore

    CommandAck(
        Success = true,
        Message = "View command accepted"
    )

let subscribe (router: MessageRouter) (callback: SimulationState -> Task) =
    let id = Guid.NewGuid()
    router.Subscribers.TryAdd(id, callback) |> ignore
    SubscriptionId id

let unsubscribe (router: MessageRouter) (SubscriptionId id) =
    router.Subscribers.TryRemove(id) |> ignore

let publishState (router: MessageRouter) (state: SimulationState) =
    task {
        MetricsCounter.incrementReceived 1 (int64 (state.CalculateSize())) router.Metrics
        StateCache.update router.StateCache state

        for kvp in router.Subscribers do
            try
                do! kvp.Value state
            with
            | _ -> ()
    }

let getLatestState (router: MessageRouter) =
    StateCache.get router.StateCache

let tryConnectSimulation (router: MessageRouter) =
    lock router.SimulationLock (fun () ->
        if router.SimulationConnected then
            false
        else
            router.SimulationConnected <- true
            true)

let disconnectSimulation (router: MessageRouter) =
    lock router.SimulationLock (fun () ->
        router.SimulationConnected <- false)

let getMetrics (router: MessageRouter) =
    MetricsCounter.snapshot router.Metrics

let metricsState (router: MessageRouter) =
    router.Metrics

let sendBatchCommand (router: MessageRouter) (batch: BatchSimulationRequest) =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let response = BatchResponse()

    if batch.Commands.Count > 100 then
        let result = CommandResult(Success = false, Message = "Batch exceeds maximum of 100 commands", Index = 0)
        response.Results.Add(result)
    else
        for i in 0 .. batch.Commands.Count - 1 do
            let cmd = batch.Commands.[i]
            let ack = submitCommand router cmd
            let result = CommandResult(Success = ack.Success, Message = ack.Message, Index = i)
            response.Results.Add(result)

    sw.Stop()
    response.TotalTimeMs <- sw.Elapsed.TotalMilliseconds
    response

let sendBatchViewCommand (router: MessageRouter) (batch: BatchViewRequest) =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let response = BatchResponse()

    if batch.Commands.Count > 100 then
        let result = CommandResult(Success = false, Message = "Batch exceeds maximum of 100 commands", Index = 0)
        response.Results.Add(result)
    else
        for i in 0 .. batch.Commands.Count - 1 do
            let cmd = batch.Commands.[i]
            let ack = submitViewCommand router cmd
            let result = CommandResult(Success = ack.Success, Message = ack.Message, Index = i)
            response.Results.Add(result)

    sw.Stop()
    response.TotalTimeMs <- sw.Elapsed.TotalMilliseconds
    response

let readCommand (router: MessageRouter) (ct: CancellationToken) =
    task {
        try
            let! cmd = router.CommandChannel.Reader.ReadAsync(ct).AsTask()
            return Some cmd
        with
        | :? OperationCanceledException -> return None
        | :? ChannelClosedException -> return None
    }

let readViewCommand (router: MessageRouter) (ct: CancellationToken) =
    task {
        try
            let! cmd = router.ViewCommandChannel.Reader.ReadAsync(ct).AsTask()
            return Some cmd
        with
        | :? OperationCanceledException -> return None
        | :? ChannelClosedException -> return None
    }
