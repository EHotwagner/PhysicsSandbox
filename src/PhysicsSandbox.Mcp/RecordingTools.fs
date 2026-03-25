module PhysicsSandbox.Mcp.RecordingTools

open System
open System.ComponentModel
open System.Text
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.Recording.RecordingEngine
open PhysicsSandbox.Mcp.Recording.SessionStore
open PhysicsSandbox.Mcp.Recording.Types

[<McpServerToolType>]
type RecordingTools() =

    [<McpServerTool>]
    [<Description("Get the status of the current recording session including storage usage and limits")>]
    static member recording_status(engine: RecordingEngine) : string =
        let sb = StringBuilder()
        match engine.ActiveSession with
        | Some session ->
            sb.AppendLine("Recording Status") |> ignore
            sb.AppendLine("---") |> ignore
            sb.AppendLine($"  Session ID: {session.Id}") |> ignore
            sb.AppendLine($"  Label: {session.Label}") |> ignore
            let statusStr =
                match session.Status with
                | SessionStatus.Recording -> "Recording"
                | SessionStatus.Completed -> "Completed"
                | SessionStatus.Failed -> "Failed"
            sb.AppendLine($"  Status: {statusStr}") |> ignore
            sb.AppendLine($"  Started: {session.StartTime:o}") |> ignore
            match session.EndTime with
            | Some endTime -> sb.AppendLine($"  Ended: {endTime:o}") |> ignore
            | None -> ()
            sb.AppendLine("") |> ignore
            sb.AppendLine("Storage:") |> ignore
            let sizeMB = float session.CurrentSizeBytes / 1_048_576.0
            let limitMB = float session.SizeLimitBytes / 1_048_576.0
            let pctSize = if session.SizeLimitBytes > 0L then float session.CurrentSizeBytes / float session.SizeLimitBytes * 100.0 else 0.0
            sb.AppendLine($"  Current size: %.2f{sizeMB} MB / %.2f{limitMB} MB (%.1f{pctSize}%%)") |> ignore
            sb.AppendLine($"  Chunk count: {session.ChunkCount}") |> ignore
            sb.AppendLine($"  Time limit: {session.TimeLimitMinutes} minutes") |> ignore
            sb.AppendLine("") |> ignore
            sb.AppendLine("Entries:") |> ignore
            sb.AppendLine($"  Snapshots: {session.SnapshotCount}") |> ignore
            sb.AppendLine($"  Events: {session.EventCount}") |> ignore
            sb.ToString()
        | None ->
            "No active recording session."

    [<McpServerTool>]
    [<Description("Start a new recording session. If one is already active, it will be stopped first.")>]
    static member start_recording
        ( engine: RecordingEngine,
          [<Description("Descriptive label for the session. Default: auto-generated timestamp label.")>] label: string,
          [<Description("Rolling time window in minutes. Default: 10.")>] time_limit_minutes: Nullable<int>,
          [<Description("Maximum storage in MB. Default: 500.")>] size_limit_mb: Nullable<int> ) : string =
        let wasRecording = engine.IsRecording
        let sizeVal = if size_limit_mb.HasValue then size_limit_mb.Value else 500
        let sizeLimitBytes = sizeVal |> int64 |> (*) 1_000_000L
        let labelOpt = if String.IsNullOrEmpty(label) then None else Some label
        let timeLimitOpt = if time_limit_minutes.HasValue then Some time_limit_minutes.Value else None
        engine.Start(?label = labelOpt, ?timeLimitMinutes = timeLimitOpt, ?sizeLimitBytes = Some sizeLimitBytes)
        let sb = StringBuilder()
        if wasRecording then
            sb.AppendLine("Warning: Previous recording session was stopped.") |> ignore
        match engine.ActiveSession with
        | Some session ->
            sb.AppendLine($"Recording started.") |> ignore
            sb.AppendLine($"  Session ID: {session.Id}") |> ignore
            sb.AppendLine($"  Label: {session.Label}") |> ignore
            sb.AppendLine($"  Time limit: {session.TimeLimitMinutes} minutes") |> ignore
            let limitMB = float session.SizeLimitBytes / 1_048_576.0
            sb.AppendLine($"  Size limit: %.0f{limitMB} MB") |> ignore
        | None ->
            sb.AppendLine("Error: Failed to start recording session.") |> ignore
        sb.ToString()

    [<McpServerTool>]
    [<Description("Stop the currently active recording session")>]
    static member stop_recording(engine: RecordingEngine) : string =
        if not engine.IsRecording then
            "Error: No active recording session to stop."
        else
            let session = engine.ActiveSession
            engine.Stop()
            match engine.ActiveSession with
            | Some session ->
                let sb = StringBuilder()
                sb.AppendLine("Recording stopped.") |> ignore
                sb.AppendLine($"  Session ID: {session.Id}") |> ignore
                sb.AppendLine($"  Label: {session.Label}") |> ignore
                match session.EndTime with
                | Some endTime ->
                    let duration = endTime - session.StartTime
                    sb.AppendLine($"  Duration: {duration.TotalMinutes:F1} minutes") |> ignore
                | None -> ()
                let sizeMB = float session.CurrentSizeBytes / 1_048_576.0
                sb.AppendLine($"  Size: %.2f{sizeMB} MB") |> ignore
                sb.AppendLine($"  Snapshots: {session.SnapshotCount}") |> ignore
                sb.AppendLine($"  Events: {session.EventCount}") |> ignore
                sb.ToString()
            | None ->
                "Recording stopped."

    [<McpServerTool>]
    [<Description("List all recording sessions with their metadata")>]
    static member list_sessions(engine: RecordingEngine) : string =
        let sessions = listSessions ()
        if sessions.IsEmpty then
            "No recording sessions found."
        else
            let sb = StringBuilder()
            sb.AppendLine($"Recording Sessions ({sessions.Length} total):") |> ignore
            sb.AppendLine("---") |> ignore
            for session in sessions do
                let statusStr =
                    match session.Status with
                    | SessionStatus.Recording -> "Recording"
                    | SessionStatus.Completed -> "Completed"
                    | SessionStatus.Failed -> "Failed"
                let sizeMB = float session.CurrentSizeBytes / 1_048_576.0
                sb.AppendLine($"  [{statusStr}] {session.Label}") |> ignore
                sb.AppendLine($"    ID: {session.Id}") |> ignore
                sb.AppendLine($"    Started: {session.StartTime:o}") |> ignore
                match session.EndTime with
                | Some endTime -> sb.AppendLine($"    Ended: {endTime:o}") |> ignore
                | None -> ()
                sb.AppendLine($"    Size: %.2f{sizeMB} MB | Chunks: {session.ChunkCount} | Snapshots: {session.SnapshotCount} | Events: {session.EventCount}") |> ignore
                sb.AppendLine("") |> ignore
            sb.ToString()

    [<McpServerTool>]
    [<Description("Delete a completed recording session and free its storage")>]
    static member delete_session
        ( engine: RecordingEngine,
          [<Description("Session ID to delete")>] session_id: string ) : string =
        match engine.ActiveSession with
        | Some active when active.Id = session_id ->
            "Error: Cannot delete the active recording session. Stop it first with stop_recording."
        | _ ->
            match loadSession session_id with
            | None ->
                $"Error: Session '{session_id}' not found."
            | Some session ->
                let sizeMB = float session.CurrentSizeBytes / 1_048_576.0
                if deleteSession session_id then
                    $"Session '{session.Label}' deleted. Freed %.2f{sizeMB} MB."
                else
                    $"Error: Failed to delete session '{session_id}'."
