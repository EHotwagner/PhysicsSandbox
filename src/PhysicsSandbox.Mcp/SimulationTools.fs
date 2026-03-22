/// <summary>MCP tool class exposing core simulation commands: adding/removing bodies, applying forces, stepping, and controlling playback.</summary>
module PhysicsSandbox.Mcp.SimulationTools

open System
open System.ComponentModel
open System.Threading
open System.Threading.Tasks
open ModelContextProtocol.Server
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Mcp.GrpcConnection

let private counters = System.Collections.Concurrent.ConcurrentDictionary<string, int>()

/// <summary>Generates a unique body ID by appending an auto-incrementing counter to the shape name (e.g., "sphere" produces "sphere-1", "sphere-2", etc.).</summary>
/// <param name="shape">The shape name prefix.</param>
/// <returns>A unique identifier string.</returns>
let nextId (shape: string) =
    let value = counters.AddOrUpdate(shape, 1, fun _ current -> current + 1)
    $"{shape}-{value}"

let private sendCmd (conn: GrpcConnection) (cmd: SimulationCommand) =
    task {
        try
            let! ack = conn.Client.SendCommandAsync(cmd)
            if ack.Success then return $"Success: {ack.Message}"
            else return $"Failed: {ack.Message}"
        with ex ->
            return $"Error: {ex.Message}"
    }

/// <summary>Parses a motion_type string to the proto enum value.</summary>
let private parseMotionType (s: string) : BodyMotionType =
    match s.ToLowerInvariant() with
    | "kinematic" -> BodyMotionType.Kinematic
    | "static" -> BodyMotionType.Static
    | _ -> BodyMotionType.Dynamic

