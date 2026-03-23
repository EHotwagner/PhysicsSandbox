# PhysicsSandbox — Main Specification

**Last Updated**: 2026-03-23
**Revision**: Updated with 005-mcp-data-logging archival

## Overview

PhysicsSandbox is an Aspire-orchestrated distributed application for real-time physics simulation. Four services communicate through a central server hub via gRPC. Each service is added incrementally as a feature spec.

## User Stories

### US-001: Solution Foundation and Orchestrator (P1)
A developer runs the Aspire AppHost to launch the orchestration dashboard, confirming the foundation is operational. [Source: specs/001-server-hub]

### US-002: Shared Communication Contracts (P1)
A developer defines shared gRPC message contracts (SimulationCommand, ViewCommand, SimulationState) in a contracts project so all services agree on message structure. [Source: specs/001-server-hub]

### US-003: Server Hub with Message Routing (P1)
A developer creates the PhysicsServer — the central hub that accepts commands from clients, receives simulation state, caches latest state for late joiners, and fans out updates to all subscribers. Only one simulation source at a time. [Source: specs/001-server-hub]

### US-004: Shared Service Defaults (P2)
A developer sets up standardized health checks, OpenTelemetry, service discovery, and resilience patterns via a shared ServiceDefaults project. [Source: specs/001-server-hub]

### US-005: Simulation Lifecycle Control (P1)
An operator starts the simulation service, connects to the server hub, and controls it via play, pause, and single-step commands. State is streamed after every step. [Source: specs/002-physics-simulation]

### US-006: Body Management (P1)
An operator adds/removes rigid bodies (sphere, box, plane) with unique IDs, position, velocity, mass, and shape. Bodies appear in the streamed state. [Source: specs/002-physics-simulation]

### US-007: Force, Torque, and Impulse Application (P2)
An operator applies persistent forces, one-shot impulses, and torques to bodies by ID. Forces accumulate until cleared. Non-existent body targets are no-ops. [Source: specs/002-physics-simulation]

### US-008: Gravity Configuration (P2)
An operator sets and changes the global gravity vector at any time, affecting all bodies each step. [Source: specs/002-physics-simulation]

### US-009: Continuous State Streaming (P1)
Every simulation step streams the complete world state (all body poses, velocities, angular velocities, time, running flag) to the server for downstream fan-out. [Source: specs/002-physics-simulation]

### US-010: View Live Simulation (P1)
A user launches the 3D viewer alongside the running simulation. The viewer connects to the server, subscribes to the state stream, and renders all physics bodies as colored 3D shapes (spheres blue, boxes orange) at correct positions and orientations. Updates in real time as simulation advances. [Source: specs/003-3d-viewer]

### US-011: Camera Control via Commands and Input (P2)
A user controls the camera via interactive mouse/keyboard input (click-drag orbit, scroll zoom, middle-click pan) and via precise REPL commands (SetCamera, SetZoom) forwarded through the server. REPL commands override the current camera state when received. [Source: specs/003-3d-viewer]

### US-012: Wireframe Toggle (P3)
A user toggles wireframe rendering mode from the REPL client. When enabled, all bodies are drawn as wireframe outlines instead of solid shapes. [Source: specs/003-3d-viewer]

### US-013: Simulation Status Display (P3)
The viewer displays simulation metadata — current time and running/paused status — as a text overlay. [Source: specs/003-3d-viewer]

### US-014: Connect and Control Simulation from REPL (P1)
A user loads the client library in FSI, connects to the server, and uses functions to add bodies, apply forces, and control playback (play/pause/step). All commands return Result-based acknowledgements. [Source: specs/004-client-repl]

### US-015: Ready-Made Body Builders (P2)
A user populates scenes quickly using pre-configured presets (marble, bowling ball, crate, etc.), random generators, and scene builders (stack, row, grid, pyramid). Auto-generated human-readable IDs with optional overrides. [Source: specs/004-client-repl]

### US-016: Body Steering and Motion Control (P2)
A user steers bodies with intent-based functions — push in a direction, launch toward a target, spin, stop — without calculating physics vectors manually. [Source: specs/004-client-repl]

### US-017: State Display and Monitoring (P3)
A user queries simulation state via formatted Spectre.Console tables, inspects individual bodies, and runs a cancellable live-watch mode with filtering (by body ID, shape, velocity threshold). Staleness timestamps shown when state is >5s old. [Source: specs/004-client-repl]

### US-018: Viewer Control from REPL (P3)
A user controls the 3D viewer's camera position, zoom, and wireframe mode from the REPL via dedicated functions. [Source: specs/004-client-repl]

### US-019: MCP-Based Physics Exploration (P1)
A developer in an AI-assisted environment (e.g., Claude Code) interacts with the running PhysicsSandbox system through MCP tool calls — sending simulation commands, view commands, reading state, and checking connection status — without writing gRPC client code. [Source: specs/005-mcp-server-testing]

### US-020: Fix Known Connection Issues (P1)
A developer expects the simulation to maintain a stable gRPC connection with SSL dev certificate bypass, and the viewer to receive the DISPLAY environment variable from Aspire. Simulation auto-reconnects with exponential backoff (1s → 10s max) on stream failure, preserving world state. [Source: specs/005-mcp-server-testing]

### US-021: Comprehensive Regression Test Suite (P2)
A developer runs the integration test suite exercising all gRPC RPCs end-to-end through the Aspire stack — command routing, state streaming, simulation lifecycle, error conditions, and concurrent subscribers — in a headless environment. [Source: specs/005-mcp-server-testing]

### US-022: MCP Server Configuration and Discovery (P3)
A developer adds the PhysicsSandbox MCP server to their AI assistant's configuration, launching it as a standalone stdio process connected to the PhysicsServer's gRPC endpoint. [Source: specs/005-mcp-server-testing]

### US-023: MCP Server Starts with Aspire (P1)
A developer runs the Aspire AppHost and the MCP server starts automatically alongside all other services, appearing in the Aspire dashboard with Running state. No manual launch required. [Source: specs/006-mcp-aspire-orchestration]

### US-024: MCP Server Connects via Service Discovery (P1)
A developer expects the MCP server to automatically discover and connect to the PhysicsServer through Aspire service references rather than a hardcoded address. Dynamic port assignment is handled transparently. [Source: specs/006-mcp-aspire-orchestration]

### US-025: MCP Server Logs in Aspire Dashboard (P2)
A developer debugging physics simulation issues through an AI assistant can view the MCP server's logs in the Aspire dashboard's structured logging view, correlated with other service activity. [Source: specs/006-mcp-aspire-orchestration]

### US-026: Persistent MCP Connection (P1)
An AI assistant connects to the MCP server via HTTP/SSE. The MCP server stays running permanently as part of the Aspire AppHost, regardless of whether any AI assistant is connected. Assistants disconnect and reconnect freely without the server shutting down. [Source: specs/001-mcp-persistent-service]

### US-027: Full Message Visibility (P1)
The MCP server receives all messages flowing through the PhysicsServer — simulation state updates, view commands, and a live audit feed of every command sent by any client. The AI assistant can observe the complete system state including raw commands (type and parameters). [Source: specs/001-mcp-persistent-service]

### US-028: Full Command Capability (P1)
The AI assistant can send any simulation command and view command the system supports — all 12 command types (9 simulation + 3 view) through the MCP server, matching the REPL client's command surface. [Source: specs/001-mcp-persistent-service]

### US-029: MCP Convenience Functions and Presets (P2)
The AI assistant has access to high-level convenience tools — 7 body presets, 5 scene generators, and 4 steering helpers — simplifying common operations without manual low-level parameter specification. [Source: specs/001-mcp-persistent-service]

### US-030: Full Command Coverage for State Testing (P2)
The AI assistant can send all 12 command types available through the server's client-facing interface (PhysicsHub only, no SimulationLink), enabling it to create any reachable application state by issuing the right sequence of commands. [Source: specs/001-mcp-persistent-service]

### US-031: Viewer FPS Display & Logging (P1)
A developer sees real-time FPS as an overlay in the viewer window (updated every frame) and FPS samples logged every 10 seconds. A warning is emitted when FPS drops below a configurable threshold (default 30). [Source: specs/002-performance-diagnostics]

### US-032: Service Message & Traffic Metrics (P1)
Each service (PhysicsServer, PhysicsSimulation, PhysicsViewer, MCP) tracks and periodically logs message counts and data volumes (bytes sent/received). Current counters are queryable on-demand via a dedicated MCP tool. [Source: specs/002-performance-diagnostics]

### US-033: Batch Commands for Simulation & UI (P1)
A developer or AI assistant submits multiple simulation or view commands in a single batch request at both the gRPC and MCP levels. Batch responses include per-command results. A batch of 50 commands completes at least 2x faster than sending them individually. [Source: specs/002-performance-diagnostics]

### US-034: Restart Simulation Command (P2)
A developer or AI assistant resets the simulation to an empty state (all bodies removed, physics time reset, running set to false) via a single command — without restarting any services. Performance metrics persist across restarts. [Source: specs/002-performance-diagnostics]

### US-035: Static Body Collision (P2)
All static bodies (planes, static boxes) participate in collision detection with dynamic bodies, acting as solid surfaces that block dynamic body movement. [Source: specs/002-performance-diagnostics]

### US-036: Performance Diagnostics & Bottleneck Detection (P2)
A developer requests a pipeline diagnostics report showing time breakdown across simulation tick, serialization, gRPC transfer, and rendering stages. The slowest stage is highlighted. Accessible via structured logs and on-demand MCP tools. [Source: specs/002-performance-diagnostics]

### US-037: Stress Testing (P2)
A developer initiates predefined stress test scenarios (body count scaling, command throughput) via MCP tools. Tests run as background jobs, returning immediately with a test ID. A separate MCP tool polls progress and retrieves summary reports with peak metrics, degradation points, and failures. [Source: specs/002-performance-diagnostics]

### US-038: MCP vs Scripting Performance Comparison (P3)
A developer runs identical test scenarios via both MCP and direct gRPC scripting, comparing timing results to quantify MCP overhead in time and message count. Includes batched vs unbatched comparison. [Source: specs/002-performance-diagnostics]

### US-039: Batch Command Support in Demo Scripts (P1)
A developer running demo scripts gets faster scene setup because body-creation commands are grouped into batches instead of sent individually. Demos placing 3+ bodies use batch commands via Prelude helpers. [Source: specs/001-demo-script-modernization]

### US-040: Server-Side Simulation Reset in Demos (P1)
A developer running the demo suite gets clean simulation state between demos via server-side reset (clears bodies, forces, resets time) instead of manual multi-step teardown. [Source: specs/001-demo-script-modernization]

### US-041: Updated Scripting Helpers in Shared Prelude (P2)
A developer writing demo scripts uses Prelude helpers (makeSphereCmd, makeBoxCmd, makeImpulseCmd, makeTorqueCmd, batchAdd, resetSimulation, nextId, toVec3) for batching and reset without additional boilerplate. [Source: specs/001-demo-script-modernization]

### US-042: AutoRun Script Consistency (P2)
A developer runs the AutoRun script and gets consistent results — all demo code is duplicated inline (no external dependencies), helpers match Prelude, and banner shows correct demo count. [Source: specs/001-demo-script-modernization]

### US-043: Progressive Body Scaling Stress Test (P1)
A developer runs a demo that progressively creates 50, 100, 200, and 500 bodies in tiers to discover where simulation performance degrades, with timing markers at each tier. [Source: specs/003-stress-test-demos]

### US-044: Collision Density Stress Test (P1)
A developer runs a demo that drops 120 spheres into a walled pit to maximize simultaneous collisions and observe settling behavior under dense collision load. [Source: specs/003-stress-test-demos]

### US-045: Bulk Force Application Stress Test (P2)
A developer runs a demo that applies 3 rounds of escalating impulses, torques, and gravity changes to 100 bodies simultaneously to stress the force-application pipeline. [Source: specs/003-stress-test-demos]

### US-046: Combined Stress Scenario (P2)
A developer runs an "overload" demo combining 200+ bodies, formations, impulse storms, gravity chaos, and camera sweeps with per-stage timing to find the overall system ceiling. [Source: specs/003-stress-test-demos]

