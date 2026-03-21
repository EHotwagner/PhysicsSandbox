"""Demo 10 — Chaos Scene: Everything combined — the full sandbox experience."""

import math

from demos_py.prelude import (
    batch_add,
    boulder,
    launch,
    make_impulse_cmd,
    pyramid,
    random_spheres,
    reset_simulation,
    row,
    run_for,
    run_standalone,
    set_camera,
    set_gravity,
    sleep,
    stack,
    status,
    wireframe,
)

name = "Chaos Scene"
description = "The full sandbox: presets, generators, steering, gravity, camera sweeps."


def run(session):
    reset_simulation(session)

    # Act 1: Build the stage
    set_camera(session, (12.0, 8.0, 12.0), (0.0, 2.0, 0.0))
    print("  Act 1: Building the stage...")

    # Pyramid on the left
    pyramid(session, 5, (-4.0, 0.0, 0.0))
    # Stack on the right
    stack(session, 6, (4.0, 0.0, 0.0))
    # Row of spheres in the middle
    row(session, 8, (-3.0, 0.0, 3.0))

    run_for(session, 1.5)
    print("  Stage built: pyramid + stack + row")

    # Act 2: Bombardment from above
    set_camera(session, (0.0, 15.0, 10.0), (0.0, 2.0, 0.0))
    print("  Act 2: Bombardment!")

    projectiles = random_spheres(session, 10, seed=99)
    # Batch-apply downward impulses to all projectiles
    impulse_cmds = [make_impulse_cmd(pid, (0.0, -20.0, 0.0)) for pid in projectiles]
    batch_add(session, impulse_cmds)

    run_for(session, 3.0)

    # Act 3: Boulder attack
    set_camera(session, (-10.0, 3.0, 0.0), (0.0, 2.0, 0.0))
    print("  Act 3: Boulder attack on pyramid!")

    rock = boulder(session, pos=(-8.0, 1.0, 0.0))
    sleep(200)
    launch(session, rock, (-4.0, 2.0, 0.0), 30.0)
    run_for(session, 3.0)

    # Act 4: Gravity chaos
    print("  Act 4: Gravity chaos!")
    set_camera(session, (8.0, 2.0, 8.0), (0.0, 3.0, 0.0))
    set_gravity(session, (0.0, 8.0, 0.0))
    run_for(session, 2.0)

    set_gravity(session, (5.0, 0.0, 5.0))
    set_camera(session, (-6.0, 4.0, -6.0), (2.0, 2.0, 2.0))
    run_for(session, 2.0)

    # Act 5: Spin everything remaining
    print("  Act 5: Spin everything!")
    set_gravity(session, (0.0, -9.81, 0.0))
    set_camera(session, (10.0, 6.0, 10.0), (0.0, 1.0, 0.0))

    # Wireframe for dramatic effect
    wireframe(session, True)
    run_for(session, 2.0)
    wireframe(session, False)

    # Final camera sweep
    print("  Final: Camera sweep")
    for angle in range(9):
        a = angle * 0.7
        cx = 10.0 * math.cos(a)
        cz = 10.0 * math.sin(a)
        set_camera(session, (cx, 5.0, cz), (0.0, 1.0, 0.0))
        sleep(400)

    print("  Chaos complete!")
    status(session)


if __name__ == "__main__":
    run_standalone(run, name)
