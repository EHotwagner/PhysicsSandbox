"""Demo 22 — Camera Showcase: ~40 seconds showcasing all smooth camera modes and narration."""

from Scripting.demos_py.prelude import (
    batch_add,
    chase_body,
    clear_narration,
    follow_body,
    frame_bodies,
    look_at_body,
    make_box_cmd,
    make_capsule_cmd,
    make_color,
    make_impulse_cmd,
    make_sphere_cmd,
    next_id,
    orbit_body,
    pause,
    play,
    reset_simulation,
    run_standalone,
    set_camera,
    set_demo_info,
    set_narration,
    shake_camera,
    sleep,
    smooth_camera,
    stop_camera,
    with_color_and_material,
)

name = "Camera Showcase"
description = "Smooth camera transitions, body tracking, orbit, chase, framing, and shake."


def run(session):
    reset_simulation(session)
    set_demo_info(session, "Demo 22: Camera Showcase",
                  "Smooth camera transitions, body tracking, orbit, chase, framing, and shake.")

    # Build scene
    ball_id = next_id("sphere")
    batch_add(session, [with_color_and_material(
        make_sphere_cmd(ball_id, (0.0, 5.0, 0.0), 0.5, 5.0),
        color=make_color(1.0, 0.2, 0.1))])
    crate_id = next_id("box")
    batch_add(session, [with_color_and_material(
        make_box_cmd(crate_id, (4.0, 3.0, 0.0), (0.4, 0.4, 0.4), 10.0),
        color=make_color(0.3, 0.6, 1.0))])
    capsule_id = next_id("capsule")
    batch_add(session, [with_color_and_material(
        make_capsule_cmd(capsule_id, (-3.0, 4.0, 2.0), 0.3, 0.8, 4.0),
        color=make_color(0.2, 0.8, 0.3))])

    # 1. Smooth establishing shot
    set_narration(session, "Smooth Camera — gliding to an establishing shot")
    smooth_camera(session, (15.0, 10.0, 15.0), (0.0, 2.0, 0.0), 2.0)
    sleep(2200)

    # 2. Start simulation
    set_narration(session, "Starting simulation — watch the shapes fall")
    play(session)
    sleep(2000)

    # 3. Close-up
    set_narration(session, "Smooth Camera — zooming in to the action")
    smooth_camera(session, (3.0, 2.0, 5.0), (0.0, 0.5, 0.0), 1.5)
    sleep(1700)

    # 4. LookAt
    set_narration(session, "LookAt — smoothly orienting toward the red ball")
    look_at_body(session, ball_id, 1.5)
    sleep(1700)

    # 5. Follow
    set_narration(session, "Follow — camera target tracks the blue crate")
    follow_body(session, crate_id)
    sleep(3000)

    # 6. Pull back
    stop_camera(session)
    set_narration(session, "Smooth Camera — pulling back for orbit")
    smooth_camera(session, (8.0, 6.0, 8.0), (0.0, 1.0, 0.0), 1.5)
    sleep(1700)

    # 7. Orbit
    set_narration(session, "Orbit — revolving 360 degrees around the scene")
    anchor_id = next_id("box")
    batch_add(session, [make_box_cmd(anchor_id, (0.0, 0.5, 0.0), (0.1, 0.1, 0.1), 0.0)])
    orbit_body(session, anchor_id, 5.0, 360.0)
    sleep(5200)

    # 8. Chase
    set_narration(session, "Chase — following green capsule with fixed offset")
    chase_body(session, capsule_id, (3.0, 3.0, 5.0))
    sleep(3000)

    # 9. Frame
    stop_camera(session)
    set_narration(session, "Frame Bodies — auto-positioning to show all objects")
    frame_bodies(session, [ball_id, crate_id, capsule_id])
    sleep(3000)

    # 10. Shake
    set_narration(session, "Camera Shake — impact effect!")
    batch_add(session, [make_impulse_cmd(ball_id, (0.0, 15.0, 0.0))])
    shake_camera(session, 0.3, 1.0)
    sleep(1200)

    # 11. Final
    stop_camera(session)
    set_narration(session, "Final shot — smooth glide to overview")
    smooth_camera(session, (12.0, 8.0, 12.0), (0.0, 1.0, 0.0), 2.0)
    sleep(2200)

    pause(session)
    clear_narration(session)
    set_narration(session, "Camera Showcase complete!")
    sleep(2000)
    clear_narration(session)
    print("  Camera showcase complete — ~40 seconds of cinematic camera work")


if __name__ == "__main__":
    run_standalone(run, name)
