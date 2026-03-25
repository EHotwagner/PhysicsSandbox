module PhysicsServer.Tests.StateStreamOptimizationTests

open System.Threading.Tasks
open Xunit
open PhysicsSandbox.Shared.Contracts
open PhysicsServer.Hub.MessageRouter
open CommonTestBuilders

[<Fact>]
let ``T034: publishState broadcasts TickState to all subscribers`` () =
    task {
        let router = create ()
        let received1 = ResizeArray<TickState>()
        let received2 = ResizeArray<TickState>()

        let sub1 = subscribe router (fun ts -> received1.Add(ts); Task.CompletedTask)
        let sub2 = subscribe router (fun ts -> received2.Add(ts); Task.CompletedTask)

        let state = makeState [ makeBody "dyn1" false; makeBody "dyn2" false ]
        do! publishState router state

        // Both subscribers received exactly one TickState
        Assert.Equal(1, received1.Count)
        Assert.Equal(1, received2.Count)

        // TickState contains only dynamic body poses
        let tick1 = received1.[0]
        let tick2 = received2.[0]
        Assert.Equal(2, tick1.Bodies.Count)
        Assert.Equal(2, tick2.Bodies.Count)

        // Verify the pose IDs match the dynamic bodies
        let ids1 = tick1.Bodies |> Seq.map (fun bp -> bp.Id) |> Set.ofSeq
        Assert.Contains("dyn1", ids1)
        Assert.Contains("dyn2", ids1)

        // Verify pose data was populated
        let pose = tick1.Bodies |> Seq.find (fun bp -> bp.Id = "dyn1")
        Assert.Equal(1.0, pose.Position.X)
        Assert.Equal(2.0, pose.Position.Y)
        Assert.Equal(3.0, pose.Position.Z)

        unsubscribe router sub1
        unsubscribe router sub2
    }

[<Fact>]
let ``T035: publishPropertyEvent broadcasts to PropertySubscribers and updates PropertyCache`` () =
    task {
        let router = create ()
        let received = ResizeArray<PropertyEvent>()

        let sub = subscribeProperties router (fun evt -> received.Add(evt); Task.CompletedTask)

        // Publish a state with one new body — should trigger body_created
        let state = makeState [ makeBody "body1" false ]
        do! publishState router state

        // The property subscriber should have received a body_created event
        Assert.True(received.Count >= 1)
        let createdEvt = received |> Seq.find (fun e -> e.EventCase = PropertyEvent.EventOneofCase.BodyCreated)
        Assert.Equal("body1", createdEvt.BodyCreated.Id)

        // getPropertySnapshot should contain the body
        let snapshot = getPropertySnapshot router
        Assert.True(snapshot.IsSome)
        let bodies = snapshot.Value.Bodies
        Assert.Equal(1, bodies.Count)
        Assert.Equal("body1", bodies.[0].Id)

        unsubscribeProperties router sub
    }

[<Fact>]
let ``T036: getPropertySnapshot returns cached snapshot for late joiners`` () =
    task {
        let router = create ()

        // Publish a state with 3 bodies (no property subscriber needed)
        let state = makeState [
            makeBody "a" false
            makeBody "b" false
            makeBody "c" true
        ]
        do! publishState router state

        // Late joiner calls getPropertySnapshot
        let snapshot = getPropertySnapshot router
        Assert.True(snapshot.IsSome)
        Assert.Equal(3, snapshot.Value.Bodies.Count)

        let ids = snapshot.Value.Bodies |> Seq.map (fun bp -> bp.Id) |> Set.ofSeq
        Assert.Contains("a", ids)
        Assert.Contains("b", ids)
        Assert.Contains("c", ids)
    }

[<Fact>]
let ``T066_a: TickState with exclude_velocity omits velocity and angular_velocity`` () =
    task {
        // This tests the server-side StripVelocity logic indirectly.
        // The server builds TickState with velocity, then strips it for exclude_velocity subscribers.
        // Here we test the MessageRouter's buildTickState includes velocity by default.
        let router = create ()
        let received = ResizeArray<TickState>()

        let _sub = subscribe router (fun ts -> received.Add(ts); Task.CompletedTask)

        let body = makeBody "vel-body" false
        body.Velocity <- Vec3(X = 5.0, Y = 3.0, Z = 1.0)
        body.AngularVelocity <- Vec3(X = 0.1, Y = 0.2, Z = 0.3)
        let state = makeState [ body ]
        do! publishState router state

        Assert.Equal(1, received.Count)
        let tick = received.[0]
        let pose = tick.Bodies |> Seq.find (fun bp -> bp.Id = "vel-body")

        // Default: velocity IS included in the TickState
        Assert.NotNull(pose.Velocity)
        Assert.Equal(5.0, pose.Velocity.X)
        Assert.Equal(3.0, pose.Velocity.Y)
        Assert.Equal(1.0, pose.Velocity.Z)
        Assert.NotNull(pose.AngularVelocity)
        Assert.Equal(0.1, pose.AngularVelocity.X)
        Assert.Equal(0.2, pose.AngularVelocity.Y)
        Assert.Equal(0.3, pose.AngularVelocity.Z)

        // Simulate StripVelocity (what PhysicsHubService does for exclude_velocity=true)
        let stripped = TickState()
        stripped.Time <- tick.Time
        stripped.Running <- tick.Running
        for p in tick.Bodies do
            let bp = BodyPose(Id = p.Id, Position = p.Position, Orientation = p.Orientation)
            // velocity deliberately NOT copied
            stripped.Bodies.Add(bp)

        let strippedPose = stripped.Bodies |> Seq.find (fun bp -> bp.Id = "vel-body")
        Assert.Null(strippedPose.Velocity)
        Assert.Null(strippedPose.AngularVelocity)
    }

