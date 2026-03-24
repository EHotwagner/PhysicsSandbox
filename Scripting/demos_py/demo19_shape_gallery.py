"""Demo 19 — Shape Gallery: All shape types displayed side by side."""

import math

from Scripting.demos_py.prelude import (
    batch_add,
    make_box_cmd,
    make_capsule_cmd,
    make_color,
    make_compound_cmd,
    make_convex_hull_cmd,
    make_cylinder_cmd,
    make_mesh_cmd,
    make_sphere_cmd,
    make_triangle_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    set_demo_info,
    with_color_and_material,
)

from Scripting.demos_py.generated import physics_hub_pb2 as pb

name = "Shape Gallery"
description = "All shape types displayed side by side — sphere, box, capsule, cylinder, triangle, convex hull, mesh, compound."


def run(session):
    reset_simulation(session)
    set_camera(session, (0.0, 8.0, 14.0), (0.0, 2.0, 0.0))
    set_demo_info(session, "Demo 19: Shape Gallery", "All shape types displayed side by side.")

    spacing = 2.5
    y = 5.0
    cmds = []

    # 1. Sphere — red
    cmd = make_sphere_cmd(next_id("sphere"), (-3 * spacing, y, 0.0), 0.4, 2.0)
    cmds.append(with_color_and_material(cmd, color=make_color(0.9, 0.2, 0.2)))

    # 2. Box — green
    cmd = make_box_cmd(next_id("box"), (-2 * spacing, y, 0.0), (0.3, 0.3, 0.3), 3.0)
    cmds.append(with_color_and_material(cmd, color=make_color(0.2, 0.9, 0.2)))

    # 3. Capsule — blue
    cmd = make_capsule_cmd(next_id("capsule"), (-1 * spacing, y, 0.0), 0.2, 0.6, 2.5)
    cmds.append(with_color_and_material(cmd, color=make_color(0.2, 0.4, 0.9)))

    # 4. Cylinder — yellow
    cmd = make_cylinder_cmd(next_id("cylinder"), (0.0, y, 0.0), 0.3, 0.5, 3.0)
    cmds.append(with_color_and_material(cmd, color=make_color(0.9, 0.9, 0.2)))

    # 5. Triangle — magenta
    cmd = make_triangle_cmd(next_id("tri"), (1 * spacing, y, 0.0),
                            (-0.4, -0.3, -0.3), (0.4, -0.3, -0.3), (0.0, 0.4, 0.3), 1.5)
    cmds.append(with_color_and_material(cmd, color=make_color(0.9, 0.2, 0.9)))

    # 6. Convex hull — cyan (octahedron-like)
    hull_points = [
        (0.0, 0.5, 0.0), (0.0, -0.5, 0.0),
        (0.5, 0.0, 0.0), (-0.5, 0.0, 0.0),
        (0.0, 0.0, 0.5), (0.0, 0.0, -0.5),
    ]
    cmd = make_convex_hull_cmd(next_id("hull"), (2 * spacing, y, 0.0), hull_points, 2.0)
    cmds.append(with_color_and_material(cmd, color=make_color(0.2, 0.9, 0.9)))

    # 7. Mesh — orange (a simple wedge from two triangles)
    mesh_tris = [
        ((-0.4, -0.3, -0.3), (0.4, -0.3, -0.3), (0.0, 0.3, 0.0)),
        ((-0.4, -0.3, -0.3), (0.0, 0.3, 0.0), (-0.4, -0.3, 0.3)),
        ((0.4, -0.3, -0.3), (-0.4, -0.3, 0.3), (0.0, 0.3, 0.0)),
        ((-0.4, -0.3, -0.3), (-0.4, -0.3, 0.3), (0.4, -0.3, -0.3)),
    ]
    cmd = make_mesh_cmd(next_id("mesh"), (3 * spacing, y, 0.0), mesh_tris, 2.0)
    cmds.append(with_color_and_material(cmd, color=make_color(0.9, 0.6, 0.1)))

    # 8. Compound — white (two boxes forming an L-shape)
    children = [
        (pb.Shape(box=pb.Box(half_extents=pb.Vec3(x=0.3, y=0.15, z=0.15))), (0.0, 0.0, 0.0)),
        (pb.Shape(box=pb.Box(half_extents=pb.Vec3(x=0.15, y=0.3, z=0.15))), (0.15, 0.3, 0.0)),
    ]
    cmd = make_compound_cmd(next_id("compound"), (4 * spacing, y, 0.0), children, 4.0)
    cmds.append(with_color_and_material(cmd, color=make_color(0.95, 0.95, 0.95)))

    batch_add(session, cmds)
    print(f"  Dropped {len(cmds)} shapes: sphere, box, capsule, cylinder, triangle, hull, mesh, compound")

    # Watch them fall and settle
    run_for(session, 3.0)

    # Ground-level pan
    set_camera(session, (0.0, 2.0, 10.0), (0.0, 0.5, 0.0))
    print("  Ground-level view of all shapes")
    run_for(session, 2.0)

    # Close-up sweep
    for i in range(8):
        x = (i - 3) * spacing
        set_camera(session, (x, 2.0, 3.0), (x, 0.5, 0.0))
        run_for(session, 0.5)

    print("  Shape gallery complete!")


if __name__ == "__main__":
    run_standalone(run, name)
