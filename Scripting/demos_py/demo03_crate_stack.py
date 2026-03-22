"""Demo 03 — Crate Stack: A tower of 8 crates -- push the top one off."""

from Scripting.demos_py.prelude import (
    Direction,
    list_bodies,
    push,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    stack,
)

name = "Crate Stack"
description = "A tower of 8 crates — push the top one off."


def run(session):
    reset_simulation(session)
    set_camera(session, (6.0, 5.0, 0.0), (0.0, 4.0, 0.0))
    ids = stack(session, 8, pos=(0.0, 0.0, 0.0))
    print(f"  Built stack of {len(ids)} crates")
    run_for(session, 2.0)
    top_id = ids[-1]
    print("  Pushing top crate east...")
    push(session, top_id, Direction.East, 15.0)
    run_for(session, 3.0)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
