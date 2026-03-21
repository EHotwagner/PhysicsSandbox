"""Demo 02 — Bouncing Marbles: Five marbles dropped from different heights."""

from demos_py.prelude import (
    batch_add,
    list_bodies,
    make_sphere_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
)

name = "Bouncing Marbles"
description = "Five marbles dropped from different heights."


def run(session):
    reset_simulation(session)
    set_camera(session, (4.0, 5.0, 4.0), (0.0, 0.5, 0.0))
    cmds = []
    for i in range(5):
        x = i * 0.3 - 0.6
        y = 3.0 + i * 2.0
        cmds.append(make_sphere_cmd(next_id("sphere"), (x, y, 0.0), 0.01, 0.005))
    batch_add(session, cmds)
    print("  Dropping 5 marbles from 3m to 11m...")
    run_for(session, 4.0)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
