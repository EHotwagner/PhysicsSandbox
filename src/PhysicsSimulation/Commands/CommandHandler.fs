module PhysicsSimulation.CommandHandler

open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld

/// <summary>
/// Dispatches a protobuf SimulationCommand to the appropriate SimulationWorld function.
/// Supports PlayPause, Step, AddBody, RemoveBody, ApplyForce, ApplyImpulse, ApplyTorque,
/// ClearForces, SetGravity, and Reset commands. Unknown commands are treated as forward-compatible no-ops.
/// </summary>
/// <param name="world">The simulation world to apply the command against.</param>
/// <param name="command">The protobuf command to process.</param>
/// <returns>A CommandAck indicating success or failure with a descriptive message.</returns>
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
    | _ ->
        CommandAck(Success = true, Message = "Unknown command (forward-compatible no-op)")
