# Data Model: 005-enhance-demos

This feature does not introduce new persistent data entities. All changes are to demo scripts (.fsx/.py) that create ephemeral physics bodies during execution.

## Entities (Existing, Unchanged)

### Demo
- **Name**: string — display name shown in AutoRun output
- **Description**: string — one-line summary of what the demo shows
- **Run**: function(Session) → unit — the demo execution logic
- **Registry**: AllDemos.fsx / all_demos.py — ordered array of Demo records

### Prelude Helpers (Extended)
New helper functions added to Prelude.fsx / prelude.py:

| Function | Parameters | Returns | Proto Type |
|----------|-----------|---------|-----------|
| `makeTriangleCmd` | id, pos, (a, b, c), mass | SimulationCommand | Triangle shape in AddBody |
| `makeConvexHullCmd` | id, pos, points list, mass | SimulationCommand | ConvexHull shape in AddBody |
| `makeCompoundCmd` | id, pos, children list, mass | SimulationCommand | Compound shape in AddBody |
| `makeKinematicCmd` | id, pos, shapeCmd | SimulationCommand | AddBody with KINEMATIC motion type |
| `withMotionType` | motionType, cmd | SimulationCommand | Sets BodyMotionType on AddBody |
| `withCollisionFilter` | group, mask, cmd | SimulationCommand | Sets collision fields on AddBody |

### Color Palette (New Constants)
8 named color constants in Prelude for consistent visual coding across demos.

### Demo Count
- Before: 15 demos (F#) + 15 demos (Python)
- After: 18 demos (F#) + 18 demos (Python)
- New: #16 Constraints, #17 Query Range, #18 Kinematic Sweep
