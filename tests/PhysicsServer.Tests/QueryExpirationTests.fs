module PhysicsServer.Tests.QueryExpirationTests

open System
open System.Threading.Tasks
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MessageRouter
open Xunit

[<Fact>]
let ``Expired entry is removed by expireStaleQueries`` () =
    // Arrange: add an entry with CreatedAt > 30s ago
    let tcs = TaskCompletionSource<QueryResponse>(TaskCreationOptions.RunContinuationsAsynchronously)
    let entry = { Tcs = tcs; CreatedAt = DateTime.UtcNow.AddSeconds(-31.0) }
    let key = Guid.NewGuid().ToString()
    pendingQueries.TryAdd(key, entry) |> ignore

    // Act
    expireStaleQueries ()

    // Assert: entry should be removed
    Assert.False(pendingQueries.ContainsKey(key))

[<Fact>]
let ``Non-expired entry is NOT removed by expireStaleQueries`` () =
    // Arrange: add a fresh entry (well within timeout)
    let tcs = TaskCompletionSource<QueryResponse>(TaskCreationOptions.RunContinuationsAsynchronously)
    let entry = { Tcs = tcs; CreatedAt = DateTime.UtcNow }
    let key = Guid.NewGuid().ToString()
    pendingQueries.TryAdd(key, entry) |> ignore

    // Act
    expireStaleQueries ()

    // Assert: entry should still be present
    Assert.True(pendingQueries.ContainsKey(key))

    // Cleanup
    pendingQueries.TryRemove(key) |> ignore

[<Fact>]
let ``Expired entry gets TimeoutException set on TCS`` () =
    // Arrange
    let tcs = TaskCompletionSource<QueryResponse>(TaskCreationOptions.RunContinuationsAsynchronously)
    let entry = { Tcs = tcs; CreatedAt = DateTime.UtcNow.AddSeconds(-35.0) }
    let key = Guid.NewGuid().ToString()
    pendingQueries.TryAdd(key, entry) |> ignore

    // Act
    expireStaleQueries ()

    // Assert: TCS should be faulted with TimeoutException
    Assert.True(tcs.Task.IsFaulted)
    let ex = Assert.IsType<AggregateException>(tcs.Task.Exception)
    let inner = Assert.IsType<TimeoutException>(ex.InnerException)
    Assert.Contains("30s", inner.Message)

[<Fact>]
let ``Normal query resolution sets TCS result properly`` () =
    // Arrange
    let tcs = TaskCompletionSource<QueryResponse>(TaskCreationOptions.RunContinuationsAsynchronously)
    let entry = { Tcs = tcs; CreatedAt = DateTime.UtcNow }
    let correlationId = Guid.NewGuid().ToString()
    pendingQueries.TryAdd(correlationId, entry) |> ignore

    // Act: simulate a response arriving
    let response = QueryResponse(CorrelationId = correlationId)
    match pendingQueries.TryRemove(correlationId) with
    | true, e -> e.Tcs.TrySetResult(response) |> ignore
    | _ -> ()

    // Assert: TCS completed with the response
    Assert.True(tcs.Task.IsCompletedSuccessfully)
    Assert.Equal(correlationId, tcs.Task.Result.CorrelationId)
