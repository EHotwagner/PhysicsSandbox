"""Demo 05 — Marble Rain: 40 mixed objects rain from the sky."""

from random import Random

from Scripting.demos_py.prelude import (
    batch_add,
    clear_narration,
    list_bodies,
    make_box_cmd,
    next_id,
    random_spheres,
    reset_simulation,
    run_for,
    run_standalone,
    set_demo_info,
    set_narration,
    sleep,
    smooth_camera,
)

name = "Marble Rain"
description = "40 mixed objects rain from the sky — spheres, crates, and dice piling up."


def run(session):
    reset_simulation(session)

    # Camera: overhead angle
    set_narration(session, "Overhead angle — objects about to rain down")
    smooth_camera(session, (6.0, 10.0, 6.0), (0.0, 0.0, 0.0), 1.5)
    sleep(1700)
    set_demo_info(session, "Demo 05: Marble Rain", "Continuous marble rain from the sky.")

    # Generate random spheres as the base
    ids = random_spheres(session, 20, seed=42)
    print(f"  Wave 1: {len(ids)} random spheres raining down...")
    run_for(session, 3.0)

    # Wave 2: Add boxes and dice for mixed-shape pile
    rng = Random(99)
    wave2 = []

    # 10 small crates
    for _ in range(10):
        x = rng.random() * 4.0 - 2.0
        z = rng.random() * 4.0 - 2.0
        y = 8.0 + rng.random() * 5.0
        half = 0.1 + rng.random() * 0.15
        wave2.append(make_box_cmd(next_id("box"), (x, y, z), (half, half, half), half * 80.0))

    # 10 tiny dice
    for _ in range(10):
        x = rng.random() * 3.0 - 1.5
        z = rng.random() * 3.0 - 1.5
        y = 10.0 + rng.random() * 4.0
        wave2.append(make_box_cmd(next_id("box"), (x, y, z), (0.05, 0.05, 0.05), 0.03))

    batch_add(session, wave2)
    print("  Wave 2: 10 crates + 10 dice joining the pile!")
    set_narration(session, "Wave 2 — crates and dice join the chaos")
    smooth_camera(session, (4.0, 6.0, 4.0), (0.0, 1.0, 0.0), 1.5)
    sleep(1700)
    run_for(session, 4.0)

    # Ground-level closeup
    set_narration(session, "Ground-level closeup of the mixed-shape pile")
    smooth_camera(session, (2.5, 0.8, 2.5), (0.0, 0.5, 0.0), 1.5)
    sleep(1700)
    print("  Close-up of the mixed-shape pile")
    sleep(1500)

    clear_narration(session)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
