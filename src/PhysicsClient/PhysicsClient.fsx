// PhysicsClient — FSI convenience script
// Load this in F# Interactive: dotnet fsi src/PhysicsClient/PhysicsClient.fsx
//
// Prerequisites: dotnet build PhysicsSandbox.slnx

#r "bin/Debug/net10.0/PhysicsClient.dll"
#r "../../src/PhysicsSandbox.Shared.Contracts/bin/Debug/net10.0/PhysicsSandbox.Shared.Contracts.dll"
#r "nuget: Grpc.Net.Client"
#r "nuget: Google.Protobuf"
#r "nuget: Spectre.Console"

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Presets
open PhysicsClient.Generators
open PhysicsClient.Steering
open PhysicsClient.StateDisplay
open PhysicsClient.LiveWatch

printfn "PhysicsClient loaded. Use Session.connect \"http://localhost:5000\" to begin."
