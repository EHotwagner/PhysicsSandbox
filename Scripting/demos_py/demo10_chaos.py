"""Demo 10 — Chaos Scene: Everything combined — the full sandbox experience."""

import math

from Scripting.demos_py.prelude import (
    batch_add,
    boulder,
    clear_narration,
    launch,
    make_impulse_cmd,
    pyramid,
    random_spheres,
    reset_simulation,
    row,
    run_for,
    run_standalone,
    set_demo_info,
    set_gravity,
    set_narration,
    sleep,
    smooth_camera,
    stack,
    status,
    wireframe,
)

name = "Chaos Scene"
description = "The full sandbox: presets, generators, steering, gravity, camera sweeps."


def run(session):
    reset_simulation(session)

    # Act 1: Build the stage
    set_narration(session, "Act 1: Building the stage — pyramid, stack, and row")
    smooth_camera(session, (12.0, 8.0, 12.0), (0.0, 2.0, 0.0), 1.5)
    sleep(1700)
    set_demo_info(session, "Demo 10: Chaos", "Large-scale chaotic system.")
    print("  Act 1: Building the stage...")

    # Pyramid on the left
    pyramid(session, 5, (-4.0, 0.0, 0.0))
    # Stack on the right
    stack(session, 6, (4.0, 0.0, 0.0))
    # Row of spheres in the middle
    row(session, 8, (-3.0, 0.0, 3.0))

    run_for(session, 1.5)
    print("  Stage built: pyramid + stack + row")

    # Dramatic pause — let the audience take in the stage
    sleep(800)

    # Act 2: Bombardment from above
    set_narration(session, "Act 2: Bombardment from above!")
    smooth_camera(session, (0.0, 15.0, 10.0), (0.0, 2.0, 0.0), 1.5)
    sleep(1700)
    print("  Act 2: Bombardment!")

    projectiles = random_spheres(session, 10, seed=99)
    # Batch-apply downward impulses to all projectiles
    impulse_cmds = [make_impulse_cmd(pid, (0.0, -20.0, 0.0)) for pid in projectiles]
    batch_add(session, impulse_cmds)

    run_for(session, 3.0)

    # Act 3: Boulder attack
    set_narration(session, "Act 3: Boulder attack on the pyramid!")
    smooth_camera(session, (-10.0, 3.0, 0.0), (0.0, 2.0, 0.0), 1.5)
    sleep(1700)
    print("  Act 3: Boulder attack on pyramid!")

    rock = boulder(session, pos=(-8.0, 1.0, 0.0))
    sleep(200)
    launch(session, rock, (-4.0, 2.0, 0.0), 30.0)
    run_for(session, 3.0)

    # Act 4: Gravity chaos
    set_narration(session, "Act 4: Gravity reversed — objects fly upward!")
    print("  Act 4: Gravity chaos!")
    smooth_camera(session, (8.0, 2.0, 8.0), (0.0, 3.0, 0.0), 1.5)
    sleep(1700)
    set_gravity(session, (0.0, 8.0, 0.0))
    run_for(session, 2.0)

    set_narration(session, "Gravity pulling diagonally — sideways chaos")
    set_gravity(session, (5.0, 0.0, 5.0))
    smooth_camera(session, (-6.0, 4.0, -6.0), (2.0, 2.0, 2.0), 1.5)
    sleep(1700)
    run_for(session, 2.0)

    # Act 5: Spin everything remaining
    set_narration(session, "Act 5: Gravity restored — wireframe finale")
    print("  Act 5: Spin everything!")
    set_gravity(session, (0.0, -9.81, 0.0))
    smooth_camera(session, (10.0, 6.0, 10.0), (0.0, 1.0, 0.0), 1.5)
    sleep(1700)

    # Wireframe for dramatic effect
    wireframe(session, True)
    run_for(session, 2.0)
    wireframe(session, False)

    # Final camera sweep (tighter pacing)
    set_narration(session, "Final camera sweep — orbiting the destruction")
    print("  Final: Camera sweep")
    for angle in range(7):
        a = angle * 0.9
        cx = 10.0 * math.cos(a)
        cz = 10.0 * math.sin(a)
        smooth_camera(session, (cx, 5.0, cz), (0.0, 1.0, 0.0), 1.0)
        sleep(1200)

    clear_narration(session)
    print("  Chaos complete!")
    status(session)


if __name__ == "__main__":
    run_standalone(run, name)
