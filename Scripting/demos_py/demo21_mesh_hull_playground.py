"""Demo 21 — Mesh & Hull Playground: Convex hulls and meshes bouncing off obstacles."""

import math

from Scripting.demos_py.prelude import (
    batch_add,
    make_box_cmd,
    make_color,
    make_convex_hull_cmd,
    make_mesh_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    set_demo_info,
    with_color_and_material,
)

name = "Mesh & Hull Playground"
description = "Convex hulls and meshes bounce off static obstacles."


def _icosahedron_points(scale=0.4):
    """Return 12 vertices of an icosahedron."""
    phi = (1.0 + math.sqrt(5.0)) / 2.0
    pts = []
    for s1 in (-1, 1):
        for s2 in (-1, 1):
            pts.append((0.0, s1 * 1.0 * scale, s2 * phi * scale))
            pts.append((s1 * 1.0 * scale, s2 * phi * scale, 0.0))
            pts.append((s2 * phi * scale, 0.0, s1 * 1.0 * scale))
    return pts


def _pyramid_mesh(scale=0.4):
    """Return triangles for a simple pyramid mesh."""
    top = (0.0, scale, 0.0)
    bl = (-scale, -scale * 0.5, -scale)
    br = (scale, -scale * 0.5, -scale)
    fl = (-scale, -scale * 0.5, scale)
    fr = (scale, -scale * 0.5, scale)
    return [
        (bl, br, top),
        (br, fr, top),
        (fr, fl, top),
        (fl, bl, top),
        (bl, fr, br),
        (bl, fl, fr),
    ]


def _wedge_mesh(scale=0.35):
    """Return triangles for a wedge shape."""
    # Wedge: flat bottom, sloped top
    b0 = (-scale, -scale * 0.4, -scale)
    b1 = (scale, -scale * 0.4, -scale)
    b2 = (scale, -scale * 0.4, scale)
    b3 = (-scale, -scale * 0.4, scale)
    t0 = (-scale, scale * 0.4, -scale)
    t1 = (scale, scale * 0.4, -scale)
    return [
        # bottom
        (b0, b2, b1), (b0, b3, b2),
        # back face
        (b0, b1, t1), (b0, t1, t0),
        # slope
        (t0, t1, b2), (t0, b2, b3),
        # left
        (b0, t0, b3),
        # right
        (b1, b2, t1),
    ]


def run(session):
    reset_simulation(session)
    set_camera(session, (0.0, 10.0, 14.0), (0.0, 2.0, 0.0))
    set_demo_info(session, "Demo 21: Mesh & Hull Playground", "Convex hulls and meshes bounce off obstacles.")

    cmds = []

    # Static obstacle ramps (boxes tilted via position, acting as shelves)
    for i in range(3):
        x = (i - 1) * 4.0
        cmd = make_box_cmd(next_id("obstacle"), (x, 2.0, 0.0), (1.5, 0.08, 1.0), 0.0)
        cmds.append(with_color_and_material(cmd, color=make_color(0.5, 0.5, 0.5)))

    # Drop convex hulls from above
    colors_hull = [
        make_color(0.9, 0.3, 0.3),
        make_color(0.3, 0.9, 0.3),
        make_color(0.3, 0.3, 0.9),
        make_color(0.9, 0.9, 0.3),
        make_color(0.9, 0.3, 0.9),
    ]
    for i in range(5):
        x = (i - 2) * 2.0
        pts = _icosahedron_points(0.25 + i * 0.04)
        cmd = make_convex_hull_cmd(next_id("hull"), (x, 8.0 + i * 0.5, -1.0), pts, 2.0 + i * 0.5)
        cmds.append(with_color_and_material(cmd, color=colors_hull[i]))

    # Drop mesh shapes from above
    colors_mesh = [
        make_color(0.9, 0.6, 0.1),
        make_color(0.1, 0.8, 0.7),
        make_color(0.7, 0.2, 0.5),
        make_color(0.4, 0.7, 0.9),
        make_color(0.9, 0.5, 0.5),
    ]
    for i in range(5):
        x = (i - 2) * 2.0
        if i % 2 == 0:
            tris = _pyramid_mesh(0.3 + i * 0.03)
        else:
            tris = _wedge_mesh(0.3 + i * 0.03)
        cmd = make_mesh_cmd(next_id("mesh"), (x, 7.0 + i * 0.3, 1.0), tris, 1.5 + i * 0.3)
        cmds.append(with_color_and_material(cmd, color=colors_mesh[i]))

    batch_add(session, cmds)
    print(f"  Dropped 5 convex hulls + 5 meshes onto 3 obstacle platforms")

    # Watch them bounce and collide
    run_for(session, 4.0)

    # Low angle view
    set_camera(session, (6.0, 2.0, 8.0), (0.0, 1.0, 0.0))
    print("  Low-angle view of settled shapes")
    run_for(session, 2.0)

    # Drop a second wave of larger hulls
    cmds2 = []
    for i in range(3):
        x = (i - 1) * 3.0
        pts = _icosahedron_points(0.5)
        cmd = make_convex_hull_cmd(next_id("hull"), (x, 12.0, 0.0), pts, 5.0)
        cmds2.append(with_color_and_material(cmd, color=make_color(1.0, 1.0, 1.0)))
    batch_add(session, cmds2)
    print("  Wave 2: 3 large white convex hulls incoming!")

    set_camera(session, (0.0, 8.0, 10.0), (0.0, 2.0, 0.0))
    run_for(session, 3.0)

    print("  Mesh & hull playground complete!")


if __name__ == "__main__":
    run_standalone(run, name)
