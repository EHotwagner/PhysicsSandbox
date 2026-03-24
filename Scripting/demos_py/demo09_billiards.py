"""Demo 09 — Billiards: Cue ball breaks a triangle formation."""

from Scripting.demos_py.prelude import (
    batch_add,
    clear_narration,
    launch,
    list_bodies,
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

name = "Billiards"
description = "Cue ball breaks a triangle formation on a table."


def run(session):
    reset_simulation(session)

    # Reduce gravity for billiards-like sliding (low friction sim)
    set_gravity(session, (0.0, -9.81, 0.0))

    # Camera: overhead billiards view
    set_narration(session, "Overhead view — billiard table formation")
    smooth_camera(session, (0.0, 10.0, 0.1), (0.0, 0.0, 0.0), 1.5)
    sleep(1700)
    set_demo_info(session, "Demo 09: Billiards", "Billiard ball collision mechanics.")

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

    # Pause to admire the triangle formation
    print("  Admiring the formation...")
    sleep(1500)

    # Camera: dramatic low angle
    set_narration(session, "Dramatic low angle — cue ball ready")
    smooth_camera(session, (-3.0, 1.5, 2.0), (1.0, 0.0, 0.0), 1.5)
    sleep(1700)

    run_for(session, 0.5)

    # BREAK!
    set_narration(session, "BREAK! Cue ball launched!")
    print("  BREAK!")
    launch(session, cue_id, (1.5, 0.0, 0.0), 15.0)

    # Low angle during the break to see balls scatter
    set_narration(session, "Low angle — balls scattering across the table")
    smooth_camera(session, (-1.0, 0.4, 1.5), (1.0, 0.1, 0.0), 1.0)
    sleep(1200)
    run_for(session, 2.0)

    # Pull back to see the spread
    set_narration(session, "Pulling back — watching the spread")
    smooth_camera(session, (0.0, 5.0, 3.0), (0.0, 0.0, 0.0), 1.5)
    sleep(1700)
    run_for(session, 2.0)

    # Top-down aftermath
    set_narration(session, "Top-down aftermath — final ball positions")
    smooth_camera(session, (0.0, 8.0, 0.1), (0.0, 0.0, 0.0), 1.5)
    sleep(1700)
    clear_narration(session)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
