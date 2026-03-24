"""Demo 06 — Domino Row: A row of 20 brick dominoes toppled by a push."""

from Scripting.demos_py.prelude import (
    Direction,
    batch_add,
    clear_narration,
    list_bodies,
    make_box_cmd,
    next_id,
    push,
    reset_simulation,
    run_for,
    run_standalone,
    set_demo_info,
    set_narration,
    sleep,
    smooth_camera,
)

name = "Domino Row"
description = "A row of 20 brick dominoes toppled by a push."


def run(session):
    reset_simulation(session)

    # Camera: side view along the row
    set_narration(session, "Side view — 20 dominoes standing in a row")
    smooth_camera(session, (-2.0, 3.0, 6.0), (5.0, 0.5, 0.0), 1.5)
    sleep(1700)
    set_demo_info(session, "Demo 06: Domino Row", "Classic domino chain reaction.")

    # Batch-create 20 dominoes — pre-generate IDs for push reference
    ids = [next_id("box") for _ in range(20)]
    cmds = [
        make_box_cmd(ids[i], (i * 0.5, 0.3, 0.0), (0.05, 0.3, 0.15), 1.0)
        for i in range(20)
    ]
    batch_add(session, cmds)
    first_id = ids[0]

    print("  Placed 20 dominoes in a row")

    # Let them settle standing
    run_for(session, 1.0)

    # Push the first domino
    print("  Toppling first domino...")
    push(session, first_id, Direction.East, 3.0)

    # Track the cascade with camera
    set_narration(session, "Chain reaction begins — dominoes 1-7")
    smooth_camera(session, (0.0, 2.5, 4.0), (2.0, 0.3, 0.0), 1.0)
    sleep(1200)
    run_for(session, 2.0)
    set_narration(session, "Cascade continues — dominoes 8-14")
    smooth_camera(session, (3.0, 2.5, 4.0), (5.0, 0.3, 0.0), 1.0)
    sleep(1200)
    run_for(session, 2.0)
    set_narration(session, "Final stretch — dominoes 15-20")
    smooth_camera(session, (6.0, 2.5, 4.0), (8.0, 0.3, 0.0), 1.0)
    sleep(1200)
    run_for(session, 2.0)

    # Pan camera to the end
    set_narration(session, "All dominoes down — surveying the aftermath")
    smooth_camera(session, (12.0, 2.0, 4.0), (9.0, 0.0, 0.0), 1.5)
    sleep(1700)
    clear_narration(session)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
