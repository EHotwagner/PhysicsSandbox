// Shared preamble for scripting library scripts
// Single reference — all dependencies resolve from the same output directory

#r "../src/PhysicsSandbox.Scripting/bin/Debug/net10.0/PhysicsSandbox.Scripting.dll"

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsSandbox.Scripting.Prelude
