module PhysicsSandbox.Mcp.RecordingQueryTools

open System
open System.ComponentModel
open System.Text
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.Recording.RecordingEngine
open PhysicsSandbox.Mcp.Recording.Types
open PhysicsSandbox.Mcp.Recording.SessionStore
open PhysicsSandbox.Mcp.Recording.ChunkReader

let private nullSafe (s: string) = if isNull s then "" else s

let private resolveSession (engine: RecordingEngine) (sessionId: string) =
    if String.IsNullOrWhiteSpace(nullSafe sessionId) then
        engine.ActiveSession
    else
        loadSession sessionId

let private resolveSessionDir (session: RecordingSession) =
    System.IO.Path.Combine(getRecordingsDir(), session.Id)

let private resolveTimeRange (session: RecordingSession) (startTime: double) (endTime: double) =
    let s =
        if startTime <= 0.0 then session.StartTime
        else DateTimeOffset.FromUnixTimeMilliseconds(int64 (startTime * 1000.0))
    let e =
        if endTime <= 0.0 then
            match session.EndTime with
            | Some et -> et
            | None -> DateTimeOffset.UtcNow
        else DateTimeOffset.FromUnixTimeMilliseconds(int64 (endTime * 1000.0))
    s, e

let private defaultPageSize (pageSize: int) =
    if pageSize <= 0 then 100 else min pageSize 500

let private appendCursorInfo (sb: StringBuilder) (nextCursor: PaginationCursor option) =
    match nextCursor with
    | Some c ->
        sb.AppendLine("") |> ignore
        sb.AppendLine($"next_cursor: {WireFormat.encodeCursor c}") |> ignore
    | None -> ()

