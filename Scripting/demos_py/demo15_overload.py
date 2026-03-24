"""Demo 15 — Overload: Everything at once — the ultimate stress ceiling test."""

import math
import time as _time

from Scripting.demos_py.prelude import (
    batch_add,
    clear_narration,
    make_impulse_cmd,
    make_sphere_cmd,
    next_id,
    pyramid,
    reset_simulation,
    row,
    run_for,
    run_standalone,
    set_demo_info,
    set_gravity,
    set_narration,
    sleep,
    smooth_camera,
    stack,
    status,
    timed,
    wireframe,
)

name = "Overload"
description = "Everything at once: 200+ bodies, forces, gravity shifts, camera sweep — stress ceiling test."


def run(session):
    reset_simulation(session)
    total_start = _time.perf_counter()
    set_narration(session, "Act 1 — Building pyramid, stack, and row")
    smooth_camera(session, (20.0, 12.0, 20.0), (0.0, 2.0, 0.0), 1.5)
    sleep(1700)
    set_demo_info(session, "Demo 15: Overload", "System stress test.")
    with timed("Act 1 — pyramid + stack + row"):
        pyramid_ids = pyramid(session, 7, (-5.0, 0.0, 0.0))
        stack(session, 10, (5.0, 0.0, 0.0))
        row(session, 12, (-5.0, 0.0, 5.0))
        run_for(session, 2.0)
    set_narration(session, "Act 2 — Dropping 100 random spheres")
    with timed("Act 2 — 100 random spheres"):
        cmds = []
        for i in range(100):
            x = (i % 10) * 1.5 - 7.0
            z = (i // 10) * 1.5 - 7.0
            y = 8.0 + (i // 20) * 2.0
            cmds.append(make_sphere_cmd(next_id("sphere"), (x, y, z), 0.25, 0.8))
        batch_add(session, cmds)
        run_for(session, 3.0)
    print("  200+ bodies active")
    status(session)
    with timed("Act 3 — impulse storm"):
        set_narration(session, "Act 3 — Impulse storm in wireframe")
        smooth_camera(session, (0.0, 20.0, 15.0), (0.0, 2.0, 0.0), 1.5)
        sleep(1700)
        wireframe(session, True)
        imp_cmds = [make_impulse_cmd(bid, (0.0, 10.0, 3.0)) for bid in pyramid_ids]
        batch_add(session, imp_cmds)
        run_for(session, 3.0)
        wireframe(session, False)
    print("  Bodies after impulse storm:")
    status(session)
    with timed("Act 4 — gravity chaos"):
        set_narration(session, "Act 4 — Gravity reversal: everything flies up")
        smooth_camera(session, (12.0, 3.0, 12.0), (0.0, 4.0, 0.0), 1.2)
        sleep(1400)
        set_gravity(session, (0.0, 10.0, 0.0))
        run_for(session, 2.0)
        set_narration(session, "Sideways gravity — chaos unleashed")
        set_gravity(session, (6.0, 0.0, 6.0))
        smooth_camera(session, (-12.0, 5.0, -12.0), (0.0, 3.0, 0.0), 1.5)
        sleep(1700)
        run_for(session, 2.0)
        set_gravity(session, (0.0, -9.81, 0.0))
    with timed("Act 5 — camera sweep"):
        set_narration(session, "Act 5 — Final sweep around the scene")
        wireframe(session, True)
        run_for(session, 1.0)
        wireframe(session, False)
        for a in range(8):
            angle = a * 0.785
            smooth_camera(session, (18.0 * math.cos(angle), 8.0, 18.0 * math.sin(angle)), (0.0, 2.0, 0.0), 1.0)
            sleep(1200)
    total_ms = int((_time.perf_counter() - total_start) * 1000)
    print(f"  [TIME] Total overload: {total_ms} ms")
    print("  Overload complete!")
    clear_narration(session)
    status(session)


if __name__ == "__main__":
    run_standalone(run, name)
