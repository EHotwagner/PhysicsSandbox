Merge the current feature branch into main, archive its spec folder, repack affected NuGet packages, and delete the feature branch.

## Steps

1. **Detect the current feature branch.** If on `main`, stop and ask which branch to merge.

2. **Pre-flight checks:**
   - Run `git status` to check the working tree.
   - If dirty, commit all changes with a descriptive message summarizing the feature work. Stage specific files (not `git add -A`). Use the repo's conventional commit style.
   - Run `git log main..HEAD --oneline` to show what will be merged.

3. **Detect NuGet repack needs:**
   - Run `git diff main..HEAD --name-only` to list all changed files.
   - For each packable project (`src/PhysicsClient`, `src/PhysicsSandbox.Shared.Contracts`, `src/PhysicsSandbox.Scripting`, `src/PhysicsSandbox.ServiceDefaults`, `src/PhysicsSandbox.Mcp`), check if any of its source files were modified.
   - A project needs repacking if:
     - Any `.fs`, `.fsi`, `.cs`, `.proto`, or `.fsproj`/`.csproj` file under that project directory was changed.
   - Also repack downstream dependents: if Contracts changed, repack PhysicsClient too. If PhysicsClient changed, consider repacking Scripting.
   - For each project that needs repacking, determine if a version bump is needed:
     - **Public API changes** (`.fsi` signature file modified, new public types/functions): bump minor version (e.g., 0.6.0 → 0.7.0).
     - **Bug fixes or internal changes only**: bump patch version (e.g., 0.6.0 → 0.6.1).
     - **No functional changes** (only comments, formatting): no version bump, no repack needed.

4. **Confirm with the user** before proceeding. Show:
   - Branch name
   - Number of commits to merge
   - Whether a matching spec directory exists in `specs/`
   - Which NuGet packages will be repacked (with version bumps), or "No NuGet repack needed"

5. **Merge into main:**
   ```bash
   git checkout main
   git merge --no-ff <branch-name>
   ```

6. **Repack NuGet packages** (if any were identified in step 3):
   - Bump the `<Version>` in each affected `.fsproj`/`.csproj`.
   - Pack in dependency order (Contracts → PhysicsClient → Scripting → others):
     ```bash
     dotnet pack src/<project> -c Release -o ~/.local/share/nuget-local/
     ```
   - Update version pins in `Scripting/demos/Prelude.fsx` and `CLAUDE.md` if PhysicsClient version changed.
   - Commit the version bumps and pin updates.

7. **Archive the spec folder** (if `specs/<branch-name>/` exists):
   ```bash
   mkdir -p .specify/archive
   mv specs/<branch-name> .specify/archive/<branch-name>
   ```

8. **Delete the feature branch:**
   ```bash
   git branch -d <branch-name>
   ```

9. **Push to GitHub:**
   ```bash
   git push origin main
   ```

10. **Report** what was done: merge commit, archived spec path, NuGet packages repacked (with versions), deleted branch, push status.

**Important:** Do NOT force-delete branches (`-D`). Use `-d` which only works if the branch is fully merged.
