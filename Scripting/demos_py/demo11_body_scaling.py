"""Demo 11 — Body Scaling: Progressive body count with tight packing."""

import math
from random import Random

from Scripting.demos_py.prelude import (
    batch_add,
    make_box_cmd,
    make_sphere_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    set_demo_info,
    status,
    timed,
)

name = "Body Scaling"
description = "Progressive body count with tight packing — collision-dense stress test."


def run(session):
    reset_simulation(session)
    set_camera(session, (10.0, 6.0, 10.0), (0.0, 2.0, 0.0))
    set_demo_info(session, "Demo 11: Body Scaling", "Variable mass and scale bodies.")
    tiers = [50, 100, 200, 500]
    for tier in tiers:
        print(f"  === Tier: {tier} bodies ===")
        reset_simulation(session)
        dist = 10.0 if tier <= 100 else 18.0 if tier <= 200 else 30.0
        set_camera(session, (dist, dist * 0.6, dist), (0.0, 2.0, 0.0))
        rng = Random(tier)
        with timed(f"Tier {tier} setup"):
            cols = int(math.sqrt(tier))
            cmds = []
            for i in range(tier):
                x = (i % cols) * 0.7 - cols * 0.35
                z = ((i // cols) % cols) * 0.7 - cols * 0.35
                y = 2.0 + (i // (cols * cols)) * 1.5
                if i % 3 == 0:
                    cmds.append(make_box_cmd(next_id("box"), (x, y, z), (0.2, 0.2, 0.2), 0.5 + rng.random() * 2.0))
                else:
                    cmds.append(make_sphere_cmd(next_id("sphere"), (x, y, z), 0.15 + rng.random() * 0.1, 0.5 + rng.random() * 2.0))
            batch_add(session, cmds)
        with timed(f"Tier {tier} simulation (3s)"):
            run_for(session, 3.0)
        print(f"  Tier {tier} complete")
    print("  All tiers complete — check [TIME] markers for degradation")
    status(session)


if __name__ == "__main__":
    run_standalone(run, name)
