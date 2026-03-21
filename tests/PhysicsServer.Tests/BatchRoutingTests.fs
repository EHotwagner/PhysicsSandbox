module PhysicsServer.Tests.BatchRoutingTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MessageRouter

[<Fact>]
let ``sendBatchCommand routes each command and returns per-command results`` () =
    let router = create ()
    // Connect a simulation so commands are forwarded
    Assert.True(tryConnectSimulation router)

    let batch = BatchSimulationRequest()
    for i in 0..4 do
        let cmd = SimulationCommand()
        cmd.Step <- StepSimulation(DeltaTime = 0.016)
        batch.Commands.Add(cmd)

    let response = sendBatchCommand router batch

    Assert.Equal(5, response.Results.Count)
    for i in 0..4 do
        Assert.True(response.Results.[i].Success)
        Assert.Equal(i, response.Results.[i].Index)
    Assert.True(response.TotalTimeMs >= 0.0)

[<Fact>]
let ``sendBatchCommand returns results with correct indices`` () =
    let router = create ()
    let batch = BatchSimulationRequest()

    let cmd1 = SimulationCommand()
    cmd1.Step <- StepSimulation(DeltaTime = 0.016)
    batch.Commands.Add(cmd1)

    let cmd2 = SimulationCommand()
    cmd2.SetGravity <- SetGravity(Gravity = Vec3(X = 0.0, Y = -9.81, Z = 0.0))
    batch.Commands.Add(cmd2)

    let response = sendBatchCommand router batch

    Assert.Equal(2, response.Results.Count)
    Assert.Equal(0, response.Results.[0].Index)
    Assert.Equal(1, response.Results.[1].Index)

[<Fact>]
let ``sendBatchCommand rejects batch exceeding 100 commands`` () =
    let router = create ()
    let batch = BatchSimulationRequest()

    for _ in 0..100 do // 101 commands
        let cmd = SimulationCommand()
        cmd.Step <- StepSimulation(DeltaTime = 0.016)
        batch.Commands.Add(cmd)

    let response = sendBatchCommand router batch

    Assert.Single(response.Results) |> ignore
    Assert.False(response.Results.[0].Success)
    Assert.Contains("100", response.Results.[0].Message)

[<Fact>]
let ``sendBatchViewCommand routes view commands`` () =
    let router = create ()
    let batch = BatchViewRequest()

    let cmd1 = ViewCommand()
    cmd1.SetZoom <- SetZoom(Level = 2.0)
    batch.Commands.Add(cmd1)

    let cmd2 = ViewCommand()
    cmd2.ToggleWireframe <- ToggleWireframe(Enabled = true)
    batch.Commands.Add(cmd2)

    let response = sendBatchViewCommand router batch

    Assert.Equal(2, response.Results.Count)
    Assert.True(response.Results.[0].Success)
    Assert.True(response.Results.[1].Success)

[<Fact>]
let ``sendBatchCommand measures total time`` () =
    let router = create ()
    let batch = BatchSimulationRequest()

    for _ in 0..9 do
        let cmd = SimulationCommand()
        cmd.Step <- StepSimulation(DeltaTime = 0.016)
        batch.Commands.Add(cmd)

    let response = sendBatchCommand router batch
    Assert.True(response.TotalTimeMs >= 0.0)
