Merge the current feature branch into main, archive its spec folder, and delete the feature branch.

## Steps

1. **Detect the current feature branch.** If on `main`, stop and ask which branch to merge.

2. **Pre-flight checks:**
   - Run `git status` to verify the working tree is clean. If dirty, stop and ask the user to commit or stash first.
   - Run `git log main..HEAD --oneline` to show what will be merged.

3. **Confirm with the user** before proceeding. Show:
   - Branch name
   - Number of commits to merge
   - Whether a matching spec directory exists in `specs/`

4. **Merge into main:**
   ```bash
   git checkout main
   git merge --no-ff <branch-name>
   ```

5. **Archive the spec folder** (if `specs/<branch-name>/` exists):
   ```bash
   mkdir -p .specify/archive
   mv specs/<branch-name> .specify/archive/<branch-name>
   ```

6. **Delete the feature branch:**
   ```bash
   git branch -d <branch-name>
   ```

7. **Report** what was done: merge commit, archived spec path, deleted branch.

**Important:** Do NOT force-delete branches (`-D`). Use `-d` which only works if the branch is fully merged. Do NOT push to remote unless the user explicitly asks.
