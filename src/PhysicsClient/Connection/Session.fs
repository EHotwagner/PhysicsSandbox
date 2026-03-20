module PhysicsClient.Session

open System
open System.Collections.Concurrent
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Grpc.Net.Client
open PhysicsSandbox.Shared.Contracts

type Session =
    { Channel: GrpcChannel
      Client: PhysicsHub.PhysicsHubClient
      ServerAddress: string
      Cts: CancellationTokenSource
      BodyRegistry: ConcurrentDictionary<string, string>
      mutable LatestState: SimulationState option
      mutable LastStateUpdate: DateTime
      mutable IsConnected: bool }

let private createChannel (address: string) =
    let handler = new SocketsHttpHandler(EnableMultipleHttp2Connections = true)
    handler.SslOptions.RemoteCertificateValidationCallback <-
        System.Net.Security.RemoteCertificateValidationCallback(fun _ _ _ _ -> true)
    let options = GrpcChannelOptions(HttpHandler = handler)
    // For plain HTTP, force HTTP/2 without TLS upgrade
    if address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) then
        options.HttpVersion <- System.Net.HttpVersion.Version20
        options.HttpVersionPolicy <- System.Net.Http.HttpVersionPolicy.RequestVersionExact
    GrpcChannel.ForAddress(address, options)

let private startStateStream (session: Session) =
    Task.Run(fun () ->
        task {
            let mutable delay = 1000
            while not session.Cts.Token.IsCancellationRequested && session.IsConnected do
                try
                    use call = session.Client.StreamState(StateRequest(), cancellationToken = session.Cts.Token)
                    let stream = call.ResponseStream
                    delay <- 1000 // reset backoff on successful connection
                    while not session.Cts.Token.IsCancellationRequested do
                        let! hasNext = stream.MoveNext(session.Cts.Token)
                        if hasNext then
                            session.LatestState <- Some stream.Current
                            session.LastStateUpdate <- DateTime.UtcNow
                with
                | :? OperationCanceledException -> ()
                | :? RpcException when session.Cts.Token.IsCancellationRequested -> ()
                | :? RpcException ->
                    // Stream error — retry with backoff (don't mark disconnected)
                    if not session.Cts.Token.IsCancellationRequested then
                        do! Task.Delay(delay, session.Cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                        delay <- min (delay * 2) 10000
                | _ ->
                    if not session.Cts.Token.IsCancellationRequested then
                        do! Task.Delay(delay, session.Cts.Token) |> Async.AwaitTask |> Async.Catch |> Async.Ignore
                        delay <- min (delay * 2) 10000
        } :> Task) |> ignore

let connect (serverAddress: string) : Result<Session, string> =
    try
        let channel = createChannel serverAddress
        let grpcClient = PhysicsHub.PhysicsHubClient(channel)
        let session =
            { Channel = channel
              Client = grpcClient
              ServerAddress = serverAddress
              Cts = new CancellationTokenSource()
              BodyRegistry = ConcurrentDictionary<string, string>()
              LatestState = None
              LastStateUpdate = DateTime.UtcNow
              IsConnected = true }
        startStateStream session
        Ok session
    with ex ->
        Error $"Failed to connect to {serverAddress}: {ex.Message}"

let disconnect (session: Session) : unit =
    session.IsConnected <- false
    try session.Cts.Cancel() with _ -> ()
    try session.Cts.Dispose() with _ -> ()
    try session.Channel.Dispose() with _ -> ()

let reconnect (session: Session) : Result<Session, string> =
    disconnect session
    connect session.ServerAddress

let isConnected (session: Session) : bool =
    session.IsConnected

let internal client (session: Session) : PhysicsHub.PhysicsHubClient =
    session.Client

let internal bodyRegistry (session: Session) : ConcurrentDictionary<string, string> =
    session.BodyRegistry

let internal latestState (session: Session) : SimulationState option =
    session.LatestState

let internal lastStateUpdate (session: Session) : DateTime =
    session.LastStateUpdate

let internal sendCommand (session: Session) (cmd: SimulationCommand) : Result<unit, string> =
    if not session.IsConnected then
        Error "Not connected to server"
    else
        try
            let ack = session.Client.SendCommand(cmd)
            if ack.Success then Ok ()
            else Error ack.Message
        with
        | :? RpcException as ex ->
            session.IsConnected <- false
            Error $"gRPC error ({ex.StatusCode}): {ex.Status.Detail}"
        | ex ->
            Error $"Command failed: {ex.Message}"

let internal sendViewCommand (session: Session) (cmd: ViewCommand) : Result<unit, string> =
    if not session.IsConnected then
        Error "Not connected to server"
    else
        try
            let ack = session.Client.SendViewCommand(cmd)
            if ack.Success then Ok ()
            else Error ack.Message
        with
        | :? RpcException as ex ->
            session.IsConnected <- false
            Error $"gRPC error ({ex.StatusCode}): {ex.Status.Detail}"
        | ex ->
            Error $"Command failed: {ex.Message}"
