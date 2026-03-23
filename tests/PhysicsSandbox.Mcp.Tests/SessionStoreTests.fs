module PhysicsSandbox.Mcp.Tests.SessionStoreTests

open System
open System.IO
open Xunit
open PhysicsSandbox.Mcp.Recording.Types
open PhysicsSandbox.Mcp.Recording.SessionStore

/// Helper to clean up a session after a test
let private cleanup (sessionId: string) =
    try deleteSession sessionId |> ignore with _ -> ()

[<Fact>]
let ``create and load session`` () =
    let session = createSession "Test Session" 30 50_000_000L
    try
        let loaded = loadSession session.Id
        Assert.True(loaded.IsSome, "Session should be loadable after creation")

        let s = loaded.Value
        Assert.Equal(session.Id, s.Id)
        Assert.Equal("Test Session", s.Label)
        Assert.Equal(SessionStatus.Recording, s.Status)
        Assert.Equal(30, s.TimeLimitMinutes)
        Assert.Equal(50_000_000L, s.SizeLimitBytes)
        Assert.Equal(0L, s.CurrentSizeBytes)
        Assert.Equal(0, s.ChunkCount)
        Assert.Equal(0L, s.SnapshotCount)
        Assert.Equal(0L, s.EventCount)
        Assert.True(s.EndTime.IsNone, "EndTime should be None for new session")
    finally
        cleanup session.Id

[<Fact>]
let ``json round trip fidelity`` () =
    let session = createSession "Round Trip Test" 60 100_000_000L
    try
        let endTime = DateTimeOffset.UtcNow
        let updated =
            { session with
                Status = SessionStatus.Completed
                EndTime = Some endTime
                CurrentSizeBytes = 12345L
                ChunkCount = 3
                SnapshotCount = 100L
                EventCount = 50L }
        updateSession updated

        let loaded = loadSession session.Id
        Assert.True(loaded.IsSome, "Updated session should be loadable")

        let s = loaded.Value
        Assert.Equal(SessionStatus.Completed, s.Status)
        Assert.True(s.EndTime.IsSome, "EndTime should be set")
        Assert.Equal(12345L, s.CurrentSizeBytes)
        Assert.Equal(3, s.ChunkCount)
        Assert.Equal(100L, s.SnapshotCount)
        Assert.Equal(50L, s.EventCount)
    finally
        cleanup session.Id

[<Fact>]
let ``delete session removes directory`` () =
    let session = createSession "Delete Me" 10 1_000_000L
    let sessionDir = Path.Combine(getRecordingsDir(), session.Id)

    Assert.True(Directory.Exists sessionDir, "Session directory should exist after creation")

    let deleted = deleteSession session.Id
    Assert.True(deleted, "deleteSession should return true")
    Assert.False(Directory.Exists sessionDir, "Session directory should be removed after deletion")

[<Fact>]
let ``list sessions returns all`` () =
    let sessions =
        [| createSession "Session A" 10 1_000_000L
           createSession "Session B" 20 2_000_000L
           createSession "Session C" 30 3_000_000L |]
    try
        let all = listSessions ()
        // At minimum, the 3 we just created should be present
        let createdIds = sessions |> Array.map (fun s -> s.Id) |> Set.ofArray
        let foundCount = all |> List.filter (fun s -> Set.contains s.Id createdIds) |> List.length
        Assert.Equal(3, foundCount)
    finally
        for s in sessions do cleanup s.Id

[<Fact>]
let ``active session tracking`` () =
    let session = createSession "Active Test" 60 100_000_000L
    try
        let active = getActiveSession ()
        Assert.True(active.IsSome, "There should be an active session")
        Assert.Equal(session.Id, active.Value.Id)
        Assert.Equal(SessionStatus.Recording, active.Value.Status)
    finally
        cleanup session.Id

[<Fact>]
let ``validation clamps invalid limits`` () =
    // TimeLimitMinutes=0 should be clamped to 1
    let session1 = createSession "Clamp Time" 0 50_000_000L
    try
        Assert.Equal(1, session1.TimeLimitMinutes)
    finally
        cleanup session1.Id

    // SizeLimitBytes=0 should be clamped to 1MB (1_000_000)
    let session2 = createSession "Clamp Size" 10 0L
    try
        Assert.Equal(1_000_000L, session2.SizeLimitBytes)
    finally
        cleanup session2.Id
