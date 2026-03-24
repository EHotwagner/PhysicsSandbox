"""Demo 02 — Bouncing Marbles: Two waves of marbles in varied sizes."""

from random import Random

from Scripting.demos_py.prelude import (
    batch_add,
    clear_narration,
    list_bodies,
    make_sphere_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_demo_info,
    set_narration,
    sleep,
    smooth_camera,
)

name = "Bouncing Marbles"
description = "Two waves of marbles in varied sizes — bouncing, colliding, settling."


def run(session):
    reset_simulation(session)

    # Camera: elevated overview of the drop zone
    set_narration(session, "Elevated overview — marble drop zone")
    smooth_camera(session, (5.0, 8.0, 5.0), (0.0, 1.0, 0.0), 1.5)
    sleep(1700)
    set_demo_info(session, "Demo 02: Bouncing Marbles", "Two waves of 75 marbles rain down.")

    # Wave 1: 15 marbles with varied sizes across a 2D spread
    rng = Random(42)
    wave1 = []
    for i in range(15):
        x = (i % 5) * 0.4 - 0.8
        z = (i // 5) * 0.4 - 0.4
        y = 5.0 + rng.random() * 5.0
        radius = 0.05 + rng.random() * 0.15  # 5cm to 20cm
        mass = radius * radius * 10.0  # heavier = bigger
        wave1.append(make_sphere_cmd(next_id("sphere"), (x, y, z), radius, mass))
    batch_add(session, wave1)

    print("  Wave 1: 15 marbles raining down...")
    run_for(session, 3.0)

    # Wave 2: 10 more marbles dropped into the pile
    set_narration(session, "Wave 2 incoming — closer view of the pile")
    smooth_camera(session, (3.0, 4.0, 3.0), (0.0, 0.5, 0.0), 1.5)
    sleep(1700)
    wave2 = []
    for i in range(10):
        x = rng.random() * 1.6 - 0.8
        z = rng.random() * 1.6 - 0.8
        y = 8.0 + rng.random() * 3.0
        radius = 0.08 + rng.random() * 0.12
        mass = radius * radius * 10.0
        wave2.append(make_sphere_cmd(next_id("sphere"), (x, y, z), radius, mass))
    batch_add(session, wave2)

    print("  Wave 2: 10 more marbles into the pile!")
    run_for(session, 3.0)

    set_narration(session, "Marbles settled — final pile")
    print("  Settled.")
    sleep(1000)

    clear_narration(session)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