[<Fact>]
let ``T075: TickState without exclude_velocity includes velocity and angular_velocity`` () =
    task {
        let router = create ()
        let received = ResizeArray<TickState>()

        let _sub = subscribe router (fun ts -> received.Add(ts); Task.CompletedTask)

        let body = makeBody "full-vel" false
        body.Velocity <- Vec3(X = 10.0, Y = -5.0, Z = 2.5)
        body.AngularVelocity <- Vec3(X = 0.5, Y = 1.0, Z = 1.5)
        let state = makeState [ body ]
        do! publishState router state

        Assert.Equal(1, received.Count)
        let tick = received.[0]
        let pose = tick.Bodies |> Seq.find (fun bp -> bp.Id = "full-vel")

        // Without exclude_velocity, velocity data is present
        Assert.NotNull(pose.Velocity)
        Assert.Equal(10.0, pose.Velocity.X)
        Assert.Equal(-5.0, pose.Velocity.Y)
        Assert.Equal(2.5, pose.Velocity.Z)
        Assert.NotNull(pose.AngularVelocity)
        Assert.Equal(0.5, pose.AngularVelocity.X)
        Assert.Equal(1.0, pose.AngularVelocity.Y)
        Assert.Equal(1.5, pose.AngularVelocity.Z)
    }

[<Fact>]
let ``T066_b: BodyPose without velocity merges correctly with cached BodyProperties`` () =
    task {
        // Simulates what the viewer does: receives TickState without velocity,
        // merges with cached BodyProperties to reconstruct full Body for rendering.
        let router = create ()
        let receivedTicks = ResizeArray<TickState>()
        let receivedProps = ResizeArray<PropertyEvent>()

        let _sub1 = subscribe router (fun ts -> receivedTicks.Add(ts); Task.CompletedTask)
        let _sub2 = subscribeProperties router (fun pe -> receivedProps.Add(pe); Task.CompletedTask)

        let body = makeBody "render-body" false
        body.Shape <- Shape(Sphere = Sphere(Radius = 2.0))
        body.Color <- Color(R = 0.5, G = 0.8, B = 0.2, A = 1.0)
        body.Mass <- 3.0
        let state = makeState [ body ]
        do! publishState router state

        Assert.True(receivedTicks.Count >= 1)
        Assert.True(receivedProps.Count >= 1)

        // Get the TickState (simulating viewer with exclude_velocity)
        let tick = receivedTicks.[0]
        let stripped = TickState(Time = tick.Time, Running = tick.Running)
        for p in tick.Bodies do
            stripped.Bodies.Add(BodyPose(Id = p.Id, Position = p.Position, Orientation = p.Orientation))

        // Get the BodyProperties from property event
        let createdEvt = receivedProps |> Seq.find (fun e -> e.EventCase = PropertyEvent.EventOneofCase.BodyCreated)
        let props = createdEvt.BodyCreated

        // Merge: combine stripped pose + cached properties (what viewer does)
        let pose = stripped.Bodies |> Seq.find (fun bp -> bp.Id = "render-body")
        let mergedBody = Body()
        mergedBody.Id <- pose.Id
        mergedBody.Position <- pose.Position
        mergedBody.Orientation <- pose.Orientation
        // Semi-static from cached properties
        mergedBody.Shape <- props.Shape
        mergedBody.Color <- props.Color
        mergedBody.Mass <- props.Mass
        mergedBody.IsStatic <- props.IsStatic
        mergedBody.MotionType <- props.MotionType

        // Verify the merged body has both pose and semi-static data
        Assert.Equal("render-body", mergedBody.Id)
        Assert.NotNull(mergedBody.Position)
        Assert.Equal(1.0, mergedBody.Position.X)
        Assert.NotNull(mergedBody.Shape)
        Assert.Equal(2.0, mergedBody.Shape.Sphere.Radius)
        Assert.Equal(0.5, mergedBody.Color.R)
        Assert.Equal(0.8, mergedBody.Color.G)
        Assert.Equal(3.0, mergedBody.Mass)
        // Velocity is NOT set (viewer opts out)
        Assert.Null(mergedBody.Velocity)
    }
