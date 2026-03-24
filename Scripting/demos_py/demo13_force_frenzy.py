"""Demo 13 — Force Frenzy: 80 tightly-packed bodies hit with 3 rounds of escalating forces."""

from Scripting.demos_py.prelude import (
    batch_add,
    make_impulse_cmd,
    make_sphere_cmd,
    make_torque_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    set_demo_info,
    set_gravity,
    status,
    timed,
)

name = "Force Frenzy"
description = "80 tightly-packed bodies hit with 3 rounds of escalating forces — collisions everywhere."


def run(session):
    reset_simulation(session)
    set_camera(session, (10.0, 8.0, 10.0), (0.0, 1.0, 0.0))
    set_demo_info(session, "Demo 13: Force Frenzy", "Force application stress.")

    # Tight 8x10 grid (0.7m spacing)
    with timed("Create 80 bodies"):
        ids = [next_id("sphere") for _ in range(80)]
        cmds = []
        for idx in range(80):
            x = (idx % 8) * 0.7 - 2.45
            z = (idx // 8) * 0.7 - 3.15
            mass = 0.5 if idx % 3 == 0 else 1.5
            radius = 0.2 if idx % 3 == 0 else 0.25
            cmds.append(make_sphere_cmd(ids[idx], (x, 0.5, z), radius, mass))
        batch_add(session, cmds)
    print("  80 spheres in tight 8x10 grid")

    with timed("Settle (1.5s)"):
        run_for(session, 1.5)

    # Round 1: upward impulses — tightly packed so they collide
    with timed("Round 1 — upward impulses (3s)"):
        batch_add(session, [make_impulse_cmd(id_, (0.0, 12.0, 0.0)) for id_ in ids])
        print("  Launch! Bodies colliding on the way up...")
        set_camera(session, (8.0, 2.0, 8.0), (0.0, 5.0, 0.0))
        run_for(session, 3.0)

    # Round 2: torques + sideways gravity
    with timed("Round 2 — torques + sideways gravity (3s)"):
        batch_add(session, [make_torque_cmd(id_, (0.0, 30.0, 15.0)) for id_ in ids])
        set_gravity(session, (10.0, -3.0, 0.0))
        set_camera(session, (-12.0, 5.0, 8.0), (0.0, 2.0, 0.0))
        print("  Spinning + sliding sideways...")
        run_for(session, 3.0)

    # Round 3: inward impulses + low gravity
    with timed("Round 3 — inward impulses + low gravity (3s)"):
        inward_cmds = []
        for idx in range(80):
            x = (idx % 8) * 0.7 - 2.45
            z = (idx // 8) * 0.7 - 3.15
            inward_cmds.append(make_impulse_cmd(ids[idx], (-x * 3.0, 8.0, -z * 3.0)))
        batch_add(session, inward_cmds)
        set_gravity(session, (0.0, -2.0, 0.0))
        set_camera(session, (0.0, 12.0, 10.0), (0.0, 3.0, 0.0))
        print("  Swarming inward under low gravity!")
        run_for(session, 3.0)

    set_gravity(session, (0.0, -9.81, 0.0))
    print("  Gravity restored — settling...")
    set_camera(session, (8.0, 5.0, 8.0), (0.0, 1.0, 0.0))
    run_for(session, 2.0)
    status(session)


if __name__ == "__main__":
    run_standalone(run, name)
