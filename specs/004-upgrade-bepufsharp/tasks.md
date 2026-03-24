# Tasks: Upgrade BepuFSharp

## Phase 1: Setup

- [x] T001 [US1] Update BepuFSharp version in src/PhysicsSimulation/PhysicsSimulation.fsproj from 0.2.0-beta.1 to 0.3.0

## Phase 2: Validation

- [x] T002 [US1] Build the full solution: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — must succeed with zero errors
- [x] T003 [US1] Run all tests: `dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true` — all existing tests must pass

## Phase 3: Documentation

- [x] T004 [US2] Update BepuFSharp version references in CLAUDE.md from 0.2.0-beta.1 to 0.3.0
- [x] T005 [US2] Update BepuFSharp version references in .specify/memory/plan.md from 0.2.0-beta.1 to 0.3.0
