module PhysicsClient.Tests.SessionTests

open Xunit
open PhysicsClient.Session

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