/// <summary>MCP server tool type providing core simulation commands for adding bodies, applying forces, and controlling simulation playback.</summary>
[<McpServerToolType>]
type SimulationTools() =

    /// <summary>Adds a rigid body to the physics simulation at the specified position with the given mass. Supports sphere, box, plane, capsule, cylinder, and triangle shapes.</summary>
    [<McpServerTool; Description("Add a rigid body to the physics simulation. Supports shapes: sphere, box, plane, capsule, cylinder, triangle.")>]
    static member add_body(
        conn: GrpcConnection,
        [<Description("Body shape: 'sphere', 'box', 'plane', 'capsule', 'cylinder', or 'triangle'")>] shape: string,
        [<Description("Sphere radius (required if shape=sphere)")>] ?radius: float,
        [<Description("Box half-extent X (required if shape=box)")>] ?half_extents_x: float,
        [<Description("Box half-extent Y (required if shape=box)")>] ?half_extents_y: float,
        [<Description("Box half-extent Z (required if shape=box)")>] ?half_extents_z: float,
        [<Description("Capsule radius (required if shape=capsule)")>] ?capsule_radius: float,
        [<Description("Capsule length (required if shape=capsule)")>] ?capsule_length: float,
        [<Description("Cylinder radius (required if shape=cylinder)")>] ?cylinder_radius: float,
        [<Description("Cylinder length (required if shape=cylinder)")>] ?cylinder_length: float,
        [<Description("Triangle vertex A X (required if shape=triangle)")>] ?tri_ax: float,
        [<Description("Triangle vertex A Y (required if shape=triangle)")>] ?tri_ay: float,
        [<Description("Triangle vertex A Z (required if shape=triangle)")>] ?tri_az: float,
        [<Description("Triangle vertex B X (required if shape=triangle)")>] ?tri_bx: float,
        [<Description("Triangle vertex B Y (required if shape=triangle)")>] ?tri_by: float,
        [<Description("Triangle vertex B Z (required if shape=triangle)")>] ?tri_bz: float,
        [<Description("Triangle vertex C X (required if shape=triangle)")>] ?tri_cx: float,
        [<Description("Triangle vertex C Y (required if shape=triangle)")>] ?tri_cy: float,
        [<Description("Triangle vertex C Z (required if shape=triangle)")>] ?tri_cz: float,
        [<Description("Plane normal X (default 0)")>] ?plane_nx: float,
        [<Description("Plane normal Y (default 1)")>] ?plane_ny: float,
        [<Description("Plane normal Z (default 0)")>] ?plane_nz: float,
        [<Description("Position X")>] ?x: float,
        [<Description("Position Y")>] ?y: float,
        [<Description("Position Z")>] ?z: float,
        [<Description("Body mass (0 = static)")>] ?mass: float,
        [<Description("Friction coefficient")>] ?friction: float,
        [<Description("Max recovery velocity")>] ?max_recovery_velocity: float,
        [<Description("Spring frequency (Hz)")>] ?spring_frequency: float,
        [<Description("Spring damping ratio")>] ?spring_damping_ratio: float,
        [<Description("Color red 0.0-1.0")>] ?color_r: float,
        [<Description("Color green 0.0-1.0")>] ?color_g: float,
        [<Description("Color blue 0.0-1.0")>] ?color_b: float,
        [<Description("Color alpha 0.0-1.0")>] ?color_a: float,
        [<Description("Motion type: 'dynamic', 'kinematic', or 'static'")>] ?motion_type: string,
        [<Description("Collision group bitmask")>] ?collision_group: int,
        [<Description("Collision mask bitmask")>] ?collision_mask: int
    ) : Task<string> =
        let px = defaultArg x 0.0
        let py = defaultArg y 5.0
        let pz = defaultArg z 0.0
        let m = defaultArg mass 1.0
        let shapeLower = shape.ToLowerInvariant()
        let id = nextId shapeLower
        let body = AddBody(
            Id = id,
            Position = Vec3(X = px, Y = py, Z = pz),
            Mass = m
        )
        match shapeLower with
        | "sphere" ->
            let r = defaultArg radius 0.5
            body.Shape <- Shape(Sphere = Sphere(Radius = r))
        | "box" ->
            let hx = defaultArg half_extents_x 0.5
            let hy = defaultArg half_extents_y 0.5
            let hz = defaultArg half_extents_z 0.5
            body.Shape <- Shape(Box = Box(HalfExtents = Vec3(X = hx, Y = hy, Z = hz)))
        | "plane" ->
            let nx = defaultArg plane_nx 0.0
            let ny = defaultArg plane_ny 1.0
            let nz = defaultArg plane_nz 0.0
            body.Shape <- Shape(Plane = Plane(Normal = Vec3(X = nx, Y = ny, Z = nz)))
        | "capsule" ->
            let cr = defaultArg capsule_radius 0.5
            let cl = defaultArg capsule_length 1.0
            body.Shape <- Shape(Capsule = Capsule(Radius = cr, Length = cl))
        | "cylinder" ->
            let cr = defaultArg cylinder_radius 0.5
            let cl = defaultArg cylinder_length 1.0
            body.Shape <- Shape(Cylinder = Cylinder(Radius = cr, Length = cl))
        | "triangle" ->
            let a = Vec3(X = defaultArg tri_ax 0.0, Y = defaultArg tri_ay 0.0, Z = defaultArg tri_az 0.0)
            let b = Vec3(X = defaultArg tri_bx 1.0, Y = defaultArg tri_by 0.0, Z = defaultArg tri_bz 0.0)
            let c = Vec3(X = defaultArg tri_cx 0.0, Y = defaultArg tri_cy 1.0, Z = defaultArg tri_cz 0.0)
            body.Shape <- Shape(Triangle = Triangle(A = a, B = b, C = c))
        | _ -> ()
        // Material properties
        let hasMaterial = friction.IsSome || max_recovery_velocity.IsSome || spring_frequency.IsSome || spring_damping_ratio.IsSome
        if hasMaterial then
            body.Material <- MaterialProperties(
                Friction = defaultArg friction 1.0,
                MaxRecoveryVelocity = defaultArg max_recovery_velocity 2.0,
                SpringFrequency = defaultArg spring_frequency 30.0,
                SpringDampingRatio = defaultArg spring_damping_ratio 1.0
            )
        // Color
        let hasColor = color_r.IsSome || color_g.IsSome || color_b.IsSome || color_a.IsSome
        if hasColor then
            body.Color <- Color(
                R = defaultArg color_r 1.0,
                G = defaultArg color_g 1.0,
                B = defaultArg color_b 1.0,
                A = defaultArg color_a 1.0
            )
        // Motion type
        match motion_type with
        | Some mt -> body.MotionType <- parseMotionType mt
        | None -> ()
        // Collision filter
        match collision_group with
        | Some cg -> body.CollisionGroup <- uint32 cg
        | None -> ()
        match collision_mask with
        | Some cm -> body.CollisionMask <- uint32 cm
        | None -> ()
        sendCmd conn (SimulationCommand(AddBody = body))

    /// <summary>Applies a continuous force vector to a body. The force persists across simulation steps until cleared.</summary>
    [<McpServerTool; Description("Apply a continuous force to a body.")>]
    static member apply_force(
        conn: GrpcConnection,
        [<Description("Target body ID")>] body_id: string,
        [<Description("Force X component")>] ?x: float,
        [<Description("Force Y component")>] ?y: float,
        [<Description("Force Z component")>] ?z: float
    ) : Task<string> =
        let cmd = ApplyForce(BodyId = body_id, Force = Vec3(X = defaultArg x 0.0, Y = defaultArg y 0.0, Z = defaultArg z 0.0))
        sendCmd conn (SimulationCommand(ApplyForce = cmd))

    /// <summary>Applies an instantaneous impulse to a body, immediately changing its velocity by the given vector.</summary>
    [<McpServerTool; Description("Apply an instantaneous impulse to a body.")>]
    static member apply_impulse(
        conn: GrpcConnection,
        [<Description("Target body ID")>] body_id: string,
        [<Description("Impulse X component")>] ?x: float,
        [<Description("Impulse Y component")>] ?y: float,
        [<Description("Impulse Z component")>] ?z: float
    ) : Task<string> =
        let cmd = ApplyImpulse(BodyId = body_id, Impulse = Vec3(X = defaultArg x 0.0, Y = defaultArg y 0.0, Z = defaultArg z 0.0))
        sendCmd conn (SimulationCommand(ApplyImpulse = cmd))

    /// <summary>Applies a rotational torque to a body around the specified axis vector.</summary>
    [<McpServerTool; Description("Apply a torque to a body.")>]
    static member apply_torque(
        conn: GrpcConnection,
        [<Description("Target body ID")>] body_id: string,
        [<Description("Torque X component")>] ?x: float,
        [<Description("Torque Y component")>] ?y: float,
        [<Description("Torque Z component")>] ?z: float
    ) : Task<string> =
        let cmd = ApplyTorque(BodyId = body_id, Torque = Vec3(X = defaultArg x 0.0, Y = defaultArg y 0.0, Z = defaultArg z 0.0))
        sendCmd conn (SimulationCommand(ApplyTorque = cmd))

    /// <summary>Sets the global gravity vector for the simulation. Defaults to Earth gravity (0, -9.81, 0) when components are omitted.</summary>
    [<McpServerTool; Description("Set the global gravity vector.")>]
    static member set_gravity(
        conn: GrpcConnection,
        [<Description("Gravity X")>] ?x: float,
        [<Description("Gravity Y")>] ?y: float,
        [<Description("Gravity Z")>] ?z: float
    ) : Task<string> =
        let cmd = SetGravity(Gravity = Vec3(X = defaultArg x 0.0, Y = defaultArg y -9.81, Z = defaultArg z 0.0))
        sendCmd conn (SimulationCommand(SetGravity = cmd))

    /// <summary>Advances the physics simulation by a single time step.</summary>
    [<McpServerTool; Description("Advance the simulation by one time step.")>]
    static member step(conn: GrpcConnection) : Task<string> =
        sendCmd conn (SimulationCommand(Step = StepSimulation()))

    /// <summary>Starts continuous simulation playback, stepping automatically each frame.</summary>
    [<McpServerTool; Description("Start continuous simulation.")>]
    static member play(conn: GrpcConnection) : Task<string> =
        sendCmd conn (SimulationCommand(PlayPause = PlayPause(Running = true)))

    /// <summary>Pauses continuous simulation playback, freezing all bodies in place.</summary>
    [<McpServerTool; Description("Pause the simulation.")>]
    static member pause(conn: GrpcConnection) : Task<string> =
        sendCmd conn (SimulationCommand(PlayPause = PlayPause(Running = false)))

    /// <summary>Removes a body from the simulation by its identifier.</summary>
    [<McpServerTool; Description("Remove a body from the simulation.")>]
    static member remove_body(
        conn: GrpcConnection,
        [<Description("Body ID to remove")>] body_id: string
    ) : Task<string> =
        sendCmd conn (SimulationCommand(RemoveBody = RemoveBody(BodyId = body_id)))

    /// <summary>Clears all accumulated continuous forces on a body.</summary>
    [<McpServerTool; Description("Clear all forces on a body.")>]
    static member clear_forces(
        conn: GrpcConnection,
        [<Description("Body ID")>] body_id: string
    ) : Task<string> =
        sendCmd conn (SimulationCommand(ClearForces = ClearForces(BodyId = body_id)))

    /// <summary>Resets the entire simulation: removes all bodies, clears forces, and resets time to zero. Performance metrics persist across restarts.</summary>
    [<McpServerTool; Description("Reset the simulation: remove all bodies, clear forces, reset time to 0. Performance metrics persist across restarts.")>]
    static member restart_simulation(conn: GrpcConnection) : Task<string> =
        sendCmd conn (SimulationCommand(Reset = ResetSimulation()))

    /// <summary>Adds a constraint between two bodies. Supports ball_socket, hinge, weld, distance_limit, distance_spring, swing_limit, twist_limit, linear_axis_motor, angular_motor, and point_on_line types.</summary>
    [<McpServerTool; Description("Add a constraint between two bodies. Types: ball_socket, hinge, weld, distance_limit, distance_spring, swing_limit, twist_limit, linear_axis_motor, angular_motor, point_on_line.")>]
    static member add_constraint(
        conn: GrpcConnection,
        [<Description("First body ID")>] body_a: string,
        [<Description("Second body ID")>] body_b: string,
        [<Description("Constraint type: ball_socket, hinge, weld, distance_limit, distance_spring, swing_limit, twist_limit, linear_axis_motor, angular_motor, point_on_line")>] constraint_type: string,
        [<Description("Constraint ID (auto-generated if omitted)")>] ?id: string,
        [<Description("Local offset A X")>] ?offset_ax: float,
        [<Description("Local offset A Y")>] ?offset_ay: float,
        [<Description("Local offset A Z")>] ?offset_az: float,
        [<Description("Local offset B X")>] ?offset_bx: float,
        [<Description("Local offset B Y")>] ?offset_by: float,
        [<Description("Local offset B Z")>] ?offset_bz: float,
        [<Description("Axis/direction X (for hinge axis, swing axis, twist axis, line direction)")>] ?axis_x: float,
        [<Description("Axis/direction Y")>] ?axis_y: float,
        [<Description("Axis/direction Z")>] ?axis_z: float,
        [<Description("Secondary axis X (for hinge axis B, twist axis B, swing axis B)")>] ?axis_bx: float,
        [<Description("Secondary axis Y")>] ?axis_by: float,
        [<Description("Secondary axis Z")>] ?axis_bz: float,
        [<Description("Spring frequency (Hz)")>] ?spring_frequency: float,
        [<Description("Spring damping ratio")>] ?spring_damping_ratio: float,
        [<Description("Min distance (distance_limit)")>] ?min_distance: float,
        [<Description("Max distance (distance_limit)")>] ?max_distance: float,
        [<Description("Target distance (distance_spring)")>] ?target_distance: float,
        [<Description("Max swing angle in radians (swing_limit)")>] ?max_swing_angle: float,
        [<Description("Min angle in radians (twist_limit)")>] ?min_angle: float,
        [<Description("Max angle in radians (twist_limit)")>] ?max_angle: float,
        [<Description("Target velocity (linear_axis_motor)")>] ?target_velocity: float,
        [<Description("Motor max force")>] ?motor_max_force: float,
        [<Description("Motor damping")>] ?motor_damping: float,
        [<Description("Angular motor target velocity X")>] ?angular_vel_x: float,
        [<Description("Angular motor target velocity Y")>] ?angular_vel_y: float,
        [<Description("Angular motor target velocity Z")>] ?angular_vel_z: float,
        [<Description("Weld local orientation X (quaternion)")>] ?weld_orient_x: float,
        [<Description("Weld local orientation Y (quaternion)")>] ?weld_orient_y: float,
        [<Description("Weld local orientation Z (quaternion)")>] ?weld_orient_z: float,
        [<Description("Weld local orientation W (quaternion)")>] ?weld_orient_w: float
    ) : Task<string> =
        let constraintId = defaultArg id (nextId "constraint")
        let offA = Vec3(X = defaultArg offset_ax 0.0, Y = defaultArg offset_ay 0.0, Z = defaultArg offset_az 0.0)
        let offB = Vec3(X = defaultArg offset_bx 0.0, Y = defaultArg offset_by 0.0, Z = defaultArg offset_bz 0.0)
        let axisA = Vec3(X = defaultArg axis_x 0.0, Y = defaultArg axis_y 1.0, Z = defaultArg axis_z 0.0)
        let axisB = Vec3(X = defaultArg axis_bx 0.0, Y = defaultArg axis_by 1.0, Z = defaultArg axis_bz 0.0)
        let spring =
            let hasSpring = spring_frequency.IsSome || spring_damping_ratio.IsSome
            if hasSpring then
                SpringSettings(Frequency = defaultArg spring_frequency 30.0, DampingRatio = defaultArg spring_damping_ratio 1.0)
            else
                null
        let constraintTypeMsg = ConstraintType()
        match constraint_type.ToLowerInvariant() with
        | "ball_socket" ->
            constraintTypeMsg.BallSocket <- BallSocketConstraint(LocalOffsetA = offA, LocalOffsetB = offB, Spring = spring)
        | "hinge" ->
            constraintTypeMsg.Hinge <- HingeConstraint(LocalHingeAxisA = axisA, LocalHingeAxisB = axisB, LocalOffsetA = offA, LocalOffsetB = offB, Spring = spring)
        | "weld" ->
            let orient = Vec4(X = defaultArg weld_orient_x 0.0, Y = defaultArg weld_orient_y 0.0, Z = defaultArg weld_orient_z 0.0, W = defaultArg weld_orient_w 1.0)
            constraintTypeMsg.Weld <- WeldConstraint(LocalOffset = offA, LocalOrientation = orient, Spring = spring)
        | "distance_limit" ->
            constraintTypeMsg.DistanceLimit <- DistanceLimitConstraint(LocalOffsetA = offA, LocalOffsetB = offB, MinDistance = defaultArg min_distance 0.0, MaxDistance = defaultArg max_distance 5.0, Spring = spring)
        | "distance_spring" ->
            constraintTypeMsg.DistanceSpring <- DistanceSpringConstraint(LocalOffsetA = offA, LocalOffsetB = offB, TargetDistance = defaultArg target_distance 1.0, Spring = spring)
        | "swing_limit" ->
            constraintTypeMsg.SwingLimit <- SwingLimitConstraint(AxisLocalA = axisA, AxisLocalB = axisB, MaxSwingAngle = defaultArg max_swing_angle 1.0, Spring = spring)
        | "twist_limit" ->
            constraintTypeMsg.TwistLimit <- TwistLimitConstraint(LocalAxisA = axisA, LocalAxisB = axisB, MinAngle = defaultArg min_angle -1.0, MaxAngle = defaultArg max_angle 1.0, Spring = spring)
        | "linear_axis_motor" ->
            let motor = MotorConfig(MaxForce = defaultArg motor_max_force 100.0, Damping = defaultArg motor_damping 1.0)
            constraintTypeMsg.LinearAxisMotor <- LinearAxisMotorConstraint(LocalOffsetA = offA, LocalOffsetB = offB, LocalAxis = axisA, TargetVelocity = defaultArg target_velocity 1.0, Motor = motor)
        | "angular_motor" ->
            let motor = MotorConfig(MaxForce = defaultArg motor_max_force 100.0, Damping = defaultArg motor_damping 1.0)
            let tv = Vec3(X = defaultArg angular_vel_x 0.0, Y = defaultArg angular_vel_y 0.0, Z = defaultArg angular_vel_z 0.0)
            constraintTypeMsg.AngularMotor <- AngularMotorConstraint(TargetVelocity = tv, Motor = motor)
        | "point_on_line" ->
            constraintTypeMsg.PointOnLine <- PointOnLineConstraint(LocalOrigin = offA, LocalDirection = axisA, LocalOffset = offB, Spring = spring)
        | other ->
            constraintTypeMsg |> ignore // unknown type handled below
        if constraintTypeMsg.ConstraintCase = ConstraintType.ConstraintOneofCase.None then
            Task.FromResult $"Error: Unknown constraint type '{constraint_type}'. Supported: ball_socket, hinge, weld, distance_limit, distance_spring, swing_limit, twist_limit, linear_axis_motor, angular_motor, point_on_line."
        else
            let addConstraint = AddConstraint(Id = constraintId, BodyA = body_a, BodyB = body_b, Type = constraintTypeMsg)
            sendCmd conn (SimulationCommand(AddConstraint = addConstraint))

    /// <summary>Removes a constraint from the simulation by its identifier.</summary>
    [<McpServerTool; Description("Remove a constraint from the simulation.")>]
    static member remove_constraint(
        conn: GrpcConnection,
        [<Description("Constraint ID to remove")>] constraint_id: string
    ) : Task<string> =
        sendCmd conn (SimulationCommand(RemoveConstraint = RemoveConstraint(ConstraintId = constraint_id)))

    /// <summary>Registers a reusable shape with a handle name so it can be referenced by multiple bodies via shape_ref.</summary>
    [<McpServerTool; Description("Register a reusable shape by handle name. Supported shapes: sphere, box, capsule, cylinder, triangle.")>]
    static member register_shape(
        conn: GrpcConnection,
        [<Description("Unique shape handle name")>] shape_handle: string,
        [<Description("Shape type: 'sphere', 'box', 'capsule', 'cylinder', 'triangle'")>] shape: string,
        [<Description("Sphere radius")>] ?radius: float,
        [<Description("Box half-extent X")>] ?half_extents_x: float,
        [<Description("Box half-extent Y")>] ?half_extents_y: float,
        [<Description("Box half-extent Z")>] ?half_extents_z: float,
        [<Description("Capsule radius")>] ?capsule_radius: float,
        [<Description("Capsule length")>] ?capsule_length: float,
        [<Description("Cylinder radius")>] ?cylinder_radius: float,
        [<Description("Cylinder length")>] ?cylinder_length: float,
        [<Description("Triangle vertex A X")>] ?tri_ax: float,
        [<Description("Triangle vertex A Y")>] ?tri_ay: float,
        [<Description("Triangle vertex A Z")>] ?tri_az: float,
        [<Description("Triangle vertex B X")>] ?tri_bx: float,
        [<Description("Triangle vertex B Y")>] ?tri_by: float,
        [<Description("Triangle vertex B Z")>] ?tri_bz: float,
        [<Description("Triangle vertex C X")>] ?tri_cx: float,
        [<Description("Triangle vertex C Y")>] ?tri_cy: float,
        [<Description("Triangle vertex C Z")>] ?tri_cz: float
    ) : Task<string> =
        let shapeMsg = Shape()
        match shape.ToLowerInvariant() with
        | "sphere" ->
            shapeMsg.Sphere <- Sphere(Radius = defaultArg radius 0.5)
        | "box" ->
            shapeMsg.Box <- Box(HalfExtents = Vec3(X = defaultArg half_extents_x 0.5, Y = defaultArg half_extents_y 0.5, Z = defaultArg half_extents_z 0.5))
        | "capsule" ->
            shapeMsg.Capsule <- Capsule(Radius = defaultArg capsule_radius 0.5, Length = defaultArg capsule_length 1.0)
        | "cylinder" ->
            shapeMsg.Cylinder <- Cylinder(Radius = defaultArg cylinder_radius 0.5, Length = defaultArg cylinder_length 1.0)
        | "triangle" ->
            let a = Vec3(X = defaultArg tri_ax 0.0, Y = defaultArg tri_ay 0.0, Z = defaultArg tri_az 0.0)
            let b = Vec3(X = defaultArg tri_bx 1.0, Y = defaultArg tri_by 0.0, Z = defaultArg tri_bz 0.0)
            let c = Vec3(X = defaultArg tri_cx 0.0, Y = defaultArg tri_cy 1.0, Z = defaultArg tri_cz 0.0)
            shapeMsg.Triangle <- Triangle(A = a, B = b, C = c)
        | _ ->
            () // unknown shape handled below
        if shapeMsg.ShapeCase = Shape.ShapeOneofCase.None then
            Task.FromResult $"Error: Unknown shape type '{shape}'. Supported: sphere, box, capsule, cylinder, triangle."
        else
            sendCmd conn (SimulationCommand(RegisterShape = RegisterShape(ShapeHandle = shape_handle, Shape = shapeMsg)))

    /// <summary>Unregisters a previously registered shape by its handle name.</summary>
    [<McpServerTool; Description("Unregister a shape by its handle name.")>]
    static member unregister_shape(
        conn: GrpcConnection,
        [<Description("Shape handle to unregister")>] shape_handle: string
    ) : Task<string> =
        sendCmd conn (SimulationCommand(UnregisterShape = UnregisterShape(ShapeHandle = shape_handle)))

    /// <summary>Sets the collision group and mask for a body, controlling which other bodies it can collide with.</summary>
    [<McpServerTool; Description("Set the collision group and mask for a body.")>]
    static member set_collision_filter(
        conn: GrpcConnection,
        [<Description("Body ID")>] body_id: string,
        [<Description("Collision group bitmask")>] collision_group: int,
        [<Description("Collision mask bitmask")>] collision_mask: int
    ) : Task<string> =
        let cmd = SetCollisionFilter(BodyId = body_id, CollisionGroup = uint32 collision_group, CollisionMask = uint32 collision_mask)
        sendCmd conn (SimulationCommand(SetCollisionFilter = cmd))

    /// <summary>Sets the position, orientation, and/or velocity of a body at runtime. Works for kinematic and dynamic bodies.</summary>
    [<McpServerTool; Description("Set the position, orientation, and/or velocity of a body at runtime.")>]
    static member set_body_pose(
        conn: GrpcConnection,
        [<Description("Body ID")>] body_id: string,
        [<Description("Position X")>] x: float,
        [<Description("Position Y")>] y: float,
        [<Description("Position Z")>] z: float,
        [<Description("Velocity X")>] ?vx: float,
        [<Description("Velocity Y")>] ?vy: float,
        [<Description("Velocity Z")>] ?vz: float
    ) : Task<string> =
        let pose = SetBodyPose(BodyId = body_id, Position = Vec3(X = x, Y = y, Z = z))
        match vx, vy, vz with
        | Some vx, Some vy, Some vz -> pose.Velocity <- Vec3(X = vx, Y = vy, Z = vz)
        | _ -> ()
        let simCmd = SimulationCommand()
        simCmd.SetBodyPose <- pose
        sendCmd conn simCmd

    /// <summary>Casts a ray into the simulation and returns hit information. Can return the closest hit or all hits along the ray.</summary>
    [<McpServerTool; Description("Cast a ray into the simulation and return hit results.")>]
    static member raycast(
        conn: GrpcConnection,
        [<Description("Ray origin X")>] origin_x: float,
        [<Description("Ray origin Y")>] origin_y: float,
        [<Description("Ray origin Z")>] origin_z: float,
        [<Description("Ray direction X")>] direction_x: float,
        [<Description("Ray direction Y")>] direction_y: float,
        [<Description("Ray direction Z")>] direction_z: float,
        [<Description("Maximum ray distance")>] ?max_distance: float,
        [<Description("Collision mask filter")>] ?collision_mask: int,
        [<Description("Return all hits instead of just closest")>] ?all_hits: bool
    ) : Task<string> =
        task {
            try
                let request = RaycastRequest(
                    Origin = Vec3(X = origin_x, Y = origin_y, Z = origin_z),
                    Direction = Vec3(X = direction_x, Y = direction_y, Z = direction_z),
                    MaxDistance = defaultArg max_distance 1000.0,
                    AllHits = defaultArg all_hits false
                )
                match collision_mask with
                | Some cm -> request.CollisionMask <- uint32 cm
                | None -> ()
                let! response = conn.Client.RaycastAsync(request)
                if not response.Hit then
                    return "No hit."
                else
                    let hits =
                        response.Hits
                        |> Seq.map (fun h ->
                            $"  body={h.BodyId} pos=({h.Position.X:F3},{h.Position.Y:F3},{h.Position.Z:F3}) normal=({h.Normal.X:F3},{h.Normal.Y:F3},{h.Normal.Z:F3}) dist={h.Distance:F3}")
                        |> String.concat "\n"
                    return $"Hit ({response.Hits.Count} result(s)):\n{hits}"
            with ex ->
                return $"Error: {ex.Message}"
        }

    static member private buildQueryShape
        (shape: string, ?radius: float, ?half_extents_x: float, ?half_extents_y: float, ?half_extents_z: float, ?capsule_radius: float, ?capsule_length: float) =
        let protoShape = Shape()
        match shape.ToLowerInvariant() with
        | "sphere" ->
            protoShape.Sphere <- Sphere(Radius = defaultArg radius 0.5)
            Ok protoShape
        | "box" ->
            protoShape.Box <- Box(HalfExtents = Vec3(X = defaultArg half_extents_x 0.5, Y = defaultArg half_extents_y 0.5, Z = defaultArg half_extents_z 0.5))
            Ok protoShape
        | "capsule" ->
            protoShape.Capsule <- Capsule(Radius = defaultArg capsule_radius 0.25, Length = defaultArg capsule_length 0.5)
            Ok protoShape
        | "cylinder" ->
            protoShape.Cylinder <- Cylinder(Radius = defaultArg capsule_radius 0.25, Length = defaultArg capsule_length 0.5)
            Ok protoShape
        | other -> Error $"Error: Unknown shape type '{other}'. Use sphere, box, capsule, or cylinder."

    /// <summary>Performs a sweep cast (shape cast) into the simulation using a specified shape.</summary>
    [<McpServerTool; Description("Sweep a shape through the simulation and return the closest hit.")>]
    static member sweep_cast(
        conn: GrpcConnection,
        [<Description("Shape type: sphere, box, capsule, cylinder")>] shape: string,
        [<Description("Start position X")>] start_x: float,
        [<Description("Start position Y")>] start_y: float,
        [<Description("Start position Z")>] start_z: float,
        [<Description("Sweep direction X")>] direction_x: float,
        [<Description("Sweep direction Y")>] direction_y: float,
        [<Description("Sweep direction Z")>] direction_z: float,
        [<Description("Maximum sweep distance")>] ?max_distance: float,
        [<Description("Sphere radius (for sphere shape)")>] ?radius: float,
        [<Description("Box half-extent X (for box shape)")>] ?half_extents_x: float,
        [<Description("Box half-extent Y (for box shape)")>] ?half_extents_y: float,
        [<Description("Box half-extent Z (for box shape)")>] ?half_extents_z: float,
        [<Description("Capsule/cylinder radius")>] ?capsule_radius: float,
        [<Description("Capsule/cylinder length")>] ?capsule_length: float,
        [<Description("Collision mask filter")>] ?collision_mask: int
    ) : Task<string> =
        task {
            try
                match SimulationTools.buildQueryShape(shape, ?radius = radius, ?half_extents_x = half_extents_x, ?half_extents_y = half_extents_y, ?half_extents_z = half_extents_z, ?capsule_radius = capsule_radius, ?capsule_length = capsule_length) with
                | Error msg -> return msg
                | Ok protoShape ->
                    let request = SweepCastRequest(
                        Shape = protoShape,
                        StartPosition = Vec3(X = start_x, Y = start_y, Z = start_z),
                        Direction = Vec3(X = direction_x, Y = direction_y, Z = direction_z),
                        MaxDistance = defaultArg max_distance 1000.0
                    )
                    match collision_mask with
                    | Some cm -> request.CollisionMask <- uint32 cm
                    | None -> ()
                    let! response = conn.Client.SweepCastAsync(request)
                    if not response.Hit then
                        return "No hit."
                    else
                        let h = response.Closest
                        return $"Hit: body={h.BodyId} pos=({h.Position.X:F3},{h.Position.Y:F3},{h.Position.Z:F3}) normal=({h.Normal.X:F3},{h.Normal.Y:F3},{h.Normal.Z:F3}) dist={h.Distance:F3}"
            with ex ->
                return $"Error: {ex.Message}"
        }

    /// <summary>Tests for shape overlap at a given position, returning IDs of all overlapping bodies.</summary>
    [<McpServerTool; Description("Test for overlapping bodies at a position using a given shape.")>]
    static member overlap(
        conn: GrpcConnection,
        [<Description("Shape type: sphere, box, capsule, cylinder")>] shape: string,
        [<Description("Test position X")>] x: float,
        [<Description("Test position Y")>] y: float,
        [<Description("Test position Z")>] z: float,
        [<Description("Sphere radius (for sphere shape)")>] ?radius: float,
        [<Description("Box half-extent X (for box shape)")>] ?half_extents_x: float,
        [<Description("Box half-extent Y (for box shape)")>] ?half_extents_y: float,
        [<Description("Box half-extent Z (for box shape)")>] ?half_extents_z: float,
        [<Description("Capsule/cylinder radius")>] ?capsule_radius: float,
        [<Description("Capsule/cylinder length")>] ?capsule_length: float,
        [<Description("Collision mask filter")>] ?collision_mask: int
    ) : Task<string> =
        task {
            try
                match SimulationTools.buildQueryShape(shape, ?radius = radius, ?half_extents_x = half_extents_x, ?half_extents_y = half_extents_y, ?half_extents_z = half_extents_z, ?capsule_radius = capsule_radius, ?capsule_length = capsule_length) with
                | Error msg -> return msg
                | Ok protoShape ->
                    let request = OverlapRequest(
                        Shape = protoShape,
                        Position = Vec3(X = x, Y = y, Z = z)
                    )
                    match collision_mask with
                    | Some cm -> request.CollisionMask <- uint32 cm
                    | None -> ()
                    let! response = conn.Client.OverlapAsync(request)
                    if response.BodyIds.Count = 0 then
                        return "No overlapping bodies."
                    else
                        let ids = response.BodyIds |> Seq.toList |> String.concat ", "
                        return $"Overlapping bodies ({response.BodyIds.Count}): {ids}"
            with ex ->
                return $"Error: {ex.Message}"
        }
