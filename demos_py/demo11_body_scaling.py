"""Demo 11 — Body Scaling: Progressive body count: 50 -> 100 -> 200 -> 500."""

from demos_py.prelude import (
    batch_add,
    make_sphere_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    status,
    timed,
)

name = "Body Scaling"
description = "Progressive body count: 50 -> 100 -> 200 -> 500. Finds degradation point."


def run(session):
    reset_simulation(session)
    tiers = [50, 100, 200, 500]
    for tier in tiers:
        print(f"  === Tier: {tier} bodies ===")
        reset_simulation(session)
        dist = 15.0 if tier <= 100 else 25.0 if tier <= 200 else 40.0
        set_camera(session, (dist, dist * 0.6, dist), (0.0, 2.0, 0.0))
        with timed(f"Tier {tier} setup"):
            cmds = []
            for i in range(tier):
                x = (i % 10) * 1.2 - 6.0
                y = 2.0 + (i // 100) * 3.0
                z = ((i // 10) % 10) * 1.2 - 6.0
                cmds.append(make_sphere_cmd(next_id("sphere"), (x, y, z), 0.3, 1.0))
            batch_add(session, cmds)
        with timed(f"Tier {tier} simulation (3s)"):
            run_for(session, 3.0)
        print(f"  Tier {tier} complete")
    print("  All tiers complete — check [TIME] markers for degradation")
    status(session)


if __name__ == "__main__":
    run_standalone(run, name)
