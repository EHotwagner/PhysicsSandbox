# PClientV2 Session Issue Report

**Date:** 2026-03-29
**Reporter:** Claude (via PClientV2 FSI session)
**Context:** Attempting to throw a ball at spheres using PClientV2 scripting library via FSI MCP

---

## Issue 1: `resetSimulation` does not reliably clear server-side bodies

**Symptom:** After calling `resetSimulation`, `overlapSphere` still returns bodies from previous sessions (e.g. `sphere-2` at x=-290, `sphere-3` at x=142). Raycasts confirm these bodies no longer exist at expected positions, but overlap queries return stale IDs.

**Root cause (suspected):** `PhysicsClient.SimulationCommands.clearAll` may not be fully clearing the server's body registry, OR the `Session` object's `CachedBodies` persists stale entries across `clearAll` calls and `overlapSphere` reads from the cache rather than querying the server.

**Evidence:**
- After `clearAll`, raycasts only find `plane-1` (correct), but `overlapSphere` returns 12 bodies including ones scattered hundreds of units away.
- Reconnecting with a fresh `Session.connect` still shows the same stale bodies in the session's `CachedBodies`.

**Possible solutions:**
1. `overlapSphere` should query the physics server directly, not read from `Session.CachedBodies`.
2. `clearAll` should invalidate/clear `Session.CachedBodies` on the client side.
3. `Session.connect` should fetch a fresh body list from the server, not inherit stale cache state.

---

## Issue 2: ID counter reset causes silent `AddBody` failures

**Symptom:** After `resetSimulation` (which calls `PhysicsClient.IdGenerator.reset()`), `makeSphereCmd` generates IDs like `sphere-1`, `sphere-2`, etc. If the server still has bodies with those IDs (see Issue 1), the `AddBody` commands fail silently — no error is raised, but the bodies are not created.

**Evidence:**
- After reset + `batchAdd` with 4 spheres, raycasts found zero new bodies.
- Using custom IDs (`target-a`, `target-b`, etc.) with the same coordinates worked immediately.

**Possible solutions:**
1. `batchAdd` / `AddBody` should return an error or log a warning when a body ID already exists.
2. `resetSimulation` should guarantee the server is in a clean state before resetting the ID counter — either by verifying `clearAll` succeeded or by using a different ID namespace per session (e.g. UUID prefix).
3. `clearAll` should be a synchronous, confirmed operation — wait for server acknowledgment that all bodies are removed before proceeding.

---

## Issue 3: `overlapSphere` and raycast give inconsistent results

**Symptom:** `overlapSphere` with radius=500 finds bodies that no raycast (even with 200-unit range across a wide grid) can locate.

**Root cause (suspected):** `overlapSphere` reads from client-side cache while `raycast` queries the actual physics server. The two data sources are out of sync.

**Possible solutions:**
1. Both query functions should use the same data source (preferably the server).
2. Add a `Session.refresh()` or `Session.sync()` method that forces a cache update from the server.
3. Document which query functions use cache vs. server, so users know which to trust.

---

## Issue 4: `batchAdd` returns `unit` with no error feedback

**Symptom:** When `AddBody` commands fail (due to duplicate IDs), `batchAdd` returns `unit` with no indication of failure. This makes debugging very difficult.

**Evidence:** Multiple `batchAdd` calls appeared to succeed but created zero bodies.

**Possible solutions:**
1. Change `batchAdd` return type to include per-command success/failure status.
2. At minimum, log warnings to console when individual commands in a batch fail.
3. Consider a `batchAddChecked` variant that throws on any failure.

---

## Summary

The core issue chain is: `clearAll` doesn't fully clean up -> ID counter resets to 1 -> new bodies collide with stale IDs -> `AddBody` silently fails -> user sees no bodies and no errors.

**Workaround used:** Construct `SimulationCommand` objects manually with unique IDs (e.g. `target-a`, `cannonball`) instead of using `makeSphereCmd` which relies on the auto-incrementing ID generator.