### US-047: MCP Stress Test Invocation (P3)
An AI assistant replicates stress demos through MCP tools (batch_commands, start_stress_test, get_diagnostics) to validate MCP handles high-volume operations comparably to direct scripting. [Source: specs/003-stress-test-demos]
A developer or CI system running AutoRun.fsx gets the same modern batching and reset behavior as individual demo scripts and RunAll. All 10 demos pass in all execution modes. [Source: specs/001-demo-script-modernization]

### US-048: Run Full Python Demo Suite Automatically (P1)
A developer runs `python -m demos_py.auto_run` against a running Aspire stack and sees all 15 Python demos execute sequentially with a pass/fail summary. Equivalent to the F# AutoRun.fsx but using Python with direct gRPC communication. [Source: specs/004-python-demo-scripts]

### US-049: Run Individual Python Demo Script (P2)
A developer runs a single Python demo script (e.g., `python -m demos_py.demo01_hello_drop`) to explore or test a specific physics scenario independently, with optional server address argument. [Source: specs/004-python-demo-scripts]

### US-050: Interactive Python Demo Runner (P3)
A developer uses `python -m demos_py.run_all` to step through demos one at a time with keypress advancement, observing each scenario in the 3D viewer before advancing. [Source: specs/004-python-demo-scripts]

### US-051: Use Library Functions in Scripts (P1)
A script author references the scripting library DLL in an .fsx file and immediately has access to all Prelude convenience functions (resetSimulation, makeSphereCmd, makeBoxCmd, makeImpulseCmd, makeTorqueCmd, batchAdd, nextId, toVec3, ok, sleep, runFor, timed) without additional gRPC or Protobuf package directives. [Source: specs/004-fsharp-scripting-library]

### US-052: Experiment in Scratch Folder (P2)
A developer creates .fsx scripts in the `scratch/` folder for rapid prototyping. Successful experiments are promoted to `scripts/` without code changes — both folders use identical library reference paths. Scratch is gitignored; scripts is tracked. [Source: specs/004-fsharp-scripting-library]

### US-053: MCP Server Uses Scripting Library (P2)
The MCP server references the scripting library as a project dependency, reusing shared helper functions (e.g., toVec3) instead of duplicating them in ClientAdapter. [Source: specs/004-fsharp-scripting-library]

### US-054: Extend Library with New Functions (P3)
A developer adds a new function to the scripting library by modifying at most 2 files per module (implementation + signature), with no changes to existing consumers. Optionally, 2 additional Prelude files are updated for script convenience re-export. [Source: specs/004-fsharp-scripting-library]

### US-055: Pack and Publish Libraries as NuGet Packages (P1)
A developer packs PhysicsSandbox.Shared.Contracts, PhysicsSandbox.ServiceDefaults, PhysicsClient, and PhysicsSandbox.Scripting into local NuGet packages in dependency order, following the BepuFSharp pattern. [Source: specs/004-scripting-nuget-package]

### US-056: Migrate Consumer Projects to PackageReferences (P2)
Projects consuming PhysicsSandbox.Scripting (MCP server, test project) switch from ProjectReference to PackageReference. The solution builds and all tests pass identically. [Source: specs/004-scripting-nuget-package]

### US-057: Version-Agnostic Script NuGet References (P3)
All F# scripts and demos use `#r "nuget: PackageName"` (without version specifier) to automatically resolve the newest package from the local feed. [Source: specs/004-scripting-nuget-package]

### US-058: Canonical Port Consistency (P4)
All scripts, demos, and documentation use canonical server ports (5180 HTTP, 7180 HTTPS). Legacy `localhost:5000` references are eliminated. [Source: specs/004-scripting-nuget-package]

### US-059: Satisfying Demo Experience (P1)
A user runs any individual demo and sees a physically rich simulation that clearly demonstrates the demo's named concept — not a minimal smoke test but a satisfying showcase. Demos 01-05 collaboratively reviewed, 06-15 accepted based on implementation following spec improvement directions. [Source: specs/004-improve-demos]

### US-060: Complete Demo Suite Integration (P2)
All 15 demos run through the AllDemos runner (interactive or automated) without any demos being excluded. AutoRun.fsx reuses AllDemos definitions (no code duplication). Demos 11-15 use sensible defaults without command-line args. [Source: specs/004-improve-demos]

### US-061: F# and Python Demo Suite Parity (P3)
All demo improvements are mirrored in both F# (.fsx) and Python (.py) suites. Both produce equivalent physics scenarios for all 15 demos. [Source: specs/004-improve-demos]

### US-062: Extended Shape Support (P1)
A developer creates physics simulations using the full range of body shapes: capsules, cylinders, triangles, convex hulls, compound shapes, and meshes, in addition to existing sphere/box/plane. The viewer renders each shape type with a visually distinct 3D model. [Source: specs/005-stride-bepu-integration]

### US-063: Physics Debug Visualization (P2)
A developer toggles a debug wireframe overlay in the viewer showing collider outlines and constraint connections for all physics bodies, aiding collision debugging and joint configuration inspection. [Source: specs/005-stride-bepu-integration]

### US-064: Constraints and Joints (P3)
A developer connects bodies with 10 constraint types (hinge, ball-socket, distance limit/spring, weld, swing/twist limits, linear/angular motors, point-on-line). Constraints auto-remove when referenced bodies are deleted. [Source: specs/005-stride-bepu-integration]

### US-065: Material Properties (P4)
A developer specifies per-body material properties (friction, bounciness, damping) that produce visibly different collision behaviors. [Source: specs/005-stride-bepu-integration]

### US-066: Physics Queries (P5)
A developer queries the physics world via dedicated RPCs: raycasts (single/all hits), sweep casts, and overlap tests, each with a batch variant. Results include hit body, contact point, normal, and distance. [Source: specs/005-stride-bepu-integration]

### US-067: Collision Layers and Filtering (P6)
A developer assigns bodies to collision layers (32 via bitmask) so non-interacting groups pass through each other. Filtering applies to both physics and queries. [Source: specs/005-stride-bepu-integration]

### US-068: Kinematic Bodies (P7)
A developer creates kinematic bodies that move by explicit position/velocity commands, push dynamic bodies on contact, and are unaffected by gravity. [Source: specs/005-stride-bepu-integration]

### US-069: Per-Body Color (P1)
A developer specifies RGBA color per body at creation. Bodies without specified color get auto-assigned defaults by shape type. The viewer renders each body in its assigned color including transparency. [Source: specs/005-stride-bepu-integration]

### US-070: Correct Shape Sizing in Viewer (P1)
A user observes physics bodies in the 3D viewer and they render at their exact physics dimensions — no visual overlap where physics bodies are separated. Spheres, capsules, and cylinders display at correct radius. [Source: specs/005-viewer-settings-sizing-fix]

### US-071: Accurate Debug Wireframes (P1)
A user toggles F3 debug view and wireframes match physics collision bounds exactly (no artificial 1.02x scaling). Compound shapes render per-child wireframes. Convex hull/mesh shapes use correctly-dimensioned bounding boxes. [Source: specs/005-viewer-settings-sizing-fix]

### US-072: Borderless Windowed Fullscreen (P2)
A user presses F11 to toggle borderless windowed fullscreen. Escape returns to windowed mode. Fullscreen state persists across restarts. Alt-tab transitions are seamless. [Source: specs/005-viewer-settings-sizing-fix]

### US-073: Display Resolution Selection (P2)
A user presses F2 to open a settings overlay, selects a resolution (1280x720, 1920x1080, 2560x1440), and the viewport resizes immediately without restart. Selection persists across restarts. [Source: specs/005-viewer-settings-sizing-fix]

### US-074: Quality Settings (P3)
A user adjusts anti-aliasing (Off/X2/X4/X8), shadow quality (Off/Low/Medium/High), texture filtering (Point/Linear/Anisotropic), and VSync via the F2 settings overlay. MSAA and shadow changes apply immediately. Settings persist across restarts. [Source: specs/005-viewer-settings-sizing-fix]

### US-075: Fix Broken Impact Demos (P1)
A user runs the Crate Stack and Bowling Alley demos and projectiles strike targets centrally with dramatic visible destruction — tower topples, pyramid scatters. [Source: specs/005-enhance-demos]

### US-076: Showcase Constraint Physics (P1)
A user runs Demo 16 and sees 4 constraint types in action: pendulum chain (ball-socket + distance-limit), hinged bridge (hinge), and weld cluster (weld). Bodies move in physically plausible linked motion. [Source: specs/005-enhance-demos]

### US-077: Showcase Advanced Shape Types (P1)
A user runs the demo suite and sees 8 of 10 shape types used across demos: sphere, box, capsule, cylinder, triangle, convex hull, compound, plane — each in contextually meaningful roles. [Source: specs/005-enhance-demos]

### US-078: Showcase Physics Queries (P2)
A user runs Demo 17 and sees raycast, overlap, and sweep cast queries with printed results showing hit body IDs, positions, and distances. [Source: specs/005-enhance-demos]

### US-079: Expand Colors and Materials Across Demos (P2)
A user runs the demo suite and 12+ demos use custom per-body colors for visual distinction, with 4 demos using bouncy/sticky/slippery material presets for visible behavioral contrast. [Source: specs/005-enhance-demos]

### US-080: Demonstrate Kinematic Bodies (P3)
A user runs Demo 18 and sees a kinematic bulldozer move through a grid of dynamic bodies, pushing them aside on a scripted path. [Source: specs/005-enhance-demos]

### US-081: Continuous Physics Data Recording (P1)
An AI assistant using MCP tools wants to capture all incoming simulation data (state updates and command events) to persistent disk storage for deferred analysis. Recording starts automatically when the MCP server connects to the simulation. [Source: specs/005-mcp-data-logging]

### US-082: Dual-Limit Storage Management (P1)
A user wants recording sessions to automatically manage storage using both a time-based retention window (default 10 minutes) and a size-based cap (default 500 MB), with automatic pruning of oldest data in 1-minute chunks. [Source: specs/005-mcp-data-logging]

### US-083: Querying Recorded Data via MCP Tools (P1)
An AI assistant wants to query recorded physics data — body trajectories, state snapshots, command events — with paginated results, enabling deferred analysis of simulation behavior. [Source: specs/005-mcp-data-logging]

### US-084: Recording Session Management (P2)
A user wants to manage multiple recording sessions — starting, stopping, listing, and deleting — to organize analysis work across simulation experiments. [Source: specs/005-mcp-data-logging]

### US-089: Record Mesh Fetch Activity (P1)
The recording captures all FetchMeshes RPC activity — requested mesh IDs, hit/miss counts, missed IDs — enabling post-hoc analysis of mesh resolution behavior and late-joiner debugging. [Source: specs/004-mcp-mesh-logging]

### US-090: Query Mesh Fetch History via MCP Tools (P2)
MCP query tools retrieve mesh fetch events from recorded sessions with time-range and mesh ID filtering, supporting paginated results for analysis workflows. [Source: specs/004-mcp-mesh-logging]

### US-085: Bandwidth-Efficient State Streaming (P1)
Running complex physics simulations with many mesh-based bodies, the system avoids resending unchanged mesh geometry every tick. State updates use lightweight identifiers instead of full vertex data for previously-transmitted complex shapes (ConvexHull, MeshShape, Compound), reducing bandwidth by 80%+. [Source: specs/004-mesh-cache-transport]

### US-086: Server-Side Mesh Cache and Distribution (P1)
Late-joining viewers or clients can request mesh geometry they don't have cached locally via a FetchMeshes RPC. The server maintains an authoritative cache of all mesh geometries keyed by content-addressed identifier. [Source: specs/004-mesh-cache-transport]

### US-087: Bounding Box Placeholder While Mesh Loads (P2)
The viewer displays correctly-sized bounding box placeholders for bodies whose mesh geometry hasn't been resolved yet, replacing them with full shape representations once geometry arrives via on-demand fetch. [Source: specs/004-mesh-cache-transport]

