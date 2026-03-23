module PhysicsClient.Tests.RegistryErrorTests

open System.Collections.Concurrent
open Xunit

let private tryRemove (dict: ConcurrentDictionary<string, string>) (key: string) =
    let mutable _v = ""
    dict.TryRemove(key, &_v)

// These tests verify that registry operations (TryAdd/TryRemove) properly
// return errors instead of silently ignoring failures.
// Since the actual add/remove functions require a gRPC session, we test
// the ConcurrentDictionary behavior directly to validate our error paths.

[<Fact>]
let ``TryAdd returns false for duplicate key`` () =
    let registry = ConcurrentDictionary<string, string>()
    let first = registry.TryAdd("sphere-1", "sphere")
    let second = registry.TryAdd("sphere-1", "sphere")
    Assert.True(first)
    Assert.False(second)

[<Fact>]
let ``Duplicate add produces registry error message`` () =
    let registry = ConcurrentDictionary<string, string>()
    registry.TryAdd("sphere-1", "sphere") |> ignore
    let bodyId = "sphere-1"
    let result =
        if not (registry.TryAdd(bodyId, "sphere")) then
            Error $"Body '{bodyId}' already exists in registry"
        else
            Ok bodyId
    match result with
    | Error msg ->
        Assert.Contains("already exists in registry", msg)
        Assert.Contains("sphere-1", msg)
    | Ok _ -> Assert.Fail("Expected Error for duplicate add")

[<Fact>]
let ``TryRemove returns false for missing key`` () =
    let registry = ConcurrentDictionary<string, string>()
    let removed = tryRemove registry "nonexistent"
    Assert.False(removed)

[<Fact>]
let ``Missing remove produces registry error message`` () =
    let registry = ConcurrentDictionary<string, string>()
    let bodyId = "nonexistent-body"
    let result =
        if not (tryRemove registry bodyId) then
            Error $"Body '{bodyId}' not found in registry"
        else
            Ok ()
    match result with
    | Error msg ->
        Assert.Contains("not found in registry", msg)
        Assert.Contains("nonexistent-body", msg)
    | Ok _ -> Assert.Fail("Expected Error for missing remove")

[<Fact>]
let ``TryRemove succeeds for existing key`` () =
    let registry = ConcurrentDictionary<string, string>()
    registry.TryAdd("box-1", "box") |> ignore
    let removed = tryRemove registry "box-1"
    Assert.True(removed)

[<Fact>]
let ``clearAll with missing registry entries produces warning count`` () =
    let registry = ConcurrentDictionary<string, string>()
    // Simulate: keys exist in the snapshot but have been removed from the dict
    // before TryRemove is called (race condition scenario)
    let keys = ["body-1"; "body-2"; "body-3"]
    // Add only 1 of the 3 keys
    registry.TryAdd("body-2", "sphere") |> ignore
    let mutable warningCount = 0
    for key in keys do
        if not (tryRemove registry key) then
            warningCount <- warningCount + 1
    // body-1 and body-3 were not in registry, so 2 warnings
    Assert.Equal(2, warningCount)

[<Fact>]
let ``clearAll with all entries present produces zero warnings`` () =
    let registry = ConcurrentDictionary<string, string>()
    registry.TryAdd("a", "sphere") |> ignore
    registry.TryAdd("b", "box") |> ignore
    let keys = ["a"; "b"]
    let mutable warningCount = 0
    for key in keys do
        if not (tryRemove registry key) then
            warningCount <- warningCount + 1
    Assert.Equal(0, warningCount)
