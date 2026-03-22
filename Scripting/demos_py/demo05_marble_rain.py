"""Demo 05 — Marble Rain: 20 random spheres rain down from the sky."""

from Scripting.demos_py.prelude import (
    list_bodies,
    random_spheres,
    reset_simulation,
    run_for,
    run_standalone,
    set_camera,
    sleep,
)

name = "Marble Rain"
description = "20 random spheres rain down from the sky."


def run(session):
    reset_simulation(session)
    set_camera(session, (8.0, 12.0, 8.0), (0.0, 0.0, 0.0))
    ids = random_spheres(session, 20, seed=42)
    print(f"  Generated {len(ids)} random spheres")
    print("  Let it rain...")
    run_for(session, 5.0)
    set_camera(session, (3.0, 1.0, 3.0), (0.0, 0.5, 0.0))
    print("  Ground-level view")
    sleep(1000)
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
