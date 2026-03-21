"""Demo 12 — Collision Pit: 120 spheres dropped into a walled pit."""

from demos_py.prelude import (
    batch_add,
    make_box_cmd,
    make_sphere_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    sleep,
    status,
    timed,
)

name = "Collision Pit"
description = "120 spheres dropped into a walled pit — maximum collision density."


def run(session):
    reset_simulation(session)
    set_camera(session, (8.0, 10.0, 8.0), (0.0, 2.0, 0.0))
    with timed("Pit walls setup"):
        wall_cmds = [
            make_box_cmd(next_id("box"), (0.0, 2.0, -2.1), (2.0, 2.0, 0.1), 0.0),
            make_box_cmd(next_id("box"), (0.0, 2.0, 2.1), (2.0, 2.0, 0.1), 0.0),
            make_box_cmd(next_id("box"), (-2.1, 2.0, 0.0), (0.1, 2.0, 2.0), 0.0),
            make_box_cmd(next_id("box"), (2.1, 2.0, 0.0), (0.1, 2.0, 2.0), 0.0),
        ]
        batch_add(session, wall_cmds)
    print("  Pit built (4x4m walled enclosure)")
    with timed("Drop 120 spheres"):
        sphere_cmds = []
        for i in range(120):
            x = (i % 10) * 0.35 - 1.6
            z = ((i // 10) % 12) * 0.35 - 1.9
            y = 6.0 + (i // 10) * 0.5
            sphere_cmds.append(make_sphere_cmd(next_id("sphere"), (x, y, z), 0.15, 0.5))
        batch_add(session, sphere_cmds)
    print("  120 spheres dropping into pit...")
    with timed("Settling simulation (8s)"):
        run_for(session, 8.0)
    set_camera(session, (4.0, 3.0, 4.0), (0.0, 1.5, 0.0))
    print("  Close-up view of settled pit")
    sleep(1000)
    status(session)


if __name__ == "__main__":
    run_standalone(run, name)
