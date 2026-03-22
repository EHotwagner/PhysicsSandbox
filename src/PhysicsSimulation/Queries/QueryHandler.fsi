module PhysicsSimulation.QueryHandler

open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld

/// Handle a raycast query against the simulation world.
val handleRaycast: World -> RaycastRequest -> RaycastResponse

/// Handle a sweep cast query.
val handleSweepCast: World -> SweepCastRequest -> SweepCastResponse

/// Handle an overlap query.
val handleOverlap: World -> OverlapRequest -> OverlapResponse
