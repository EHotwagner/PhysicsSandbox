# Feature Specification: Refactor Evaluation Analysis

**Feature Branch**: `005-refactor-evaluation`
**Created**: 2026-03-23
**Status**: Draft
**Input**: User description: "Analyse each project for unnecessary complexity/spaghetti code and the whole solution. Evaluate refactoring vs rewrite alternatives. Produce refactorEvaluation.md report."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developer Reviews Codebase Health (Priority: P1)

A developer or technical lead wants to understand the current state of code quality across all projects in the PhysicsSandbox solution, identifying areas of unnecessary complexity, duplication, and architectural issues, so they can make informed decisions about where to invest refactoring effort.

**Why this priority**: Without understanding current code quality, any refactoring or rewrite decision is uninformed. This is the foundational deliverable.

**Independent Test**: Can be fully tested by reviewing the produced report against the actual codebase and verifying that identified issues are accurate and actionable.

**Acceptance Scenarios**:

1. **Given** the complete PhysicsSandbox solution, **When** the evaluation is performed, **Then** each project receives a quality rating with specific justifications and identified issues
2. **Given** identified code issues, **When** the report is reviewed, **Then** each issue includes location, severity, and a concrete improvement suggestion

---

### User Story 2 - Decision Maker Evaluates Alternatives (Priority: P2)

A decision maker wants to compare refactoring the existing codebase versus rewriting parts or all of it, with clear trade-off analysis, so they can choose the most cost-effective path forward.

**Why this priority**: The evaluation is only useful if it leads to actionable decisions about what to do next.

**Independent Test**: Can be tested by reviewing the alternatives section and verifying that each option has clear pros, cons, effort estimates, and risk assessments.

**Acceptance Scenarios**:

1. **Given** the quality analysis of all projects, **When** alternatives are presented, **Then** at least 3 distinct approaches are described with trade-offs
2. **Given** multiple alternatives, **When** a recommendation is made, **Then** it is supported by evidence from the per-project analysis

---

### User Story 3 - Team Prioritizes Refactoring Work (Priority: P3)

A development team wants a prioritized list of refactoring targets ranked by impact and effort, so they can plan incremental improvements without disrupting ongoing feature work.

**Why this priority**: Even if a full rewrite is not chosen, targeted refactoring delivers immediate value.

**Independent Test**: Can be tested by verifying the priority list includes effort estimates and expected quality improvements for each item.

**Acceptance Scenarios**:

1. **Given** the full evaluation report, **When** the team reviews priorities, **Then** each refactoring target has a clear ROI assessment (impact vs effort)

---

### Edge Cases

- What happens when a project has no significant issues? Report it as healthy with a high rating.
- How does the report handle projects with unavoidable complexity (e.g., Stride3D interop)? Distinguish between accidental and essential complexity.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Report MUST analyse every source project in the solution (9 projects + tests + scripting)
- **FR-002**: Each project MUST receive a numeric quality rating (1-10) with justification
- **FR-003**: Report MUST identify specific code duplication, spaghetti patterns, tight coupling, and unnecessary complexity per project
- **FR-004**: Report MUST provide solution-level architectural analysis (cross-project concerns, dependency topology)
- **FR-005**: Report MUST describe and evaluate at least 3 alternative approaches (targeted refactor, major refactor, full rewrite)
- **FR-006**: Report MUST include a prioritized refactoring roadmap with effort/impact assessment
- **FR-007**: Report MUST be delivered as `reports/refactorEvaluation.md` in the repository
- **FR-008**: Report MUST include line-of-code metrics per project for quantitative comparison

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 9 source projects + test suite + scripting layer analysed with individual quality ratings
- **SC-002**: At least 3 alternative approaches described with pros, cons, and trade-offs
- **SC-003**: Prioritized refactoring roadmap with at least 10 specific, actionable items
- **SC-004**: Report is self-contained and readable without additional context
