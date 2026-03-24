"""Demo 01 — Hello Drop: Six shapes with custom colors fall side by side."""

from Scripting.demos_py.prelude import (
    add_capsule,
    add_cylinder,
    batch_add,
    bowling_ball,
    BOUNCY_MATERIAL,
    list_bodies,
    make_box_cmd,
    make_color,
    make_capsule_cmd,
    make_cylinder_cmd,
    make_impulse_cmd,
    make_sphere_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    set_demo_info,
)

name = "Hello Drop"
description = "Six different shapes with custom colors fall — same gravity, different bounces."


def run(session):
    reset_simulation(session)

    # Camera: wide side view to see all objects
    set_camera(session, (8.0, 6.0, 10.0), (0.0, 3.0, 0.0))
    set_demo_info(session, "Demo 01: Hello Drop", "Six different shapes fall side by side.")

    drop_height = 10.0

    # Original shapes
    bowling_ball(session, pos=(-3.0, drop_height, 0.0))
    beach_id = next_id("sphere")
    batch_add(session, [make_sphere_cmd(beach_id, (-1.0, drop_height, 0.0), 0.2, 0.1)])
    crate_id = next_id("box")
    batch_add(session, [make_box_cmd(crate_id, (1.0, drop_height, 0.0), (0.25, 0.25, 0.25), 20.0)])

    # New shapes with colors
    capsule_id = add_capsule(session, (3.0, drop_height, 0.0), 0.2, 0.6, 3.0,
                             color=make_color(0.2, 0.8, 0.2))
    cylinder_id = add_cylinder(session, (5.0, drop_height, 0.0), 0.25, 0.4, 5.0,
                               color=make_color(0.8, 0.2, 0.8),
                               material=BOUNCY_MATERIAL)

    print(f"  Dropping 5 shapes from {drop_height:.0f}m: bowling ball, beach ball, crate, green capsule, purple bouncy cylinder")

    # Watch the drop and initial bounces
    run_for(session, 2.5)

    # Move camera to ground level for closeup of settled objects
    set_camera(session, (5.0, 1.0, 6.0), (1.0, 0.2, 0.0))
    print("  Ground-level view — notice different resting positions and colors")
    run_for(session, 1.5)

    # Now give them all an upward kick
    impulse_cmds = [make_impulse_cmd(id_, (0.0, 5.0, 0.0))
                    for id_ in [beach_id, crate_id, capsule_id, cylinder_id]]
    batch_add(session, impulse_cmds)
    print("  Upward impulse applied — the purple cylinder is bouncy!")
    set_camera(session, (8.0, 6.0, 10.0), (0.0, 3.0, 0.0))
    run_for(session, 3.0)

    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
