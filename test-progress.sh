#!/usr/bin/env bash
#
# test-progress.sh — Run all test projects with progress reporting and ETA
#
# Usage:
#   ./test-progress.sh              Run all test projects discovered from PhysicsSandbox.slnx
#   ./test-progress.sh --help       Show this help message
#
# Description:
#   Parses PhysicsSandbox.slnx to discover test projects under the /tests/ folder,
#   builds the solution once, then runs each test project individually with progress
#   updates showing pass/fail counts, elapsed time, and ETA for remaining projects.
#
# Requirements:
#   - dotnet SDK (10.0+)
#   - PhysicsSandbox.slnx in the same directory as this script
#
# Exit codes:
#   0 — All tests passed
#   1 — One or more tests failed or build failure
#   2 — Missing .slnx or other setup error
#

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SLNX_FILE="$SCRIPT_DIR/PhysicsSandbox.slnx"

# ---------------------------------------------------------------------------
# Help
# ---------------------------------------------------------------------------
if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
    sed -n '2,/^$/{ s/^# \?//; p }' "$0"
    exit 0
fi

# ---------------------------------------------------------------------------
# T009: Validate .slnx exists
# ---------------------------------------------------------------------------
if [[ ! -f "$SLNX_FILE" ]]; then
    echo "ERROR: Solution file not found: $SLNX_FILE" >&2
    exit 2
fi

