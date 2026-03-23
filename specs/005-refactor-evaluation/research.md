# Research: Refactor Evaluation Analysis

**Feature**: 005-refactor-evaluation | **Date**: 2026-03-23

## Research Questions & Findings

### RQ-1: What analysis methodology produces actionable refactoring recommendations?

**Decision**: Per-project code review with quantitative metrics (LOC, duplication %, quality rating) combined with cross-cutting architectural analysis and alternative comparison.

**Rationale**: Pure metrics (cyclomatic complexity, coupling metrics) miss architectural issues. Pure code review misses patterns across projects. Combining both — quantitative LOC/duplication metrics with qualitative module-by-module review — produces findings that are both measurable and actionable.

**Alternatives considered**:
- Static analysis tooling only (SonarQube, FSharpLint) — rejected because F# tooling is limited and doesn't understand domain-specific patterns like proto type aliasing or Stride interop constraints
- Architecture Decision Records only — rejected because ADRs document future decisions, not current state assessment
- Informal code walkthrough — rejected because lacks reproducible metrics and prioritization framework

### RQ-2: How to distinguish essential vs accidental complexity?

**Decision**: Essential complexity is inherent in the problem domain (Stride3D mutable game loop, BepuPhysics2 type constraints, proto null semantics). Accidental complexity is complexity not required by the domain (code duplication, god objects, imperative patterns in functional code).

**Rationale**: This distinction prevents false positives. The PhysicsViewer's 12 mutable bindings in Program.fs are essential complexity (Stride's architecture requires it). The PhysicsServer's MessageRouter god object is accidental complexity (nothing in the domain requires mixing subscriptions, commands, queries, and metrics in one type).

**Alternatives considered**:
- Treat all complexity equally — rejected because it would flag unavoidable Stride/BepuPhysics patterns as issues
- Only flag accidental complexity — adopted as the primary approach, with essential complexity documented but not penalized

### RQ-3: What quality rating scale produces useful comparisons?

**Decision**: 1-10 numeric scale with explicit criteria per band:
- 9-10: Excellent — minimal issues, well-documented, highly testable
- 7-8: Good — some issues but sound architecture, production-ready
- 5-6: Needs attention — significant issues affecting maintainability
- 3-4: Poor — systemic issues requiring major intervention
- 1-2: Critical — fundamental design flaws

**Rationale**: Numeric ratings enable direct comparison across projects and tracking over time. The bands provide qualitative interpretation.

### RQ-4: What is the threshold for recommending rewrite vs refactor?

**Decision**: Rewrite is justified only when architecture is fundamentally flawed (rating < 4) OR when the cost of incremental refactoring exceeds the cost of rewrite. At 7.4/10 average with sound architecture, targeted refactoring is clearly superior.

**Rationale**: Industry experience (Joel Spolsky's "Things You Should Never Do", Martin Fowler's refactoring guidance) consistently shows that rewrites of working systems are underestimated by 2-3x and lose embedded domain knowledge. The PhysicsSandbox solution has a sound architecture with localized implementation issues — the textbook case for refactoring over rewriting.

### RQ-5: What are the cross-project duplication patterns?

**Decision**: Six major duplication patterns identified totaling ~645 LOC (~7.5% of codebase):
1. Vector conversions (50 LOC across 5 locations)
2. Shape construction (200 LOC across 3 projects)
3. Stream reconnection (120 LOC across 4 projects)
4. Body add boilerplate (180 LOC across 2 projects)
5. Test helpers (55 LOC across 12 test files)
6. Proto type aliases (40 LOC across 5+ files)

**Rationale**: The 5-10% industry threshold for duplication concern is approached but not exceeded. Items 1-4 are addressable through targeted refactoring (Alternative A). Items 5-6 require cross-project changes (Alternative B level).

### RQ-6: What is the BepuFSharp wrapper's refactoring potential?

**Decision**: BepuFSharp is not a refactoring target. It is an external dependency (0.2.0-beta.1) with comprehensive coverage (10 shapes, 10 constraints) and stable API.

**Rationale**: The friction BepuFSharp creates (proto type name conflicts requiring aliases) is a naming issue best solved by shared type alias modules, not by rewriting the wrapper. The wrapper's API surface matches the physics domain well and is unlikely to change significantly.

## Unresolved Items

None. All research questions have been resolved with clear decisions and rationale.