### US-088: Separate Mesh Channel (P2)
Mesh geometry exchange happens on a dedicated FetchMeshes RPC separate from the main state stream, so mesh fetching does not block or degrade the 60 Hz state updates. [Source: specs/004-mesh-cache-transport]

## Functional Requirements

- **FR-001**: Solution structure with Aspire AppHost, shared contracts, service defaults, and server hub. [Source: specs/001-server-hub]
- **FR-002**: Contracts define `PhysicsHub` service (SendCommand, SendViewCommand, StreamState) and `SimulationLink` service (ConnectSimulation bidirectional stream). [Source: specs/001-server-hub]
- **FR-003**: `SimulationCommand` with variants: AddBody, ApplyForce, SetGravity, StepSimulation, PlayPause, RemoveBody, ApplyImpulse, ApplyTorque, ClearForces. [Source: specs/001-server-hub, specs/002-physics-simulation]
- **FR-004**: `ViewCommand` with variants: SetCamera, ToggleWireframe, SetZoom. [Source: specs/001-server-hub]
- **FR-005**: `SimulationState` with bodies (id, position, velocity, angular_velocity, mass, shape, orientation), time, running flag. [Source: specs/001-server-hub, specs/002-physics-simulation]
- **FR-006**: Server hub accepts simulation commands and forwards to simulation. [Source: specs/001-server-hub]
- **FR-007**: Server hub accepts view commands and forwards to viewer. [Source: specs/001-server-hub]
- **FR-008**: Server hub fans out simulation state to all subscribers. [Source: specs/001-server-hub]
- **FR-009**: Graceful handling when downstream services not connected (ack without error, drop commands). [Source: specs/001-server-hub]
- **FR-010**: AppHost registers server hub as first service. [Source: specs/001-server-hub]
- **FR-011**: ServiceDefaults provides health endpoints, OpenTelemetry, service discovery, resilience. [Source: specs/001-server-hub]
- **FR-012**: Server hub references ServiceDefaults for health and observability. [Source: specs/001-server-hub]
- **FR-013**: Server hub caches latest state and delivers it immediately to late-joining subscribers. [Source: specs/001-server-hub]
- **FR-014**: Single simulation source enforcement — reject second connection with ALREADY_EXISTS. [Source: specs/001-server-hub]
- **FR-015**: Simulation service connects to server hub on startup via SimulationLink protocol. [Source: specs/002-physics-simulation]
- **FR-016**: Simulation starts paused by default; supports play, pause, single-step lifecycle commands. [Source: specs/002-physics-simulation]
- **FR-017**: Simulation advances at 60Hz fixed time step when playing, streams state after each step. [Source: specs/002-physics-simulation]
- **FR-018**: Simulation supports adding dynamic bodies (sphere, box) with position, velocity, mass, shape; assigns unique ID. Plane shapes create statics. [Source: specs/002-physics-simulation]
- **FR-019**: Simulation supports removing bodies by identifier (idempotent). [Source: specs/002-physics-simulation]
- **FR-020**: Simulation supports persistent forces (accumulated per-body, applied each step until cleared). [Source: specs/002-physics-simulation]
- **FR-021**: Simulation supports one-shot impulses (immediate velocity change, not stored). [Source: specs/002-physics-simulation]
- **FR-022**: Simulation supports torques (rotational force on specific body). [Source: specs/002-physics-simulation]
- **FR-023**: Simulation supports clear-forces command (removes all persistent forces on a body). [Source: specs/002-physics-simulation]
- **FR-024**: Simulation supports global gravity vector, changeable at runtime, applied as mass*gravity force each step. [Source: specs/002-physics-simulation]
- **FR-025**: Streamed state includes each body's position, velocity, angular velocity, mass, shape, identifier, orientation, and is_static flag. Static bodies now included with `is_static = true`. [Source: specs/002-physics-simulation, specs/002-performance-diagnostics]
- **FR-026**: Streamed state includes simulation time and running/paused flag. [Source: specs/002-physics-simulation]
- **FR-027**: Commands targeting non-existent body IDs handled gracefully (success ack, no-op). [Source: specs/002-physics-simulation]
- **FR-028**: Simulation handles server disconnection by logging and shutting down cleanly (no reconnect). [Source: specs/002-physics-simulation]
- **FR-029**: Simulation rejects bodies with zero or negative mass. [Source: specs/002-physics-simulation]
- **FR-030**: Simulation registered in Aspire AppHost with WithReference(server).WaitFor(server). [Source: specs/002-physics-simulation]
- **FR-031**: Proto contracts extended with RemoveBody, ApplyImpulse, ApplyTorque, ClearForces commands and Body angular_velocity/orientation fields. Backward compatible. [Source: specs/002-physics-simulation]
- **FR-032**: Viewer connects to server and subscribes to simulation state stream on startup. [Source: specs/003-3d-viewer]
- **FR-033**: Viewer renders each body as a 3D shape (sphere, box) with shape-type-based colors (spheres blue, boxes orange, unknown red). [Source: specs/003-3d-viewer]
- **FR-034**: Viewer positions and orients bodies from proto Vec3 position and Vec4 quaternion orientation. [Source: specs/003-3d-viewer]
- **FR-035**: Viewer updates the rendered scene each time a new simulation state is received. [Source: specs/003-3d-viewer]
- **FR-036**: Viewer applies SetCamera commands by repositioning camera to specified position, target, and up vector. [Source: specs/003-3d-viewer]
- **FR-037**: Viewer applies SetZoom commands by scaling camera distance from target. [Source: specs/003-3d-viewer]
- **FR-038**: Viewer applies ToggleWireframe commands by switching between solid and flat materials. Entity recreation on toggle. [Source: specs/003-3d-viewer]
- **FR-039**: Viewer displays simulation time and running/paused indicator as a DebugText overlay. [Source: specs/003-3d-viewer]
- **FR-040**: Viewer provides default camera position (10,8,10) looking at origin on startup. [Source: specs/003-3d-viewer]
- **FR-041**: Viewer handles late-join gracefully — renders first state received without errors. [Source: specs/003-3d-viewer]
- **FR-042**: Viewer supports interactive mouse/keyboard camera: left-drag orbit, scroll zoom, middle-drag pan. [Source: specs/003-3d-viewer]
- **FR-043**: REPL camera commands override interactive camera state (applied after interactive input each frame). [Source: specs/003-3d-viewer]
- **FR-044**: Viewer displays ground reference grid at Y=0 via Add3DGround + AddGroundGizmo. [Source: specs/003-3d-viewer]
- **FR-045**: Viewer registered in Aspire AppHost with WithReference(server).WaitFor(server). [Source: specs/003-3d-viewer]
- **FR-046**: Viewer uses AddServiceDefaults via background host for OpenTelemetry and structured logging. [Source: specs/003-3d-viewer]
- **FR-047**: Proto extended with StreamViewCommands RPC on PhysicsHub service; server extended with readViewCommand and StreamViewCommands override. [Source: specs/003-3d-viewer]
- **FR-048**: Client library provides connect function returning opaque Session handle; disconnect and explicit reconnect (no auto-reconnect). [Source: specs/004-client-repl]
- **FR-049**: Client library wraps all 9 proto simulation commands (addSphere/addBox/addPlane, removeBody, applyForce/Impulse/Torque, clearForces, setGravity, play, pause, step) as Result-returning functions. [Source: specs/004-client-repl]
- **FR-050**: Client library provides clear-all function that removes all session-tracked bodies via individual RemoveBody commands. [Source: specs/004-client-repl]
- **FR-051**: Client library provides 7 body presets (marble, bowlingBall, beachBall, crate, brick, boulder, die) with optional position, mass, and ID overrides. Auto-generated human-readable IDs ("sphere-1", "box-3"). [Source: specs/004-client-repl]
- **FR-052**: Client library provides randomized body generators (randomSpheres, randomBoxes, randomBodies) with seedable RNG and configurable bounds. [Source: specs/004-client-repl]
- **FR-053**: Client library provides scene-builder functions: stack, row, grid, pyramid. [Source: specs/004-client-repl]
- **FR-054**: Client library provides steering functions: push (named direction + magnitude), pushVec (raw vector), launch (toward target), spin (torque axis), stop (clear forces + counter-impulse). [Source: specs/004-client-repl]
- **FR-055**: Client library provides state query functions: listBodies (Spectre.Console table), inspect (body panel), status (simulation panel), snapshot (raw state). [Source: specs/004-client-repl]
- **FR-056**: Client library provides cancellable live-watch mode with filtering by body ID, shape type, and velocity threshold. Uses Spectre.Console Live context. [Source: specs/004-client-repl]
- **FR-057**: Client library provides viewer control functions: setCamera, setZoom, wireframe. [Source: specs/004-client-repl]
- **FR-058**: Client library loadable in F# Interactive (FSI) via #r directive. Convenience .fsx script provided. [Source: specs/004-client-repl]
- **FR-059**: All client library functions return Result<'T, string> — no unhandled exceptions. RpcException mapped to Error strings. [Source: specs/004-client-repl]
- **FR-060**: Client library registered in Aspire AppHost with WithReference(server).WaitFor(server). [Source: specs/004-client-repl]
- **FR-061**: MCP server exposes ~15 fine-grained tools (one per operation): add_body, apply_force, apply_impulse, apply_torque, set_gravity, step, play, pause, remove_body, clear_forces, set_camera, set_zoom, toggle_wireframe, get_state, get_status. [Source: specs/005-mcp-server-testing]
- **FR-062**: MCP get_state tool returns cached simulation state from background StreamState subscription with staleness timestamp. Instant response (no per-call stream). [Source: specs/005-mcp-server-testing]
- **FR-063**: Each MCP tool accepts structured parameters matching gRPC schemas and returns human-readable results ("Success/Failed/Error" format). [Source: specs/005-mcp-server-testing]
- **FR-064**: ~~MCP server communicates via stdio transport.~~ Superseded by FR-080: MCP server now uses HTTP/SSE transport via ModelContextProtocol.AspNetCore. [Source: specs/005-mcp-server-testing → specs/001-mcp-persistent-service]
- **FR-065**: MCP server handles gRPC connection failures gracefully with descriptive error messages through the MCP protocol. [Source: specs/005-mcp-server-testing]
- **FR-066**: Simulation service maintains stable gRPC connection over HTTPS with dev certificate bypass and auto-reconnection using exponential backoff (1s → 10s max) on stream failure. World state preserved across reconnections. [Source: specs/005-mcp-server-testing]
- **FR-067**: Viewer service receives DISPLAY environment variable from Aspire orchestrator (fallback `:0`). [Source: specs/005-mcp-server-testing]
- **FR-068**: Integration tests cover all PhysicsHub RPCs end-to-end through Aspire stack (32 tests across 5 classes). [Source: specs/005-mcp-server-testing]
- **FR-069**: Integration tests verify simulation connection, command delivery, and state streaming with real physics data (gravity, forces, impulses, torques produce position/velocity changes). [Source: specs/005-mcp-server-testing]
- **FR-070**: Integration tests cover error conditions: commands without simulation, empty commands, rapid command stress (200 commands). [Source: specs/005-mcp-server-testing]
- **FR-071**: Integration tests verify concurrent state stream subscribers receive consistent data and late joiners receive cached state. [Source: specs/005-mcp-server-testing]
- **FR-072**: All integration tests run without GPU, display server, or manual setup (headless-compatible). [Source: specs/005-mcp-server-testing]
- **FR-073**: AppHost registers MCP server as a project resource in the Aspire orchestration. [Source: specs/006-mcp-aspire-orchestration]
- **FR-074**: MCP server resource has a service reference to the PhysicsServer for runtime address resolution. [Source: specs/006-mcp-aspire-orchestration]
- **FR-075**: MCP server waits for the PhysicsServer to be ready before starting. [Source: specs/006-mcp-aspire-orchestration]
- **FR-076**: MCP server uses Aspire service-discovered PhysicsServer address (env vars `services__server__https__0` / `services__server__http__0`) instead of hardcoded default. CLI arg override and standalone fallback preserved. [Source: specs/006-mcp-aspire-orchestration]
- **FR-077**: MCP server appears in the Aspire dashboard with its resource name, state, and logs. [Source: specs/006-mcp-aspire-orchestration]
- **FR-078**: MCP server shuts down gracefully when the AppHost is stopped. [Source: specs/006-mcp-aspire-orchestration]
- **FR-079**: Existing MCP server functionality (15 tools, stdio transport, gRPC connection management) remains unchanged after orchestration integration. [Source: specs/006-mcp-aspire-orchestration]
- **FR-080**: MCP server MUST use HTTP/SSE network transport (via ModelContextProtocol.AspNetCore) instead of stdio, persisting independently of client connections. Supersedes FR-064. [Source: specs/001-mcp-persistent-service]
- **FR-081**: MCP server MUST accept multiple concurrent client connections, all sharing a single underlying gRPC connection, state cache, and body ID counter. [Source: specs/001-mcp-persistent-service]
- **FR-082**: MCP server MUST receive all simulation state updates, all view commands, and a live audit feed of every command via the new StreamCommands RPC. Three background streams with independent exponential backoff reconnection. [Source: specs/001-mcp-persistent-service]
- **FR-083**: Proto contract extended with `CommandEvent` message (oneof wrapping SimulationCommand/ViewCommand) and `StreamCommands` RPC on PhysicsHub. Additive, no breaking changes. [Source: specs/001-mcp-persistent-service]
- **FR-084**: PhysicsServer MessageRouter extended with `CommandSubscribers` for audit stream fan-out. `submitCommand` and `submitViewCommand` publish to audit subscribers. [Source: specs/001-mcp-persistent-service]
- **FR-085**: MCP server exposes 7 body preset tools (marble, bowling ball, beach ball, crate, brick, boulder, die) with configurable position, mass, and ID. [Source: specs/001-mcp-persistent-service]
- **FR-086**: MCP server exposes 5 scene generator tools (random bodies, stack, row, grid, pyramid) with configurable parameters. [Source: specs/001-mcp-persistent-service]
- **FR-087**: MCP server exposes 4 steering tools (push in direction, launch to target, spin around axis, stop body). [Source: specs/001-mcp-persistent-service]
- **FR-088**: MCP server exposes 1 audit tool (get_command_log) returning recent commands from a bounded circular buffer (100 entries). [Source: specs/001-mcp-persistent-service]
- **FR-089**: MCP server can send all 12 command types via PhysicsHub only (no SimulationLink access). [Source: specs/001-mcp-persistent-service]
- **FR-090**: MCP server reports connection status for all three streams (state, view, audit) via get_status tool. [Source: specs/001-mcp-persistent-service]
- **FR-091**: MCP server uses ServiceDefaults for health checks and structured logging. [Source: specs/001-mcp-persistent-service]
- **FR-092**: Viewer MUST display current FPS as an on-screen overlay, updated at least once per second (exponential moving average, α=0.1). [Source: specs/002-performance-diagnostics]
- **FR-093**: Viewer MUST log FPS samples every 10 seconds with timestamps to the structured logging system. [Source: specs/002-performance-diagnostics]
- **FR-094**: Viewer MUST emit a warning log when smoothed FPS falls below a configurable threshold (default 30 FPS). [Source: specs/002-performance-diagnostics]
- **FR-095**: Each service MUST track and log message counts (sent/received) per reporting interval using thread-safe Interlocked counters. Counters exposed via on-demand `get_metrics` MCP tool. [Source: specs/002-performance-diagnostics]
- **FR-096**: Each service MUST track and log data volume (bytes sent/received) per reporting interval. Byte counts estimated from proto message `CalculateSize()`. [Source: specs/002-performance-diagnostics]
- **FR-097**: System MUST support batch submission of multiple simulation commands in a single request via `SendBatchCommand` gRPC RPC and `batch_commands` MCP tool. Commands execute in order with per-command results. [Source: specs/002-performance-diagnostics]
- **FR-098**: System MUST support batch submission of multiple view commands in a single request via `SendBatchViewCommand` gRPC RPC and `batch_view_commands` MCP tool. [Source: specs/002-performance-diagnostics]
- **FR-099**: Batch responses MUST include per-command `CommandResult` (success, message, index). Invalid commands return errors without blocking valid commands. Max 100 commands per batch. [Source: specs/002-performance-diagnostics]
- **FR-100**: System MUST provide a `ResetSimulation` command (variant in SimulationCommand oneof) that clears all bodies, resets simulation time to 0, and sets running to false. No service restarts or reconnections required. [Source: specs/002-performance-diagnostics]
- **FR-101**: Performance metrics and diagnostics counters MUST persist across simulation restarts. [Source: specs/002-performance-diagnostics]
- **FR-102**: All static bodies MUST participate in collision detection with dynamic bodies. Static bodies tracked in `world.Bodies` with `IsStatic = true` and included in `SimulationState` with `is_static` proto field. [Source: specs/002-performance-diagnostics]
- **FR-103**: System MUST provide pipeline diagnostics showing time breakdown across simulation tick (`Stopwatch`), serialization (`Stopwatch`), gRPC transfer (send-to-receive delta), and rendering stages. Accessible via `get_diagnostics` MCP tool and periodic structured logs. [Source: specs/002-performance-diagnostics]
- **FR-104**: System MUST provide predefined stress test scenarios (body-scaling, command-throughput) invocable via `start_stress_test` MCP tool. Tests run as background tasks returning immediately with test ID. Progress/results queryable via `get_stress_test_status` MCP tool. Only one stress test may run at a time. [Source: specs/002-performance-diagnostics]
- **FR-105**: Stress test summary reports MUST include: peak body count, degradation body count, peak command rate, average/min FPS, total/failed commands, and error messages. Logged to structured logs and returned via MCP tool. [Source: specs/002-performance-diagnostics]
- **FR-106**: System MUST support running identical scenarios via MCP and direct gRPC scripting (PhysicsClient library) for performance comparison (`start_comparison_test` MCP tool). [Source: specs/002-performance-diagnostics]
- **FR-107**: Comparison results MUST quantify MCP overhead in time (ms), message count, and overhead percentage. Includes batched MCP path comparison. [Source: specs/002-performance-diagnostics]
- **FR-108**: Demo scripts placing 3+ bodies MUST use batch commands (via Prelude `batchAdd` helper) to group body-creation into fewer round-trips. [Source: specs/001-demo-script-modernization]
- **FR-109**: Shared Prelude module MUST provide `resetSimulation` helper using server-side `reset` command followed by ground plane re-establishment and gravity restore. [Source: specs/001-demo-script-modernization]
- **FR-110**: Shared Prelude module MUST provide command builder functions (`makeSphereCmd`, `makeBoxCmd`, `makeImpulseCmd`, `makeTorqueCmd`) and `batchAdd` helper with auto-split at 100 commands. [Source: specs/001-demo-script-modernization]
- **FR-111**: All 10 demo scripts, AllDemos.fsx, AutoRun.fsx, and RunAll.fsx MUST use `resetSimulation` instead of manual multi-step `resetScene`. [Source: specs/001-demo-script-modernization]
- **FR-112**: AutoRun.fsx MUST duplicate all Prelude helpers inline (self-contained) and mirror all demo changes. [Source: specs/001-demo-script-modernization]
- **FR-113**: Batch and reset error handling MUST produce clear, actionable error messages (print failed command indices, reset fallback to manual clear). [Source: specs/001-demo-script-modernization]
- **FR-114**: Demo suite MUST include at least 5 stress test demos beyond the original 10 demos. [Source: specs/003-stress-test-demos]
- **FR-115**: Each stress demo MUST follow existing conventions: use Prelude.fsx helpers, resetSimulation at start, batchAdd for bulk operations. [Source: specs/003-stress-test-demos]
- **FR-116**: At least one demo MUST progressively scale body count in tiers (50, 100, 200, 500) and report timing per tier. [Source: specs/003-stress-test-demos]
- **FR-117**: At least one demo MUST focus on collision density by creating 100+ bodies in a confined walled space. [Source: specs/003-stress-test-demos]
- **FR-118**: At least one demo MUST apply bulk forces (impulses, torques, gravity changes) to 100+ bodies simultaneously. [Source: specs/003-stress-test-demos]
- **FR-119**: At least one demo MUST combine multiple stress axes (body count + collisions + forces + camera movement) in a single scenario. [Source: specs/003-stress-test-demos]
- **FR-120**: Each stress demo MUST print `[TIME]` timing markers via the `timed` helper so operators can identify degradation. [Source: specs/003-stress-test-demos]
- **FR-121**: All new demos MUST be integrated into AllDemos.fsx, RunAll.fsx, and AutoRun.fsx. [Source: specs/003-stress-test-demos]
- **FR-122**: Demos MUST handle failures gracefully — batch failures reported via `[BATCH FAIL]`, reset failures fall back to manual clear. [Source: specs/003-stress-test-demos]
- **FR-123**: Prelude.fsx MUST provide a `timed` helper that wraps actions with Stopwatch and prints `[TIME] label: N ms`. [Source: specs/003-stress-test-demos]
- **FR-124**: System MUST provide 15 Python demo scripts covering the same scenarios as the F# demos (Hello Drop through Overload). [Source: specs/004-python-demo-scripts]
- **FR-125**: Python demos MUST communicate with the PhysicsServer via gRPC using Python-generated protobuf stubs from the existing `physics_hub.proto` contract. [Source: specs/004-python-demo-scripts]
- **FR-126**: System MUST provide a shared Python prelude module (`prelude.py`) with helpers equivalent to the F# Prelude: session management, simulation/view commands, message builders, batch helpers, presets, generators, steering, display, ID generation. [Source: specs/004-python-demo-scripts]
- **FR-127**: System MUST provide an automated Python runner (`auto_run.py`) that executes all 15 demos sequentially with pass/fail reporting. [Source: specs/004-python-demo-scripts]
- **FR-128**: System MUST provide an interactive Python runner (`run_all.py`) with keypress advancement. [Source: specs/004-python-demo-scripts]
- **FR-129**: Each Python demo MUST be self-contained: reset the scene, configure camera, create bodies, run simulation, display results. [Source: specs/004-python-demo-scripts]
- **FR-130**: Each Python demo MUST accept an optional server address argument, defaulting to `http://localhost:5180`. [Source: specs/004-python-demo-scripts]
- **FR-131**: Python demos MUST be runnable via `python` without requiring .NET tooling — only Python and pip dependencies. [Source: specs/004-python-demo-scripts]
- **FR-132**: Python `batch_add` helper MUST automatically split command lists exceeding 100 items into multiple batches. [Source: specs/004-python-demo-scripts]
- **FR-133**: System MUST provide a proto stub generation script (`generate_stubs.sh`) that generates Python stubs from the existing `physics_hub.proto`. [Source: specs/004-python-demo-scripts]
- **FR-134**: System MUST provide a compiled F# library (`PhysicsSandbox.Scripting`) bundling all Prelude.fsx convenience functions as reusable modules. [Source: specs/004-fsharp-scripting-library]
- **FR-135**: The scripting library MUST be referenceable from .fsx scripts via a single `#r` directive to the compiled DLL. [Source: specs/004-fsharp-scripting-library]
- **FR-136**: The scripting library MUST be addable as a standard project reference by other solution projects (including PhysicsSandbox.Mcp). [Source: specs/004-fsharp-scripting-library]
- **FR-137**: Project MUST include a gitignored `scratch/` folder at repo root for experimental scripts, excluded from CI. [Source: specs/004-fsharp-scripting-library]
- **FR-138**: Project MUST include a git-tracked `scripts/` folder at repo root for curated scripts. [Source: specs/004-fsharp-scripting-library]
- **FR-139**: The scripting library MUST be organized into logical modules (Helpers, Vec3Builders, CommandBuilders, BatchOperations, SimulationLifecycle, Prelude). [Source: specs/004-fsharp-scripting-library]
- **FR-140**: The scripting library MUST include .fsi signature files for all public modules. [Source: specs/004-fsharp-scripting-library]
- **FR-141**: The scripting library MUST re-export dependencies so scripts don't need separate gRPC/Protobuf package directives. [Source: specs/004-fsharp-scripting-library]
- **FR-142**: Moving a script from `scratch/` to `scripts/` MUST NOT require code changes — both use identical library reference paths. [Source: specs/004-fsharp-scripting-library]
- **FR-143**: The scripting library MUST be part of the solution file and buildable with standard `dotnet build`. [Source: specs/004-fsharp-scripting-library]
- **FR-144**: PhysicsClient, PhysicsSandbox.Shared.Contracts, PhysicsSandbox.ServiceDefaults, and PhysicsSandbox.Scripting MUST all be packable into .nupkg files with defined package identities and versions. [Source: specs/004-scripting-nuget-package]
- **FR-145**: All packages MUST be publishable to the existing local NuGet feed at `~/.local/share/nuget-local/`. [Source: specs/004-scripting-nuget-package]
- **FR-146**: All in-solution ProjectReferences to PhysicsSandbox.Scripting MUST be replaced with PackageReferences in consumer projects (MCP, tests). [Source: specs/004-scripting-nuget-package]
- **FR-147**: All F# script and demo `#r` directives MUST use version-agnostic NuGet references (e.g., `#r "nuget: PhysicsSandbox.Scripting"`) to automatically resolve the newest package. [Source: specs/004-scripting-nuget-package]
- **FR-148**: PhysicsSandbox.Scripting MUST declare PhysicsClient as a NuGet package dependency (PackageReference, not ProjectReference). [Source: specs/004-scripting-nuget-package]
- **FR-149**: The packaging workflow MUST follow BepuFSharp conventions (local feed path, version scheme, `-p:NoWarn=NU5104` pack flag). [Source: specs/004-scripting-nuget-package]
- **FR-150**: Each new package publish MUST use an incremented version number to prevent stale cached packages. [Source: specs/004-scripting-nuget-package]
- **FR-151**: All server port references across scripts and documentation MUST use canonical ports: 5180 for HTTP, 7180 for HTTPS. [Source: specs/004-scripting-nuget-package]
- **FR-152**: Each demo MUST produce physically correct, interesting interactions as verified by simulation state output. Visual rendering accuracy is a viewer concern outside demo scope. [Source: specs/004-improve-demos]
- **FR-153**: Each demo MUST clearly demonstrate its named physics concept (e.g., "Spinning Tops" shows rotational collision dynamics, not just static spinning). [Source: specs/004-improve-demos]
- **FR-154**: Demos 11-15 MUST be integrated into AllDemos.fsx and all_demos.py using the same function-record pattern as demos 1-10. [Source: specs/004-improve-demos]
- **FR-155**: AutoRun.fsx MUST reuse AllDemos definitions instead of duplicating helper and demo code. [Source: specs/004-improve-demos]
- **FR-156**: Each demo improvement MUST be applied to both F# and Python versions. [Source: specs/004-improve-demos]
- **FR-157**: Individual demo runtime MUST remain under 30 seconds. Body counts MUST stay within 500 per demo. [Source: specs/004-improve-demos]
- **FR-158**: Demos MUST use existing Prelude capabilities — no new server-side features required. [Source: specs/004-improve-demos]
- **FR-159**: F# Prelude.fsx MUST provide a `runStandalone` helper for direct demo execution via `dotnet fsi DemoNN.fsx`. [Source: specs/004-improve-demos]
- **FR-160**: System MUST support capsule, cylinder, triangle, convex hull, compound, and mesh shapes (extending from 3 to 10 shape types). [Source: specs/005-stride-bepu-integration]
- **FR-161**: Shape registration mechanism: register once with unique handle, reference by ID in AddBody. Cache cleared on reset. [Source: specs/005-stride-bepu-integration]
- **FR-162**: Viewer MUST render each shape type with geometry matching physics collider dimensions (bounding box for complex shapes). [Source: specs/005-stride-bepu-integration]
- **FR-163**: Debug wireframe overlay mode showing collider outlines and constraint connections, togglable at runtime (F3). [Source: specs/005-stride-bepu-integration]
- **FR-164**: 10 constraint types: hinge, ball-socket, distance limit, distance spring, weld, swing limit, twist limit, linear motor, angular motor, point-on-line. [Source: specs/005-stride-bepu-integration]
- **FR-165**: Auto-remove constraints when referenced body is removed. Remove individual constraints by ID. [Source: specs/005-stride-bepu-integration]
- **FR-166**: Per-body material properties (friction, max_recovery_velocity, spring_frequency, spring_damping_ratio). [Source: specs/005-stride-bepu-integration]
- **FR-167**: Dedicated RPCs for raycast (single/all hits), sweep cast, overlap queries — each with batch variant. [Source: specs/005-stride-bepu-integration]
- **FR-168**: Queries respect collision mask filtering. [Source: specs/005-stride-bepu-integration]
- **FR-169**: 32 collision layers via uint32 group/mask bitmask. SetCollisionFilter for runtime updates. [Source: specs/005-stride-bepu-integration]
- **FR-170**: Kinematic bodies unaffected by gravity, collide with and displace dynamic bodies. [Source: specs/005-stride-bepu-integration]
- **FR-171**: SetBodyPose command for runtime position/orientation/velocity updates on kinematic and dynamic bodies. [Source: specs/005-stride-bepu-integration]
- **FR-172**: Per-body RGBA color at creation. Default color palette by shape type. Color in state stream. [Source: specs/005-stride-bepu-integration]
- **FR-173**: BepuFSharp 0.2.0-beta.1 wrapper with constraints, materials, queries, collision filters, Stride interop. [Source: specs/005-stride-bepu-integration]
- **FR-174**: Viewer renders all physics body shapes at exact physics dimensions — shapeSize passes radius (not diameter) for sphere/capsule/cylinder to Stride's Create3DPrimitive. [Source: specs/005-viewer-settings-sizing-fix]
- **FR-175**: Debug wireframe visualization accurately represents physics collision boundaries for all 10 shape types with no artificial scaling offset. [Source: specs/005-viewer-settings-sizing-fix]
- **FR-176**: Viewer provides borderless windowed fullscreen toggle via F11 keyboard shortcut. Escape exits fullscreen. [Source: specs/005-viewer-settings-sizing-fix]
- **FR-177**: Viewer allows resolution selection from supported resolutions (1280x720, 1920x1080, 2560x1440) via on-screen settings overlay (F2). [Source: specs/005-viewer-settings-sizing-fix]
- **FR-178**: Viewer provides basic quality settings: anti-aliasing (MSAA Off/X2/X4/X8), shadow quality (Off/Low/Medium/High via cascade count), texture filtering (Point/Linear/Anisotropic), VSync. [Source: specs/005-viewer-settings-sizing-fix]
- **FR-179**: Display and quality settings persist across viewer restarts via JSON at ~/.config/PhysicsSandbox/viewer-settings.json. [Source: specs/005-viewer-settings-sizing-fix]
- **FR-180**: All settings changes (resolution, fullscreen, quality) apply immediately without requiring a viewer restart. [Source: specs/005-viewer-settings-sizing-fix]
- **FR-181**: Compound shape debug wireframes render each child shape individually at correct local position/orientation within the compound. [Source: specs/005-viewer-settings-sizing-fix]
- **FR-182**: Near-zero shape dimensions clamped to 0.01f minimum to prevent invisible bodies. [Source: specs/005-viewer-settings-sizing-fix]
- **FR-183**: Demo 03 (Crate Stack) launches projectile along same axis as tower, striking centrally with sufficient impulse to topple majority. [Source: specs/005-enhance-demos]
- **FR-184**: Demo 04 (Bowling Alley) launches projectile from directly in front of pyramid (frontal Z-axis approach) with sufficient impulse to scatter majority. [Source: specs/005-enhance-demos]
- **FR-185**: Demo suite includes Demo 16 demonstrating 4 constraint types: ball-socket, distance-limit, hinge, weld. [Source: specs/005-enhance-demos]
- **FR-186**: Demo suite includes compound shapes (multi-child rigid bodies) in Demos 11 and 12. [Source: specs/005-enhance-demos]
- **FR-187**: Demo suite features capsule, cylinder, convex hull, triangle, and compound shapes distributed across multiple demos (not a single showcase). [Source: specs/005-enhance-demos]
- **FR-188**: Demo 17 performs raycast, sweep cast, and overlap queries with printed results. [Source: specs/005-enhance-demos]
- **FR-189**: At least 12 demos use custom per-body colors via 8-color palette (projectile, target, structure, yellow, green, purple, orange, kinematic). [Source: specs/005-enhance-demos]
- **FR-190**: At least 4 demos use material presets (bouncy, sticky, slippery) with visible behavioral contrast. [Source: specs/005-enhance-demos]
- **FR-191**: Demo 18 uses kinematic body (bulldozer) animated via setBodyPose in a loop, pushing dynamic bodies. [Source: specs/005-enhance-demos]
- **FR-192**: All 18 F# demos pass in AutoRun.fsx with 0 failures. [Source: specs/005-enhance-demos]
- **FR-193**: Prelude.fsx extended with: makeTriangleCmd, makeConvexHullCmd, makeCompoundCmd, makeKinematicCmd, withMotionType, withCollisionFilter, setPose, queryRaycast, queryOverlapSphere, querySweepSphere, and 8 color palette constants. [Source: specs/005-enhance-demos]
- **FR-194**: PhysicsClient NuGet package repacked to version 0.2.0 with raycast, sweepCast, overlap, setBodyPose APIs. [Source: specs/005-enhance-demos]
- **FR-195**: MCP server MUST capture every simulation state update at full fidelity (no sampling) to persistent on-disk log during active recording. [Source: specs/005-mcp-data-logging]
- **FR-196**: MCP server MUST capture all command events (simulation and view) into the recording log alongside state updates. [Source: specs/005-mcp-data-logging]
- **FR-197**: Each recorded entry MUST have a high-resolution timestamp (millisecond precision) for frame-level event correlation. [Source: specs/005-mcp-data-logging]
- **FR-198**: Recording sessions MUST enforce dual retention limits: configurable time window (default 10 minutes) and configurable size cap (default 500 MB). [Source: specs/005-mcp-data-logging]
- **FR-199**: System MUST automatically prune oldest recorded data in 1-minute chunks when either retention limit is reached. [Source: specs/005-mcp-data-logging]
- **FR-200**: MCP tools MUST be provided to start, stop, list, and delete recording sessions. [Source: specs/005-mcp-data-logging]
- **FR-201**: MCP tool MUST query body trajectory (position, velocity, rotation over time) from recorded data with pagination. [Source: specs/005-mcp-data-logging]
- **FR-202**: MCP tool MUST query state snapshots within a time range from recorded data with pagination. [Source: specs/005-mcp-data-logging]
- **FR-203**: MCP tool MUST query command events by type and/or time range from recorded data with pagination. [Source: specs/005-mcp-data-logging]
- **FR-204**: MCP tool MUST retrieve a recording session summary (time range, counts, storage) without scanning chunk files. [Source: specs/005-mcp-data-logging]
- **FR-205**: Recording MUST NOT degrade existing MCP tool responsiveness — uses async Channel-based pipeline with non-blocking writes. [Source: specs/005-mcp-data-logging]
- **FR-206**: Recording data MUST survive MCP server restarts. Interrupted sessions marked as Completed on recovery. [Source: specs/005-mcp-data-logging]
- **FR-207**: Only one recording session may be active at a time. Starting a new session stops the previous one. [Source: specs/005-mcp-data-logging]
- **FR-208**: System MUST report current storage usage and configured limits via recording_status tool. [Source: specs/005-mcp-data-logging]
- **FR-209**: All query tools MUST support cursor-based pagination with configurable page size (default 100, max 500). [Source: specs/005-mcp-data-logging]
- **FR-210**: Recording MUST auto-start when MCP server first receives a simulation state update, without explicit user command. [Source: specs/005-mcp-data-logging]
- **FR-211**: System MUST assign a unique, stable content-addressed identifier (SHA-256 truncated to 128 bits, 32 hex chars) to each distinct mesh geometry (ConvexHull, MeshShape, Compound). Single Triangle shapes are primitives. [Source: specs/004-mesh-cache-transport]
- **FR-212**: State update messages MUST use CachedShapeRef (mesh_id + bbox) instead of inline geometry for complex shapes that have been previously transmitted. [Source: specs/004-mesh-cache-transport]
- **FR-213**: System MUST include full mesh geometry in SimulationState.new_meshes on the first tick a shape identifier appears. [Source: specs/004-mesh-cache-transport]
- **FR-214**: Server MUST maintain a ConcurrentDictionary mesh cache of all mesh geometries keyed by identifier, serving on-demand FetchMeshes requests. [Source: specs/004-mesh-cache-transport]
- **FR-215**: Subscribers MUST be able to request mesh geometry by identifier via the FetchMeshes unary RPC at any time. [Source: specs/004-mesh-cache-transport]
- **FR-216**: Subscribers (viewer, client, MCP) MUST maintain a local ConcurrentDictionary cache of received mesh geometries. [Source: specs/004-mesh-cache-transport]
- **FR-217**: Mesh geometry exchange MUST occur on a FetchMeshes RPC channel separate from the StreamState server-streaming RPC. [Source: specs/004-mesh-cache-transport]
- **FR-218**: Viewer MUST display a bounding box placeholder (Cube primitive, semi-transparent magenta) for bodies whose mesh geometry has not yet been resolved locally. [Source: specs/004-mesh-cache-transport]
- **FR-219**: Viewer MUST replace bounding box placeholders with actual shape representations once geometry is received, by recreating the entity. [Source: specs/004-mesh-cache-transport]
- **FR-220**: System MUST invalidate all mesh caches when the simulation is reset or the simulation disconnects. Individual body removal does not evict mesh entries. [Source: specs/004-mesh-cache-transport]
- **FR-221**: Compound shapes are cached as atomic units — a single content-addressed identifier for the entire compound (all children + local poses). Identical compounds produce the same identifier. [Source: specs/004-mesh-cache-transport]
- **FR-222**: Bodies sharing identical geometry MUST reuse the same mesh identifier and cached geometry. [Source: specs/004-mesh-cache-transport]
- **FR-223**: Primitive shapes (Sphere, Box, Capsule, Cylinder, Plane, Triangle) MUST continue to be transported inline without identifiers. [Source: specs/004-mesh-cache-transport]
- **FR-224**: System MUST precompute axis-aligned bounding box extents when a mesh geometry is first seen, stored in BodyRecord. [Source: specs/004-mesh-cache-transport]
- **FR-225**: CachedShapeRef messages MUST include precomputed bbox_min and bbox_max alongside mesh_id. [Source: specs/004-mesh-cache-transport]
- **FR-226**: MCP recording MUST persist MeshDefinition log entries alongside state snapshots for self-contained session replay. [Source: specs/004-mesh-cache-transport]
- **FR-227**: System MUST record a MeshFetchEvent log entry each time the server handles a FetchMeshes RPC, capturing timestamp, requested mesh IDs, hits, misses, and missed IDs. [Source: specs/004-mcp-mesh-logging]
- **FR-228**: MeshFetchEvent entries MUST use the established binary wire format (EntryType=3) and be written to the same chunk files as other entry types. [Source: specs/004-mcp-mesh-logging]
- **FR-229**: Recording mesh fetch events MUST NOT degrade FetchMeshes RPC response time — non-blocking via async Channel pipeline. [Source: specs/004-mcp-mesh-logging]
- **FR-230**: An MCP `query_mesh_fetches` tool MUST retrieve mesh fetch events with time-range filtering, optional mesh ID filtering, and cursor-based pagination. [Source: specs/004-mcp-mesh-logging]
- **FR-231**: The server MUST publish FetchMeshes observations through the CommandEvent audit stream (MeshFetchLog oneof case) for MCP recording. [Source: specs/004-mcp-mesh-logging]
- **FR-232**: Recording session summary MUST include mesh fetch event count alongside snapshot and command event counts. [Source: specs/004-mcp-mesh-logging]

