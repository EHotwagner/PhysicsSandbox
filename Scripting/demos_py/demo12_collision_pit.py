"""Demo 12 — Collision Pit: Three waves of varied spheres dropped into a walled pit."""

from random import Random

from Scripting.demos_py.prelude import (
    batch_add,
    clear_narration,
    make_box_cmd,
    make_sphere_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_demo_info,
    set_narration,
    sleep,
    smooth_camera,
    status,
    timed,
)

name = "Collision Pit"
description = "Three waves of varied spheres dropped into a walled pit — maximum collision density."


def run(session):
    reset_simulation(session)
    set_narration(session, "Building the collision pit")
    smooth_camera(session, (8.0, 10.0, 8.0), (0.0, 2.0, 0.0), 1.5)
    sleep(1700)
    set_demo_info(session, "Demo 12: Collision Pit", "Collision detection pit.")

    # Build the pit
    with timed("Pit walls setup"):
        wall_cmds = [
            make_box_cmd(next_id("box"), (0.0, 2.0, -2.1), (2.0, 2.0, 0.1), 0.0),
            make_box_cmd(next_id("box"), (0.0, 2.0, 2.1), (2.0, 2.0, 0.1), 0.0),
            make_box_cmd(next_id("box"), (-2.1, 2.0, 0.0), (0.1, 2.0, 2.0), 0.0),
            make_box_cmd(next_id("box"), (2.1, 2.0, 0.0), (0.1, 2.0, 2.0), 0.0),
        ]
        batch_add(session, wall_cmds)
    print("  Pit built (4x4m walled enclosure)")

    rng = Random(55)

    # Wave 1: 40 large spheres
    with timed("Wave 1 — 40 large spheres"):
        wave1 = []
        for i in range(40):
            x = (i % 8) * 0.45 - 1.6
            z = (i // 8) * 0.45 - 0.9
            y = 6.0 + rng.random() * 2.0
            wave1.append(make_sphere_cmd(next_id("sphere"), (x, y, z), 0.15 + rng.random() * 0.1, 0.3 + rng.random() * 1.0))
        batch_add(session, wave1)
    print("  Wave 1: 40 large spheres dropping...")
    run_for(session, 3.0)

    # Wave 2: 60 small marbles
    set_narration(session, "Wave 2 — 60 small marbles raining down")
    smooth_camera(session, (5.0, 8.0, 5.0), (0.0, 3.0, 0.0), 1.5)
    sleep(1700)
    with timed("Wave 2 — 60 small marbles"):
        wave2 = []
        for _ in range(60):
            x = rng.random() * 3.2 - 1.6
            z = rng.random() * 3.2 - 1.6
            y = 10.0 + rng.random() * 4.0
            wave2.append(make_sphere_cmd(next_id("sphere"), (x, y, z), 0.06 + rng.random() * 0.06, 0.1 + rng.random() * 0.3))
        batch_add(session, wave2)
    print("  Wave 2: 60 small marbles into the pile!")
    run_for(session, 4.0)

    # Wave 3: 20 heavy spheres
    set_narration(session, "Wave 3 — 20 heavy spheres incoming!")
    with timed("Wave 3 — 20 heavy spheres"):
        wave3 = []
        for _ in range(20):
            x = rng.random() * 2.4 - 1.2
            z = rng.random() * 2.4 - 1.2
            y = 12.0 + rng.random() * 3.0
            wave3.append(make_sphere_cmd(next_id("sphere"), (x, y, z), 0.2, 3.0))
        batch_add(session, wave3)
    print("  Wave 3: 20 heavy spheres — IMPACT!")
    run_for(session, 4.0)

    # Close-up view
    set_narration(session, "Close-up of the overflowing pit")
    smooth_camera(session, (3.0, 2.0, 3.0), (0.0, 1.5, 0.0), 1.5)
    sleep(1700)
    print("  Close-up of the overflowing pit")
    sleep(1500)
    clear_narration(session)
    status(session)


if __name__ == "__main__":
    run_standalone(run, name)
