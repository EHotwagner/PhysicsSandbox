module PhysicsClient.Tests.SimulationCommandsTests

open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsClient.IdGenerator

[<Fact>]
let ``IdGenerator produces sequential IDs for unique prefix`` () =
    reset ()
    let id1 = nextId "simcmd-sphere"
    let id2 = nextId "simcmd-sphere"
    Assert.Equal("simcmd-sphere-1", id1)
    Assert.Equal("simcmd-sphere-2", id2)

[<Fact>]
let ``IdGenerator produces independent counters per shape kind`` () =
    reset ()
    let s1 = nextId "simcmd-s"
    let b1 = nextId "simcmd-b"
    let s2 = nextId "simcmd-s"
    Assert.Equal("simcmd-s-1", s1)
    Assert.Equal("simcmd-b-1", b1)
    Assert.Equal("simcmd-s-2", s2)

[<Fact>]
let ``IdGenerator reset then generate starts from 1`` () =
    let _ = nextId "simcmd-reset"
    let _ = nextId "simcmd-reset"
    reset ()
    let id = nextId "simcmd-reset"
    Assert.Equal("simcmd-reset-1", id)

[<Fact>]
let ``IdGenerator plane IDs use plane prefix`` () =
    reset ()
    let id = nextId "simcmd-plane"
    Assert.Equal("simcmd-plane-1", id)

[<Fact>]
let ``Vec3 proto message stores values correctly`` () =
    let v = Vec3()
    v.X <- 1.0
    v.Y <- 2.5
    v.Z <- -3.0
    Assert.Equal(1.0, v.X)
    Assert.Equal(2.5, v.Y)
    Assert.Equal(-3.0, v.Z)

[<Fact>]
let ``SimulationCommand wraps AddBody correctly`` () =
    let sphere = Sphere()
    sphere.Radius <- 1.5
    let shape = Shape()
    shape.Sphere <- sphere
    let addBody = AddBody()
    addBody.Id <- "test-1"
    addBody.Mass <- 5.0
    let pos = Vec3()
    pos.X <- 1.0
    pos.Y <- 2.0
    pos.Z <- 3.0
    addBody.Position <- pos
    addBody.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- addBody
    Assert.NotNull(cmd.AddBody)
    Assert.Equal("test-1", cmd.AddBody.Id)
    Assert.Equal(5.0, cmd.AddBody.Mass)
    Assert.Equal(1.5, cmd.AddBody.Shape.Sphere.Radius)

[<Fact>]
let ``SimulationCommand wraps PlayPause correctly`` () =
    let pp = PlayPause()
    pp.Running <- true
    let cmd = SimulationCommand()
    cmd.PlayPause <- pp
    Assert.True(cmd.PlayPause.Running)

[<Fact>]
let ``ViewCommand wraps SetCamera correctly`` () =
    let sc = SetCamera()
    let pos = Vec3()
    pos.X <- 10.0
    pos.Y <- 20.0
    pos.Z <- 30.0
    sc.Position <- pos
    let target = Vec3()
    target.X <- 0.0
    target.Y <- 0.0
    target.Z <- 0.0
    sc.Target <- target
    let up = Vec3()
    up.X <- 0.0
    up.Y <- 1.0
    up.Z <- 0.0
    sc.Up <- up
    let cmd = ViewCommand()
    cmd.SetCamera <- sc
    Assert.Equal(10.0, cmd.SetCamera.Position.X)
    Assert.Equal(1.0, cmd.SetCamera.Up.Y)
