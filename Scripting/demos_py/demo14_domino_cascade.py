"""Demo 14 — Domino Cascade: 120 dominoes in a semicircular chain reaction."""

import math

from Scripting.demos_py.prelude import (
    Direction,
    batch_add,
    clear_narration,
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
    status,
    timed,
)

name = "Domino Cascade"
description = "120 dominoes in a semicircular path — chain reaction at scale."


def run(session):
    reset_simulation(session)
    set_narration(session, "Placing 120 dominoes in a semicircle")
    smooth_camera(session, (0.0, 12.0, 0.1), (0.0, 0.0, 0.0), 1.5)
    sleep(1700)
    set_demo_info(session, "Demo 14: Domino Cascade", "Extended domino cascade.")
    count = 120
    radius = 8.0
    with timed(f"Place {count} dominoes"):
        ids = [next_id("box") for _ in range(count)]
        cmds = []
        for i in range(count):
            angle = i / count * math.pi
            x = radius * math.cos(angle)
            z = radius * math.sin(angle)
            cmds.append(make_box_cmd(ids[i], (x, 0.3, z), (0.05, 0.3, 0.15), 1.0))
        batch_add(session, cmds)
    print(f"  {count} dominoes in semicircle (radius {radius:.0f}m)")
    run_for(session, 1.0)

    # Brief overhead view to show the full semicircle layout
    set_narration(session, "Overhead view — the full semicircle")
    smooth_camera(session, (0.0, 14.0, 0.1), (0.0, 0.0, 0.0), 1.2)
    sleep(1400)
    print("  Overhead view — full semicircle")
    sleep(1000)

    # Move to side view for the push
    set_narration(session, "Pushing the first domino — let the cascade begin!")
    smooth_camera(session, (radius + 2.0, 3.0, 0.0), (0.0, 0.5, 0.0), 1.5)
    sleep(1700)
    print("  Pushing first domino...")
    push(session, ids[0], Direction.East, 4.0)
    with timed("Cascade propagation"):
        run_for(session, 10.0)
    set_narration(session, "Following the cascade around the arc")
    for i in range(6):
        angle = i / 5.0 * math.pi
        cx = (radius + 4.0) * math.cos(angle)
        cz = (radius + 4.0) * math.sin(angle)
        smooth_camera(session, (cx, 3.0, cz), (0.0, 0.5, 0.0), 1.0)
        sleep(1200)
    print("  Cascade complete")
    clear_narration(session)
    status(session)


if __name__ == "__main__":
    run_standalone(run, name)
