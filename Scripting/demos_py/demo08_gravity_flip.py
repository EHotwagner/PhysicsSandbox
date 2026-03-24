"""Demo 08 — Gravity Flip: Light objects under four gravity directions."""

from random import Random

from Scripting.demos_py.prelude import (
    batch_add,
    clear_narration,
    list_bodies,
    make_box_cmd,
    make_sphere_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_demo_info,
    set_gravity,
    set_narration,
    sleep,
    smooth_camera,
)

name = "Gravity Flip"
description = "Light objects under four gravity directions — up, sideways, diagonal, restored."


def run(session):
    reset_simulation(session)

    set_narration(session, "25 objects settling under normal gravity")
    smooth_camera(session, (6.0, 5.0, 6.0), (0.0, 2.0, 0.0), 1.5)
    sleep(1700)
    set_demo_info(session, "Demo 08: Gravity Flip", "Gravity reversal — objects fly up.")

    # Mix of light objects: beach balls, dice, and small marbles
    rng = Random(77)
    body_cmds = []

    # 8 beach balls scattered at various heights
    for i in range(8):
        x = (i % 4) * 1.2 - 1.8
        z = (i // 4) * 1.2 - 0.6
        y = 3.0 + rng.random() * 4.0
        body_cmds.append(make_sphere_cmd(next_id("sphere"), (x, y, z), 0.2, 0.1))

    # 12 dice scattered higher
    for i in range(12):
        x = (i % 4) * 0.8 - 1.2
        z = (i // 4) * 0.8 - 1.0
        y = 5.0 + rng.random() * 3.0
        body_cmds.append(make_box_cmd(next_id("box"), (x, y, z), (0.05, 0.05, 0.05), 0.03))

    # 5 slightly heavier spheres as anchors
    for i in range(5):
        x = i * 1.0 - 2.0
        body_cmds.append(make_sphere_cmd(next_id("sphere"), (x, 1.5, 0.0), 0.15, 1.0))

    batch_add(session, body_cmds)
    print("  25 objects: beach balls, dice, and spheres")

    # Normal gravity — let things settle
    run_for(session, 2.5)
    print("  Settled. Now the fun begins...")

    # Phase 1: REVERSE — objects fly upward
    set_narration(session, "GRAVITY REVERSED — objects fly upward!")
    smooth_camera(session, (5.0, 1.0, 5.0), (0.0, 6.0, 0.0), 1.5)
    sleep(1700)
    print("  GRAVITY UP!")
    set_gravity(session, (0.0, 15.0, 0.0))
    run_for(session, 2.0)

    # Phase 2: SIDEWAYS — everything slides east
    set_narration(session, "GRAVITY EAST — everything slides sideways")
    print("  GRAVITY EAST!")
    set_gravity(session, (12.0, 0.0, 0.0))
    smooth_camera(session, (-8.0, 4.0, 4.0), (2.0, 2.0, 0.0), 1.5)
    sleep(1700)
    run_for(session, 2.0)

    # Phase 3: DIAGONAL — pulls to a corner
    set_narration(session, "GRAVITY DIAGONAL — pulling to a corner")
    print("  GRAVITY DIAGONAL!")
    set_gravity(session, (-8.0, -5.0, 8.0))
    smooth_camera(session, (4.0, 6.0, -6.0), (-2.0, 1.0, 2.0), 1.5)
    sleep(1700)
    run_for(session, 2.0)

    # Phase 4: RESTORED — everything falls back
    set_narration(session, "Gravity restored — everything falls back down")
    print("  Gravity restored — everything falls!")
    set_gravity(session, (0.0, -9.81, 0.0))
    smooth_camera(session, (6.0, 5.0, 6.0), (0.0, 1.0, 0.0), 1.5)
    sleep(1700)
    run_for(session, 2.5)

    clear_narration(session)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
