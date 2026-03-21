"""Demo 09 — Billiards: Cue ball breaks a triangle formation."""

from demos_py.prelude import (
    batch_add,
    launch,
    list_bodies,
    make_sphere_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    set_gravity,
    sleep,
)

name = "Billiards"
description = "Cue ball breaks a triangle formation on a table."


def run(session):
    reset_simulation(session)

    # Reduce gravity for billiards-like sliding (low friction sim)
    set_gravity(session, (0.0, -9.81, 0.0))

    # Camera: overhead billiards view
    set_camera(session, (0.0, 10.0, 0.1), (0.0, 0.0, 0.0))

    # Batch-create 15 balls in triangle + 1 cue ball
    r = 0.1
    spacing = 0.22
    cue_id = "cue"
    cmds = []
    for row in range(5):
        for col in range(row + 1):
            x = row * spacing * 0.866 + 1.0
            z = (col - row / 2.0) * spacing
            cmds.append(make_sphere_cmd(next_id("sphere"), (x, r, z), r, 0.17))
    cmds.append(make_sphere_cmd(cue_id, (-2.0, r, 0.0), r * 1.1, 0.17))
    batch_add(session, cmds)
    print("  Placed 15 balls in triangle + cue ball")

    # Camera: dramatic low angle
    set_camera(session, (-3.0, 1.5, 2.0), (1.0, 0.0, 0.0))

    run_for(session, 0.5)

    # BREAK!
    print("  BREAK!")
    launch(session, cue_id, (1.5, 0.0, 0.0), 15.0)
    run_for(session, 4.0)

    # Top-down aftermath
    set_camera(session, (0.0, 8.0, 0.1), (0.0, 0.0, 0.0))
    sleep(1000)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
