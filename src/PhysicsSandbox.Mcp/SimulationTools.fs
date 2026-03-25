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

    static member private nopt(n: Nullable<'T>) : 'T option = if n.HasValue then Some n.Value else None

    /// <summary>Adds a rigid body to the physics simulation at the specified position with the given mass. Supports sphere, box, plane, capsule, cylinder, and triangle shapes.</summary>
    [<McpServerTool; Description("Add a rigid body to the physics simulation. Shape parameter groups: sphere (radius), box (half_extents_x/y/z), plane (plane_nx/ny/nz), capsule (capsule_radius, capsule_length), cylinder (cylinder_radius, cylinder_length), triangle (tri_ax..tri_cz). Also supports material properties, color, motion_type, and collision filtering.")>]
    static member add_body(
        conn: GrpcConnection,
        [<Description("Body shape: 'sphere', 'box', 'plane', 'capsule', 'cylinder', or 'triangle'")>] shape: string,
        [<Description("Required when shape='sphere'. Sphere radius. Default: 0.5. Ignored for other shapes.")>] radius: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent X. Default: 0.5. Ignored for other shapes.")>] half_extents_x: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent Y. Default: 0.5. Ignored for other shapes.")>] half_extents_y: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent Z. Default: 0.5. Ignored for other shapes.")>] half_extents_z: Nullable<float>,
        [<Description("Required when shape='capsule'. Capsule radius. Default: 0.5. Ignored for other shapes.")>] capsule_radius: Nullable<float>,
        [<Description("Required when shape='capsule'. Capsule length. Default: 1.0. Ignored for other shapes.")>] capsule_length: Nullable<float>,
        [<Description("Required when shape='cylinder'. Cylinder radius. Default: 0.5. Ignored for other shapes.")>] cylinder_radius: Nullable<float>,
        [<Description("Required when shape='cylinder'. Cylinder length. Default: 1.0. Ignored for other shapes.")>] cylinder_length: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex A X coordinate. Default: 0.0. Ignored for other shapes.")>] tri_ax: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex A Y coordinate. Default: 0.0. Ignored for other shapes.")>] tri_ay: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex A Z coordinate. Default: 0.0. Ignored for other shapes.")>] tri_az: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex B X coordinate. Default: 1.0. Ignored for other shapes.")>] tri_bx: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex B Y coordinate. Default: 0.0. Ignored for other shapes.")>] tri_by: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex B Z coordinate. Default: 0.0. Ignored for other shapes.")>] tri_bz: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex C X coordinate. Default: 0.0. Ignored for other shapes.")>] tri_cx: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex C Y coordinate. Default: 1.0. Ignored for other shapes.")>] tri_cy: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex C Z coordinate. Default: 0.0. Ignored for other shapes.")>] tri_cz: Nullable<float>,
        [<Description("Required when shape='plane'. Plane normal X. Default: 0. Ignored for other shapes.")>] plane_nx: Nullable<float>,
        [<Description("Required when shape='plane'. Plane normal Y. Default: 1. Ignored for other shapes.")>] plane_ny: Nullable<float>,
        [<Description("Required when shape='plane'. Plane normal Z. Default: 0. Ignored for other shapes.")>] plane_nz: Nullable<float>,
        [<Description("Position X. Default: 0.")>] x: Nullable<float>,
        [<Description("Position Y. Default: 5.")>] y: Nullable<float>,
        [<Description("Position Z. Default: 0.")>] z: Nullable<float>,
        [<Description("Body mass. 0 = static body. Default: 1.0.")>] mass: Nullable<float>,
        [<Description("Friction coefficient. Default: 1.0. Only applied when any material property is set.")>] friction: Nullable<float>,
        [<Description("Max recovery velocity. Default: 2.0. Only applied when any material property is set.")>] max_recovery_velocity: Nullable<float>,
        [<Description("Spring frequency in Hz. Default: 30.0. Only applied when any material property is set.")>] spring_frequency: Nullable<float>,
        [<Description("Spring damping ratio. Default: 1.0. Only applied when any material property is set.")>] spring_damping_ratio: Nullable<float>,
        [<Description("Color red component 0.0-1.0. Default: 1.0. Only applied when any color component is set.")>] color_r: Nullable<float>,
        [<Description("Color green component 0.0-1.0. Default: 1.0. Only applied when any color component is set.")>] color_g: Nullable<float>,
        [<Description("Color blue component 0.0-1.0. Default: 1.0. Only applied when any color component is set.")>] color_b: Nullable<float>,
        [<Description("Color alpha component 0.0-1.0. Default: 1.0. Only applied when any color component is set.")>] color_a: Nullable<float>,
        [<Description("Motion type: 'dynamic' (default), 'kinematic', or 'static'. Omit for dynamic.")>] motion_type: string,
        [<Description("Collision group bitmask. Optional. Omit to use default collision group.")>] collision_group: Nullable<int>,
        [<Description("Collision mask bitmask. Optional. Omit to use default collision mask.")>] collision_mask: Nullable<int>
    ) : Task<string> =
        let px = (if x.HasValue then x.Value else 0.0)
        let py = (if y.HasValue then y.Value else 5.0)
        let pz = (if z.HasValue then z.Value else 0.0)
        let m = (if mass.HasValue then mass.Value else 1.0)
        let shapeLower = shape.ToLowerInvariant()
        let id = nextId shapeLower
        let body = AddBody(
            Id = id,
            Position = Vec3(X = px, Y = py, Z = pz),
            Mass = m
        )
        match shapeLower with
        | "sphere" ->
            let r = (if radius.HasValue then radius.Value else 0.5)
            body.Shape <- Shape(Sphere = Sphere(Radius = r))
        | "box" ->
            let hx = (if half_extents_x.HasValue then half_extents_x.Value else 0.5)
            let hy = (if half_extents_y.HasValue then half_extents_y.Value else 0.5)
            let hz = (if half_extents_z.HasValue then half_extents_z.Value else 0.5)
            body.Shape <- Shape(Box = Box(HalfExtents = Vec3(X = hx, Y = hy, Z = hz)))
        | "plane" ->
            let nx = (if plane_nx.HasValue then plane_nx.Value else 0.0)
            let ny = (if plane_ny.HasValue then plane_ny.Value else 1.0)
            let nz = (if plane_nz.HasValue then plane_nz.Value else 0.0)
            body.Shape <- Shape(Plane = Plane(Normal = Vec3(X = nx, Y = ny, Z = nz)))
        | "capsule" ->
            let cr = (if capsule_radius.HasValue then capsule_radius.Value else 0.5)
            let cl = (if capsule_length.HasValue then capsule_length.Value else 1.0)
            body.Shape <- Shape(Capsule = Capsule(Radius = cr, Length = cl))
        | "cylinder" ->
            let cr = (if cylinder_radius.HasValue then cylinder_radius.Value else 0.5)
            let cl = (if cylinder_length.HasValue then cylinder_length.Value else 1.0)
            body.Shape <- Shape(Cylinder = Cylinder(Radius = cr, Length = cl))
        | "triangle" ->
            let a = Vec3(X = (if tri_ax.HasValue then tri_ax.Value else 0.0), Y = (if tri_ay.HasValue then tri_ay.Value else 0.0), Z = (if tri_az.HasValue then tri_az.Value else 0.0))
            let b = Vec3(X = (if tri_bx.HasValue then tri_bx.Value else 1.0), Y = (if tri_by.HasValue then tri_by.Value else 0.0), Z = (if tri_bz.HasValue then tri_bz.Value else 0.0))
            let c = Vec3(X = (if tri_cx.HasValue then tri_cx.Value else 0.0), Y = (if tri_cy.HasValue then tri_cy.Value else 1.0), Z = (if tri_cz.HasValue then tri_cz.Value else 0.0))
            body.Shape <- Shape(Triangle = Triangle(A = a, B = b, C = c))
        | _ -> ()
        // Material properties
        let hasMaterial = friction.HasValue || max_recovery_velocity.HasValue || spring_frequency.HasValue || spring_damping_ratio.HasValue
        if hasMaterial then
            body.Material <- MaterialProperties(
                Friction = (if friction.HasValue then friction.Value else 1.0),
                MaxRecoveryVelocity = (if max_recovery_velocity.HasValue then max_recovery_velocity.Value else 2.0),
                SpringFrequency = (if spring_frequency.HasValue then spring_frequency.Value else 30.0),
                SpringDampingRatio = (if spring_damping_ratio.HasValue then spring_damping_ratio.Value else 1.0)
            )
        // Color
        let hasColor = color_r.HasValue || color_g.HasValue || color_b.HasValue || color_a.HasValue
        if hasColor then
            body.Color <- Color(
                R = (if color_r.HasValue then color_r.Value else 1.0),
                G = (if color_g.HasValue then color_g.Value else 1.0),
                B = (if color_b.HasValue then color_b.Value else 1.0),
                A = (if color_a.HasValue then color_a.Value else 1.0)
            )
        // Motion type
        if motion_type <> null then
            body.MotionType <- parseMotionType motion_type
        // Collision filter
        if collision_group.HasValue then
            body.CollisionGroup <- uint32 collision_group.Value
        if collision_mask.HasValue then
            body.CollisionMask <- uint32 collision_mask.Value
        sendCmd conn (SimulationCommand(AddBody = body))

    /// <summary>Applies a continuous force vector to a body. The force persists across simulation steps until cleared.</summary>
    [<McpServerTool; Description("Apply a continuous force to a body.")>]
    static member apply_force(
        conn: GrpcConnection,
        [<Description("Target body ID")>] body_id: string,
        [<Description("Force X component. Default: 0.")>] x: Nullable<float>,
        [<Description("Force Y component. Default: 0.")>] y: Nullable<float>,
        [<Description("Force Z component. Default: 0.")>] z: Nullable<float>
    ) : Task<string> =
        let cmd = ApplyForce(BodyId = body_id, Force = Vec3(X = (if x.HasValue then x.Value else 0.0), Y = (if y.HasValue then y.Value else 0.0), Z = (if z.HasValue then z.Value else 0.0)))
        sendCmd conn (SimulationCommand(ApplyForce = cmd))

    /// <summary>Applies an instantaneous impulse to a body, immediately changing its velocity by the given vector.</summary>
    [<McpServerTool; Description("Apply an instantaneous impulse to a body.")>]
    static member apply_impulse(
        conn: GrpcConnection,
        [<Description("Target body ID")>] body_id: string,
        [<Description("Impulse X component. Default: 0.")>] x: Nullable<float>,
        [<Description("Impulse Y component. Default: 0.")>] y: Nullable<float>,
        [<Description("Impulse Z component. Default: 0.")>] z: Nullable<float>
    ) : Task<string> =
        let cmd = ApplyImpulse(BodyId = body_id, Impulse = Vec3(X = (if x.HasValue then x.Value else 0.0), Y = (if y.HasValue then y.Value else 0.0), Z = (if z.HasValue then z.Value else 0.0)))
        sendCmd conn (SimulationCommand(ApplyImpulse = cmd))

    /// <summary>Applies a rotational torque to a body around the specified axis vector.</summary>
    [<McpServerTool; Description("Apply a torque to a body.")>]
    static member apply_torque(
        conn: GrpcConnection,
        [<Description("Target body ID")>] body_id: string,
        [<Description("Torque X component. Default: 0.")>] x: Nullable<float>,
        [<Description("Torque Y component. Default: 0.")>] y: Nullable<float>,
        [<Description("Torque Z component. Default: 0.")>] z: Nullable<float>
    ) : Task<string> =
        let cmd = ApplyTorque(BodyId = body_id, Torque = Vec3(X = (if x.HasValue then x.Value else 0.0), Y = (if y.HasValue then y.Value else 0.0), Z = (if z.HasValue then z.Value else 0.0)))
        sendCmd conn (SimulationCommand(ApplyTorque = cmd))

    /// <summary>Sets the global gravity vector for the simulation. Defaults to Earth gravity (0, -9.81, 0) when components are omitted.</summary>
    [<McpServerTool; Description("Set the global gravity vector.")>]
    static member set_gravity(
        conn: GrpcConnection,
        [<Description("Gravity X. Default: 0.")>] x: Nullable<float>,
        [<Description("Gravity Y. Default: -9.81.")>] y: Nullable<float>,
        [<Description("Gravity Z. Default: 0.")>] z: Nullable<float>
    ) : Task<string> =
        let cmd = SetGravity(Gravity = Vec3(X = (if x.HasValue then x.Value else 0.0), Y = (if y.HasValue then y.Value else -9.81), Z = (if z.HasValue then z.Value else 0.0)))
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
    [<McpServerTool; Description("Add a constraint between two bodies. Constraint types and their parameter groups: ball_socket (offsets, spring), hinge (offsets, axis, axis_b, spring), weld (offset_a as local offset, weld_orient, spring), distance_limit (offsets, min_distance, max_distance, spring), distance_spring (offsets, target_distance, spring), swing_limit (axis, axis_b, max_swing_angle, spring), twist_limit (axis, axis_b, min_angle, max_angle, spring), linear_axis_motor (offsets, axis, target_velocity, motor), angular_motor (angular_vel, motor), point_on_line (offset_a=origin, axis=direction, offset_b=point, spring).")>]
    static member add_constraint(
        conn: GrpcConnection,
        [<Description("First body ID")>] body_a: string,
        [<Description("Second body ID")>] body_b: string,
        [<Description("Constraint type: ball_socket, hinge, weld, distance_limit, distance_spring, swing_limit, twist_limit, linear_axis_motor, angular_motor, point_on_line")>] constraint_type: string,
        [<Description("Constraint ID. Default: auto-generated. Omit for auto-naming.")>] id: string,
        [<Description("Local offset A X. Default: 0. Used by ball_socket, hinge, distance_limit, distance_spring, linear_axis_motor, point_on_line (as origin), weld (as local offset).")>] offset_ax: Nullable<float>,
        [<Description("Local offset A Y. Default: 0. Used by ball_socket, hinge, distance_limit, distance_spring, linear_axis_motor, point_on_line, weld.")>] offset_ay: Nullable<float>,
        [<Description("Local offset A Z. Default: 0. Used by ball_socket, hinge, distance_limit, distance_spring, linear_axis_motor, point_on_line, weld.")>] offset_az: Nullable<float>,
        [<Description("Local offset B X. Default: 0. Used by ball_socket, hinge, distance_limit, distance_spring, linear_axis_motor, point_on_line (as point offset).")>] offset_bx: Nullable<float>,
        [<Description("Local offset B Y. Default: 0. Used by ball_socket, hinge, distance_limit, distance_spring, linear_axis_motor, point_on_line.")>] offset_by: Nullable<float>,
        [<Description("Local offset B Z. Default: 0. Used by ball_socket, hinge, distance_limit, distance_spring, linear_axis_motor, point_on_line.")>] offset_bz: Nullable<float>,
        [<Description("Primary axis X. Default: 0. Used by hinge (hinge axis A), swing_limit (axis A), twist_limit (axis A), linear_axis_motor (local axis), point_on_line (direction).")>] axis_x: Nullable<float>,
        [<Description("Primary axis Y. Default: 1. Used by hinge, swing_limit, twist_limit, linear_axis_motor, point_on_line.")>] axis_y: Nullable<float>,
        [<Description("Primary axis Z. Default: 0. Used by hinge, swing_limit, twist_limit, linear_axis_motor, point_on_line.")>] axis_z: Nullable<float>,
        [<Description("Secondary axis X. Default: 0. Used by hinge (hinge axis B), swing_limit (axis B), twist_limit (axis B).")>] axis_bx: Nullable<float>,
        [<Description("Secondary axis Y. Default: 1. Used by hinge, swing_limit, twist_limit.")>] axis_by: Nullable<float>,
        [<Description("Secondary axis Z. Default: 0. Used by hinge, swing_limit, twist_limit.")>] axis_bz: Nullable<float>,
        [<Description("Spring frequency in Hz. Default: 30.0. Used by ball_socket, hinge, weld, distance_limit, distance_spring, swing_limit, twist_limit, point_on_line. Only applied when spring_frequency or spring_damping_ratio is set.")>] spring_frequency: Nullable<float>,
        [<Description("Spring damping ratio. Default: 1.0. Used by same constraints as spring_frequency. Only applied when spring_frequency or spring_damping_ratio is set.")>] spring_damping_ratio: Nullable<float>,
        [<Description("Applies to distance_limit. Minimum distance. Default: 0.0. Ignored for other constraint types.")>] min_distance: Nullable<float>,
        [<Description("Applies to distance_limit. Maximum distance. Default: 5.0. Ignored for other constraint types.")>] max_distance: Nullable<float>,
        [<Description("Applies to distance_spring. Target rest distance. Default: 1.0. Ignored for other constraint types.")>] target_distance: Nullable<float>,
        [<Description("Applies to swing_limit. Maximum swing angle in radians. Default: 1.0. Ignored for other constraint types.")>] max_swing_angle: Nullable<float>,
        [<Description("Applies to twist_limit. Minimum twist angle in radians. Default: -1.0. Ignored for other constraint types.")>] min_angle: Nullable<float>,
        [<Description("Applies to twist_limit. Maximum twist angle in radians. Default: 1.0. Ignored for other constraint types.")>] max_angle: Nullable<float>,
        [<Description("Applies to linear_axis_motor. Target velocity along the axis. Default: 1.0. Ignored for other constraint types.")>] target_velocity: Nullable<float>,
        [<Description("Motor max force. Default: 100.0. Applies to linear_axis_motor and angular_motor. Ignored for other constraint types.")>] motor_max_force: Nullable<float>,
        [<Description("Motor damping. Default: 1.0. Applies to linear_axis_motor and angular_motor. Ignored for other constraint types.")>] motor_damping: Nullable<float>,
        [<Description("Applies to angular_motor. Target angular velocity X. Default: 0. Ignored for other constraint types.")>] angular_vel_x: Nullable<float>,
        [<Description("Applies to angular_motor. Target angular velocity Y. Default: 0. Ignored for other constraint types.")>] angular_vel_y: Nullable<float>,
        [<Description("Applies to angular_motor. Target angular velocity Z. Default: 0. Ignored for other constraint types.")>] angular_vel_z: Nullable<float>,
        [<Description("Applies to weld. Local orientation quaternion X. Default: 0. Ignored for other constraint types.")>] weld_orient_x: Nullable<float>,
        [<Description("Applies to weld. Local orientation quaternion Y. Default: 0. Ignored for other constraint types.")>] weld_orient_y: Nullable<float>,
        [<Description("Applies to weld. Local orientation quaternion Z. Default: 0. Ignored for other constraint types.")>] weld_orient_z: Nullable<float>,
        [<Description("Applies to weld. Local orientation quaternion W. Default: 1.0. Ignored for other constraint types.")>] weld_orient_w: Nullable<float>
    ) : Task<string> =
        let constraintId = (if id <> null then id else nextId "constraint")
        let offA = Vec3(X = (if offset_ax.HasValue then offset_ax.Value else 0.0), Y = (if offset_ay.HasValue then offset_ay.Value else 0.0), Z = (if offset_az.HasValue then offset_az.Value else 0.0))
        let offB = Vec3(X = (if offset_bx.HasValue then offset_bx.Value else 0.0), Y = (if offset_by.HasValue then offset_by.Value else 0.0), Z = (if offset_bz.HasValue then offset_bz.Value else 0.0))
        let axisA = Vec3(X = (if axis_x.HasValue then axis_x.Value else 0.0), Y = (if axis_y.HasValue then axis_y.Value else 1.0), Z = (if axis_z.HasValue then axis_z.Value else 0.0))
        let axisB = Vec3(X = (if axis_bx.HasValue then axis_bx.Value else 0.0), Y = (if axis_by.HasValue then axis_by.Value else 1.0), Z = (if axis_bz.HasValue then axis_bz.Value else 0.0))
        let spring =
            let hasSpring = spring_frequency.HasValue || spring_damping_ratio.HasValue
            if hasSpring then
                SpringSettings(Frequency = (if spring_frequency.HasValue then spring_frequency.Value else 30.0), DampingRatio = (if spring_damping_ratio.HasValue then spring_damping_ratio.Value else 1.0))
            else
                null
        let constraintTypeMsg = ConstraintType()
        match constraint_type.ToLowerInvariant() with
        | "ball_socket" ->
            constraintTypeMsg.BallSocket <- BallSocketConstraint(LocalOffsetA = offA, LocalOffsetB = offB, Spring = spring)
        | "hinge" ->
            constraintTypeMsg.Hinge <- HingeConstraint(LocalHingeAxisA = axisA, LocalHingeAxisB = axisB, LocalOffsetA = offA, LocalOffsetB = offB, Spring = spring)
        | "weld" ->
            let orient = Vec4(X = (if weld_orient_x.HasValue then weld_orient_x.Value else 0.0), Y = (if weld_orient_y.HasValue then weld_orient_y.Value else 0.0), Z = (if weld_orient_z.HasValue then weld_orient_z.Value else 0.0), W = (if weld_orient_w.HasValue then weld_orient_w.Value else 1.0))
            constraintTypeMsg.Weld <- WeldConstraint(LocalOffset = offA, LocalOrientation = orient, Spring = spring)
        | "distance_limit" ->
            constraintTypeMsg.DistanceLimit <- DistanceLimitConstraint(LocalOffsetA = offA, LocalOffsetB = offB, MinDistance = (if min_distance.HasValue then min_distance.Value else 0.0), MaxDistance = (if max_distance.HasValue then max_distance.Value else 5.0), Spring = spring)
        | "distance_spring" ->
            constraintTypeMsg.DistanceSpring <- DistanceSpringConstraint(LocalOffsetA = offA, LocalOffsetB = offB, TargetDistance = (if target_distance.HasValue then target_distance.Value else 1.0), Spring = spring)
        | "swing_limit" ->
            constraintTypeMsg.SwingLimit <- SwingLimitConstraint(AxisLocalA = axisA, AxisLocalB = axisB, MaxSwingAngle = (if max_swing_angle.HasValue then max_swing_angle.Value else 1.0), Spring = spring)
        | "twist_limit" ->
            constraintTypeMsg.TwistLimit <- TwistLimitConstraint(LocalAxisA = axisA, LocalAxisB = axisB, MinAngle = (if min_angle.HasValue then min_angle.Value else -1.0), MaxAngle = (if max_angle.HasValue then max_angle.Value else 1.0), Spring = spring)
        | "linear_axis_motor" ->
            let motor = MotorConfig(MaxForce = (if motor_max_force.HasValue then motor_max_force.Value else 100.0), Damping = (if motor_damping.HasValue then motor_damping.Value else 1.0))
            constraintTypeMsg.LinearAxisMotor <- LinearAxisMotorConstraint(LocalOffsetA = offA, LocalOffsetB = offB, LocalAxis = axisA, TargetVelocity = (if target_velocity.HasValue then target_velocity.Value else 1.0), Motor = motor)
        | "angular_motor" ->
            let motor = MotorConfig(MaxForce = (if motor_max_force.HasValue then motor_max_force.Value else 100.0), Damping = (if motor_damping.HasValue then motor_damping.Value else 1.0))
            let tv = Vec3(X = (if angular_vel_x.HasValue then angular_vel_x.Value else 0.0), Y = (if angular_vel_y.HasValue then angular_vel_y.Value else 0.0), Z = (if angular_vel_z.HasValue then angular_vel_z.Value else 0.0))
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
    [<McpServerTool; Description("Register a reusable shape by handle name. Shape parameter groups: sphere (radius), box (half_extents_x/y/z), capsule (capsule_radius, capsule_length), cylinder (cylinder_radius, cylinder_length), triangle (tri_ax..tri_cz).")>]
    static member register_shape(
        conn: GrpcConnection,
        [<Description("Unique shape handle name")>] shape_handle: string,
        [<Description("Shape type: 'sphere', 'box', 'capsule', 'cylinder', 'triangle'")>] shape: string,
        [<Description("Required when shape='sphere'. Sphere radius. Default: 0.5. Ignored for other shapes.")>] radius: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent X. Default: 0.5. Ignored for other shapes.")>] half_extents_x: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent Y. Default: 0.5. Ignored for other shapes.")>] half_extents_y: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent Z. Default: 0.5. Ignored for other shapes.")>] half_extents_z: Nullable<float>,
        [<Description("Required when shape='capsule'. Capsule radius. Default: 0.5. Ignored for other shapes.")>] capsule_radius: Nullable<float>,
        [<Description("Required when shape='capsule'. Capsule length. Default: 1.0. Ignored for other shapes.")>] capsule_length: Nullable<float>,
        [<Description("Required when shape='cylinder'. Cylinder radius. Default: 0.5. Ignored for other shapes.")>] cylinder_radius: Nullable<float>,
        [<Description("Required when shape='cylinder'. Cylinder length. Default: 1.0. Ignored for other shapes.")>] cylinder_length: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex A X coordinate. Default: 0.0. Ignored for other shapes.")>] tri_ax: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex A Y coordinate. Default: 0.0. Ignored for other shapes.")>] tri_ay: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex A Z coordinate. Default: 0.0. Ignored for other shapes.")>] tri_az: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex B X coordinate. Default: 1.0. Ignored for other shapes.")>] tri_bx: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex B Y coordinate. Default: 0.0. Ignored for other shapes.")>] tri_by: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex B Z coordinate. Default: 0.0. Ignored for other shapes.")>] tri_bz: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex C X coordinate. Default: 0.0. Ignored for other shapes.")>] tri_cx: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex C Y coordinate. Default: 1.0. Ignored for other shapes.")>] tri_cy: Nullable<float>,
        [<Description("Required when shape='triangle'. Vertex C Z coordinate. Default: 0.0. Ignored for other shapes.")>] tri_cz: Nullable<float>
    ) : Task<string> =
        let shapeMsg = Shape()
        match shape.ToLowerInvariant() with
        | "sphere" ->
            shapeMsg.Sphere <- Sphere(Radius = (if radius.HasValue then radius.Value else 0.5))
        | "box" ->
            shapeMsg.Box <- Box(HalfExtents = Vec3(X = (if half_extents_x.HasValue then half_extents_x.Value else 0.5), Y = (if half_extents_y.HasValue then half_extents_y.Value else 0.5), Z = (if half_extents_z.HasValue then half_extents_z.Value else 0.5)))
        | "capsule" ->
            shapeMsg.Capsule <- Capsule(Radius = (if capsule_radius.HasValue then capsule_radius.Value else 0.5), Length = (if capsule_length.HasValue then capsule_length.Value else 1.0))
        | "cylinder" ->
            shapeMsg.Cylinder <- Cylinder(Radius = (if cylinder_radius.HasValue then cylinder_radius.Value else 0.5), Length = (if cylinder_length.HasValue then cylinder_length.Value else 1.0))
        | "triangle" ->
            let a = Vec3(X = (if tri_ax.HasValue then tri_ax.Value else 0.0), Y = (if tri_ay.HasValue then tri_ay.Value else 0.0), Z = (if tri_az.HasValue then tri_az.Value else 0.0))
            let b = Vec3(X = (if tri_bx.HasValue then tri_bx.Value else 1.0), Y = (if tri_by.HasValue then tri_by.Value else 0.0), Z = (if tri_bz.HasValue then tri_bz.Value else 0.0))
            let c = Vec3(X = (if tri_cx.HasValue then tri_cx.Value else 0.0), Y = (if tri_cy.HasValue then tri_cy.Value else 1.0), Z = (if tri_cz.HasValue then tri_cz.Value else 0.0))
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
        [<Description("Velocity X. Optional. Omit to keep current velocity. All three vx/vy/vz must be set together.")>] vx: Nullable<float>,
        [<Description("Velocity Y. Optional. Omit to keep current velocity. All three vx/vy/vz must be set together.")>] vy: Nullable<float>,
        [<Description("Velocity Z. Optional. Omit to keep current velocity. All three vx/vy/vz must be set together.")>] vz: Nullable<float>
    ) : Task<string> =
        let pose = SetBodyPose(BodyId = body_id, Position = Vec3(X = x, Y = y, Z = z))
        if vx.HasValue && vy.HasValue && vz.HasValue then
            pose.Velocity <- Vec3(X = vx.Value, Y = vy.Value, Z = vz.Value)
        sendCmd conn (SimulationCommand(SetBodyPose = pose))

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
        [<Description("Maximum ray distance. Default: 1000.0.")>] max_distance: Nullable<float>,
        [<Description("Collision mask bitmask filter. Optional. Omit for no filtering.")>] collision_mask: Nullable<int>,
        [<Description("Return all hits instead of just closest. Default: false.")>] all_hits: Nullable<bool>
    ) : Task<string> =
        task {
            try
                let request = RaycastRequest(
                    Origin = Vec3(X = origin_x, Y = origin_y, Z = origin_z),
                    Direction = Vec3(X = direction_x, Y = direction_y, Z = direction_z),
                    MaxDistance = (if max_distance.HasValue then max_distance.Value else 1000.0),
                    AllHits = (if all_hits.HasValue then all_hits.Value else false)
                )
                if collision_mask.HasValue then
                    request.CollisionMask <- uint32 collision_mask.Value
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
    [<McpServerTool; Description("Sweep a shape through the simulation and return the closest hit. Shape parameter groups: sphere (radius), box (half_extents_x/y/z), capsule (capsule_radius, capsule_length), cylinder (capsule_radius, capsule_length).")>]
    static member sweep_cast(
        conn: GrpcConnection,
        [<Description("Shape type: sphere, box, capsule, cylinder")>] shape: string,
        [<Description("Start position X")>] start_x: float,
        [<Description("Start position Y")>] start_y: float,
        [<Description("Start position Z")>] start_z: float,
        [<Description("Sweep direction X")>] direction_x: float,
        [<Description("Sweep direction Y")>] direction_y: float,
        [<Description("Sweep direction Z")>] direction_z: float,
        [<Description("Maximum sweep distance. Default: 1000.0.")>] max_distance: Nullable<float>,
        [<Description("Required when shape='sphere'. Sphere radius. Default: 0.5. Ignored for other shapes.")>] radius: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent X. Default: 0.5. Ignored for other shapes.")>] half_extents_x: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent Y. Default: 0.5. Ignored for other shapes.")>] half_extents_y: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent Z. Default: 0.5. Ignored for other shapes.")>] half_extents_z: Nullable<float>,
        [<Description("Required when shape='capsule' or shape='cylinder'. Radius. Default: 0.25. Ignored for other shapes.")>] capsule_radius: Nullable<float>,
        [<Description("Required when shape='capsule' or shape='cylinder'. Length. Default: 0.5. Ignored for other shapes.")>] capsule_length: Nullable<float>,
        [<Description("Collision mask bitmask filter. Optional. Omit for no filtering.")>] collision_mask: Nullable<int>
    ) : Task<string> =
        task {
            try
                match SimulationTools.buildQueryShape(shape, ?radius = SimulationTools.nopt radius, ?half_extents_x = SimulationTools.nopt half_extents_x, ?half_extents_y = SimulationTools.nopt half_extents_y, ?half_extents_z = SimulationTools.nopt half_extents_z, ?capsule_radius = SimulationTools.nopt capsule_radius, ?capsule_length = SimulationTools.nopt capsule_length) with
                | Error msg -> return msg
                | Ok protoShape ->
                    let request = SweepCastRequest(
                        Shape = protoShape,
                        StartPosition = Vec3(X = start_x, Y = start_y, Z = start_z),
                        Direction = Vec3(X = direction_x, Y = direction_y, Z = direction_z),
                        MaxDistance = (if max_distance.HasValue then max_distance.Value else 1000.0)
                    )
                    if collision_mask.HasValue then
                        request.CollisionMask <- uint32 collision_mask.Value
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
    [<McpServerTool; Description("Test for overlapping bodies at a position using a given shape. Shape parameter groups: sphere (radius), box (half_extents_x/y/z), capsule (capsule_radius, capsule_length), cylinder (capsule_radius, capsule_length).")>]
    static member overlap(
        conn: GrpcConnection,
        [<Description("Shape type: sphere, box, capsule, cylinder")>] shape: string,
        [<Description("Test position X")>] x: float,
        [<Description("Test position Y")>] y: float,
        [<Description("Test position Z")>] z: float,
        [<Description("Required when shape='sphere'. Sphere radius. Default: 0.5. Ignored for other shapes.")>] radius: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent X. Default: 0.5. Ignored for other shapes.")>] half_extents_x: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent Y. Default: 0.5. Ignored for other shapes.")>] half_extents_y: Nullable<float>,
        [<Description("Required when shape='box'. Box half-extent Z. Default: 0.5. Ignored for other shapes.")>] half_extents_z: Nullable<float>,
        [<Description("Required when shape='capsule' or shape='cylinder'. Radius. Default: 0.25. Ignored for other shapes.")>] capsule_radius: Nullable<float>,
        [<Description("Required when shape='capsule' or shape='cylinder'. Length. Default: 0.5. Ignored for other shapes.")>] capsule_length: Nullable<float>,
        [<Description("Collision mask bitmask filter. Optional. Omit for no filtering.")>] collision_mask: Nullable<int>
    ) : Task<string> =
        task {
            try
                match SimulationTools.buildQueryShape(shape, ?radius = SimulationTools.nopt radius, ?half_extents_x = SimulationTools.nopt half_extents_x, ?half_extents_y = SimulationTools.nopt half_extents_y, ?half_extents_z = SimulationTools.nopt half_extents_z, ?capsule_radius = SimulationTools.nopt capsule_radius, ?capsule_length = SimulationTools.nopt capsule_length) with
                | Error msg -> return msg
                | Ok protoShape ->
                    let request = OverlapRequest(
                        Shape = protoShape,
                        Position = Vec3(X = x, Y = y, Z = z)
                    )
                    if collision_mask.HasValue then
                        request.CollisionMask <- uint32 collision_mask.Value
                    let! response = conn.Client.OverlapAsync(request)
                    if response.BodyIds.Count = 0 then
                        return "No overlapping bodies."
                    else
                        let ids = response.BodyIds |> Seq.toList |> String.concat ", "
                        return $"Overlapping bodies ({response.BodyIds.Count}): {ids}"
            with ex ->
                return $"Error: {ex.Message}"
        }
