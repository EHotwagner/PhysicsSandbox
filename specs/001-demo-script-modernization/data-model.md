# Data Model: 001-demo-script-modernization

No new data entities are introduced. This feature modifies script files only and uses existing proto message types.

## Existing Entities Used

### SimulationCommand (proto)
Discriminated union containing one of: AddBody, ApplyForce, ApplyImpulse, ApplyTorque, SetGravity, StepSimulation, PlayPause, RemoveBody, ClearForces, ResetSimulation.

### AddBody (proto)
Fields: Id (string), Position (Vec3), Shape (Shape oneof Sphere/Box/Plane), Mass (double).

### BatchSimulationRequest (proto)
Fields: repeated SimulationCommand commands.

### BatchResponse (proto)
Fields: repeated CommandResult results, total_time_ms (double).

### CommandResult (proto)
Fields: success (bool), message (string), index (int32).

## New Script-Level Helpers (not compiled entities)

| Helper | Input | Output | Purpose |
|--------|-------|--------|---------|
| makeSphereCmd | id, pos, radius, mass | SimulationCommand | Build sphere AddBody without sending |
| makeBoxCmd | id, pos, halfExtents, mass | SimulationCommand | Build box AddBody without sending |
| makeImpulseCmd | bodyId, impulse | SimulationCommand | Build ApplyImpulse without sending |
| makeTorqueCmd | bodyId, torque | SimulationCommand | Build ApplyTorque without sending |
| batchAdd | session, commands | unit | Send batch with auto-split at 100 |
