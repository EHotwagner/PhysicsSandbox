module PhysicsViewer.Tests.FpsCounterTests

open Xunit
open PhysicsViewer.Rendering

[<Fact>]
let ``create initializes with default 60 FPS`` () =
    let state = FpsCounter.create 30.0f
    Assert.Equal(60.0f, FpsCounter.currentFps state)

[<Fact>]
let ``update returns smoothed FPS using EMA`` () =
    let state = FpsCounter.create 30.0f
    // Simulate 60 FPS (16.67ms per frame)
    let fps = FpsCounter.update (1.0f / 60.0f) state
    // EMA: 0.1 * 60 + 0.9 * 60 = 60
    Assert.InRange(fps, 59.0f, 61.0f)

[<Fact>]
let ``update smooths FPS drop gradually`` () =
    let state = FpsCounter.create 30.0f
    // Start at 60 FPS
    FpsCounter.update (1.0f / 60.0f) state |> ignore
    // Sudden drop to 30 FPS
    let fps = FpsCounter.update (1.0f / 30.0f) state
    // EMA: 0.1 * 30 + 0.9 * ~60 = ~57 (not instant drop)
    Assert.True(fps > 50.0f, $"FPS should be smoothed, got {fps}")

[<Fact>]
let ``update caps FPS at 0 for very large delta`` () =
    let state = FpsCounter.create 30.0f
    // delta > 1s (e.g., window minimized)
    let fps = FpsCounter.update 2.0f state
    // Should trend toward 0, not calculate 0.5 FPS
    Assert.True(fps < 60.0f, $"FPS should decrease for large delta, got {fps}")

[<Fact>]
let ``shouldLog returns true after interval elapsed`` () =
    let state = FpsCounter.create 30.0f
    // Accumulate 9 seconds
    for _ in 1..540 do // 540 frames at 60fps = 9 seconds
        FpsCounter.update (1.0f / 60.0f) state |> ignore
    Assert.False(FpsCounter.shouldLog 10.0f state)
    // Accumulate 1 more second (total 10)
    for _ in 1..60 do
        FpsCounter.update (1.0f / 60.0f) state |> ignore
    Assert.True(FpsCounter.shouldLog 10.0f state)

[<Fact>]
let ``shouldLog resets elapsed after triggering`` () =
    let state = FpsCounter.create 30.0f
    for _ in 1..600 do
        FpsCounter.update (1.0f / 60.0f) state |> ignore
    Assert.True(FpsCounter.shouldLog 10.0f state)
    // Should not trigger again immediately
    Assert.False(FpsCounter.shouldLog 10.0f state)

[<Fact>]
let ``isBelowThreshold detects low FPS`` () =
    let state = FpsCounter.create 30.0f
    Assert.False(FpsCounter.isBelowThreshold state) // starts at 60
    // Drive FPS down by simulating very slow frames
    for _ in 1..100 do
        FpsCounter.update 0.1f state |> ignore // 10 FPS
    Assert.True(FpsCounter.isBelowThreshold state)

[<Fact>]
let ``update handles zero delta gracefully`` () =
    let state = FpsCounter.create 30.0f
    let fps = FpsCounter.update 0.0f state
    Assert.Equal(60.0f, fps) // should keep previous value
