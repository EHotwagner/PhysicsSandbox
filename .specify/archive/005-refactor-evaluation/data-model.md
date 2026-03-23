# Data Model: Refactor Evaluation Analysis

**Feature**: 005-refactor-evaluation | **Date**: 2026-03-23

## Quality Assessment Model

### Entity: ProjectAnalysis

Represents the evaluation of a single project within the solution.

| Field | Type | Description |
|-------|------|-------------|
| projectName | string | Name of the project (e.g., "PhysicsServer") |
| linesOfCode | int | Total hand-written LOC (excluding generated) |
| qualityRating | decimal (1-10) | Overall quality score |
| verdict | string | One-line summary of findings |
| issues | Issue[] | List of identified code quality issues |
| refactorEstimate | string | Estimated effort to address issues |

### Entity: Issue

Represents a specific code quality problem within a project.

| Field | Type | Description |
|-------|------|-------------|
| description | string | What the issue is |
| severity | enum(High, Medium, Low) | Impact on maintainability/correctness |
| locImpact | int? | Lines of code affected (if quantifiable) |
| location | string | File path and line numbers |
| suggestion | string | Concrete improvement recommendation |

### Entity: Alternative

Represents a possible approach to addressing identified issues.

| Field | Type | Description |
|-------|------|-------------|
| name | string | Alternative identifier (A, B, C, D) |
| scope | string | What this alternative changes |
| estimatedEffort | string | Days/weeks of work |
| qualityImprovement | string | Expected rating change |
| risk | enum(Low, Medium, High) | Risk level |
| pros | string[] | Advantages |
| cons | string[] | Disadvantages |

### Entity: RefactorItem

Represents a single prioritized refactoring target.

| Field | Type | Description |
|-------|------|-------------|
| priority | int (1-13) | Execution order |
| target | string | What to refactor |
| impact | enum(High, Medium, Low) | Expected quality improvement |
| effort | string | Estimated time |
| roi | string | Return on investment justification |

## Relationships

```
Solution 1──* ProjectAnalysis
ProjectAnalysis 1──* Issue
Solution 1──* Alternative
Alternative 1──* RefactorItem (recommended alternative contains all items)
```

## State Transitions

This feature produces a static report. No state transitions apply.

## Validation Rules

- Quality ratings must be in range [1.0, 10.0]
- Every High-severity issue must have a corresponding RefactorItem in the roadmap
- Every Alternative must have at least 1 pro and 1 con
- RefactorItems must be ordered by descending ROI
