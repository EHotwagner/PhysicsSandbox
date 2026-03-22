"""Interactive demo runner — step through demos with keypress advancement.

Usage: python -m Scripting.demos_py.run_all [server-address]
"""

import sys

from Scripting.demos_py.all_demos import demos
from Scripting.demos_py.prelude import connect, disconnect, reset_simulation, sleep


def wait_for_key():
    """Wait for Enter or Space keypress."""
    try:
        input("  Press Enter to continue...")
    except EOFError:
        pass


def main():
    addr = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5000"

    print()
    print("╔══════════════════════════════════════════════╗")
    print("║       PhysicsSandbox Demo Runner             ║")
    print(f"║  {len(demos)} demos • Press Enter to advance          ║")
    print("╚══════════════════════════════════════════════╝")
    print()
    print(f"Connecting to {addr}...")
    session = connect(addr)
    print("Connected!")
    print()

    for i, (name, desc, run_fn) in enumerate(demos):
        print("━" * 46)
        print(f"  Demo {i + 1}/{len(demos)}: {name}")
        print(f"  {desc}")
        print("━" * 46)
        print()
        wait_for_key()
        print()
        try:
            run_fn(session)
        except Exception as ex:
            print(f"  [ERROR] {ex}")
        print()
        if i < len(demos) - 1:
            print("  Done. Press Enter for next demo...")
            wait_for_key()
        else:
            print("  All demos complete!")

    print()
    print("Cleaning up...")
    reset_simulation(session)
    disconnect(session)
    print("Disconnected. Goodbye!")


if __name__ == "__main__":
    main()
