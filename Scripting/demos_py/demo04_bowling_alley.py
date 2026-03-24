"""Demo 04 — Bowling Alley: Launch a bowling ball at a pyramid of bricks."""

from Scripting.demos_py.prelude import (
    bowling_ball,
    clear_narration,
    launch,
    list_bodies,
    pyramid,
    reset_simulation,
    run_for,
    run_standalone,
    set_demo_info,
    set_narration,
    sleep,
    smooth_camera,
)

name = "Bowling Alley"
description = "Launch a bowling ball at a pyramid of bricks."


def run(session):
    reset_simulation(session)
    set_narration(session, "Bowling alley — setting up the pins")
    smooth_camera(session, (-5.0, 3.0, 3.0), (3.0, 1.0, 0.0), 1.5)
    sleep(1700)
    set_demo_info(session, "Demo 04: Bowling Alley", "Bowling pins and ball collision.")
    pyramid(session, 4, pos=(5.0, 0.0, 0.0))
    print("  Built pyramid (4 layers)")
    ball = bowling_ball(session, pos=(-3.0, 0.15, 0.0))
    print("  Bowling ball ready")
    run_for(session, 1.0)
    set_narration(session, "Admiring the pyramid formation")
    print("  Admiring the pyramid...")
    sleep(1500)
    set_narration(session, "STRIKE! Ball launched at the pyramid")
    print("  STRIKE! Launching ball...")
    launch(session, ball, (5.0, 0.5, 0.0), 25.0)
    run_for(session, 2.5)
    # Low angle to see debris scatter
    set_narration(session, "Low-angle view — debris scattering")
    smooth_camera(session, (2.0, 0.5, 3.0), (5.0, 0.5, 0.0), 1.5)
    sleep(1700)
    print("  Low-angle debris view")
    run_for(session, 2.0)
    clear_narration(session)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
