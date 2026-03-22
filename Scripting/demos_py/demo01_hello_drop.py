"""Demo 01 — Hello Drop: Four objects of different shapes and masses fall side by side."""

from Scripting.demos_py.prelude import (
    batch_add,
    bowling_ball,
    list_bodies,
    make_box_cmd,
    make_impulse_cmd,
    make_sphere_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
)

name = "Hello Drop"
description = "Four different objects fall side by side — same gravity, different bounces."


def run(session):
    reset_simulation(session)

    # Camera: wide side view to see all four objects
    set_camera(session, (6.0, 5.0, 8.0), (0.0, 3.0, 0.0))

    drop_height = 10.0

    # Drop four different objects from the same height, spaced apart
    bowling_ball(session, pos=(-2.0, drop_height, 0.0))
    beach_id = next_id("sphere")
    batch_add(session, [make_sphere_cmd(beach_id, (0.0, drop_height, 0.0), 0.2, 0.1)])
    crate_id = next_id("box")
    batch_add(session, [make_box_cmd(crate_id, (2.0, drop_height, 0.0), (0.25, 0.25, 0.25), 20.0)])
    die_id = next_id("box")
    batch_add(session, [make_box_cmd(die_id, (3.5, drop_height, 0.0), (0.05, 0.05, 0.05), 0.03)])

    print(f"  Dropping: bowling ball, beach ball, crate, die — all from {drop_height:.0f}m")
    print("  Same gravity, different shapes and masses...")

    # Watch the drop and initial bounces
    run_for(session, 2.5)

    # Move camera to ground level for closeup of settled objects
    set_camera(session, (4.0, 1.0, 5.0), (0.5, 0.2, 0.0))
    print("  Ground-level view — notice different resting positions")
    run_for(session, 1.5)

    # Now give them all an upward kick to see how they respond differently
    impulse_cmds = [make_impulse_cmd(id_, (0.0, 5.0, 0.0)) for id_ in [beach_id, crate_id, die_id]]
    batch_add(session, impulse_cmds)
    print("  Upward impulse applied — watch the light ones fly!")
    set_camera(session, (6.0, 5.0, 8.0), (0.0, 3.0, 0.0))
    run_for(session, 3.0)

    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
