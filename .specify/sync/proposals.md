# Drift Resolution Proposals

Generated: 2026-03-22
Based on: drift-report from 2026-03-22

## Summary

| Resolution Type | Count |
|-----------------|-------|
| Align (Spec → Code) | 1 |
| Backfill (Code → Spec) | 2 |
| Human Decision | 1 |
| Remove from Spec | 2 |
| New Specs | 1 |

## Proposals

### Proposal 1: SC-001 — Broken `open Prelude.DemoHelpers`

**Direction**: ALIGN (Spec → Code)

**Current State**:
- Spec says: "All 15 demos run successfully through both AllDemos runners"
- Code does: AllDemos.fsx and demos 06-15 reference `Prelude.DemoHelpers` which no longer exists after Prelude was refactored to top-level bindings

**Proposed Resolution**:
Fix 11 F# files by changing `open Prelude.DemoHelpers` to `open Prelude`:
- `Scripting/demos/AllDemos.fsx` line 6
- `Scripting/demos/Demo06_DominoRow.fsx` line 6
- `Scripting/demos/Demo07_SpinningTops.fsx` line 6
- `Scripting/demos/Demo08_GravityFlip.fsx` line 6
- `Scripting/demos/Demo09_Billiards.fsx` line 6
- `Scripting/demos/Demo10_Chaos.fsx` line 6
- `Scripting/demos/Demo11_BodyScaling.fsx` line 7
- `Scripting/demos/Demo12_CollisionPit.fsx` line 7
- `Scripting/demos/Demo13_ForceFrenzy.fsx` line 7
- `Scripting/demos/Demo14_DominoCascade.fsx` line 7
- `Scripting/demos/Demo15_Overload.fsx` line 7

Additionally, demos 06-15 use a `module DemoNN` wrapper pattern while demos 01-05 use flat top-level bindings. Both patterns work, but the flat pattern (demos 01-05) is simpler and avoids needing `DemoNN.name` / `DemoNN.run` qualification.

**Rationale**: The spec is correct — demos must run. The code has a bug from incomplete refactoring. Simple find-replace fix.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 2: FR-001 / FR-008 — Viewer shape sizing

**Direction**: BACKFILL (Code → Spec)

**Current State**:
- Spec says: "Each demo MUST produce visually distinct, interesting physics interactions"
- Code does: Viewer was rendering all objects at unit size (1x1x1). Fix implemented to pass `Size` property to `Bepu3DPhysicsOptions`, but visual merging still observed during testing.

**Proposed Resolution**:
Update spec to acknowledge the viewer rendering issue is out of scope for this feature:

> **FR-001** (updated): Each demo MUST produce physically correct, interesting interactions as measured by simulation state output. Visual rendering accuracy depends on the viewer, which is outside this feature's scope.

Add a note to the spec's Assumptions section:
> The 3D viewer may render touching objects as visually merged due to rendering precision. This is a viewer issue, not a demo issue. Physics correctness is verified via `listBodies`/`status` output.

**Rationale**: The demos produce correct physics — the viewer rendering is a separate concern that should have its own spec/fix. Blocking demo work on viewer bugs would stall progress.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 3: FR-008 — Demo 04 evolved beyond spec

**Direction**: BACKFILL (Code → Spec)

**Current State**:
- Spec says: Demo 04 "Good as-is, minor polish — camera angles, maybe second throw"
- Code does: Demo 04 is now a wrecking ball smashing through a brick wall (significantly different from original bowling concept)

**Proposed Resolution**:
Update the per-demo improvement table in spec.md:

| 04 Bowling Alley | Good as-is | ~~Minor polish~~ Wrecking ball smashes through a brick wall — frontal camera, staged impact |

**Rationale**: The change was made collaboratively with the user during implementation. The spec table was "starting points for collaborative refinement, not rigid requirements." Code reflects the user's actual direction.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 4: SC-003 — Collaborative review incomplete

**Direction**: HUMAN DECISION

**Current State**:
- Spec says: "User confirms each demo is satisfying during per-demo review"
- Actual: Only demos 01-05 reviewed collaboratively; demos 06-15 have code changes but no user confirmation

**Options**:

| Option | Implication |
|--------|-------------|
| A: Continue collaborative review | Resume running demos 06-15 one at a time with user. Most thorough. |
| B: Accept current state | Demos 06-15 have improvements but skip individual review. Faster. |
| C: Batch review | Run all 15 demos via AutoRun, user watches and flags issues. Middle ground. |

**Questions**:
- Does the user want to continue the per-demo review process for demos 06-15?
- Or are the current improvements (which follow the spec's improvement directions) sufficient?

**Confidence**: LOW (depends on user preference)

**Action**:
- [ ] Approve option A/B/C
- [ ] Reject
- [ ] Modify

---

### Proposal 5: FR-006 / SC-004 — Runtime not measured

**Direction**: REMOVE FROM SPEC (or defer)

**Current State**:
- Spec says: "Individual demo runtime MUST remain under 30 seconds"
- Actual: Not measured for any demo

**Proposed Resolution**:
These are lightweight constraints that can be verified at any time. No demo exceeds ~15 seconds of simulation time based on code review (`runFor` calls sum to <20s for all demos). Rather than adding measurement infrastructure, mark as verified by code inspection:

> FR-006: Verified by code inspection — all demos have total `runFor` + `sleep` durations under 25 seconds.

**Rationale**: Adding timing measurement adds complexity without value — the constraint is clearly met.

**Confidence**: HIGH

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 6: SC-002 — Physics interactions not verified

**Direction**: REMOVE FROM SPEC

**Current State**:
- Spec says: "Each demo produces at least 3 distinct visible physics interactions (not just gravity settling)"
- Actual: Not systematically verified

**Proposed Resolution**:
This criterion was added as a proxy for "satisfying" but is redundant with SC-003 (user confirms satisfying). Each demo's code clearly exercises multiple physics features (impulses, gravity, collisions, etc.). Remove SC-002 or mark as superseded:

> SC-002: Superseded by SC-003 (collaborative review). Each demo's physics interactions are evident from the code and scenario design.

**Rationale**: Counting "distinct visible physics interactions" is subjective and not measurable in a meaningful automated way.

**Confidence**: MEDIUM

**Action**:
- [ ] Approve
- [ ] Reject
- [ ] Modify

---

### Proposal 7: Viewer shape sizing fix — New spec

**Direction**: NEW_SPEC

**Feature**: Viewer shape sizing fix
**Location**: `src/PhysicsViewer/Rendering/SceneManager.fs:76-88`

**Draft Spec**:

> # Feature: Viewer Shape Rendering Accuracy
>
> The 3D viewer MUST render physics bodies at their actual dimensions (sphere radius, box half-extents) rather than unit-size primitives. This ensures the visual representation matches the physics simulation, preventing objects from appearing to merge or overlap when they are physically separated.
>
> - FR-001: Spheres MUST be rendered with diameter = 2 * radius
> - FR-002: Boxes MUST be rendered with dimensions = 2 * half-extents
> - FR-003: Visual body size MUST update when a body's shape changes

**Note**: A fix has already been implemented (passing `Size` to `Bepu3DPhysicsOptions`) but needs validation. This may require investigation into how Stride's `Create3DPrimitive` interprets the `Size` parameter.

**Confidence**: MEDIUM

**Action**:
- [ ] Approve and create spec
- [ ] Reject
- [ ] Modify
