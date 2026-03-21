"""Demo 13 — Force Frenzy: 100 bodies hit with 3 rounds of escalating forces."""

from demos_py.prelude import (
    batch_add,
    make_impulse_cmd,
    make_sphere_cmd,
    make_torque_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    set_gravity,
    status,
    timed,
)

name = "Force Frenzy"
description = "100 bodies hit with 3 rounds of escalating impulses, torques, and gravity shifts."


def run(session):
    reset_simulation(session)
    set_camera(session, (15.0, 10.0, 15.0), (0.0, 1.0, 0.0))
    with timed("Create 100 bodies"):
        ids = [next_id("sphere") for _ in range(100)]
        cmds = []
        for idx in range(100):
            x = (idx % 10) * 1.5 - 7.0
            z = (idx // 10) * 1.5 - 7.0
            cmds.append(make_sphere_cmd(ids[idx], (x, 0.5, z), 0.3, 1.0))
        batch_add(session, cmds)
    print("  100 spheres in 10x10 grid")
    with timed("Settle (2s)"):
        run_for(session, 2.0)
    with timed("Round 1 — impulses (3s)"):
        imp_cmds = [make_impulse_cmd(bid, (0.0, 8.0, 0.0)) for bid in ids]
        batch_add(session, imp_cmds)
        run_for(session, 3.0)
    with timed("Round 2 — torques + sideways gravity (3s)"):
        tor_cmds = [make_torque_cmd(bid, (0.0, 20.0, 10.0)) for bid in ids]
        batch_add(session, tor_cmds)
        set_gravity(session, (8.0, -2.0, 0.0))
        set_camera(session, (-15.0, 5.0, 10.0), (0.0, 2.0, 0.0))
        run_for(session, 3.0)
    with timed("Round 3 — strong impulses + reversed gravity (3s)"):
        imp_cmds2 = [make_impulse_cmd(bid, (5.0, 15.0, -5.0)) for bid in ids]
        batch_add(session, imp_cmds2)
        set_gravity(session, (0.0, 12.0, 0.0))
        set_camera(session, (10.0, 2.0, 15.0), (0.0, 5.0, 0.0))
        run_for(session, 3.0)
    set_gravity(session, (0.0, -9.81, 0.0))
    print("  Gravity restored")
    run_for(session, 2.0)
    status(session)


if __name__ == "__main__":
    run_standalone(run, name)
