module PhysicsSimulation.CommandHandler

open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld
open PhysicsSimulation.QueryHandler

/// Dispatches a protobuf SimulationCommand to the appropriate SimulationWorld function.
let handle (world: World) (command: SimulationCommand) =
    match command.CommandCase with
    | SimulationCommand.CommandOneofCase.PlayPause ->
        setRunning world command.PlayPause.Running
        let state = if command.PlayPause.Running then "playing" else "paused"
        CommandAck(Success = true, Message = $"Simulation {state}")
    | SimulationCommand.CommandOneofCase.Step ->
        let _state = step world
        CommandAck(Success = true, Message = "Step completed")
    | SimulationCommand.CommandOneofCase.AddBody ->
        addBody world command.AddBody
    | SimulationCommand.CommandOneofCase.RemoveBody ->
        removeBody world command.RemoveBody.BodyId
    | SimulationCommand.CommandOneofCase.ApplyForce ->
        applyForce world command.ApplyForce.BodyId command.ApplyForce.Force
    | SimulationCommand.CommandOneofCase.ApplyImpulse ->
        applyImpulse world command.ApplyImpulse.BodyId command.ApplyImpulse.Impulse
    | SimulationCommand.CommandOneofCase.ApplyTorque ->
        applyTorque world command.ApplyTorque.BodyId command.ApplyTorque.Torque
    | SimulationCommand.CommandOneofCase.ClearForces ->
        clearForces world command.ClearForces.BodyId
    | SimulationCommand.CommandOneofCase.SetGravity ->
        setGravity world command.SetGravity.Gravity
        CommandAck(Success = true, Message = "Gravity updated")
    | SimulationCommand.CommandOneofCase.Reset ->
        resetSimulation world
    | SimulationCommand.CommandOneofCase.AddConstraint ->
        addConstraint world command.AddConstraint
    | SimulationCommand.CommandOneofCase.RemoveConstraint ->
        removeConstraint world command.RemoveConstraint.ConstraintId
    | SimulationCommand.CommandOneofCase.RegisterShape ->
        registerShape world command.RegisterShape
    | SimulationCommand.CommandOneofCase.UnregisterShape ->
        unregisterShape world command.UnregisterShape.ShapeHandle
    | SimulationCommand.CommandOneofCase.SetCollisionFilter ->
        setCollisionFilter world command.SetCollisionFilter
    | SimulationCommand.CommandOneofCase.SetBodyPose ->
        setBodyPose world command.SetBodyPose
    | SimulationCommand.CommandOneofCase.QueryRequest ->
        let req = command.QueryRequest
        let response = QueryResponse(CorrelationId = req.CorrelationId)
        match req.QueryCase with
        | QueryRequest.QueryOneofCase.Raycast ->
            response.Raycast <- handleRaycast world req.Raycast
        | QueryRequest.QueryOneofCase.SweepCast ->
            response.SweepCast <- handleSweepCast world req.SweepCast
        | QueryRequest.QueryOneofCase.Overlap ->
            response.Overlap <- handleOverlap world req.Overlap
        | _ -> ()
        addQueryResponse world response
        CommandAck(Success = true, Message = "Query processed")
    | _ ->
        CommandAck(Success = true, Message = "Unknown command (forward-compatible no-op)")
