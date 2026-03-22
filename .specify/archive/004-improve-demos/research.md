# Research: Improve Physics Demos

**Date**: 2026-03-22

## Research Summary

No significant unknowns. This feature modifies existing demo scripts using existing APIs and capabilities.

## Decisions

### D1: Improvement approach — collaborative per-demo iteration

**Decision**: Work through each demo collaboratively (review → improve → mirror → confirm) rather than batch-specifying all changes upfront.

**Rationale**: "Satisfying" is subjective. The user wants to be involved in each demo's improvement. Batch specification would produce guesses about what the user finds satisfying.

**Alternatives considered**: Full upfront specification of every change (rejected — too prescriptive for a subjective quality goal).

### D2: Structural fixes first, then demo content

**Decision**: Fix AllDemos integration and AutoRun deduplication before improving individual demo content.

**Rationale**: Structural fixes provide the unified runner needed to validate all demos. Demo content changes are easier to test once all 15 demos run through AllDemos.

**Alternatives considered**: Interleaving structural and content fixes (rejected — creates merge complexity and harder validation).

### D3: No Prelude or server changes

**Decision**: All improvements use existing Prelude helpers, presets, generators, and steering functions. No new server-side capabilities.

**Rationale**: The existing API surface (7 body presets, 5 generators, push/launch steering, camera/wireframe view commands, gravity control) provides sufficient variety for dramatic demos. The bottleneck is creative composition, not missing features.

**Alternatives considered**: Adding new Prelude helpers (rejected — unnecessary complexity; existing helpers cover all needed scenarios).

### D4: Demo ordering — thin demos first

**Decision**: Start with the thinnest demos (01, 02) and progress to moderate then polish-only demos.

**Rationale**: Thin demos have the highest improvement potential per unit effort. Working through them first builds momentum and establishes the quality bar for the rest.

**Alternatives considered**: Working in numeric order (acceptable but less impactful early on), working by category (rejected — less natural flow).
