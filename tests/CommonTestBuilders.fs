module CommonTestBuilders

open PhysicsSandbox.Shared.Contracts

/// Create a test Body with standard fields populated.
/// Position (1,2,3), Orientation identity, Velocity/AngularVelocity for dynamic bodies,
/// plus a default Sphere shape and red Color.
let makeBody id isStatic =
    let b = Body(Id = id, IsStatic = isStatic)
    b.Mass <- if isStatic then 0.0 else 1.0
    b.MotionType <- if isStatic then BodyMotionType.Static else BodyMotionType.Dynamic
    b.Position <- Vec3(X = 1.0, Y = 2.0, Z = 3.0)
    b.Orientation <- Vec4(X = 0.0, Y = 0.0, Z = 0.0, W = 1.0)

    if not isStatic then
        b.Velocity <- Vec3(X = 0.1, Y = 0.2, Z = 0.3)
        b.AngularVelocity <- Vec3(X = 0.01, Y = 0.02, Z = 0.03)

    b.Shape <- Shape(Sphere = Sphere(Radius = 1.0))
    b.Color <- Color(R = 1.0, G = 0.0, B = 0.0, A = 1.0)
    b

/// Create a SimulationState from a list of bodies with Time=1.0, Running=true.
let makeState (bodies: Body list) =
    let s = SimulationState(Time = 1.0, Running = true)
    for b in bodies do s.Bodies.Add(b)
    s
