"""Demo 08 — Gravity Flip: Objects settle, then gravity flips upward!"""

from demos_py.prelude import (
    batch_add,
    grid,
    list_bodies,
    make_sphere_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    set_gravity,
)

name = "Gravity Flip"
description = "Objects settle, then gravity flips upward — chaos!"


def run(session):
    reset_simulation(session)

    # Camera: wide angle
    set_camera(session, (6.0, 4.0, 6.0), (0.0, 2.0, 0.0))

    # Build a 3x3 grid of crates
    grid(session, 3, 3, (-2.0, 0.0, -2.0))

    # Batch-create 5 beach balls (r=0.2, m=0.1)
    ball_cmds = [
        make_sphere_cmd(
            next_id("sphere"),
            (i * 0.8 - 1.6, 5.0 + i, 0.3),
            0.2,
            0.1,
        )
        for i in range(5)
    ]
    batch_add(session, ball_cmds)
    print("  Grid of crates + 5 beach balls dropping")

    # Normal gravity — let things settle
    run_for(session, 3.0)
    print("  Settled under normal gravity")

    # Camera: looking up to see them fly
    set_camera(session, (5.0, 1.0, 5.0), (0.0, 5.0, 0.0))

    # FLIP GRAVITY!
    print("  GRAVITY REVERSED!")
    set_gravity(session, (0.0, 15.0, 0.0))
    run_for(session, 3.0)

    # Sideways gravity
    print("  Sideways gravity...")
    set_gravity(session, (10.0, 0.0, 0.0))
    set_camera(session, (-8.0, 3.0, 0.0), (0.0, 2.0, 0.0))
    run_for(session, 2.0)

    # Restore
    set_gravity(session, (0.0, -9.81, 0.0))
    print("  Gravity restored")
    run_for(session, 2.0)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