## Key Entities

- **SimulationCommand**: User command to control physics (add body, apply force, set gravity, step, play/pause). [Source: specs/001-server-hub]
- **ViewCommand**: User command to control 3D viewer (camera, wireframe, zoom). [Source: specs/001-server-hub]
- **SimulationState**: Snapshot of physics world — bodies, time, running flag, tick_ms (physics step duration), serialize_ms (state serialization duration). [Source: specs/001-server-hub, specs/002-performance-diagnostics]
- **Body**: Physical object — id, Vec3 position, Vec3 velocity, Vec3 angular_velocity, mass, Shape, Vec4 orientation, is_static (bool). [Source: specs/001-server-hub, specs/002-physics-simulation, specs/002-performance-diagnostics]
- **Vec3**: 3D vector (x, y, z doubles). [Source: specs/001-server-hub]
- **Vec4**: 4D vector / quaternion (x, y, z, w doubles). [Source: specs/002-physics-simulation]
- **Shape**: Geometric descriptor — Sphere (radius), Box (half_extents), Plane (normal). [Source: specs/001-server-hub]
- **World**: Simulation environment — physics engine instance, body registry, active forces map, gravity, simulation time, running state. [Source: specs/002-physics-simulation]
- **Force**: Persistent 3D vector applied to a body each step until cleared. [Source: specs/002-physics-simulation]
- **Impulse**: One-shot velocity change applied once on next step. [Source: specs/002-physics-simulation]
- **Torque**: Rotational force vector applied to a body. [Source: specs/002-physics-simulation]
- **CommandAck**: Acknowledgment with success flag and message. [Source: specs/001-server-hub]
- **PhysicsHub**: Client/viewer-facing gRPC service. [Source: specs/001-server-hub]
- **SimulationLink**: Simulation-facing gRPC service (bidirectional streaming). [Source: specs/001-server-hub]
- **SceneState**: Viewer's internal state — tracked body entities (Map<string, Entity>), simulation time, running flag, wireframe flag. [Source: specs/003-3d-viewer]
- **CameraState**: Camera parameters — position, target, up (Vector3), zoom level (float). [Source: specs/003-3d-viewer]
- **ShapeKind**: Discriminated union for visual classification — Sphere, Box, Unknown. Maps to colors and Stride PrimitiveModelType. [Source: specs/003-3d-viewer]
- **Session**: Client connection handle — holds GrpcChannel, PhysicsHubClient, CancellationTokenSource, body ID registry, cached latest SimulationState, last-update timestamp, connection status. [Source: specs/004-client-repl]
- **BodyPreset**: Named pre-configured body parameters (shape, mass, size) instantiated with optional overrides. 7 presets: marble, bowlingBall, beachBall, crate, brick, boulder, die. [Source: specs/004-client-repl]
- **Direction**: Discriminated union for steering — Up (+Y), Down (-Y), North (-Z), South (+Z), East (+X), West (-X). [Source: specs/004-client-repl]
- **IdGenerator**: Thread-safe per-shape-type counter producing human-readable IDs ("sphere-1", "box-3"). CAS-based ConcurrentDictionary. [Source: specs/004-client-repl]
- **MCP Tool**: A named operation exposed by the MCP server. 38 tools across 11 categories: simulation (10), view (3), query (2), audit (1), presets (7), generators (5), steering (4), batch (2), metrics (2), stress test (2), comparison (1). [Source: specs/005-mcp-server-testing, specs/001-mcp-persistent-service, specs/002-performance-diagnostics]
- **GrpcConnection**: MCP server's gRPC bridge — holds PhysicsHubClient, 3 background stream subscriptions (state, view commands, command audit) with independent exponential backoff, cached SimulationState, LatestViewCommand, CommandLog (bounded 100-entry buffer). Registered as DI singleton. [Source: specs/005-mcp-server-testing, specs/001-mcp-persistent-service]
- **CommandEvent**: Proto message wrapping SimulationCommand or ViewCommand in a oneof for the audit stream. Used by StreamCommands RPC. [Source: specs/001-mcp-persistent-service]
- **ScriptingLibrary**: Compiled F# library (`PhysicsSandbox.Scripting`) with 6 modules (Helpers, Vec3Builders, CommandBuilders, BatchOperations, SimulationLifecycle, Prelude) wrapping PhysicsClient convenience functions. AutoOpen Prelude re-exports all functions for script use. [Source: specs/004-fsharp-scripting-library]
- **ClientAdapter**: MCP-side adapter bridging GrpcConnection with convenience tool functions (addSphere, addBox, applyImpulse, applyTorque, clearForces). [Source: specs/001-mcp-persistent-service]
- **ServiceMetrics (MetricsCounter)**: Per-service performance counters — messages sent/received, bytes sent/received. Thread-safe via Interlocked. Monotonically increasing, persist across simulation restarts. Logged periodically (default 10s). [Source: specs/002-performance-diagnostics]
- **PipelineTimings**: Timing breakdown per pipeline stage — SimulationTickMs, StateSerializationMs, GrpcTransferMs, ViewerRenderMs, TotalPipelineMs. Point-in-time snapshots. [Source: specs/002-performance-diagnostics]
- **BatchRequest/BatchResponse**: Ordered list of commands submitted as a unit. BatchResponse contains per-command CommandResult (success, message, index) and TotalTimeMs. Max 100 commands. [Source: specs/002-performance-diagnostics]
- **StressTestRun**: Tracks a running/completed stress test — TestId, ScenarioName (body-scaling, command-throughput), Status (Pending→Running→Complete/Failed), Progress (0.0–1.0), Results. Single-test guard. [Source: specs/002-performance-diagnostics]
- **StressTestResults**: Summary of completed stress test — PeakBodyCount, DegradationBodyCount, PeakCommandRate, AverageFps, MinFps, TotalCommands, FailedCommands, ErrorMessages. [Source: specs/002-performance-diagnostics]
- **ComparisonResult**: MCP-vs-scripting comparison data — ScriptTimeMs, McpTimeMs, BatchedMcpTimeMs, message counts, OverheadPercent. [Source: specs/002-performance-diagnostics]
- **FpsState**: Viewer FPS tracking — SmoothedFps (EMA α=0.1), ElapsedSinceLog, WarningThreshold. [Source: specs/002-performance-diagnostics]
- **Python Session**: Python gRPC connection handle — dataclass holding grpc.Channel, PhysicsHubStub, server address. Simplified vs F# Session (no background streaming, no body registry). [Source: specs/004-python-demo-scripts]
- **Constraint**: Physics relationship between two bodies restricting relative motion. 10 types: BallSocket, Hinge, Weld, DistanceLimit, DistanceSpring, SwingLimit, TwistLimit, LinearAxisMotor, AngularMotor, PointOnLine. Auto-removed when either body is deleted. Cleared on reset. [Source: specs/005-stride-bepu-integration]
- **MaterialProperties**: Per-body surface interaction properties — friction (float), max_recovery_velocity (float), spring_frequency (float), spring_damping_ratio (float). Defaults applied when absent. [Source: specs/005-stride-bepu-integration]
- **RegisteredShape**: Server-side cached shape definition (mesh, convex hull) registered once by handle, referenced by ID in AddBody without retransmitting vertex data. Cleared on reset. [Source: specs/005-stride-bepu-integration]
- **Color**: Per-body RGBA (0.0–1.0 each). Auto-assigned by shape type when not specified: Sphere=blue, Box=orange, Capsule=green, Cylinder=yellow, Plane=gray, Triangle=cyan, ConvexHull=purple, Compound=white, Mesh=teal. [Source: specs/005-stride-bepu-integration]
- **DebugState**: Viewer debug visualization state — wireframe entity cache (Map<string, Entity list> for compound children), constraint line cache, enabled toggle. Updated each frame from SimulationState. [Source: specs/005-stride-bepu-integration, specs/005-viewer-settings-sizing-fix]
- **ViewerSettings**: Persisted display/quality configuration — ResolutionWidth, ResolutionHeight, IsFullscreen, AntiAliasing (Off/X2/X4/X8), ShadowQuality (Off/Low/Medium/High), TextureFiltering (Point/Linear/Anisotropic), VSync. JSON at ~/.config/PhysicsSandbox/viewer-settings.json. [Source: specs/005-viewer-settings-sizing-fix]
- **DisplayState**: Runtime display manager state — Game reference, current ViewerSettings, previous windowed dimensions for fullscreen restore. [Source: specs/005-viewer-settings-sizing-fix]
- **OverlayState**: Settings overlay UI state — visible flag, active category (Display/Quality), selected row, menu items with current selections. [Source: specs/005-viewer-settings-sizing-fix]
- **Recording Session**: Identifier (GUID), label, status (Recording/Completed/Failed), start/end time, dual retention limits, storage counters. Persisted as `session.json`. [Source: specs/005-mcp-data-logging]
- **Chunk File**: Time-based binary recording segment (~1 minute), containing length-prefixed protobuf entries. Named `chunk-{timestampMs}.bin`. [Source: specs/005-mcp-data-logging]
- **Log Entry**: Timestamped protobuf binary record. Wire format: `[uint32 totalSize | int64 timestampMs | byte entryType | byte[] payload]`. [Source: specs/005-mcp-data-logging]
- **QueryHandler**: Simulation-side query dispatch — converts proto RaycastRequest/SweepCastRequest/OverlapRequest to BepuFSharp calls, resolves BodyId→string IDs. [Source: specs/005-stride-bepu-integration]
- **QueryBuilders (Scripting)**: Convenience wrappers returning typed F# results — raycast, raycastAll, sweepSphere, overlapSphere. [Source: specs/005-stride-bepu-integration]
- **Python Prelude**: Shared Python module providing 40+ functions mirroring the F# PhysicsClient + Prelude: session management, all simulation/view commands, 7 body presets, 5 generators, steering (push/launch), display (list_bodies/status), timing (timed context manager), batch helpers, ID generation. [Source: specs/004-python-demo-scripts]
- **MeshId**: Content-addressed identifier (32 hex chars) for a unique mesh geometry. SHA-256 of proto-serialized geometry bytes, truncated to 128 bits. [Source: specs/004-mesh-cache-transport]
- **CachedShapeRef**: Proto message (mesh_id + bbox_min + bbox_max) replacing inline geometry in Body.Shape oneof for complex shapes after first transmission. ~56 bytes vs 1200+ bytes for a 50-point ConvexHull. [Source: specs/004-mesh-cache-transport]
- **MeshGeometry**: Proto message (mesh_id + full Shape) for transmitting complete geometry via SimulationState.new_meshes or FetchMeshes response. [Source: specs/004-mesh-cache-transport]
- **MeshCache (Server)**: ConcurrentDictionary<string, Shape> in MessageRouter, populated from state updates, cleared on reset/disconnect. [Source: specs/004-mesh-cache-transport]
- **MeshResolver (Subscriber)**: Local ConcurrentDictionary<string, Shape> cache on each subscriber (viewer, client, MCP) with processNewMeshes/resolve/fetchMissing interface. [Source: specs/004-mesh-cache-transport]
- **MeshIdGenerator**: Simulation-side module computing content-addressed mesh IDs and axis-aligned bounding boxes for ConvexHull, MeshShape, and Compound shapes. [Source: specs/004-mesh-cache-transport]
- **MeshFetchEvent**: Recorded observation of a FetchMeshes RPC call — timestamp, requested mesh IDs, hit count, miss count, missed mesh IDs. Serialized as MeshFetchLog proto (EntryType=3). [Source: specs/004-mcp-mesh-logging]

