# PhysicsSandbox Refactor Evaluation Report

**Date**: 2026-03-23
**Scope**: Full solution analysis — 9 source projects, 6 test projects, 2 scripting layers
**Method**: Per-project code review for complexity, duplication, coupling, and architectural quality

---

## Executive Summary

The PhysicsSandbox solution is a **well-architected, moderately complex** distributed physics simulation platform. Across ~8,500 LOC of hand-written source code (excluding generated protobuf), the codebase demonstrates strong F# discipline (signature files, functional patterns, clear module boundaries) with localized quality issues that are fixable through targeted refactoring.

**Overall Solution Rating: 7.4/10**

| Project | LOC | Rating | Verdict |
|---------|-----|--------|---------|
| PhysicsServer | 892 | 6.5/10 | God object + duplication; needs decomposition |
| PhysicsSimulation | 1,204 | 7.5/10 | Solid domain logic; deep nesting in `addBody` |
| PhysicsViewer | 1,628 | 8.5/10 | Best-structured project; minimal issues |
| PhysicsClient | 1,358 | 7.5/10 | Good API; significant builder duplication |
| PhysicsSandbox.Mcp | 2,093 | 6.5/10 | 550-LOC monolith; stream duplication |
| PhysicsSandbox.Scripting | 741 | 8.0/10 | Clean design; incomplete coverage |
| AppHost | 24 | 9.0/10 | Minimal, correct |
| ServiceDefaults | 127 | 8.0/10 | Standard Aspire patterns |
| Shared.Contracts | 590 | 7.0/10 | 81 proto messages; approaching split threshold |
| Tests (6 projects) | 4,816 | 7.5/10 | 275 tests; good coverage with some duplication |
| Scripting (F# + Python) | 4,667 | 7.0/10 | 33 demos; Python lacks parity |

**Key Finding**: A full rewrite is **not justified**. The architecture is sound, module boundaries are clear, and issues are concentrated in 3-4 specific modules. Targeted refactoring of ~800 LOC would raise the solution quality to 8.5+/10.

---

## Part 1: Per-Project Analysis

### 1.1 PhysicsServer (6.5/10)

**Size**: 892 LOC (525 implementation, 197 signatures, 170 tests)

**Architecture**: gRPC message routing hub — StateCache, MetricsCounter, MessageRouter, two gRPC services.

**Critical Issues**:

| Issue | Severity | LOC Impact | Location |
|-------|----------|-----------|----------|
| **MessageRouter god object** | High | 301 LOC | `Hub/MessageRouter.fs` |
| **Query batch duplication** | High | 60 LOC | `Services/PhysicsHubService.fs:101-152` |
| **Batch command duplication** | Medium | 36 LOC | `Hub/MessageRouter.fs:214-252` |
| **Global mutable pendingQueries** | Medium | — | `Hub/MessageRouter.fs:34` |
| **Race condition in diagnostics** | Medium | — | `Services/SimulationLinkService.fs:13` |
| **Silent exception swallowing** | Medium | — | `Hub/MessageRouter.fs:72`, `Services/SimulationLinkService.fs:60` |
| **WhenAny instead of WhenAll** | Medium | — | `Services/SimulationLinkService.fs:76` |
| **No pending query expiration** | Low | — | `Hub/MessageRouter.fs` (ConcurrentDict unbounded) |

**MessageRouter decomposition** is the highest-value refactor in the entire solution. It mixes subscriptions, command channels, query correlation, metrics, and connection state in a single 301-LOC type with 22 public functions. Splitting into `SubscriptionRegistry`, `CommandRouter`, `QueryCoordinator` would improve testability, reduce coupling, and eliminate the implicit shared state between query submission and response handling.

**Refactor Estimate**: 2-3 days to decompose MessageRouter + DRY up batch methods.

---

### 1.2 PhysicsSimulation (7.5/10)

**Size**: 1,204 LOC (669 SimulationWorld, 184 SimulationClient, 119 QueryHandler, 60 CommandHandler)

**Architecture**: Physics engine client — BepuFSharp world management, command handling, query dispatch.

**Critical Issues**:

| Issue | Severity | LOC Impact | Location |
|-------|----------|-----------|----------|
| **`addBody` deep nesting** | High | 131 LOC | `SimulationWorld.fs:311-441` |
| **Vector conversion duplication** | Medium | 30 LOC | `SimulationWorld.fs` + `QueryHandler.fs` |
| **Null-check triangle duplication** | Medium | 12 LOC | `SimulationWorld.fs:179-181, 223-226` |
| **Internal type exposure for QueryHandler** | Medium | — | `SimulationWorld.fsi:72-95` |
| **Unprotected mutable metrics** | Low | — | `SimulationWorld.fs:262-263` |
| **Unconditional fallback (default Sphere)** | Low | — | `SimulationWorld.fs:238` |

**The `addBody` function** is the most complex single function in the codebase. Static/Kinematic/Dynamic branches duplicate 90% of record construction with only 4 fields varying. Refactoring to a shared `createBodyRecord` helper and flattening the nested if-else to pattern matching would cut this from 131 to ~60 LOC.

**Refactor Estimate**: 1-2 days to flatten addBody + extract shared conversions.

---

### 1.3 PhysicsViewer (8.5/10)

**Size**: 1,628 LOC (288 Program, 727 Rendering, 438 Settings, 101 Streaming)

**Architecture**: Stride3D-based 3D physics visualization with camera, debug wireframes, settings overlay.

**This is the best-structured project** in the solution. Clear three-tier architecture, opaque types hiding implementation, immutable-first functional style, comprehensive documentation.

**Minor Issues**:

| Issue | Severity | LOC Impact | Location |
|-------|----------|-----------|----------|
| **12 mutable bindings in Program.fs** | Medium | — | `Program.fs` (Stride boundary; unavoidable) |
| **Stream startup duplication** | Low | 45 LOC | `Program.fs` (startStateStream/startViewCommandStream) |
| **Magic numbers in SettingsOverlay** | Low | — | `Settings/SettingsOverlay.fs` |
| **Bounding-box approximations** | Low | — | `Rendering/ShapeGeometry.fs` (documented limitation) |

**No significant refactoring needed.** The mutable state in Program.fs is justified by Stride's callback-based game loop architecture, and the stream duplication is only 2 instances.

---

### 1.4 PhysicsClient (7.5/10)

**Size**: 1,358 LOC (551 SimulationCommands, 193 Generators, 151 Session, 106 StateDisplay, 99 Steering, 82 Presets)

**Architecture**: gRPC REPL client library — session management, command builders, body generators, terminal display.

**Critical Issues**:

| Issue | Severity | LOC Impact | Location |
|-------|----------|-----------|----------|
| **Body builder duplication** | High | ~140 LOC | `Commands/SimulationCommands.fs` (addSphere/Box/Capsule/Cylinder 90% identical) |
| **Imperative loops in Generators** | High | ~70 LOC | `Bodies/Generators.fs` (7 functions use mutable accumulation) |
| **Silent TryAdd/TryRemove failures** | Medium | — | `Commands/SimulationCommands.fs` (7 instances) |
| **O(n) body lookups** | Medium | — | `Display/StateDisplay.fs`, `Steering.fs` |
| **Inconsistent API signatures** | Medium | — | addSphere has 10 params; addCapsule has 6 |
| **toVec3 coupling** | Low | — | `ViewCommands.fs` imports SimulationCommands just for toVec3 |

**The `addBodyInternal` extraction** is the single highest-impact DRY refactor. All four shape-adding functions follow an identical 35-line pattern differing only in which shape field is set. Extracting this to a higher-order function would eliminate ~100 LOC of duplication and make the API extension pattern obvious.

**The imperative loops** in Generators are un-idiomatic F#. Each of the 7 bulk-generation functions uses `mutable ids = []` + `mutable lastError = None` + for-loop + manual list reversal. These should use `List.fold` or `Result.traverse` patterns.

**Refactor Estimate**: 1-2 days to extract addBodyInternal + functional generators.

---

### 1.5 PhysicsSandbox.Mcp (6.5/10)

**Size**: 2,093 LOC (550 SimulationTools, 461 StressTestRunner, 221 GrpcConnection, 156 BatchTools, rest distributed)

**Architecture**: MCP server with 38 AI-callable tools — simulation commands, queries, views, metrics, stress tests.

**Critical Issues**:

| Issue | Severity | LOC Impact | Location |
|-------|----------|-----------|----------|
| **SimulationTools monolith** | High | 550 LOC | `Tools/SimulationTools.fs` (34 optional params on add_body) |
| **Stream startup 3x duplication** | High | ~60 LOC | `GrpcConnection.fs:50-147` |
| **Hand-coded JSON parsing** | Medium | 156 LOC | `Tools/BatchTools.fs` (manual JsonElement inspection) |
| **Stub/incomplete modules** | Medium | ~250 LOC | SteeringTools, ComparisonTools, GeneratorTools |
| **No error abstraction** | Medium | — | All tools return `Task<string>` |
| **Shape builder duplication** | Medium | ~100 LOC | SimulationTools duplicates shapes that exist in CommandBuilders |

**SimulationTools.fs** is the largest single module in the solution at 550 LOC. Its `add_body` method accepts 34 optional parameters and manually constructs shapes inline rather than reusing `CommandBuilders` from the Scripting library. The stream startup pattern is copy-pasted 3 times with trivial variation.

**Refactor Estimate**: 2-3 days to decompose SimulationTools + genericize streams + reuse CommandBuilders.

---

### 1.6 PhysicsSandbox.Scripting (8.0/10)

**Size**: 741 LOC (199 Prelude, 187 CommandBuilders, 102 ConstraintBuilders, 73 SimulationLifecycle, rest distributed)

**Architecture**: Convenience scripting library wrapping PhysicsClient with auto-opened Prelude.

**This is a well-designed library** with minimal, focused modules and excellent documentation (161 LOC of Prelude signature docs alone).

**Issues**:

| Issue | Severity | LOC Impact | Location |
|-------|----------|-----------|----------|
| **Prelude is pure re-export** | Medium | ~360 LOC | `Prelude.fs` + `Prelude.fsi` (delegation boilerplate) |
| **Missing 6/10 constraint types** | Medium | — | Only BallSocket, Hinge, Weld, DistanceLimit implemented |
| **No input validation** | Low | — | Bad input silently propagates to server |
| **Missing material/color presets** | Low | — | Users manually construct common values |
| **batchAdd silent on partial failure** | Low | — | Logs errors but doesn't return count |

**Refactor Estimate**: 1 day to add missing constraints + presets + validation.

---

### 1.7 Infrastructure Projects (AppHost 9/10, ServiceDefaults 8/10, Contracts 7/10)

**AppHost** (24 LOC): Minimal, correct, no issues. Declarative Aspire orchestration.

**ServiceDefaults** (127 LOC): Standard Aspire patterns. Minor issue: 3 blocks of commented-out code should be removed or documented.

**Shared.Contracts** (590 LOC proto, 25K generated): Well-structured proto with 81 messages covering 10 shapes, 10 constraints, 3 query types. Approaching the complexity threshold where splitting into sub-packages would improve maintainability. The `SimulationCommand` oneof has 17 variants — manageable but growing.

---

### 1.8 Tests & Scripting (7.5/10 and 7.0/10)

**Tests** (275 tests, 4,816 LOC across 6 projects):
- Good domain coverage (all 10 shapes, 10 constraints tested)
- Thread-safety validation (Parallel.For tests in multiple projects)
- Surface area tests prevent API regressions
- Integration tests validate full Aspire stack (42 tests)
- **Duplication**: `getPublicMembers` helper copy-pasted in 4 SurfaceArea tests; `StartAppAndConnect` duplicated in ~8 integration test files; proto type aliases repeated across 5 test files

**Scripting** (33 demos, 4,667 LOC across F# + Python):
- F# has 18 demos; Python has 15 (missing constraint, query, kinematic demos)
- F# Prelude.fsx (315 LOC) and Python prelude.py (732 LOC) well-designed
- Demo 16 defines inline constraint helpers that should be in Prelude

---

## Part 2: Cross-Cutting Issues

### 2.1 Solution-Wide Code Duplication

| Pattern | Locations | Est. Duplicated LOC |
|---------|-----------|-------------------|
| Vector conversion (toVec3/fromVec3) | SimulationWorld, QueryHandler, ClientAdapter, Vec3Builders, Prelude | ~50 |
| Shape construction (proto builders) | SimulationCommands, SimulationTools (MCP), CommandBuilders | ~200 |
| Stream reconnection with backoff | PhysicsServer, PhysicsViewer, PhysicsClient, MCP (GrpcConnection) | ~120 |
| Body add boilerplate | PhysicsClient (4 functions), MCP SimulationTools | ~180 |
| Test helpers (getPublicMembers, StartAppAndConnect) | 4 + 8 test files | ~55 |
| Proto type aliases | 5+ files | ~40 |
| **Total estimated duplication** | | **~645 LOC** |

This represents ~7.5% of the codebase. Industry threshold for concern is typically 5-10%, so this is at the upper boundary.

### 2.2 ID Generation Inconsistency

Three separate ID generation mechanisms exist:
1. **PhysicsClient.IdGenerator** — sequential counter with namespacing
2. **MCP SimulationTools.nextId** — module-level counter dictionary
3. **Scripting SimulationLifecycle** — delegates to PhysicsClient.IdGenerator

If MCP and PhysicsClient are used in the same session, ID collisions are possible.

### 2.3 Error Handling Inconsistency

| Layer | Error Pattern |
|-------|--------------|
| PhysicsClient | `Result<'a, string>` |
| Scripting | `Result<'a, string>` + `ok` helper that throws |
| MCP | `Task<string>` (unstructured success/error in same type) |
| PhysicsServer | `CommandAck` with success bool + message |
| PhysicsSimulation | `CommandAck` proto |

No shared error type exists. MCP tool results cannot compose with Scripting results.

### 2.4 Proto Type Name Conflicts

BepuFSharp and proto both define `Sphere`, `Box`, etc. Every F# file that uses both must add type aliases (`ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere`). This is repeated in 5+ files and is an upstream naming issue.

---

## Part 3: Alternative Approaches

### Alternative A: Targeted Refactoring (Recommended)

**Scope**: Fix the 5 highest-impact issues without changing architecture.

**What changes**:
1. Decompose MessageRouter into 3 modules (~300 LOC restructured)
2. Extract `addBodyInternal` in PhysicsClient (~100 LOC saved)
3. Decompose SimulationTools.fs in MCP (~200 LOC restructured, reuse CommandBuilders)
4. Extract shared vector/stream helpers (~80 LOC saved)
5. Flatten `addBody` in PhysicsSimulation (~70 LOC simplified)

**Estimated effort**: 5-8 days
**Expected quality improvement**: 7.4 → 8.5/10
**Risk**: Low — each refactor is isolated and testable
**What stays the same**: Architecture, project structure, proto contracts, test suite, all external interfaces

**Pros**:
- Minimal disruption to ongoing feature work
- Each change is independently reviewable and deployable
- 275 existing tests provide safety net
- No proto/API breaking changes

**Cons**:
- Doesn't address proto type naming (upstream issue)
- Doesn't address error type inconsistency (cross-cutting change)
- Some duplication patterns remain (acceptable level)

---

### Alternative B: Major Refactoring (Extract Shared Libraries)

**Scope**: Create shared foundation libraries and restructure dependencies.

**What changes** (in addition to Alternative A):
1. New `PhysicsSandbox.Shared.Helpers` project with:
   - Vector conversions (toVec3, fromVec3, toQuaternion)
   - Proto builders (shape construction, constraint construction)
   - Stream reconnection helper (generic backoff loop)
   - Error types (shared Result/Error discriminated union)
2. Unify ID generation into Shared.Helpers
3. MCP reuses Scripting/CommandBuilders instead of inline shape construction
4. Add missing 6 constraint types to Scripting library
5. Complete Python demo parity (demos 16-18)
6. Extract shared test helpers project

**Estimated effort**: 12-18 days
**Expected quality improvement**: 7.4 → 9.0/10
**Risk**: Medium — shared library changes ripple across all projects; proto builder changes require careful coordination

**Pros**:
- Eliminates ~90% of cross-project duplication
- Single source of truth for vector/shape/constraint construction
- Unified error handling improves composability
- Test helper extraction reduces test maintenance

**Cons**:
- Shared library becomes a coupling point (all projects depend on it)
- Requires coordinated changes across 6+ projects simultaneously
- Risk of over-abstraction (creating frameworks for one-off patterns)
- Python demo parity is effort with limited technical value

---

### Alternative C: Full Rewrite

**Scope**: Rebuild the solution from scratch with current knowledge.

**What would change**:
- New proto contract design (namespaced to avoid type conflicts)
- Single unified command/error type system
- Physics simulation with proper domain model (not proto-coupled)
- MCP tools auto-generated from proto schema
- Viewer built on cleaner Stride abstractions
- Unified scripting across F#/Python with shared proto stubs

**Estimated effort**: 40-60 days
**Expected quality improvement**: Potentially 9.0+/10 (but uncertain)
**Risk**: **High** — regressions in 275 tests' worth of behavior; loss of battle-tested edge case handling; extended period with no working system

**Pros**:
- Clean slate eliminates all accumulated technical debt
- Opportunity to redesign proto schema without backward compatibility
- Could adopt newer patterns (e.g., source-generated MCP tools)

**Cons**:
- **Not justified by current quality level** — 7.4/10 is solid for a sandbox/research project
- Rewrites historically take 2-3x longer than estimated
- Loses institutional knowledge embedded in workarounds (Stride interop quirks, BepuPhysics edge cases, proto null handling)
- 275 tests and 33 demos would need to be revalidated
- Architecture is already sound — issues are in implementation details, not design

---

### Alternative D: Partial Rewrite (Server + MCP Only)

**Scope**: Rewrite the two weakest projects (PhysicsServer 6.5, MCP 6.5) while keeping the rest.

**What changes**:
- New PhysicsServer with properly decomposed message routing
- New MCP server reusing Scripting library as foundation
- Existing Simulation, Viewer, Client, Scripting untouched

**Estimated effort**: 15-20 days
**Expected quality improvement**: 7.4 → 8.5/10
**Risk**: Medium — must maintain proto/gRPC interface compatibility

**Pros**:
- Targets the two lowest-rated projects specifically
- Viewer/Simulation/Client are already good quality
- Proto contracts provide stable interface boundary

**Cons**:
- More effort than Alternative A for similar quality outcome
- New server must pass all 42 integration tests
- MCP rewrite risks losing stress test infrastructure
- Rewrites of working code carry regression risk

---

## Part 4: Recommended Approach

**Recommendation: Alternative A (Targeted Refactoring)** with selective elements from Alternative B.

The solution's architecture is sound. Issues are concentrated in specific modules, not systemic design flaws. A full or partial rewrite would cost 3-10x more than targeted refactoring for marginal quality improvement.

### Prioritized Refactoring Roadmap

| Priority | Target | Impact | Effort | ROI |
|----------|--------|--------|--------|-----|
| **1** | Decompose MessageRouter (PhysicsServer) | High | 2 days | Highest — fixes god object, race condition, memory leak risk |
| **2** | Extract `addBodyInternal` (PhysicsClient) | High | 0.5 days | High — eliminates 140 LOC duplication, makes API extensible |
| **3** | Flatten `addBody` (PhysicsSimulation) | High | 1 day | High — simplifies most complex function in codebase |
| **4** | Decompose SimulationTools (MCP) | High | 2 days | High — fixes 550-LOC monolith + reuses CommandBuilders |
| **5** | Genericize stream reconnection | Medium | 1 day | Medium — eliminates ~120 LOC cross-project duplication |
| **6** | Extract shared vector/proto helpers | Medium | 1 day | Medium — single source of truth for conversions |
| **7** | Functional generators (PhysicsClient) | Medium | 0.5 days | Medium — replaces 7 imperative loops with idiomatic F# |
| **8** | Add missing constraint types (Scripting) | Medium | 0.5 days | Medium — completes library coverage (4/10 → 10/10) |
| **9** | Extract shared test helpers | Low | 0.5 days | Low — reduces test maintenance burden |
| **10** | Fix silent TryAdd/TryRemove (PhysicsClient) | Low | 0.5 days | Low — prevents silent registry corruption |
| **11** | Add pending query expiration (PhysicsServer) | Low | 0.5 days | Low — prevents memory leak under failure conditions |
| **12** | Complete Python demo parity (demos 16-18) | Low | 1 day | Low — documentation value only |
| **13** | Remove commented-out code (ServiceDefaults) | Low | 0.25 days | Low — housekeeping |

**Total estimated effort**: 11-12 days for all items; **5-6 days for top 5 priorities** which deliver ~80% of the quality improvement.

---

## Part 5: BepuFSharp Wrapper Assessment

The BepuFSharp wrapper (external dependency at 0.2.0-beta.1) is consumed primarily by PhysicsSimulation. Key observations:

- **10 shape types, 10 constraint types** — comprehensive coverage
- **Proto type name conflicts** (Sphere, Box) force type aliasing in every consumer — this is the wrapper's main friction point
- **Plane approximation** (1000x0.1x1000 box) is a known limitation of BepuPhysics2, not the wrapper
- **The wrapper itself is not a refactoring target** — it's an external dependency. The friction it creates (type aliasing, null checks on proto types) is best addressed by shared helper modules (Alternative A, item 6)

A rewrite of BepuFSharp is **not recommended**. The wrapper is stable, the type conflicts are a naming issue solvable with aliases, and the 0.2.0-beta.1 API surface matches the physics domain well.

---

## Appendix: Lines of Code Summary

| Category | Project | LOC |
|----------|---------|-----|
| **Source** | PhysicsServer | 892 |
| | PhysicsSimulation | 1,204 |
| | PhysicsViewer | 1,628 |
| | PhysicsClient | 1,358 |
| | PhysicsSandbox.Mcp | 2,093 |
| | PhysicsSandbox.Scripting | 741 |
| | AppHost | 24 |
| | ServiceDefaults | 127 |
| | Shared.Contracts (proto) | 590 |
| **Source Total** | | **8,657** |
| **Tests** | 6 test projects | 4,816 |
| **Scripting** | F# demos + Python demos | 4,667 |
| **Generated** | Proto C# | ~25,296 |
| **Grand Total (excluding generated)** | | **18,140** |
