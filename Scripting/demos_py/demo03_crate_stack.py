"""Demo 03 — Crate Stack: A 12-crate tower hit by a bowling ball."""

from Scripting.demos_py.prelude import (
    bowling_ball,
    launch,
    list_bodies,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    set_demo_info,
    sleep,
    stack,
)

name = "Crate Stack"
description = "A 12-crate tower hit by a bowling ball — dramatic toppling."


def run(session):
    reset_simulation(session)

    # Camera: side view, framing the full tower
    set_camera(session, (8.0, 7.0, 4.0), (0.0, 5.0, 0.0))
    set_demo_info(session, "Demo 03: Crate Stack", "Stacking dynamics with crates.")

    # Build a tall stack of 12 crates
    ids = stack(session, 12, (0.0, 0.0, 0.0))
    print(f"  Built tower of {len(ids)} crates")

    # Let it settle
    run_for(session, 2.0)

    # Place a bowling ball to the side
    ball = bowling_ball(session, pos=(-5.0, 3.0, 0.0))
    print("  Bowling ball ready — aiming at tower base...")
    sleep(500)

    # Launch the ball at the tower base
    print("  STRIKE!")
    launch(session, ball, (0.0, 3.0, 0.0), 30.0)
    run_for(session, 2.0)

    # Camera move to see the debris
    set_camera(session, (5.0, 3.0, 5.0), (0.0, 2.0, 0.0))
    print("  Watching debris settle...")
    run_for(session, 2.5)

    # Top-down view of the aftermath
    set_camera(session, (0.0, 12.0, 0.1), (0.0, 0.0, 0.0))
    print("  Overhead view of destruction")
    sleep(1500)

    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
