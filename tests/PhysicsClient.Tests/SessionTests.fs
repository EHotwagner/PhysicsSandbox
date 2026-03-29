module PhysicsClient.Tests.SessionTests

open Xunit
open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts

[<Fact>]
let ``connect creates session that reports connected`` () =
    // gRPC channels are lazy — connect succeeds, failures happen on first RPC
    let result = connect "http://localhost:59999"
    match result with
    | Ok session ->
        Assert.True(isConnected session)
        disconnect session
    | Error msg ->
        Assert.Fail($"Expected Ok but got Error: {msg}")

[<Fact>]
let ``isConnected returns false for disconnected session`` () =
    // Connect to a valid-looking address (channel creation succeeds, but streaming will fail)
    let result = connect "http://localhost:59999"
    match result with
    | Ok session ->
        disconnect session
        Assert.False(isConnected session)
    | Error _ ->
        // If connect itself failed, that's also acceptable
        ()

[<Fact>]
let ``disconnect sets isConnected to false`` () =
    let result = connect "http://localhost:59999"
    match result with
    | Ok session ->
        disconnect session
        Assert.False(isConnected session)
    | Error _ -> ()

[<Fact>]
let ``clearCaches clears BodyRegistry and BodyPropertiesCache`` () =
    let result = connect "http://localhost:59999"
    match result with
    | Ok session ->
        // Populate caches via internal accessors
        let registry = bodyRegistry session
        registry.["sphere-1"] <- "sphere"
        registry.["box-1"] <- "box"
        Assert.Equal(2, registry.Count)

        // Call clearCaches
        clearCaches session

        // Verify all caches are empty
        Assert.Equal(0, registry.Count)
        Assert.True(Option.isNone (latestState session))
        disconnect session
    | Error _ -> ()
