module PhysicsServer.Tests.StateCacheTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.StateCache

[<Fact>]
let ``Get returns None when cache is empty`` () =
    let cache = create ()
    let result = get cache
    Assert.True(result.IsNone)

[<Fact>]
let ``Get returns latest state after update`` () =
    let cache = create ()
    let state = SimulationState(Time = 1.0, Running = true)
    update cache state
    let result = get cache
    Assert.True(result.IsSome)
    Assert.Equal(1.0, result.Value.Time)
    Assert.True(result.Value.Running)

[<Fact>]
let ``Update overwrites previous state`` () =
    let cache = create ()
    let state1 = SimulationState(Time = 1.0, Running = true)
    let state2 = SimulationState(Time = 2.0, Running = false)
    update cache state1
    update cache state2
    let result = get cache
    Assert.True(result.IsSome)
    Assert.Equal(2.0, result.Value.Time)
    Assert.False(result.Value.Running)

[<Fact>]
let ``Clear removes cached state`` () =
    let cache = create ()
    let state = SimulationState(Time = 1.0, Running = true)
    update cache state
    clear cache
    let result = get cache
    Assert.True(result.IsNone)
