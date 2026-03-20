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
      CommandChannel: Channel<SimulationCommand>
      ViewCommandChannel: Channel<ViewCommand>
      mutable SimulationConnected: bool
      SimulationLock: obj }

let create () =
    { StateCache = StateCache.create ()
      Subscribers = ConcurrentDictionary<Guid, SimulationState -> Task>()
      CommandChannel = Channel.CreateBounded<SimulationCommand>(100)
      ViewCommandChannel = Channel.CreateBounded<ViewCommand>(100)
      SimulationConnected = false
      SimulationLock = obj () }

let submitCommand (router: MessageRouter) (cmd: SimulationCommand) =
    // Try to write to the command channel; drop if full or no simulation connected
    let written =
        lock router.SimulationLock (fun () ->
            if router.SimulationConnected then
                router.CommandChannel.Writer.TryWrite(cmd)
            else
                false)

    CommandAck(
        Success = true,
        Message =
            if written then
                "Command forwarded to simulation"
            else
                "Command accepted (no simulation connected — dropped)"
    )

let submitViewCommand (router: MessageRouter) (cmd: ViewCommand) =
    // Try to write to the view command channel; drop if full or no viewer connected
    let _written = router.ViewCommandChannel.Writer.TryWrite(cmd)

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