[<McpServerToolType>]
type RecordingQueryTools() =

    [<McpServerTool>]
    [<Description("Query the trajectory of a specific body across recorded simulation snapshots, showing position, velocity, and orientation over time")>]
    static member query_body_trajectory
        (
            engine: RecordingEngine,
            [<Description("Body ID to track (e.g. 'sphere-1', 'box-2')")>] body_id: string,
            [<Description("Session ID (empty for active session)")>] session_id: string,
            [<Description("Start time as Unix epoch seconds (0 for session start)")>] start_time: double,
            [<Description("End time as Unix epoch seconds (0 for session end/now)")>] end_time: double,
            [<Description("Number of results per page (default 100, max 500)")>] page_size: int,
            [<Description("Pagination cursor from previous query")>] cursor: string
        ) : string =
        try
        let sb = StringBuilder()
        match resolveSession engine session_id with
        | None ->
            "No recording session found. Start a recording or provide a valid session_id."
        | Some session ->
            let sessionDir = resolveSessionDir session
            let startT, endT = resolveTimeRange session start_time end_time
            let ps = defaultPageSize page_size
            let cur =
                if String.IsNullOrWhiteSpace(nullSafe cursor) then None
                else WireFormat.decodeCursor cursor

            let entries, nextCursor = readPage sessionDir startT endT cur ps

            let bodyIdStr = nullSafe body_id

            let snapshots =
                entries
                |> List.choose (fun e ->
                    match e with
                    | LogEntry.StateSnapshot(ts, state) ->
                        state.Bodies
                        |> Seq.tryFind (fun b -> b.Id = bodyIdStr)
                        |> Option.map (fun b -> ts, b)
                    | _ -> None)

            if snapshots.IsEmpty then
                sb.AppendLine($"No trajectory data found for body {body_id} in the queried range.") |> ignore
            else
                sb.AppendLine($"Body {body_id} Trajectory ({snapshots.Length} snapshots):") |> ignore
                sb.AppendLine("---") |> ignore
                sb.AppendLine(sprintf "%-28s  %-36s  %-36s  %-40s"
                    "Time" "Position (x, y, z)" "Velocity (x, y, z)" "Orientation (x, y, z, w)") |> ignore
                for ts, body in snapshots do
                    let pos = body.Position
                    let vel = body.Velocity
                    let ori = body.Orientation
                    let posStr =
                        if pos <> null then sprintf "(%.3f, %.3f, %.3f)" pos.X pos.Y pos.Z
                        else "(-, -, -)"
                    let velStr =
                        if vel <> null then sprintf "(%.3f, %.3f, %.3f)" vel.X vel.Y vel.Z
                        else "(-, -, -)"
                    let oriStr =
                        if ori <> null then sprintf "(%.3f, %.3f, %.3f, %.3f)" ori.X ori.Y ori.Z ori.W
                        else "(-, -, -, -)"
                    sb.AppendLine(sprintf "%-28s  %-36s  %-36s  %-40s"
                        (ts.ToString("o")) posStr velStr oriStr) |> ignore

            appendCursorInfo sb nextCursor
            sb.ToString()
        with ex -> $"Error in query_body_trajectory: {ex.Message}"

    [<McpServerTool>]
    [<Description("Query recorded simulation state snapshots, returning a summary of each snapshot including body count, sim time, and tick duration")>]
    static member query_snapshots
        (
            engine: RecordingEngine,
            [<Description("Session ID (empty for active session)")>] session_id: string,
            [<Description("Start time as Unix epoch seconds (0 for session start)")>] start_time: double,
            [<Description("End time as Unix epoch seconds (0 for session end/now)")>] end_time: double,
            [<Description("Number of results per page (default 100, max 500)")>] page_size: int,
            [<Description("Pagination cursor from previous query")>] cursor: string
        ) : string =
        try
        let sb = StringBuilder()
        match resolveSession engine session_id with
        | None ->
            "No recording session found. Start a recording or provide a valid session_id."
        | Some session ->
            let sessionDir = resolveSessionDir session
            let startT, endT = resolveTimeRange session start_time end_time
            let ps = defaultPageSize page_size
            let cur =
                if String.IsNullOrWhiteSpace(nullSafe cursor) then None
                else WireFormat.decodeCursor cursor

            let entries, nextCursor = readPage sessionDir startT endT cur ps

            let snapshots =
                entries
                |> List.choose (fun e ->
                    match e with
                    | LogEntry.StateSnapshot(ts, state) -> Some (ts, state)
                    | _ -> None)

            if snapshots.IsEmpty then
                sb.AppendLine("No snapshots found in the queried range.") |> ignore
            else
                sb.AppendLine($"Simulation Snapshots ({snapshots.Length} entries):") |> ignore
                sb.AppendLine("---") |> ignore
                sb.AppendLine(sprintf "%-28s  %10s  %12s  %10s  %s"
                    "Time" "Bodies" "Sim Time" "Tick (ms)" "Running") |> ignore
                for ts, state in snapshots do
                    sb.AppendLine(sprintf "%-28s  %10d  %12.3f  %10.2f  %s"
                        (ts.ToString("o"))
                        state.Bodies.Count
                        state.Time
                        state.TickMs
                        (if state.Running then "yes" else "no")) |> ignore

            appendCursorInfo sb nextCursor
            sb.ToString()
        with ex -> $"Error in query_snapshots: {ex.Message}"

    [<McpServerTool>]
    [<Description("Query recorded command events from a recording session, optionally filtered by command type")>]
    static member query_events
        (
            engine: RecordingEngine,
            [<Description("Session ID (empty for active session)")>] session_id: string,
            [<Description("Start time as Unix epoch seconds (0 for session start)")>] start_time: double,
            [<Description("End time as Unix epoch seconds (0 for session end/now)")>] end_time: double,
            [<Description("Filter by event type (e.g. 'AddBody', 'ApplyForce', 'SetGravity'). Empty for all.")>] event_type: string,
            [<Description("Number of results per page (default 100, max 500)")>] page_size: int,
            [<Description("Pagination cursor from previous query")>] cursor: string
        ) : string =
        try
        let sb = StringBuilder()
        match resolveSession engine session_id with
        | None ->
            "No recording session found. Start a recording or provide a valid session_id."
        | Some session ->
            let sessionDir = resolveSessionDir session
            let startT, endT = resolveTimeRange session start_time end_time
            let ps = defaultPageSize page_size
            let cur =
                if String.IsNullOrWhiteSpace(nullSafe cursor) then None
                else WireFormat.decodeCursor cursor

            let entries, nextCursor = readPage sessionDir startT endT cur ps

            let describeEvent (evt: PhysicsSandbox.Shared.Contracts.CommandEvent) =
                if evt.SimulationCommand <> null then
                    let cmd = evt.SimulationCommand
                    if cmd.AddBody <> null then "AddBody", $"id={cmd.AddBody.Id} mass={cmd.AddBody.Mass}"
                    elif cmd.ApplyForce <> null then "ApplyForce", $"body={cmd.ApplyForce.BodyId}"
                    elif cmd.ApplyImpulse <> null then "ApplyImpulse", $"body={cmd.ApplyImpulse.BodyId}"
                    elif cmd.ApplyTorque <> null then "ApplyTorque", $"body={cmd.ApplyTorque.BodyId}"
                    elif cmd.SetGravity <> null then "SetGravity", $"({cmd.SetGravity.Gravity.X}, {cmd.SetGravity.Gravity.Y}, {cmd.SetGravity.Gravity.Z})"
                    elif cmd.Step <> null then "Step", ""
                    elif cmd.PlayPause <> null then "PlayPause", $"running={cmd.PlayPause.Running}"
                    elif cmd.RemoveBody <> null then "RemoveBody", $"body={cmd.RemoveBody.BodyId}"
                    elif cmd.ClearForces <> null then "ClearForces", $"body={cmd.ClearForces.BodyId}"
                    else "Unknown", ""
                elif evt.ViewCommand <> null then
                    let vcmd = evt.ViewCommand
                    if vcmd.SetCamera <> null then "SetCamera", ""
                    elif vcmd.SetZoom <> null then "SetZoom", $"level={vcmd.SetZoom.Level}"
                    elif vcmd.ToggleWireframe <> null then "ToggleWireframe", $"enabled={vcmd.ToggleWireframe.Enabled}"
                    else "UnknownView", ""
                else
                    "Empty", ""

            let events =
                entries
                |> List.choose (fun e ->
                    match e with
                    | LogEntry.CommandEvent(ts, evt) ->
                        let evtType, detail = describeEvent evt
                        Some (ts, evtType, detail)
                    | _ -> None)
                |> List.filter (fun (_, evtType, _) ->
                    if String.IsNullOrWhiteSpace(nullSafe event_type) then true
                    else evtType.Equals(event_type, StringComparison.OrdinalIgnoreCase))

            if events.IsEmpty then
                sb.AppendLine("No command events found in the queried range.") |> ignore
            else
                sb.AppendLine($"Command Events ({events.Length} entries):") |> ignore
                sb.AppendLine("---") |> ignore
                for ts, evtType, detail in events do
                    let detailStr = if String.IsNullOrEmpty(detail) then "" else $" {detail}"
                    sb.AppendLine($"  {ts:o}  {evtType}{detailStr}") |> ignore

            appendCursorInfo sb nextCursor
            sb.ToString()
        with ex -> $"Error in query_events: {ex.Message}"

    [<McpServerTool>]
    [<Description("Get a summary of a recording session metadata without scanning chunk files")>]
    static member query_summary
        (
            engine: RecordingEngine,
            [<Description("Session ID (empty for active session)")>] session_id: string
        ) : string =
        try
        let sb = StringBuilder()
        match resolveSession engine session_id with
        | None ->
            "No recording session found. Start a recording or provide a valid session_id."
        | Some session ->
            let statusStr =
                match session.Status with
                | SessionStatus.Recording -> "Recording"
                | SessionStatus.Completed -> "Completed"
                | SessionStatus.Failed -> "Failed"
            sb.AppendLine("Recording Session Summary") |> ignore
            sb.AppendLine("---") |> ignore
            sb.AppendLine($"  Session ID: {session.Id}") |> ignore
            sb.AppendLine($"  Label: {session.Label}") |> ignore
            sb.AppendLine($"  Status: {statusStr}") |> ignore
            sb.AppendLine($"  Started: {session.StartTime:o}") |> ignore
            match session.EndTime with
            | Some endTime ->
                sb.AppendLine($"  Ended: {endTime:o}") |> ignore
                let duration = endTime - session.StartTime
                sb.AppendLine($"  Duration: {duration.TotalMinutes:F1} minutes") |> ignore
            | None ->
                let elapsed = DateTimeOffset.UtcNow - session.StartTime
                sb.AppendLine($"  Elapsed: {elapsed.TotalMinutes:F1} minutes (ongoing)") |> ignore
            sb.AppendLine("") |> ignore
            sb.AppendLine("Limits:") |> ignore
            sb.AppendLine($"  Time limit: {session.TimeLimitMinutes} minutes") |> ignore
            let limitMB = float session.SizeLimitBytes / 1_048_576.0
            sb.AppendLine($"  Size limit: %.2f{limitMB} MB") |> ignore
            sb.AppendLine("") |> ignore
            sb.AppendLine("Statistics:") |> ignore
            let sizeMB = float session.CurrentSizeBytes / 1_048_576.0
            sb.AppendLine($"  Current size: %.2f{sizeMB} MB") |> ignore
            sb.AppendLine($"  Chunk count: {session.ChunkCount}") |> ignore
            sb.AppendLine($"  Snapshots: {session.SnapshotCount}") |> ignore
            sb.AppendLine($"  Events: {session.EventCount}") |> ignore
            sb.ToString()
        with ex -> $"Error in query_summary: {ex.Message}"
