"""Demo 24 — Halfpipe Arena: Objects oscillate in a halfpipe bowl built from mesh triangles."""

import math

from Scripting.demos_py.prelude import (
    batch_add,
    clear_narration,
    list_bodies,
    make_capsule_cmd,
    make_mesh_cmd,
    make_sphere_cmd,
    make_material,
    next_id,
    pause,
    play,
    reset_simulation,
    run_standalone,
    set_demo_info,
    set_narration,
    sleep,
    smooth_camera,
    with_color_and_material,
    with_motion_type,
    ACCENT_GREEN,
    ACCENT_ORANGE,
    ACCENT_PURPLE,
    ACCENT_YELLOW,
    KINEMATIC_COLOR,
    PROJECTILE_COLOR,
    TARGET_COLOR,
)

name = "Halfpipe Arena"
description = "Objects oscillate in a halfpipe bowl built from mesh triangles."

HALFPIPE_MATERIAL = make_material(0.3, 4.0, 30.0, 0.8)


def _pipe_height(x, z):
    """Halfpipe height function: U-shaped cross-section.
    x: lateral (-4 to 4), z: along pipe (-8 to 8).
    Returns y = bowl-shaped height."""
    radius = 3.5
    # U-shape: y = radius - sqrt(radius^2 - x^2) for |x| < radius
    ax = min(abs(x), radius)
    base_y = radius - math.sqrt(radius * radius - ax * ax)
    # End caps: raise the floor at z extremes to form a bowl
    z_edge = max(abs(z) - 5.0, 0.0)  # starts rising at |z|>5
    cap_lift = z_edge * z_edge * 0.15
    return base_y + cap_lift


def _generate_halfpipe():
    """Generate the halfpipe as a heightmap grid."""
    x_min, x_max = -4.0, 4.0
    z_min, z_max = -8.0, 8.0
    x_steps = 6
    z_steps = 8
    dx = (x_max - x_min) / x_steps
    dz = (z_max - z_min) / z_steps

    triangles = []
    for zi in range(z_steps):
        for xi in range(x_steps):
            x0 = x_min + xi * dx
            x1 = x0 + dx
            z0 = z_min + zi * dz
            z1 = z0 + dz
            p00 = (x0, _pipe_height(x0, z0), z0)
            p10 = (x1, _pipe_height(x1, z0), z0)
            p01 = (x0, _pipe_height(x0, z1), z1)
            p11 = (x1, _pipe_height(x1, z1), z1)
            triangles.append((p00, p10, p01))
            triangles.append((p10, p11, p01))
    return triangles


def run(session):
    reset_simulation(session)
    set_demo_info(session, "Demo 24: Halfpipe Arena",
                  "Objects oscillate in a halfpipe bowl built from mesh triangles.")

    # Opening: elevated view looking down into the bowl
    set_narration(session, "Building halfpipe arena...")
    smooth_camera(session, (0.0, 12.0, 14.0), (0.0, 1.0, 0.0), 1.5)
    sleep(1700)

    pipe_tris = _generate_halfpipe()
    print(f"  Halfpipe generated: {len(pipe_tris)} triangles")
    cmd = make_mesh_cmd(next_id("halfpipe"), (0.0, 0.0, 0.0), pipe_tris, 0.0)
    cmd = with_motion_type(cmd, 2)  # STATIC
    batch_add(session, [with_color_and_material(cmd, color=ACCENT_YELLOW, material=HALFPIPE_MATERIAL)])

    sleep(500)

    # Camera from end of pipe, looking along interior
    set_narration(session, "Dropping balls into the halfpipe!")
    smooth_camera(session, (0.0, 6.0, -14.0), (0.0, 1.0, 0.0), 1.5)
    sleep(1700)

    # Drop balls inside the pipe
    ball_colors = [PROJECTILE_COLOR, ACCENT_GREEN, TARGET_COLOR,
                   ACCENT_ORANGE, ACCENT_PURPLE, KINEMATIC_COLOR]
    ball_cmds = []
    for i in range(6):
        x = (i - 2.5) * 0.5  # x from -1.25 to 1.25 (inside the U)
        z = (i - 2.5) * 1.5  # z from -3.75 to 3.75
        cmd = make_sphere_cmd(next_id("ball"), (x, 5.0, z), 0.35, 2.5)
        ball_cmds.append(with_color_and_material(cmd, color=ball_colors[i]))
    batch_add(session, ball_cmds)

    # Drop capsules
    cap_cmds = []
    for i in range(2):
        cmd = make_capsule_cmd(next_id("capsule"), (0.0, 6.0, i * 3.0 - 1.5), 0.25, 0.7, 3.0)
        cap_cmds.append(with_color_and_material(cmd, color=ACCENT_PURPLE))
    batch_add(session, cap_cmds)

    # Watch the drop from above
    set_narration(session, "Objects falling into the bowl!")
    play(session)
    smooth_camera(session, (0.0, 10.0, 12.0), (0.0, 1.0, 0.0), 2.0)
    sleep(4000)

    # Side view — see oscillation profile
    set_narration(session, "Oscillation — rolling back and forth")
    smooth_camera(session, (10.0, 5.0, 0.0), (0.0, 1.5, 0.0), 2.0)
    sleep(4000)

    # End-on view
    set_narration(session, "Looking down the halfpipe")
    smooth_camera(session, (0.0, 5.0, -14.0), (0.0, 1.0, 0.0), 2.0)
    sleep(3500)

    # Above close-up
    set_narration(session, "Objects settling at the bottom")
    smooth_camera(session, (0.0, 8.0, 5.0), (0.0, 0.5, 0.0), 2.0)
    sleep(3500)

    # Second wave
    set_narration(session, "Second wave incoming!")
    wave2 = []
    for i in range(3):
        cmd = make_sphere_cmd(next_id("ball"), (i - 1.0, 6.0, i * 2.0 - 2.0), 0.4, 3.0)
        wave2.append(with_color_and_material(cmd, color=ACCENT_GREEN))
    batch_add(session, wave2)
    sleep(3000)

    # Final wide shot
    set_narration(session, "Wide view — halfpipe arena")
    smooth_camera(session, (10.0, 10.0, 10.0), (0.0, 2.0, 0.0), 2.0)
    sleep(2500)

    pause(session)
    clear_narration(session)
    print("  Halfpipe arena demo complete!")
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
