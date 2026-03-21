"""Demo 01 — Hello Drop: A single bowling ball falls from height onto the ground."""

from demos_py.prelude import (
    bowling_ball,
    list_bodies,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
)

name = "Hello Drop"
description = "A single bowling ball falls from height onto the ground."


def run(session):
    reset_simulation(session)
    set_camera(session, (5.0, 3.0, 5.0), (0.0, 1.0, 0.0))
    bowling_ball(session, pos=(0.0, 10.0, 0.0))
    print("  Dropping bowling ball from 10m...")
    run_for(session, 3.0)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