## Edge Cases

- Commands with no simulation connected: acknowledged, dropped gracefully. [Source: specs/001-server-hub]
- No state subscribers: state from simulation accepted but discarded. [Source: specs/001-server-hub]
- Server shutdown during streaming: streams terminated cleanly. [Source: specs/001-server-hub]
- Malformed commands: rejected with descriptive error. [Source: specs/001-server-hub]
- Second simulation connection: rejected with ALREADY_EXISTS. [Source: specs/001-server-hub]
- Server disconnects while simulation running: simulation logs event and shuts down cleanly, no reconnect. [Source: specs/002-physics-simulation]
- Body with zero or negative mass: rejected with error ack. [Source: specs/002-physics-simulation]
- Extremely large forces: simulation continues without crashing (results may be unrealistic). [Source: specs/002-physics-simulation]
- Empty world set to play: continues stepping and streaming empty state. [Source: specs/002-physics-simulation]
- Force/impulse/torque on non-existent body: success ack, no-op. [Source: specs/002-physics-simulation]
- Viewer starts before simulation sends state: shows empty scene, renders once state arrives. [Source: specs/003-3d-viewer]
- Viewer server connection drops: displays last known state, auto-reconnects with exponential backoff (1s→30s). [Source: specs/003-3d-viewer]
- Unknown or unset body shape type: rendered as small red sphere (fallback). [Source: specs/003-3d-viewer]
- Viewer receives state with zero bodies: displays empty scene (ground grid, skybox only). [Source: specs/003-3d-viewer]
- Client commands after server disconnect: returns clear Error, no hang or crash. User calls reconnect explicitly. [Source: specs/004-client-repl]
- Client references non-existent body ID: server CommandAck error surfaced as Result Error. [Source: specs/004-client-repl]
- Client generators with zero or negative count: validates input, returns Error. [Source: specs/004-client-repl]
- Client state display with stale data: shows last known state with "Last updated: Xs ago" when >5 seconds old. [Source: specs/004-client-repl]
- Multiple REPL sessions to same server: each Session is independent. [Source: specs/004-client-repl]
- MCP server cannot reach PhysicsServer: returns clear connection-failure errors per tool invocation, no crash. [Source: specs/005-mcp-server-testing]
- Simulation disconnects mid-stream: MCP get_state returns last cached state with staleness indicator. [Source: specs/005-mcp-server-testing]
- Multiple MCP clients simultaneously: each creates independent gRPC channel. [Source: specs/005-mcp-server-testing]
- Server command channel full (100 capacity): MCP relays "dropped" response. [Source: specs/005-mcp-server-testing]
- Integration tests without GPU/display: tests only exercise gRPC communication, not rendering. [Source: specs/005-mcp-server-testing]
- Simulation gRPC stream dies but process alive: auto-reconnects with exponential backoff (1s → 10s max), preserving world state. [Source: specs/005-mcp-server-testing]
- MCP server crashes under Aspire: reported as failed in dashboard, consistent with other project resources. [Source: specs/006-mcp-aspire-orchestration]
- MCP server started but no AI assistant connects: idles gracefully without errors. [Source: specs/006-mcp-aspire-orchestration]
- Multiple AI assistants connect simultaneously: all sessions share single gRPC connection, state cache, and body ID counter. [Source: specs/001-mcp-persistent-service]
- MCP client disconnects via HTTP/SSE: MCP server continues running and accepts new connections. [Source: specs/001-mcp-persistent-service]
- PhysicsServer goes down while MCP running: MCP remains running, reports disconnection, attempts reconnection with backoff. [Source: specs/001-mcp-persistent-service]
- Invalid MCP command (bad body ID, malformed params): clear error message returned without crash. [Source: specs/001-mcp-persistent-service]
- FPS drops to zero or viewer minimized: metrics still logged (smoothed FPS approaches 0). [Source: specs/002-performance-diagnostics]
- Batch command exceeds 100 max: rejected with error. [Source: specs/002-performance-diagnostics]
- Restart issued while stress test running: stress test observes cleared state. [Source: specs/002-performance-diagnostics]
- Service disconnects and reconnects: metric counters persist (module-level, never cleared). [Source: specs/002-performance-diagnostics]
- Stress test causes high resource usage: test records degradation point and stops gracefully. [Source: specs/002-performance-diagnostics]
- Metrics queried with partial system (not all services running): returns data from available services only. [Source: specs/002-performance-diagnostics]
- Batch contains invalid command among valid ones: valid commands execute, invalid returns error in per-command results. [Source: specs/002-performance-diagnostics]
- Demo server-side reset fails: Prelude `resetSimulation` prints error and falls back to manual `clearAll`. [Source: specs/001-demo-script-modernization]
- Demo batch exceeds 100-command limit: `batchAdd` auto-splits into chunks of 100. [Source: specs/001-demo-script-modernization]
- Demo run standalone vs in suite: both modes work — `resetSimulation` called at start of each demo. [Source: specs/001-demo-script-modernization]
- Body creation at 500-body tier may exceed available physics engine capacity: demo reports degradation via timing markers, does not crash. [Source: specs/003-stress-test-demos]
- Collision pit with 120+ spheres: bodies may stack above pit walls; settling time varies with count. [Source: specs/003-stress-test-demos]
- Domino cascade with 120 dominoes may not fully propagate: `timed` reports cascade duration regardless. [Source: specs/003-stress-test-demos]
- Rapid gravity changes during force frenzy: bodies respond to each change without desynchronization. [Source: specs/003-stress-test-demos]
- Python demo Aspire stack not running: scripts fail at connection step with clear gRPC error. [Source: specs/004-python-demo-scripts]
- Python proto stubs not generated: import error with clear message. [Source: specs/004-python-demo-scripts]
- Python `launch()` called immediately after body creation: requires small sleep (200ms) for simulation state to include the new body. [Source: specs/004-python-demo-scripts]
- Constraint references a removed body: auto-removed from constraint registry. [Source: specs/005-stride-bepu-integration]
- Convex hull with fewer than 4 points: rejected with error. [Source: specs/005-stride-bepu-integration]
- Compound shape with zero children: rejected with error. [Source: specs/005-stride-bepu-integration]
- Mesh shape with zero triangles: rejected with error. [Source: specs/005-stride-bepu-integration]
- Shape reference to unknown handle: rejected with error. [Source: specs/005-stride-bepu-integration]
- SetBodyPose on static body: rejected (static bodies cannot be repositioned). [Source: specs/005-stride-bepu-integration]
- Raycast originates inside a body: behavior defined by BepuPhysics2. [Source: specs/005-stride-bepu-integration]
- Conflicting material properties on contact: BepuPhysics2 combined/averaged values. [Source: specs/005-stride-bepu-integration]
- Late-joining client: registered shapes included in every state stream update. [Source: specs/005-stride-bepu-integration]
- Near-zero body dimensions (radius 0.001): clamped to 0.01f minimum for visibility. [Source: specs/005-viewer-settings-sizing-fix]
- Resolution exceeds display bounds: should clamp to maximum supported. [Source: specs/005-viewer-settings-sizing-fix]
- GPU does not support selected MSAA level: should fall back to nearest supported level. [Source: specs/005-viewer-settings-sizing-fix]
- Settings file missing or corrupt: returns defaults, no crash. [Source: specs/005-viewer-settings-sizing-fix]
- Mesh identifier references evicted geometry: server returns partial response (found meshes only). No error for unknown IDs. [Source: specs/004-mesh-cache-transport]
- Simulation reset: EmittedMeshIds cleared (simulation), MeshCache cleared (server). Next state re-emits all mesh geometry. Content-addressed IDs prevent stale data conflicts. [Source: specs/004-mesh-cache-transport]
- Compound shape with mesh children: cached as atomic unit (single ID for whole compound). Children not independently cached. [Source: specs/004-mesh-cache-transport]
- Two bodies share identical geometry: same content-addressed mesh ID, single cache entry, single new_meshes transmission. [Source: specs/004-mesh-cache-transport]
- CachedShapeRef received in simulation commands: rejected with error (only valid in state output, not command input). [Source: specs/004-mesh-cache-transport]
- Mesh fetch fails or times out in viewer: bounding box placeholder persists, viewer does not crash. [Source: specs/004-mesh-cache-transport]