# ---------------------------------------------------------------------------
# T003: Parse test project paths from .slnx
# ---------------------------------------------------------------------------
# Extract <Project Path="..."> entries inside <Folder Name="/tests/"> block
in_tests=false
test_projects=()
while IFS= read -r line; do
    if [[ "$line" =~ \<Folder\ Name=\"/tests/\" ]]; then
        in_tests=true
        continue
    fi
    if $in_tests; then
        if [[ "$line" =~ \</Folder\> ]]; then
            break
        fi
        if [[ "$line" =~ Path=\"([^\"]+)\" ]]; then
            test_projects+=("${BASH_REMATCH[1]}")
        fi
    fi
done < "$SLNX_FILE"

total_projects=${#test_projects[@]}

if [[ $total_projects -eq 0 ]]; then
    echo "ERROR: No test projects found in $SLNX_FILE" >&2
    exit 2
fi

echo "Discovered $total_projects test projects from PhysicsSandbox.slnx"
echo ""

# ---------------------------------------------------------------------------
# T004: Build the solution upfront
# ---------------------------------------------------------------------------
echo "Building solution..."
build_start=$SECONDS
if ! dotnet build "$SLNX_FILE" -p:StrideCompilerSkipBuild=true --verbosity quiet 2>&1; then
    echo ""
    echo "ERROR: Solution build failed. Fix build errors before running tests." >&2
    exit 1
fi
build_elapsed=$(( SECONDS - build_start ))
echo "Build succeeded (${build_elapsed}s)"
echo ""

# ---------------------------------------------------------------------------
# Counters
# ---------------------------------------------------------------------------
total_passed=0
total_failed=0
total_skipped=0
projects_completed=0
projects_with_failures=0
cumulative_test_time=0
overall_start=$SECONDS
failed_project_details=()

# ---------------------------------------------------------------------------
# T005/T006/T007: Run each test project with progress
# ---------------------------------------------------------------------------
for project_path in "${test_projects[@]}"; do
    projects_completed=$((projects_completed + 1))
    project_name=$(basename "$(dirname "$project_path")")

    project_start=$SECONDS

    # Run tests, capture output; don't let set -e kill us on test failure
    test_output=""
    test_exit=0
    test_output=$(dotnet test "$SCRIPT_DIR/$project_path" \
        -p:StrideCompilerSkipBuild=true \
        --no-build \
        -v normal 2>&1) || test_exit=$?

    project_elapsed=$(( SECONDS - project_start ))
    cumulative_test_time=$((cumulative_test_time + project_elapsed))

    # Parse summary line: "  Passed: X  Failed: Y  Skipped: Z"
    # Also handle "Passed!" with no counts (all passed, zero failed/skipped)
    passed=0
    failed=0
    skipped=0
    has_summary=false

    # Try the detailed summary line first (has explicit counts)
    summary_line=$(echo "$test_output" | grep -E '(Passed|Failed|Skipped)\s*[:!]' | tail -1 || true)

    if [[ -n "$summary_line" ]]; then
        p=$(echo "$summary_line" | sed -n 's/.*Passed[: ]*\([0-9]\+\).*/\1/p')
        f=$(echo "$summary_line" | sed -n 's/.*Failed[: ]*\([0-9]\+\).*/\1/p')
        s=$(echo "$summary_line" | sed -n 's/.*Skipped[: ]*\([0-9]\+\).*/\1/p')
        [[ -n "$p" ]] && passed=$p
        [[ -n "$f" ]] && failed=$f
        [[ -n "$s" ]] && skipped=$s
        has_summary=true
    fi

    # Also check for total test count from "Total tests: N" line
    if ! $has_summary; then
        total_line=$(echo "$test_output" | grep -E 'Total tests:' | tail -1 || true)
        if [[ -n "$total_line" ]]; then
            t=$(echo "$total_line" | sed -n 's/.*Total tests: *\([0-9]\+\).*/\1/p')
            if [[ -n "$t" && $test_exit -eq 0 ]]; then
                passed=$t
                has_summary=true
            fi
        fi
    fi

    # Update totals
    total_passed=$((total_passed + passed))
    total_failed=$((total_failed + failed))
    total_skipped=$((total_skipped + skipped))

    # ETA calculation
    remaining=$((total_projects - projects_completed))
    if [[ $projects_completed -gt 0 ]]; then
        avg_time=$((cumulative_test_time / projects_completed))
        eta=$((avg_time * remaining))
    else
        eta=0
    fi

    # Display progress
    if [[ $failed -gt 0 || ($test_exit -ne 0 && $has_summary == false) ]]; then
        projects_with_failures=$((projects_with_failures + 1))
        if $has_summary; then
            echo "[$projects_completed/$total_projects] $project_name \u2717 $failed failed, $passed passed (${project_elapsed}s)"
        else
            # T009: zero-test project or unparseable output with nonzero exit
            echo "[$projects_completed/$total_projects] $project_name \u2717 test run failed (${project_elapsed}s)"
        fi
        # Show immediate failure details
        echo "--- Failure details for $project_name ---"
        echo "$test_output" | grep -A 3 -E '(Failed |Error Message:|Stack Trace:|FAILED)' | head -40 || true
        echo "---"
        failed_project_details+=("$project_name")
    elif $has_summary; then
        status="$passed passed"
        [[ $skipped -gt 0 ]] && status="$status, $skipped skipped"
        if [[ $remaining -gt 0 ]]; then
            echo "[$projects_completed/$total_projects] $project_name \u2713 $status (${project_elapsed}s) | ETA: ~${eta}s"
        else
            echo "[$projects_completed/$total_projects] $project_name \u2713 $status (${project_elapsed}s)"
        fi
    else
        # T009: zero-test project (no summary, but exit 0)
        if [[ $remaining -gt 0 ]]; then
            echo "[$projects_completed/$total_projects] $project_name \u2713 0 tests (${project_elapsed}s) | ETA: ~${eta}s"
        else
            echo "[$projects_completed/$total_projects] $project_name \u2713 0 tests (${project_elapsed}s)"
        fi
    fi
done

# ---------------------------------------------------------------------------
# T008: Final summary
# ---------------------------------------------------------------------------
wall_time=$(( SECONDS - overall_start ))
echo ""
echo "========================================"
echo " Test Summary"
echo "========================================"
echo " Passed:  $total_passed"
echo " Failed:  $total_failed"
echo " Skipped: $total_skipped"
echo " Total:   $((total_passed + total_failed + total_skipped))"
echo ""
echo " Projects: $total_projects ($projects_with_failures with failures)"
echo " Wall time: ${wall_time}s (build: ${build_elapsed}s, tests: ${cumulative_test_time}s)"
echo "========================================"

if [[ ${#failed_project_details[@]} -gt 0 ]]; then
    echo ""
    echo "Failed projects:"
    for name in "${failed_project_details[@]}"; do
        echo "  - $name"
    done
fi

if [[ $total_failed -gt 0 || $projects_with_failures -gt 0 ]]; then
    exit 1
fi

exit 0
