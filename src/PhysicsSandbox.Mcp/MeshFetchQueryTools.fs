module PhysicsSandbox.Mcp.MeshFetchQueryTools

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

let private defaultPageSize (pageSize: int) =
    if pageSize <= 0 then 100 else min pageSize 500

[<McpServerToolType>]
type MeshFetchQueryTools() =

    [<McpServerTool>]
    [<Description("Query mesh fetch events from a recorded session, showing which mesh IDs were requested, cache hits/misses, and timing")>]
    static member query_mesh_fetches
        (
            engine: RecordingEngine,
            [<Description("Session ID (empty for active session)")>] session_id: string,
            [<Description("Time window in minutes from now (default 5)")>] minutes_ago: int,
            [<Description("Filter to events involving this mesh ID (empty for all)")>] mesh_id: string,
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
            let mins = if minutes_ago <= 0 then 5 else minutes_ago
            let endTime = DateTimeOffset.UtcNow
            let startTime = endTime.AddMinutes(float -mins)
            let ps = defaultPageSize page_size
            let cur =
                if String.IsNullOrWhiteSpace(nullSafe cursor) then None
                else WireFormat.decodeCursor cursor

            let entries, nextCursor = readPage sessionDir startTime endTime cur ps

            let meshIdFilter = nullSafe mesh_id

            let fetchEvents =
                entries
                |> List.choose (fun e ->
                    match e with
                    | LogEntry.MeshFetchEvent(ts, requestedIds, hits, misses, missedIds) ->
                        if String.IsNullOrEmpty(meshIdFilter) then
                            Some (ts, requestedIds, hits, misses, missedIds)
                        else
                            // Filter: include if the mesh_id appears in requested or missed
                            if requestedIds |> List.exists (fun id -> id = meshIdFilter) then
                                Some (ts, requestedIds, hits, misses, missedIds)
                            else None
                    | _ -> None)

            if fetchEvents.IsEmpty then
                sb.AppendLine("No mesh fetch events found in the specified time range.") |> ignore
            else
                sb.AppendLine($"Mesh Fetch Events (session: {session.Id})") |> ignore
                sb.AppendLine("---") |> ignore
                for (ts, requestedIds, hits, misses, missedIds) in fetchEvents do
                    sb.AppendLine($"[{ts:o}] FetchMeshes: {requestedIds.Length} requested, {hits} hits, {misses} misses") |> ignore
                    if requestedIds.Length > 0 then
                        let reqStr = String.Join(", ", requestedIds)
                        sb.AppendLine($"  Requested: {reqStr}") |> ignore
                    if missedIds.Length > 0 then
                        let missStr = String.Join(", ", missedIds)
                        sb.AppendLine($"  Missed: {missStr}") |> ignore
                    sb.AppendLine("") |> ignore
                sb.AppendLine($"Showing {fetchEvents.Length} event(s)") |> ignore

            match nextCursor with
            | Some c ->
                sb.AppendLine("") |> ignore
                sb.AppendLine($"next_cursor: {WireFormat.encodeCursor c}") |> ignore
            | None -> ()

            sb.ToString()
        with ex -> $"Error in query_mesh_fetches: {ex.Message}"
