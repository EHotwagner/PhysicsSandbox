"""Demo 20 — Compound Constructions: L-shapes, T-shapes, and dumbbells from compound bodies."""

from Scripting.demos_py.prelude import (
    batch_add,
    make_color,
    make_compound_cmd,
    next_id,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    set_demo_info,
    with_color_and_material,
)

from Scripting.demos_py.generated import physics_hub_pb2 as pb

name = "Compound Constructions"
description = "L-shapes, T-shapes, and dumbbells built from compound bodies."


def _box_shape(hx, hy, hz):
    return pb.Shape(box=pb.Box(half_extents=pb.Vec3(x=hx, y=hy, z=hz)))


def _sphere_shape(r):
    return pb.Shape(sphere=pb.Sphere(radius=r))


def run(session):
    reset_simulation(session)
    set_camera(session, (0.0, 8.0, 12.0), (0.0, 2.0, 0.0))
    set_demo_info(session, "Demo 20: Compound Constructions", "L-shapes, T-shapes, and dumbbells from compound bodies.")

    cmds = []
    y = 6.0

    # Row 1: L-shapes (3 of them)
    for i in range(3):
        x = (i - 1) * 3.0
        children = [
            (_box_shape(0.4, 0.1, 0.1), (0.0, 0.0, 0.0)),
            (_box_shape(0.1, 0.4, 0.1), (-0.3, 0.4, 0.0)),
        ]
        cmd = make_compound_cmd(next_id("L"), (x, y, -2.0), children, 5.0)
        cmds.append(with_color_and_material(cmd, color=make_color(0.8, 0.3, 0.2)))

    # Row 2: T-shapes (3 of them)
    for i in range(3):
        x = (i - 1) * 3.0
        children = [
            (_box_shape(0.5, 0.08, 0.08), (0.0, 0.4, 0.0)),
            (_box_shape(0.08, 0.4, 0.08), (0.0, 0.0, 0.0)),
        ]
        cmd = make_compound_cmd(next_id("T"), (x, y, 0.0), children, 5.0)
        cmds.append(with_color_and_material(cmd, color=make_color(0.2, 0.6, 0.9)))

    # Row 3: Dumbbells (3 of them)
    for i in range(3):
        x = (i - 1) * 3.0
        children = [
            (_sphere_shape(0.25), (-0.5, 0.0, 0.0)),
            (_box_shape(0.4, 0.06, 0.06), (0.0, 0.0, 0.0)),
            (_sphere_shape(0.25), (0.5, 0.0, 0.0)),
        ]
        cmd = make_compound_cmd(next_id("dumbbell"), (x, y, 2.0), children, 6.0)
        cmds.append(with_color_and_material(cmd, color=make_color(0.6, 0.9, 0.3)))

    batch_add(session, cmds)
    print(f"  Dropped {len(cmds)} compound bodies: 3 L-shapes, 3 T-shapes, 3 dumbbells")

    # Watch them fall and tumble
    run_for(session, 4.0)

    # Side view to see how they settled
    set_camera(session, (8.0, 3.0, 0.0), (0.0, 0.5, 0.0))
    print("  Side view — compound shapes have interesting resting poses")
    run_for(session, 2.0)

    # Overhead
    set_camera(session, (0.0, 10.0, 0.1), (0.0, 0.0, 0.0))
    print("  Overhead view")
    run_for(session, 1.5)

    print("  Compound constructions complete!")


if __name__ == "__main__":
    run_standalone(run, name)
