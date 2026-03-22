"""Automated demo runner — runs all 15 demos with pass/fail summary.

Usage: python -m Scripting.demos_py.auto_run [server-address]
"""

import sys

from Scripting.demos_py.all_demos import demos
from Scripting.demos_py.prelude import connect, disconnect, reset_simulation, sleep


def main():
    addr = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5000"

    print()
    print("============================================")
    print(f"  PhysicsSandbox Demo Runner — {len(demos)} demos")
    print("============================================")
    print()
    print(f"Connecting to {addr}...")
    session = connect(addr)
    print("Connected!")
    print()

    passed = 0
    failed = 0

    for i, (name, desc, run_fn) in enumerate(demos):
        print("━" * 46)
        print(f"  Demo {i + 1}/{len(demos)}: {name}")
        print(f"  {desc}")
        print("━" * 46)
        print()
        try:
            run_fn(session)
            passed += 1
            print("\n  ✓ Complete")
        except Exception as ex:
            failed += 1
            print(f"\n  ✗ FAILED: {ex}")
        print()
        sleep(1000)

    print("============================================")
    print(f"  Results: {passed} passed, {failed} failed")
    print("============================================")
    print()

    reset_simulation(session)
    disconnect(session)
    print("Done!")


if __name__ == "__main__":
    main()
