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

    /// <summary>Strip velocity fields from a TickState for exclude_velocity subscribers.</summary>
    static member private StripVelocity(tickState: TickState) =
        let stripped = TickState()
        stripped.Time <- tickState.Time
        stripped.Running <- tickState.Running
        stripped.TickMs <- tickState.TickMs
        stripped.SerializeMs <- tickState.SerializeMs
        for pose in tickState.Bodies do
            let bp = BodyPose()
            bp.Id <- pose.Id
            bp.Position <- pose.Position
            bp.Orientation <- pose.Orientation
            // velocity and angular_velocity deliberately omitted
            stripped.Bodies.Add(bp)
        for qr in tickState.QueryResponses do
            stripped.QueryResponses.Add(qr)
        stripped

    /// <summary>Stream lean tick state updates (60 Hz, dynamic body poses). Sends cached tick for late joiners, then subscribes to live updates. If exclude_velocity is set, velocity fields are stripped.</summary>
    override this.StreamState
        (request: StateRequest, responseStream: IServerStreamWriter<TickState>, context: ServerCallContext)
        =
        task {
            let excludeVelocity = request.ExcludeVelocity

            // Send cached tick state immediately for late joiners
            match getLatestState router with
            | Some state ->
                let toSend = if excludeVelocity then PhysicsHubService.StripVelocity(state) else state
                do! responseStream.WriteAsync(toSend)
            | None -> ()

            // Subscribe and stream live tick updates
            let tcs = TaskCompletionSource()

            let subId =
                subscribe router (fun tickState ->
                    task {
                        if not context.CancellationToken.IsCancellationRequested then
                            let toSend = if excludeVelocity then PhysicsHubService.StripVelocity(tickState) else tickState
                            do! responseStream.WriteAsync(toSend)
                    })

            use _registration =
                context.CancellationToken.Register(fun () ->
                    unsubscribe router subId
                    tcs.TrySetResult() |> ignore)

            do! tcs.Task
        }

    /// <summary>Stream property events (body lifecycle, semi-static changes). Sends PropertySnapshot backfill for late joiners, then subscribes to live events.</summary>
    override _.StreamProperties
        (request: StateRequest, responseStream: IServerStreamWriter<PropertyEvent>, context: ServerCallContext)
        =
        task {
            // Send property snapshot backfill for late joiners
            match getPropertySnapshot router with
            | Some snapshot ->
                let backfillEvent = PropertyEvent(Snapshot = snapshot)
                do! responseStream.WriteAsync(backfillEvent)
            | None -> ()

            // Subscribe and stream live property events
            let tcs = TaskCompletionSource()

            let subId =
                subscribeProperties router (fun evt ->
                    task {
                        if not context.CancellationToken.IsCancellationRequested then
                            do! responseStream.WriteAsync(evt)
                    })

            use _registration =
                context.CancellationToken.Register(fun () ->
                    unsubscribeProperties router subId
                    tcs.TrySetResult() |> ignore)

            do! tcs.Task
        }

    /// <summary>Stream view commands to the viewer via per-subscriber broadcast channel. Each viewer gets its own bounded channel.</summary>
    override _.StreamViewCommands
        (request: StateRequest, responseStream: IServerStreamWriter<ViewCommand>, context: ServerCallContext)
        =
        task {
            let (subId, reader) = subscribeViewCommands router
            System.Diagnostics.Trace.TraceInformation($"ViewCommand subscriber {subId} connected")
            try
                while not context.CancellationToken.IsCancellationRequested do
                    try
                        let! cmd = reader.ReadAsync(context.CancellationToken).AsTask()
                        do! responseStream.WriteAsync(cmd)
                    with
                    | :? System.OperationCanceledException -> ()
                    | :? System.Threading.Channels.ChannelClosedException -> ()
            finally
                unsubscribeViewCommands router subId
                System.Diagnostics.Trace.TraceInformation($"ViewCommand subscriber {subId} disconnected")
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
        // Publish mesh fetch observation to the audit stream for MCP recording
        let fetchLog = MeshFetchLog()
        for id in ids do fetchLog.RequestedIds.Add(id)
        fetchLog.Hits <- hits
        fetchLog.Misses <- misses
        let hitIds = meshes |> List.map (fun mg -> mg.MeshId) |> Set.ofList
        for id in ids do
            if not (Set.contains id hitIds) then
                fetchLog.MissedIds.Add(id)
        let evt = CommandEvent(MeshFetchLog = fetchLog)
        publishCommandEvent router evt |> ignore
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

    /// <summary>Reset the simulation and block until processing is confirmed.
    /// Submits a ResetSimulation command then uses a fence query to ensure the
    /// simulation has fully processed the reset before returning.</summary>
    override _.ConfirmedReset(request: ConfirmedResetRequest, context: ServerCallContext) =
        task {
            // 1. Submit the reset command (fire-and-forget to command channel)
            let resetCmd = SimulationCommand(Reset = ResetSimulation())
            let _ack = submitCommand router resetCmd

            // 2. Submit a fence query — it enters the command channel after the reset,
            //    so when it completes, we know the reset has been processed.
            let fenceQuery = QueryRequest(CorrelationId = System.Guid.NewGuid().ToString("N"))
            let fenceOverlap = OverlapRequest()
            let shape = Shape()
            shape.Sphere <- Sphere(Radius = 0.0)
            fenceOverlap.Shape <- shape
            fenceOverlap.Position <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
            fenceQuery.Overlap <- fenceOverlap
            let! _fenceResponse = submitQuery router fenceQuery context.CancellationToken

            // 3. The fence query has completed — reset is confirmed
            return ConfirmedResetResponse(
                Success = true,
                Message = "Simulation reset confirmed")
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
