"""Demo 23 — Ball Rollercoaster: Balls roll down a mesh terrain with drops, hills, and banked curves."""

import math

from Scripting.demos_py.prelude import (
    batch_add,
    clear_narration,
    list_bodies,
    make_mesh_cmd,
    make_sphere_cmd,
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
    SLIPPERY_MATERIAL,
    PROJECTILE_COLOR,
    ACCENT_YELLOW,
    ACCENT_GREEN,
    ACCENT_ORANGE,
    ACCENT_PURPLE,
    TARGET_COLOR,
    KINEMATIC_COLOR,
)

name = "Ball Rollercoaster"
description = "Balls roll down a mesh terrain with drops, hills, and banked curves."


def _terrain_height(x, z):
    """Height function for the rollercoaster terrain.
    x: lateral position, z: forward position along the track (-15 to 15).
    Returns y height."""
    pi = math.pi
    # Normalize z to [0..1] range
    t = max(0.0, min(1.0, (z + 15.0) / 30.0))
    # Base elevation profile along the track
    if t < 0.1:
        base_y = 8.0  # launch platform
    elif t < 0.25:
        base_y = 8.0 - (t - 0.1) / 0.15 * 6.0  # steep drop
    elif t < 0.45:
        base_y = 2.0 + 3.0 * math.sin((t - 0.25) / 0.20 * pi)  # hill
    elif t < 0.65:
        base_y = 2.0 - (t - 0.45) * 3.0  # descent
    elif t < 0.85:
        base_y = 0.8 + 2.0 * math.sin((t - 0.65) / 0.20 * pi)  # second hill
    else:
        base_y = 0.8 - (t - 0.85) / 0.15 * 0.3  # final run-out
    # Channel shape: raised edges to keep balls centered
    edge_lift = 0.3 * (x * x) / 4.0  # parabolic channel walls
    # Banking in the middle section
    if t > 0.35 and t < 0.55:
        bank = 0.3 * math.sin((t - 0.35) / 0.20 * pi) * x / 3.0
    else:
        bank = 0.0
    return base_y + edge_lift + bank


def _generate_track():
    """Generate the track as a heightmap grid of triangles."""
    x_min, x_max = -3.0, 3.0
    z_min, z_max = -15.0, 15.0
    x_steps = 4   # 4 columns (each ~1.5m wide)
    z_steps = 15  # 15 rows (each 2m long)
    dx = (x_max - x_min) / x_steps
    dz = (z_max - z_min) / z_steps

    triangles = []
    for zi in range(z_steps):
        for xi in range(x_steps):
            x0 = x_min + xi * dx
            x1 = x0 + dx
            z0 = z_min + zi * dz
            z1 = z0 + dz
            p00 = (x0, _terrain_height(x0, z0), z0)
            p10 = (x1, _terrain_height(x1, z0), z0)
            p01 = (x0, _terrain_height(x0, z1), z1)
            p11 = (x1, _terrain_height(x1, z1), z1)
            # Two triangles per quad
            triangles.append((p00, p10, p01))
            triangles.append((p10, p11, p01))
    return triangles


def run(session):
    reset_simulation(session)
    set_demo_info(session, "Demo 23: Ball Rollercoaster",
                  "Balls roll down a mesh terrain with drops, hills, and banked curves.")

    set_narration(session, "Building rollercoaster terrain...")
    smooth_camera(session, (8.0, 14.0, -10.0), (0.0, 4.0, 0.0), 1.5)
    sleep(1700)

    track_tris = _generate_track()
    print(f"  Track generated: {len(track_tris)} triangles")
    cmd = make_mesh_cmd(next_id("track"), (0.0, 0.0, 0.0), track_tris, 0.0)
    cmd = with_motion_type(cmd, 2)  # STATIC
    batch_add(session, [with_color_and_material(cmd, color=ACCENT_YELLOW, material=SLIPPERY_MATERIAL)])

    sleep(500)

    set_narration(session, "Releasing balls at the top!")
    smooth_camera(session, (4.0, 10.0, -16.0), (0.0, 8.0, -13.0), 1.5)
    sleep(1700)

    ball_colors = [PROJECTILE_COLOR, ACCENT_GREEN, TARGET_COLOR,
                   ACCENT_ORANGE, ACCENT_PURPLE, KINEMATIC_COLOR]
    ball_cmds = []
    for i in range(6):
        cmd = make_sphere_cmd(next_id("ball"), (0.0, 9.0, -14.5 + i * 0.6), 0.3, 2.0)
        ball_cmds.append(with_color_and_material(cmd, color=ball_colors[i]))
    batch_add(session, ball_cmds)

    # Watch the drop
    set_narration(session, "Steep drop — balls accelerating!")
    play(session)
    smooth_camera(session, (5.0, 7.0, -8.0), (0.0, 3.0, -3.0), 2.0)
    sleep(3500)

    # Hill
    set_narration(session, "Over the hill!")
    smooth_camera(session, (-5.0, 7.0, 0.0), (0.0, 3.0, 3.0), 2.0)
    sleep(3000)

    # Banked descent
    set_narration(session, "Banked descent")
    smooth_camera(session, (-6.0, 5.0, 6.0), (0.0, 1.5, 9.0), 2.0)
    sleep(3000)

    # Second hill
    set_narration(session, "Second hill and run-out")
    smooth_camera(session, (5.0, 5.0, 10.0), (0.0, 1.5, 14.0), 2.0)
    sleep(3000)

    # Wide overview
    set_narration(session, "Full terrain overview")
    smooth_camera(session, (12.0, 14.0, 0.0), (0.0, 3.0, 0.0), 2.0)
    sleep(2500)

    pause(session)
    clear_narration(session)
    print("  Rollercoaster demo complete!")
    list_bodies(session)


if __name__ == "__main__":
    run_standalone(run, name)
