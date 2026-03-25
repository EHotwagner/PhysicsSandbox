module PhysicsSimulation.ShapeConversion

open System.Numerics
open PhysicsSandbox.Shared.Contracts
open BepuFSharp

/// Convert proto MaterialProperties to BepuFSharp MaterialProperties.
val toBepuMaterial : ProtoConversions.ProtoMaterialProperties -> BepuFSharp.MaterialProperties

/// Convert proto ConstraintType to BepuFSharp ConstraintDesc. Returns None for unknown types.
val convertConstraintType : ConstraintType -> ConstraintDesc option

/// Convert proto Shape to BepuFSharp PhysicsShape.
/// Takes the PhysicsWorld (for compound child shape registration) and registered shapes map
/// (for ShapeRef resolution) as explicit dependencies.
val convertShape :
    physicsWorld: PhysicsWorld ->
    registeredShapes: Map<string, ShapeId * Shape> ->
    shape: Shape ->
    Result<PhysicsShape * Shape, string>
