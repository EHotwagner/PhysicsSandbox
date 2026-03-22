"""Demo 15 — Overload: Everything at once — the ultimate stress ceiling test."""

import math
import time as _time

from Scripting.demos_py.prelude import (
    batch_add,
    make_impulse_cmd,
    make_sphere_cmd,
    next_id,
    pyramid,
    reset_simulation,
    row,
    run_for,
    run_standalone,
    set_camera,
    set_gravity,
    sleep,
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
    set_camera(session, (20.0, 12.0, 20.0), (0.0, 2.0, 0.0))
    with timed("Act 1 — pyramid + stack + row"):
        pyramid_ids = pyramid(session, 7, (-5.0, 0.0, 0.0))
        stack(session, 10, (5.0, 0.0, 0.0))
        row(session, 12, (-5.0, 0.0, 5.0))
        run_for(session, 2.0)
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
    with timed("Act 3 — impulse storm"):
        set_camera(session, (0.0, 20.0, 15.0), (0.0, 2.0, 0.0))
        imp_cmds = [make_impulse_cmd(bid, (0.0, 10.0, 3.0)) for bid in pyramid_ids]
        batch_add(session, imp_cmds)
        run_for(session, 3.0)
    with timed("Act 4 — gravity chaos"):
        set_camera(session, (12.0, 3.0, 12.0), (0.0, 4.0, 0.0))
        set_gravity(session, (0.0, 10.0, 0.0))
        run_for(session, 2.0)
        set_gravity(session, (6.0, 0.0, 6.0))
        set_camera(session, (-12.0, 5.0, -12.0), (0.0, 3.0, 0.0))
        run_for(session, 2.0)
        set_gravity(session, (0.0, -9.81, 0.0))
    with timed("Act 5 — camera sweep"):
        wireframe(session, True)
        run_for(session, 1.0)
        wireframe(session, False)
        for a in range(8):
            angle = a * 0.785
            set_camera(session, (18.0 * math.cos(angle), 8.0, 18.0 * math.sin(angle)), (0.0, 2.0, 0.0))
            sleep(400)
    total_ms = int((_time.perf_counter() - total_start) * 1000)
    print(f"  [TIME] Total overload: {total_ms} ms")
    print("  Overload complete!")
    status(session)


if __name__ == "__main__":
    run_standalone(run, name)
