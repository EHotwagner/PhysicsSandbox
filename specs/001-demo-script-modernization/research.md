# Research: 001-demo-script-modernization

## Decision 1: Batch API Approach for Demo Scripts

**Decision**: Construct `SimulationCommand` proto objects manually in Prelude helpers, then pass lists to `batchCommands`.

**Rationale**: Presets (`marble`, `bowlingBall`, etc.) and Generators (`stack`, `pyramid`, etc.) make direct gRPC calls internally and return body IDs — they don't return `SimulationCommand` objects. To batch body creation, demos must construct commands at the proto level. The Prelude provides `makeSphereCmd`/`makeBoxCmd` helpers to make this ergonomic.

**Alternatives considered**:
- Modifying Presets/Generators to optionally return commands instead of executing: rejected because it would change compiled library APIs and `.fsi` signatures for a scripting-only benefit.
- Using MCP batch tools (JSON-based): rejected because demo scripts use the F# client library directly, not MCP.

## Decision 2: Vec3 Construction in Scripts

**Decision**: Define a local `toVec3` helper in Prelude.fsx that constructs `Vec3` proto objects.

**Rationale**: `SimulationCommands.toVec3` is declared `val internal` in the `.fsi` file, making it inaccessible from `.fsx` scripts. A 1-line local helper is trivial and avoids modifying the library's API surface.

**Alternatives considered**:
- Making `toVec3` public in the `.fsi`: rejected because it exposes proto internals in the public API, would require surface-area baseline updates, and is a disproportionate change for a scripting convenience.

## Decision 3: Reset Strategy

**Decision**: Replace `resetScene` with `resetSimulation` that calls `SimulationCommands.reset` (server-side `ResetSimulation` proto command) followed by `addPlane` and `setGravity`.

**Rationale**: Server-side reset atomically clears all bodies, forces, and resets simulation time to zero. The current `resetScene` uses `clearAll` which only removes bodies but doesn't reset time. Post-reset, ground plane and gravity must be re-established since reset clears everything.

**Alternatives considered**:
- Keeping `resetScene` as-is: rejected because it doesn't reset simulation time and uses 5 separate commands with timing sensitivity.
- Using `reset` alone without re-adding plane: rejected because reset clears static bodies too.

## Decision 4: Generator-Using Demos

**Decision**: Demos that use generators (Demo03, 04, 05, parts of 08 and 10) keep generator calls as-is — no batching change for those calls.

**Rationale**: Generators are in compiled PhysicsClient.dll and make their own sequential gRPC calls internally. Rewriting generator logic in scripts would duplicate code and diverge from the library. The generators already handle error accumulation and ID collection.

**Alternatives considered**:
- Replicating generator logic with batch commands in scripts: rejected as code duplication with no meaningful benefit for demos in the 8-20 body range.
