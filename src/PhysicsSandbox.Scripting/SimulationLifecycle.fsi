/// <summary>High-level simulation lifecycle control: reset, run, and ID generation.</summary>
module PhysicsSandbox.Scripting.SimulationLifecycle

open PhysicsClient.Session

/// <summary>Fully resets the simulation to a clean state with a ground plane and standard gravity.</summary>
/// <param name="s">Active session connected to the physics server.</param>
/// <remarks>
/// Steps: pause → server-side reset (fallback to clearAll) → reset ID counters →
/// add ground plane at Y=0 → set gravity (0, -9.81, 0) → sleep 100ms.
/// Call at the start of every demo or experiment for a predictable starting state.
/// </remarks>
val resetSimulation : Session -> unit

/// <summary>Runs the simulation for the specified duration, then pauses automatically.</summary>
/// <param name="s">Active session connected to the physics server.</param>
/// <param name="seconds">Duration in seconds. Typical: 1–2 quick drops, 3–5 bouncing/rolling,
/// 10+ long scenarios. Simulation runs at 60 Hz, so 3.0s = 180 physics steps.</param>
/// <example>
/// <code>
/// runFor session 3.0    // run for 3 seconds then pause
/// </code>
/// </example>
val runFor : Session -> float -> unit

/// <summary>Generates the next sequential ID for a body with the given shape prefix.</summary>
/// <param name="prefix">Shape prefix like <c>"sphere"</c>, <c>"box"</c>, <c>"crate"</c>, <c>"wall"</c>.
/// Each prefix has an independent counter. Counters reset on <c>resetSimulation</c>.</param>
/// <returns>Human-readable ID like <c>"sphere-1"</c>, <c>"box-3"</c>.</returns>
/// <example>
/// <code>
/// let id1 = nextId "sphere"   // "sphere-1"
/// let id2 = nextId "sphere"   // "sphere-2"
/// let id3 = nextId "box"      // "box-1"
/// </code>
/// </example>
val nextId : string -> string
