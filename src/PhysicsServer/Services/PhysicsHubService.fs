namespace PhysicsServer.Services

open System.Threading.Tasks
open Grpc.Core
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MessageRouter

/// <summary>
/// gRPC service implementation for the client/viewer-facing PhysicsHub.
/// Handles command submission, state streaming, batch operations, and metrics reporting
/// by delegating to the central MessageRouter.
/// </summary>
type PhysicsHubService(router: MessageRouter) =
    inherit PhysicsHub.PhysicsHubBase()

    /// <summary>Forward a single simulation command to the router and return an acknowledgment.</summary>
    override _.SendCommand(request: SimulationCommand, context: ServerCallContext) =
        let ack = submitCommand router request
        Task.FromResult(ack)

    /// <summary>Forward a single view command to the router and return an acknowledgment.</summary>
    override _.SendViewCommand(request: ViewCommand, context: ServerCallContext) =
        let ack = submitViewCommand router request
        Task.FromResult(ack)

    /// <summary>Stream live simulation state updates to the client. Sends the cached state immediately for late joiners, then subscribes to live updates until the client disconnects.</summary>
    override _.StreamState
        (request: StateRequest, responseStream: IServerStreamWriter<SimulationState>, context: ServerCallContext)
        =
        task {
            // Send cached state immediately for late joiners
            match getLatestState router with
            | Some state -> do! responseStream.WriteAsync(state)
            | None -> ()

            // Subscribe and stream live updates
            let tcs = TaskCompletionSource()

            let subId =
                subscribe router (fun state ->
                    task {
                        if not context.CancellationToken.IsCancellationRequested then
                            do! responseStream.WriteAsync(state)
                    })

            use _registration =
                context.CancellationToken.Register(fun () ->
                    unsubscribe router subId
                    tcs.TrySetResult() |> ignore)

            do! tcs.Task
        }

    /// <summary>Stream pending view commands to the viewer. Reads from the view command channel until the client disconnects.</summary>
    override _.StreamViewCommands
        (request: StateRequest, responseStream: IServerStreamWriter<ViewCommand>, context: ServerCallContext)
        =
        task {
            while not context.CancellationToken.IsCancellationRequested do
                match! readViewCommand router context.CancellationToken with
                | Some cmd -> do! responseStream.WriteAsync(cmd)
                | None -> ()
        }

    /// <summary>Submit a batch of simulation commands and return per-command results with total timing.</summary>
    override _.SendBatchCommand(request: BatchSimulationRequest, context: ServerCallContext) =
        let response = sendBatchCommand router request
        Task.FromResult(response)

    /// <summary>Submit a batch of view commands and return per-command results with total timing.</summary>
    override _.SendBatchViewCommand(request: BatchViewRequest, context: ServerCallContext) =
        let response = sendBatchViewCommand router request
        Task.FromResult(response)

    /// <summary>Fetch mesh geometries by mesh ID from the server cache.</summary>
    override _.FetchMeshes(request: MeshRequest, context: ServerCallContext) =
        let ids = request.MeshIds |> Seq.toList
        let meshes = PhysicsServer.Hub.MeshCache.getMany ids (meshCache router)
        let hits = meshes.Length
        let misses = ids.Length - hits
        PhysicsServer.Hub.MetricsCounter.incrementFetchRequest hits misses (metricsState router)
        let response = MeshResponse()
        for mg in meshes do
            response.Meshes.Add(mg)
        Task.FromResult(response)

    /// <summary>Return server throughput metrics and pipeline timing diagnostics (simulation tick, serialization, gRPC transfer).</summary>
    override _.GetMetrics(request: MetricsRequest, context: ServerCallContext) =
        let response = MetricsResponse()
        response.Services.Add(getMetrics router)
        // Pipeline timings from cached state + transfer measurement
        let timings = PipelineTimings()
        match getLatestState router with
        | Some state ->
            timings.SimulationTickMs <- state.TickMs
            timings.StateSerializationMs <- state.SerializeMs
        | None -> ()
        timings.GrpcTransferMs <- SimulationLinkDiagnostics.getLastTransferMs ()
        timings.TotalPipelineMs <- timings.SimulationTickMs + timings.StateSerializationMs + timings.GrpcTransferMs + timings.ViewerRenderMs
        response.Pipeline <- timings
        Task.FromResult(response)

    /// <summary>Execute a raycast query by forwarding to the simulation.</summary>
    override _.Raycast(request: RaycastRequest, context: ServerCallContext) =
        task {
            let qr = QueryRequest(CorrelationId = System.Guid.NewGuid().ToString("N"))
            qr.Raycast <- request
            let! response = submitQuery router qr context.CancellationToken
            return response.Raycast
        }

    /// <summary>Execute a batch of raycast queries.</summary>
    override _.RaycastBatch(request: RaycastBatchRequest, context: ServerCallContext) =
        task {
            let response = RaycastBatchResponse()
            for ray in request.Rays do
                let qr = QueryRequest(CorrelationId = System.Guid.NewGuid().ToString("N"))
                qr.Raycast <- ray
                let! r = submitQuery router qr context.CancellationToken
                response.Results.Add(r.Raycast)
            return response
        }

    /// <summary>Execute a sweep cast query.</summary>
    override _.SweepCast(request: SweepCastRequest, context: ServerCallContext) =
        task {
            let qr = QueryRequest(CorrelationId = System.Guid.NewGuid().ToString("N"))
            qr.SweepCast <- request
            let! response = submitQuery router qr context.CancellationToken
            return response.SweepCast
        }

    /// <summary>Execute a batch of sweep cast queries.</summary>
    override _.SweepCastBatch(request: SweepCastBatchRequest, context: ServerCallContext) =
        task {
            let response = SweepCastBatchResponse()
            for sweep in request.Sweeps do
                let qr = QueryRequest(CorrelationId = System.Guid.NewGuid().ToString("N"))
                qr.SweepCast <- sweep
                let! r = submitQuery router qr context.CancellationToken
                response.Results.Add(r.SweepCast)
            return response
        }

    /// <summary>Execute an overlap query.</summary>
    override _.Overlap(request: OverlapRequest, context: ServerCallContext) =
        task {
            let qr = QueryRequest(CorrelationId = System.Guid.NewGuid().ToString("N"))
            qr.Overlap <- request
            let! response = submitQuery router qr context.CancellationToken
            return response.Overlap
        }

    /// <summary>Execute a batch of overlap queries.</summary>
    override _.OverlapBatch(request: OverlapBatchRequest, context: ServerCallContext) =
        task {
            let response = OverlapBatchResponse()
            for overlap in request.Overlaps do
                let qr = QueryRequest(CorrelationId = System.Guid.NewGuid().ToString("N"))
                qr.Overlap <- overlap
                let! r = submitQuery router qr context.CancellationToken
                response.Results.Add(r.Overlap)
            return response
        }

    /// <summary>Stream command audit events to the client. Subscribes to all command events and forwards them until the client disconnects.</summary>
    override _.StreamCommands
        (request: StateRequest, responseStream: IServerStreamWriter<CommandEvent>, context: ServerCallContext)
        =
        task {
            let tcs = TaskCompletionSource()

            let subId =
                subscribeCommands router (fun evt ->
                    task {
                        if not context.CancellationToken.IsCancellationRequested then
                            do! responseStream.WriteAsync(evt)
                    })

            use _registration =
                context.CancellationToken.Register(fun () ->
                    unsubscribeCommands router subId
                    tcs.TrySetResult() |> ignore)

            do! tcs.Task
        }
