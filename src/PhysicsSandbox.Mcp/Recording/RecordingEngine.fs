module PhysicsSandbox.Mcp.Recording.RecordingEngine

open System
open PhysicsSandbox.Mcp.Recording.Types
open PhysicsSandbox.Mcp.Recording.ChunkWriter
open PhysicsSandbox.Mcp.Recording.SessionStore
open PhysicsSandbox.Shared.Contracts

[<Sealed>]
type RecordingEngine() =
    let mutable currentWriter: ChunkWriter option = None
    let mutable currentSession: RecordingSession option = None
    let mutable isRecording = false
    let mutable started = false

    member this.Start(?label: string, ?timeLimitMinutes: int, ?sizeLimitBytes: int64) =
        if isRecording then
            this.Stop()

        let lbl = defaultArg label (sprintf "recording-%s" (DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")))
        let tl = defaultArg timeLimitMinutes 10
        let sl = defaultArg sizeLimitBytes 500_000_000L

        let session = SessionStore.createSession lbl tl sl
        let sessionDir = System.IO.Path.Combine(getRecordingsDir(), session.Id)

        let writer =
            ChunkWriter.create
                { SessionDir = sessionDir
                  TimeLimitMinutes = tl
                  SizeLimitBytes = sl }

        writer.Start()

        isRecording <- true
        started <- true
        currentWriter <- Some writer
        currentSession <- Some session

    member this.Stop() =
        if not isRecording then ()
        else

        match currentWriter with
        | None -> ()
        | Some writer ->
            writer.Stop() |> Async.RunSynchronously

            let now = DateTimeOffset.UtcNow

            match currentSession with
            | Some session ->
                let updated =
                    { session with
                        Status = SessionStatus.Completed
                        EndTime = Some now
                        SnapshotCount = writer.SnapshotCount
                        EventCount = writer.EventCount
                        CurrentSizeBytes = writer.CurrentSizeBytes
                        ChunkCount = writer.ChunkCount }

                SessionStore.updateSession updated
                currentSession <- Some updated
            | None -> ()

            (writer :> IDisposable).Dispose()

        isRecording <- false
        currentWriter <- None

    member _.IsRecording = isRecording

    member _.ActiveSession =
        if isRecording then
            match currentSession, currentWriter with
            | Some session, Some writer ->
                Some
                    { session with
                        SnapshotCount = writer.SnapshotCount
                        EventCount = writer.EventCount
                        CurrentSizeBytes = writer.CurrentSizeBytes
                        ChunkCount = writer.ChunkCount }
            | _ -> currentSession
        else
            currentSession

    member this.OnStateReceived(state: SimulationState) =
        if not started && not isRecording then
            try
                eprintfn "[Recording] Auto-starting recording on first state received"
                this.Start()
                eprintfn "[Recording] Auto-start succeeded, session: %s" (match currentSession with Some s -> s.Id | None -> "?")
            with ex ->
                eprintfn "[Recording] Auto-start FAILED: %s" ex.Message
                started <- true // prevent retrying on every state update

        match currentWriter with
        | Some writer when isRecording ->
            let ts = DateTimeOffset.UtcNow
            writer.Enqueue(LogEntry.StateSnapshot(ts, state))
        | _ -> ()

    member _.OnCommandReceived(event: CommandEvent) =
        if not isRecording then ()
        else

        match currentWriter with
        | Some writer ->
            let ts = DateTimeOffset.UtcNow
            writer.Enqueue(LogEntry.CommandEvent(ts, event))
        | None -> ()

    interface IDisposable with
        member this.Dispose() =
            if isRecording then
                this.Stop()

let create () =
    eprintfn "[Recording] Initializing RecordingEngine, recordings dir: %s" (getRecordingsDir())
    // Restart recovery: mark any interrupted sessions as Completed
    try
        let sessions = SessionStore.listSessions ()
        for session in sessions do
            if session.Status = SessionStatus.Recording then
                let updated =
                    { session with
                        Status = SessionStatus.Completed
                        EndTime = Some DateTimeOffset.UtcNow }
                SessionStore.updateSession updated
                eprintfn "[Recording] Recovery: marked interrupted session '%s' as Completed" session.Label
    with ex ->
        eprintfn "[Recording] Recovery scan failed: %s" ex.Message
    new RecordingEngine()
