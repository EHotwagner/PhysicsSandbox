// PhysicsClient — FSI convenience script
// Load this in F# Interactive: dotnet fsi src/PhysicsClient/PhysicsClient.fsx
//
// Prerequisites: local NuGet packages published (see nuget.config)

#r "nuget: PhysicsClient"
#r "nuget: Spectre.Console"

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Presets
open PhysicsClient.Generators
open PhysicsClient.Steering
open PhysicsClient.StateDisplay
open PhysicsClient.LiveWatch

printfn "PhysicsClient loaded. Use Session.connect \"http://localhost:5180\" to begin."
