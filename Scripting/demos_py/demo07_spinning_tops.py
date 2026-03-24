"""Demo 07 — Spinning Tops: Six spinning objects collide in the center."""

import math

from Scripting.demos_py.prelude import (
    batch_add,
    list_bodies,
    make_box_cmd,
    make_impulse_cmd,
    make_sphere_cmd,
    make_torque_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    set_demo_info,
    wireframe,
)

name = "Spinning Tops"
description = "Six spinning objects collide in the center — angular momentum chaos."


def run(session):
    reset_simulation(session)

    # Camera: top-down angled view
    set_camera(session, (0.0, 10.0, 8.0), (0.0, 0.5, 0.0))
    set_demo_info(session, "Demo 07: Spinning Tops", "Spinning top physics.")

    # Place 6 objects in a ring (radius 2m), alternating spheres and boxes
    radius = 2.0
    ids = []
    body_cmds = []
    for i in range(6):
        angle = i * math.pi / 3.0
        x = radius * math.cos(angle)
        z = radius * math.sin(angle)
        if i % 2 == 0:
            id_ = next_id("sphere")
            ids.append(id_)
            body_cmds.append(make_sphere_cmd(id_, (x, 0.3, z), 0.25, 2.0))
        else:
            id_ = next_id("box")
            ids.append(id_)
            body_cmds.append(make_box_cmd(id_, (x, 0.4, z), (0.3, 0.3, 0.3), 5.0))
    batch_add(session, body_cmds)
    print("  6 objects placed in a ring")

    run_for(session, 0.5)

    # Spin them all — varied torque axes
    torque_cmds = [
        make_torque_cmd(ids[0], (0.0, 80.0, 0.0)),
        make_torque_cmd(ids[1], (0.0, -60.0, 30.0)),
        make_torque_cmd(ids[2], (0.0, 70.0, 0.0)),
        make_torque_cmd(ids[3], (40.0, 0.0, -50.0)),
        make_torque_cmd(ids[4], (0.0, -90.0, 0.0)),
        make_torque_cmd(ids[5], (-30.0, 60.0, 0.0)),
    ]
    batch_add(session, torque_cmds)
    print("  All spinning...")
    run_for(session, 2.0)

    # Wireframe on to see rotation clearly
    wireframe(session, True)
    print("  Wireframe on — watch the collisions!")

    # Push all objects inward toward center
    impulse_cmds = []
    for i in range(6):
        angle = i * math.pi / 3.0
        ix = -math.cos(angle) * 8.0
        iz = -math.sin(angle) * 8.0
        impulse_cmds.append(make_impulse_cmd(ids[i], (ix, 0.5, iz)))
    batch_add(session, impulse_cmds)
    print("  Pushed inward — COLLISION!")

    # Camera drops to side view for dramatic impact
    set_camera(session, (5.0, 3.0, 5.0), (0.0, 0.5, 0.0))
    run_for(session, 3.0)

    # Let chaos settle
    wireframe(session, False)
    set_camera(session, (4.0, 2.0, 4.0), (0.0, 0.3, 0.0))
    print("  Settling...")
    run_for(session, 2.0)

    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
