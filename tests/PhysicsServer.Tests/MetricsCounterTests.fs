module PhysicsServer.Tests.MetricsCounterTests

open Xunit
open PhysicsServer.Hub

[<Fact>]
let ``create initializes all counters to zero`` () =
    let state = MetricsCounter.create "TestService"
    let report = MetricsCounter.snapshot state
    Assert.Equal("TestService", report.ServiceName)
    Assert.Equal(0L, report.MessagesSent)
    Assert.Equal(0L, report.MessagesReceived)
    Assert.Equal(0L, report.BytesSent)
    Assert.Equal(0L, report.BytesReceived)

[<Fact>]
let ``incrementSent accumulates message count and bytes`` () =
    let state = MetricsCounter.create "TestService"
    MetricsCounter.incrementSent 3 100L state
    MetricsCounter.incrementSent 2 50L state
    let report = MetricsCounter.snapshot state
    Assert.Equal(5L, report.MessagesSent)
    Assert.Equal(150L, report.BytesSent)

[<Fact>]
let ``incrementReceived accumulates message count and bytes`` () =
    let state = MetricsCounter.create "TestService"
    MetricsCounter.incrementReceived 1 200L state
    MetricsCounter.incrementReceived 4 300L state
    let report = MetricsCounter.snapshot state
    Assert.Equal(5L, report.MessagesReceived)
    Assert.Equal(500L, report.BytesReceived)

[<Fact>]
let ``sent and received counters are independent`` () =
    let state = MetricsCounter.create "TestService"
    MetricsCounter.incrementSent 10 1000L state
    MetricsCounter.incrementReceived 5 500L state
    let report = MetricsCounter.snapshot state
    Assert.Equal(10L, report.MessagesSent)
    Assert.Equal(1000L, report.BytesSent)
    Assert.Equal(5L, report.MessagesReceived)
    Assert.Equal(500L, report.BytesReceived)

[<Fact>]
let ``snapshot is consistent point-in-time read`` () =
    let state = MetricsCounter.create "Snap"
    MetricsCounter.incrementSent 1 10L state
    let snap1 = MetricsCounter.snapshot state
    MetricsCounter.incrementSent 1 10L state
    let snap2 = MetricsCounter.snapshot state
    Assert.Equal(1L, snap1.MessagesSent)
    Assert.Equal(2L, snap2.MessagesSent)

[<Fact>]
let ``concurrent increments are thread-safe`` () =
    let state = MetricsCounter.create "Concurrent"
    let iterations = 10000

    System.Threading.Tasks.Parallel.For(0, iterations, fun _ ->
        MetricsCounter.incrementSent 1 1L state
        MetricsCounter.incrementReceived 1 1L state
    )
    |> ignore

    let report = MetricsCounter.snapshot state
    Assert.Equal(int64 iterations, report.MessagesSent)
    Assert.Equal(int64 iterations, report.MessagesReceived)
    Assert.Equal(int64 iterations, report.BytesSent)
    Assert.Equal(int64 iterations, report.BytesReceived)