## Success Criteria

- **SC-001**: Clone, build, and run AppHost in under 2 minutes. [Source: specs/001-server-hub]
- **SC-002**: Dashboard shows server hub as healthy in Development mode. [Source: specs/001-server-hub]
- **SC-003**: Command acknowledgment within 1 second. [Source: specs/001-server-hub]
- **SC-004**: State stream updates with <100ms latency. [Source: specs/001-server-hub]
- **SC-005**: Health check endpoints (/health, /alive) operational. [Source: specs/001-server-hub]
- **SC-006**: Contracts buildable by any referencing project. [Source: specs/001-server-hub]
- **SC-007**: No errors when sending commands with no downstream. [Source: specs/001-server-hub]
- **SC-008**: Simulation connects to server and is ready within 5 seconds of startup. [Source: specs/002-physics-simulation]
- **SC-009**: All simulation commands produce expected physical result within one step. [Source: specs/002-physics-simulation]
- **SC-010**: Zero skipped steps in state streaming; backpressure paces rather than drops. [Source: specs/002-physics-simulation]
- **SC-011**: Stable operation with 100+ bodies simultaneously. [Source: specs/002-physics-simulation]
- **SC-012**: 37 unit tests + 10 existing tests pass. [Source: specs/002-physics-simulation]
- **SC-013**: All bodies visible in viewer within 1 second of state receipt. [Source: specs/003-3d-viewer]
- **SC-014**: Camera commands reflected in viewer within 1 second. [Source: specs/003-3d-viewer]
- **SC-015**: Viewer maintains responsive display during continuous state updates. [Source: specs/003-3d-viewer]
- **SC-016**: Wireframe toggle without viewer restart. [Source: specs/003-3d-viewer]
- **SC-017**: Viewer starts and displays scene within 5 seconds when server running. [Source: specs/003-3d-viewer]
- **SC-018**: Viewer renders 100 bodies simultaneously. [Source: specs/003-3d-viewer]
- **SC-019**: 66 total tests passing (16 viewer + 13 server + 37 simulation). [Source: specs/003-3d-viewer]
- **SC-020**: User goes from loading library to running simulation in ≤5 function calls. [Source: specs/004-client-repl]
- **SC-021**: All 7 body presets produce valid bodies with correct shape and mass. [Source: specs/004-client-repl]
- **SC-022**: Random generators produce varied bodies (no duplicates in batch of 10+). [Source: specs/004-client-repl]
- **SC-023**: All 9 simulation commands and 3 view commands accessible via dedicated functions. [Source: specs/004-client-repl]
- **SC-024**: Library loads and connects in FSI. [Source: specs/004-client-repl]
- **SC-025**: Steering functions produce observable motion in expected direction. [Source: specs/004-client-repl]
- **SC-026**: 123 total tests passing (52 client + 16 viewer + 13 server + 37 simulation + 5 integration). [Source: specs/004-client-repl]
- **SC-027**: All PhysicsHub and SimulationLink query operations invocable from MCP client — 100% RPC coverage as MCP tools. [Source: specs/005-mcp-server-testing]
- **SC-028**: Simulation maintains stable connection for at least 10 minutes under normal operation after SSL fix. [Source: specs/005-mcp-server-testing]
- **SC-029**: Integration test suite covers at least 15 distinct scenarios across command routing, state streaming, simulation lifecycle, and error conditions. Actual: 32 tests. [Source: specs/005-mcp-server-testing]
- **SC-030**: All integration tests pass in headless CI within 5 minutes. [Source: specs/005-mcp-server-testing]
- **SC-031**: MCP command → state change round-trip completes within 5 seconds end-to-end. [Source: specs/005-mcp-server-testing]
- **SC-032**: Zero regressions — all existing 118 unit tests + 5 integration tests pass after changes. [Source: specs/005-mcp-server-testing]
- **SC-033**: 150 total tests passing (52 client + 16 viewer + 13 server + 37 simulation + 32 integration). [Source: specs/005-mcp-server-testing]
- **SC-034**: Starting the AppHost results in all 5 project resources (server, simulation, viewer, client, mcp) appearing in the Aspire dashboard. [Source: specs/006-mcp-aspire-orchestration]
- **SC-035**: MCP server connects to PhysicsServer without any manually specified address when launched through Aspire. [Source: specs/006-mcp-aspire-orchestration]
- **SC-036**: All existing tests continue to pass after orchestration change. [Source: specs/006-mcp-aspire-orchestration]
- **SC-037**: 153 total tests passing (52 client + 16 viewer + 13 server + 37 simulation + 35 integration). [Source: specs/006-mcp-aspire-orchestration]
- **SC-038**: MCP server remains running for entire AppHost lifetime with zero unplanned shutdowns due to client disconnections. [Source: specs/001-mcp-persistent-service]
- **SC-039**: AI assistant can connect, disconnect, and reconnect to MCP server without service interruption. [Source: specs/001-mcp-persistent-service]
- **SC-040**: All 12 command types (9 simulation + 3 view) executable through MCP, covering 100% of the protocol's command surface. [Source: specs/001-mcp-persistent-service]
- **SC-041**: Convenience tools (presets, generators, steering) available and functional. [Source: specs/001-mcp-persistent-service]
- **SC-042**: State queries return data with staleness under 2 seconds during normal operation. [Source: specs/001-mcp-persistent-service]
- **SC-043**: 16 PhysicsServer.Tests pass (3 new audit subscriber tests). [Source: specs/001-mcp-persistent-service]
- **SC-044**: Live FPS visible in viewer at all times during a running simulation. [Source: specs/002-performance-diagnostics]
- **SC-045**: Per-service message count and traffic metrics available in logs within 30 seconds of startup. [Source: specs/002-performance-diagnostics]
- **SC-046**: A batch of 50 commands completes at least 2x faster than 50 individual sequential commands. [Source: specs/002-performance-diagnostics]
- **SC-047**: Simulation restarts to clean state in under 2 seconds via single command. [Source: specs/002-performance-diagnostics]
- **SC-048**: All static bodies block dynamic bodies — zero pass-through incidents. [Source: specs/002-performance-diagnostics]
- **SC-049**: Diagnostics correctly identify the slowest pipeline stage within 10% accuracy. [Source: specs/002-performance-diagnostics]
- **SC-050**: Stress tests scale to at least 500 simultaneous bodies while measuring degradation. [Source: specs/002-performance-diagnostics]
- **SC-051**: MCP-vs-scripting comparison quantifies overhead with reproducible results (<15% variance). [Source: specs/002-performance-diagnostics]
- **SC-052**: All 10 demos run successfully in both RunAll and AutoRun modes with zero failures after modernization. [Source: specs/001-demo-script-modernization]
- **SC-053**: Every demo starts from clean state — zero leftover bodies when running full suite. [Source: specs/001-demo-script-modernization]
- **SC-054**: No demo script requires more than one import beyond Prelude for batching/reset capabilities. [Source: specs/001-demo-script-modernization]
- **SC-055**: At least 5 new stress demos runnable end-to-end without unhandled crashes. [Source: specs/003-stress-test-demos]
- **SC-056**: Body-scaling demo identifies a concrete degradation point (body count where step time > 100ms). [Source: specs/003-stress-test-demos]
- **SC-057**: Collision-density demo creates at least 100 simultaneously interacting bodies in a confined space. [Source: specs/003-stress-test-demos]
- **SC-058**: Combined scenario demo runs with 200+ bodies, forces, and camera movement, reporting per-stage timing. [Source: specs/003-stress-test-demos]
- **SC-059**: All stress demos complete within 5 minutes each when run individually. [Source: specs/003-stress-test-demos]
- **SC-060**: Full demo suite (15 demos) runs end-to-end via AutoRun without manual intervention. [Source: specs/003-stress-test-demos]
- **SC-061**: At least one stress scenario executable via MCP batch tools with comparable results to scripting. [Source: specs/003-stress-test-demos]
- **SC-062**: All 15 Python demos execute successfully against a running Aspire stack, producing equivalent visual physics behavior to the F# demos. [Source: specs/004-python-demo-scripts]
- **SC-063**: Python automated runner completes all 15 demos and reports results within 3 minutes. [Source: specs/004-python-demo-scripts]
- **SC-064**: Python demos require no .NET tooling to run — only Python and pip dependencies. [Source: specs/004-python-demo-scripts]
- **SC-065**: A Python developer unfamiliar with F# can run and understand any demo from its script alone. [Source: specs/004-python-demo-scripts]
- **SC-066**: The full solution builds successfully with zero ProjectReferences to PhysicsSandbox.Scripting or PhysicsClient from external consumers. [Source: specs/004-scripting-nuget-package]
- **SC-067**: All existing tests pass after the NuGet migration. [Source: specs/004-scripting-nuget-package]
- **SC-068**: F# scripts load and execute correctly using version-agnostic NuGet references. [Source: specs/004-scripting-nuget-package]
- **SC-069**: The pack-and-publish workflow follows the established local NuGet pattern (dependency-ordered `dotnet pack`). [Source: specs/004-scripting-nuget-package]
- **SC-070**: Zero references to `localhost:5000` remain in script or documentation files. [Source: specs/004-scripting-nuget-package]
- **SC-071**: Each subsequent package publish uses a higher version number than the previous. [Source: specs/004-scripting-nuget-package]
- **SC-072**: All 10 shape types (sphere, box, plane, capsule, cylinder, triangle, convex hull, compound, mesh, shape reference) can be created, simulated, and visualized. [Source: specs/005-stride-bepu-integration]
- **SC-073**: Debug wireframe togglable at runtime displaying collider outlines and constraint connections. [Source: specs/005-stride-bepu-integration]
- **SC-074**: 10 constraint types connect bodies with correct constrained motion. [Source: specs/005-stride-bepu-integration]
- **SC-075**: Different material properties produce visibly different collision behaviors. [Source: specs/005-stride-bepu-integration]
- **SC-076**: Raycasts correctly identify hit bodies in scenes with 50+ bodies. [Source: specs/005-stride-bepu-integration]
- **SC-077**: Non-interacting collision layers pass through, same-layer collide normally. [Source: specs/005-stride-bepu-integration]
- **SC-078**: Kinematic bodies move at set velocity, push dynamic bodies, unaffected by gravity. [Source: specs/005-stride-bepu-integration]
- **SC-079**: All new features accessible via REPL and MCP. Scripting has convenience builders for common constraint types. [Source: specs/005-stride-bepu-integration]
- **SC-080**: Existing demos and tests pass without modification (backward compatibility). [Source: specs/005-stride-bepu-integration]
- **SC-081**: Two bodies of same shape with different colors render in respective colors. [Source: specs/005-stride-bepu-integration]
- **SC-082**: All 10 supported shape types render at visually correct dimensions — no visual overlap where physics bodies are separated. [Source: specs/005-viewer-settings-sizing-fix]
- **SC-083**: Debug wireframe outlines align with solid body edges for all shape types. [Source: specs/005-viewer-settings-sizing-fix]
- **SC-084**: Fullscreen toggle responds within 0.5 seconds of F11 keypress with no visual corruption. [Source: specs/005-viewer-settings-sizing-fix]
- **SC-085**: Resolution changes apply within 1 second without requiring a restart. [Source: specs/005-viewer-settings-sizing-fix]
- **SC-086**: Settings persist across viewer restarts. [Source: specs/005-viewer-settings-sizing-fix]
- **SC-087**: Viewer maintains above 30 FPS at minimum quality settings with 100 active bodies. [Source: specs/005-viewer-settings-sizing-fix]
- **SC-088**: State update message size for scenes with 10+ mesh-based bodies reduced by at least 80% after initial geometry exchange. [Source: specs/004-mesh-cache-transport]
- **SC-089**: Late-joining subscribers can fully resolve all mesh geometries within 5 seconds of connecting. [Source: specs/004-mesh-cache-transport]
- **SC-090**: 60 Hz state delivery maintained with <5% jitter during concurrent mesh transfers. [Source: specs/004-mesh-cache-transport]
- **SC-091**: Identical shapes used by multiple bodies result in only one cached copy. [Source: specs/004-mesh-cache-transport]
- **SC-092**: Viewers display placeholder bounding boxes for unresolved meshes within one frame of state receipt. [Source: specs/004-mesh-cache-transport]
- **SC-093**: All FetchMeshes RPC calls during an active recording are captured with zero data loss under normal operation. [Source: specs/004-mcp-mesh-logging]
- **SC-094**: Recording mesh fetch events adds less than 1ms overhead to FetchMeshes RPC. [Source: specs/004-mcp-mesh-logging]
