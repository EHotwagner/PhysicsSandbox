"""Demo 06 — Domino Row: A row of 12 brick dominoes toppled by a push."""

from Scripting.demos_py.prelude import (
    Direction,
    batch_add,
    list_bodies,
    make_box_cmd,
    next_id,
    push,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    sleep,
)

name = "Domino Row"
description = "A row of 12 brick dominoes toppled by a push."


def run(session):
    reset_simulation(session)

    # Camera: side view along the row
    set_camera(session, (-2.0, 3.0, 6.0), (3.0, 0.5, 0.0))

    # Batch-create 12 dominoes — pre-generate IDs for push reference
    ids = [next_id("box") for _ in range(12)]
    cmds = [
        make_box_cmd(ids[i], (i * 0.5, 0.3, 0.0), (0.05, 0.3, 0.15), 1.0)
        for i in range(12)
    ]
    batch_add(session, cmds)
    first_id = ids[0]

    print("  Placed 12 dominoes in a row")

    # Let them settle standing
    run_for(session, 1.0)

    # Push the first domino
    print("  Toppling first domino...")
    push(session, first_id, Direction.East, 3.0)
    run_for(session, 5.0)

    # Pan camera to the end
    set_camera(session, (8.0, 2.0, 4.0), (5.0, 0.0, 0.0))
    sleep(500)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
