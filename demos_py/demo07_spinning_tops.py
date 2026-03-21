"""Demo 07 — Spinning Tops: Beach balls and crates spinning with applied torques."""

from demos_py.prelude import (
    batch_add,
    list_bodies,
    make_box_cmd,
    make_sphere_cmd,
    make_torque_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    sleep,
    wireframe,
)

name = "Spinning Tops"
description = "Beach balls and crates spinning with applied torques."


def run(session):
    reset_simulation(session)

    # Camera: top-down angled view
    set_camera(session, (0.0, 8.0, 6.0), (0.0, 0.5, 0.0))

    # Batch-create 4 bodies: 2 beach balls (r=0.2, m=0.1) + 2 crates (half=0.5, m=20)
    b1id = next_id("sphere")
    b2id = next_id("sphere")
    b3id = next_id("box")
    b4id = next_id("box")
    body_cmds = [
        make_sphere_cmd(b1id, (-2.0, 0.25, 0.0), 0.2, 0.1),
        make_sphere_cmd(b2id, (2.0, 0.25, 0.0), 0.2, 0.1),
        make_box_cmd(b3id, (0.0, 0.55, -2.0), (0.5, 0.5, 0.5), 20.0),
        make_box_cmd(b4id, (0.0, 0.55, 2.0), (0.5, 0.5, 0.5), 20.0),
    ]
    batch_add(session, body_cmds)
    print("  Placed 4 bodies in a circle")

    run_for(session, 0.5)

    # Batch-apply torques
    torque_cmds = [
        make_torque_cmd(b1id, (0.0, 50.0, 0.0)),
        make_torque_cmd(b2id, (0.0, 0.0, -30.0)),
        make_torque_cmd(b3id, (0.0, 80.0, 0.0)),
        make_torque_cmd(b4id, (40.0, 0.0, 0.0)),
    ]
    batch_add(session, torque_cmds)
    print("  Applied torques — spinning...")

    run_for(session, 4.0)

    # Wireframe mode for visual effect
    wireframe(session, True)
    print("  Wireframe view")
    sleep(2000)
    wireframe(session, False)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
