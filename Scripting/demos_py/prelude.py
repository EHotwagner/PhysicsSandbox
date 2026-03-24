"""Shared prelude for all Python physics demos.

Provides session management, simulation/view commands, message builders,
body presets, generators, steering, display helpers, and utility functions.
Mirrors the F# Prelude.fsx + PhysicsClient library API.
"""

import math
import sys
import time as _time
from contextlib import contextmanager
from dataclasses import dataclass
from enum import Enum
from random import Random

import grpc

from Scripting.demos_py.generated import physics_hub_pb2 as pb
from Scripting.demos_py.generated import physics_hub_pb2_grpc as pb_grpc

# ─── Session ────────────────────────────────────────────────────────────────


@dataclass
class Session:
    channel: grpc.Channel
    stub: pb_grpc.PhysicsHubStub
    address: str


def connect(address: str = "http://localhost:5180") -> Session:
    target = address.replace("http://", "").replace("https://", "")
    channel = grpc.insecure_channel(target)
    stub = pb_grpc.PhysicsHubStub(channel)
    return Session(channel=channel, stub=stub, address=address)


def disconnect(session: Session) -> None:
    session.channel.close()


# ─── Direction Enum ─────────────────────────────────────────────────────────


class Direction(Enum):
    Up = (0.0, 1.0, 0.0)
    Down = (0.0, -1.0, 0.0)
    North = (0.0, 0.0, -1.0)
    South = (0.0, 0.0, 1.0)
    East = (1.0, 0.0, 0.0)
    West = (-1.0, 0.0, 0.0)


# ─── ID Generation ─────────────────────────────────────────────────────────

_counters: dict[str, int] = {}


def next_id(prefix: str) -> str:
    _counters[prefix] = _counters.get(prefix, 0) + 1
    return f"{prefix}-{_counters[prefix]}"


def reset_ids() -> None:
    _counters.clear()


# ─── Utility Helpers ────────────────────────────────────────────────────────


def sleep(ms: int) -> None:
    _time.sleep(ms / 1000.0)


def to_vec3(x: float, y: float, z: float) -> pb.Vec3:
    return pb.Vec3(x=x, y=y, z=z)


def run_for(session: Session, seconds: float) -> None:
    play(session)
    sleep(int(seconds * 1000))
    pause(session)


@contextmanager
def timed(label: str):
    start = _time.perf_counter()
    yield
    elapsed_ms = int((_time.perf_counter() - start) * 1000)
    print(f"  [TIME] {label}: {elapsed_ms} ms")


# ─── Simulation Commands ───────────────────────────────────────────────────


def _send(session: Session, cmd: pb.SimulationCommand) -> pb.CommandAck:
    return session.stub.SendCommand(cmd)


def _send_view(session: Session, cmd: pb.ViewCommand) -> pb.CommandAck:
    return session.stub.SendViewCommand(cmd)


def play(session: Session) -> pb.CommandAck:
    return _send(session, pb.SimulationCommand(play_pause=pb.PlayPause(running=True)))


def pause(session: Session) -> pb.CommandAck:
    return _send(session, pb.SimulationCommand(play_pause=pb.PlayPause(running=False)))


def step(session: Session, delta_time: float = 1.0 / 60.0) -> pb.CommandAck:
    return _send(session, pb.SimulationCommand(step=pb.StepSimulation(delta_time=delta_time)))


def reset(session: Session) -> pb.CommandAck:
    return _send(session, pb.SimulationCommand(reset=pb.ResetSimulation()))


def set_gravity(session: Session, gravity: tuple[float, float, float]) -> pb.CommandAck:
    return _send(session, pb.SimulationCommand(
        set_gravity=pb.SetGravity(gravity=to_vec3(*gravity))))


def add_sphere(session: Session, pos: tuple[float, float, float],
               radius: float, mass: float, body_id: str | None = None) -> str:
    bid = body_id or next_id("sphere")
    _send(session, pb.SimulationCommand(add_body=pb.AddBody(
        id=bid, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(sphere=pb.Sphere(radius=radius)))))
    return bid


def add_box(session: Session, pos: tuple[float, float, float],
            half_extents: tuple[float, float, float], mass: float,
            body_id: str | None = None) -> str:
    bid = body_id or next_id("box")
    _send(session, pb.SimulationCommand(add_body=pb.AddBody(
        id=bid, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(box=pb.Box(half_extents=to_vec3(*half_extents))))))
    return bid


def add_plane(session: Session, normal: tuple[float, float, float] | None = None,
              body_id: str | None = None) -> str:
    bid = body_id or next_id("plane")
    n = normal or (0.0, 1.0, 0.0)
    _send(session, pb.SimulationCommand(add_body=pb.AddBody(
        id=bid, position=to_vec3(0, 0, 0), mass=0.0,
        shape=pb.Shape(plane=pb.Plane(normal=to_vec3(*n))))))
    return bid


def add_capsule(session: Session, pos: tuple[float, float, float],
                radius: float, length: float, mass: float,
                body_id: str | None = None,
                color: pb.Color | None = None,
                material: pb.MaterialProperties | None = None) -> str:
    bid = body_id or next_id("capsule")
    body = pb.AddBody(
        id=bid, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(capsule=pb.Capsule(radius=radius, length=length)))
    if color:
        body.color.CopyFrom(color)
    if material:
        body.material.CopyFrom(material)
    _send(session, pb.SimulationCommand(add_body=body))
    return bid


def add_cylinder(session: Session, pos: tuple[float, float, float],
                 radius: float, length: float, mass: float,
                 body_id: str | None = None,
                 color: pb.Color | None = None,
                 material: pb.MaterialProperties | None = None) -> str:
    bid = body_id or next_id("cylinder")
    body = pb.AddBody(
        id=bid, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(cylinder=pb.Cylinder(radius=radius, length=length)))
    if color:
        body.color.CopyFrom(color)
    if material:
        body.material.CopyFrom(material)
    _send(session, pb.SimulationCommand(add_body=body))
    return bid


def make_color(r: float, g: float, b: float, a: float = 1.0) -> pb.Color:
    return pb.Color(r=r, g=g, b=b, a=a)


def make_material(friction: float = 1.0, max_recovery: float = 2.0,
                  spring_freq: float = 30.0, spring_damping: float = 1.0) -> pb.MaterialProperties:
    return pb.MaterialProperties(
        friction=friction, max_recovery_velocity=max_recovery,
        spring_frequency=spring_freq, spring_damping_ratio=spring_damping)


BOUNCY_MATERIAL = make_material(0.4, 8.0, 60.0, 0.5)
STICKY_MATERIAL = make_material(2.0, 0.5, 30.0, 1.0)
SLIPPERY_MATERIAL = make_material(0.01, 2.0, 30.0, 1.0)


def make_capsule_cmd(body_id: str, pos: tuple[float, float, float],
                     radius: float, length: float, mass: float) -> pb.SimulationCommand:
    return pb.SimulationCommand(add_body=pb.AddBody(
        id=body_id, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(capsule=pb.Capsule(radius=radius, length=length))))


def make_cylinder_cmd(body_id: str, pos: tuple[float, float, float],
                      radius: float, length: float, mass: float) -> pb.SimulationCommand:
    return pb.SimulationCommand(add_body=pb.AddBody(
        id=body_id, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(cylinder=pb.Cylinder(radius=radius, length=length))))


def remove_body(session: Session, body_id: str) -> pb.CommandAck:
    return _send(session, pb.SimulationCommand(
        remove_body=pb.RemoveBody(body_id=body_id)))


def clear_all(session: Session) -> None:
    state = get_state(session)
    if state:
        for body in state.bodies:
            if not body.is_static:
                remove_body(session, body.id)


def apply_force(session: Session, body_id: str,
                force: tuple[float, float, float]) -> pb.CommandAck:
    return _send(session, pb.SimulationCommand(
        apply_force=pb.ApplyForce(body_id=body_id, force=to_vec3(*force))))


def apply_impulse(session: Session, body_id: str,
                  impulse: tuple[float, float, float]) -> pb.CommandAck:
    return _send(session, pb.SimulationCommand(
        apply_impulse=pb.ApplyImpulse(body_id=body_id, impulse=to_vec3(*impulse))))


def apply_torque(session: Session, body_id: str,
                 torque: tuple[float, float, float]) -> pb.CommandAck:
    return _send(session, pb.SimulationCommand(
        apply_torque=pb.ApplyTorque(body_id=body_id, torque=to_vec3(*torque))))


def clear_forces(session: Session, body_id: str) -> pb.CommandAck:
    return _send(session, pb.SimulationCommand(
        clear_forces=pb.ClearForces(body_id=body_id)))


# ─── View Commands ──────────────────────────────────────────────────────────


def set_camera(session: Session, position: tuple[float, float, float],
               target: tuple[float, float, float]) -> pb.CommandAck:
    return _send_view(session, pb.ViewCommand(
        set_camera=pb.SetCamera(
            position=to_vec3(*position), target=to_vec3(*target),
            up=to_vec3(0, 1, 0))))


def wireframe(session: Session, enabled: bool) -> pb.CommandAck:
    return _send_view(session, pb.ViewCommand(
        toggle_wireframe=pb.ToggleWireframe(enabled=enabled)))


def set_zoom(session: Session, level: float) -> pb.CommandAck:
    return _send_view(session, pb.ViewCommand(
        set_zoom=pb.SetZoom(level=level)))


def set_demo_info(session: Session, name: str, description: str) -> None:
    _send_view(session, pb.ViewCommand(
        set_demo_metadata=pb.SetDemoMetadata(name=name, description=description)))


def smooth_camera(session: Session, position: tuple[float, float, float],
                  target: tuple[float, float, float], duration_seconds: float) -> None:
    _send_view(session, pb.ViewCommand(
        smooth_camera=pb.SmoothCamera(
            position=to_vec3(*position), target=to_vec3(*target),
            up=to_vec3(0, 1, 0), duration_seconds=duration_seconds)))


def look_at_body(session: Session, body_id: str, duration_seconds: float) -> None:
    _send_view(session, pb.ViewCommand(
        camera_look_at=pb.CameraLookAt(body_id=body_id, duration_seconds=duration_seconds)))


def follow_body(session: Session, body_id: str) -> None:
    _send_view(session, pb.ViewCommand(
        camera_follow=pb.CameraFollow(body_id=body_id)))


def orbit_body(session: Session, body_id: str, duration_seconds: float, degrees: float = 360.0) -> None:
    _send_view(session, pb.ViewCommand(
        camera_orbit=pb.CameraOrbit(body_id=body_id, duration_seconds=duration_seconds, degrees=degrees)))


def chase_body(session: Session, body_id: str, offset: tuple[float, float, float]) -> None:
    _send_view(session, pb.ViewCommand(
        camera_chase=pb.CameraChase(body_id=body_id, offset=to_vec3(*offset))))


def frame_bodies(session: Session, body_ids: list[str]) -> None:
    _send_view(session, pb.ViewCommand(
        camera_frame_bodies=pb.CameraFrameBodies(body_ids=body_ids)))


def shake_camera(session: Session, intensity: float, duration_seconds: float) -> None:
    _send_view(session, pb.ViewCommand(
        camera_shake=pb.CameraShake(intensity=intensity, duration_seconds=duration_seconds)))


def stop_camera(session: Session) -> None:
    _send_view(session, pb.ViewCommand(camera_stop=pb.CameraStop()))


def set_narration(session: Session, text: str) -> None:
    _send_view(session, pb.ViewCommand(
        set_narration=pb.SetNarration(text=text)))


def clear_narration(session: Session) -> None:
    _send_view(session, pb.ViewCommand(
        set_narration=pb.SetNarration(text="")))


# ─── Message Construction Helpers ───────────────────────────────────────────


def make_sphere_cmd(body_id: str, pos: tuple[float, float, float],
                    radius: float, mass: float) -> pb.SimulationCommand:
    return pb.SimulationCommand(add_body=pb.AddBody(
        id=body_id, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(sphere=pb.Sphere(radius=radius))))


def make_box_cmd(body_id: str, pos: tuple[float, float, float],
                 half_extents: tuple[float, float, float],
                 mass: float) -> pb.SimulationCommand:
    return pb.SimulationCommand(add_body=pb.AddBody(
        id=body_id, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(box=pb.Box(half_extents=to_vec3(*half_extents)))))


def make_impulse_cmd(body_id: str,
                     impulse: tuple[float, float, float]) -> pb.SimulationCommand:
    return pb.SimulationCommand(
        apply_impulse=pb.ApplyImpulse(body_id=body_id, impulse=to_vec3(*impulse)))


def make_torque_cmd(body_id: str,
                    torque: tuple[float, float, float]) -> pb.SimulationCommand:
    return pb.SimulationCommand(
        apply_torque=pb.ApplyTorque(body_id=body_id, torque=to_vec3(*torque)))


# ─── Batch Helpers ──────────────────────────────────────────────────────────


def batch_commands(session: Session,
                   commands: list[pb.SimulationCommand]) -> pb.BatchResponse:
    return session.stub.SendBatchCommand(
        pb.BatchSimulationRequest(commands=commands))


def batch_view_commands(session: Session,
                        commands: list[pb.ViewCommand]) -> pb.BatchResponse:
    return session.stub.SendBatchViewCommand(
        pb.BatchViewRequest(commands=commands))


def batch_add(session: Session, commands: list[pb.SimulationCommand]) -> None:
    for i in range(0, len(commands), 100):
        chunk = commands[i:i + 100]
        response = batch_commands(session, chunk)
        for r in response.results:
            if not r.success:
                print(f"  [BATCH FAIL] command {r.index}: {r.message}")


# ─── Body Presets ───────────────────────────────────────────────────────────


def marble(session: Session, pos: tuple[float, float, float] | None = None,
           mass: float | None = None, body_id: str | None = None) -> str:
    return add_sphere(session, pos or (0, 0, 0), 0.01, mass or 0.005, body_id)


def bowling_ball(session: Session, pos: tuple[float, float, float] | None = None,
                 mass: float | None = None, body_id: str | None = None) -> str:
    return add_sphere(session, pos or (0, 0, 0), 0.11, mass or 6.35, body_id)


def beach_ball(session: Session, pos: tuple[float, float, float] | None = None,
               mass: float | None = None, body_id: str | None = None) -> str:
    return add_sphere(session, pos or (0, 0, 0), 0.2, mass or 0.1, body_id)


def boulder(session: Session, pos: tuple[float, float, float] | None = None,
            mass: float | None = None, body_id: str | None = None) -> str:
    return add_sphere(session, pos or (0, 0, 0), 0.5, mass or 200.0, body_id)


def crate(session: Session, pos: tuple[float, float, float] | None = None,
          mass: float | None = None, body_id: str | None = None) -> str:
    return add_box(session, pos or (0, 0, 0), (0.5, 0.5, 0.5), mass or 20.0, body_id)


def brick(session: Session, pos: tuple[float, float, float] | None = None,
          mass: float | None = None, body_id: str | None = None) -> str:
    return add_box(session, pos or (0, 0, 0), (0.2, 0.1, 0.05), mass or 3.0, body_id)


def die(session: Session, pos: tuple[float, float, float] | None = None,
        mass: float | None = None, body_id: str | None = None) -> str:
    return add_box(session, pos or (0, 0, 0), (0.05, 0.05, 0.05), mass or 0.03, body_id)


# ─── Generators ─────────────────────────────────────────────────────────────


def stack(session: Session, count: int,
          pos: tuple[float, float, float] | None = None) -> list[str]:
    base = pos or (0, 0, 0)
    ids = []
    for i in range(count):
        y = base[1] + 0.5 + i * 1.0
        bid = add_box(session, (base[0], y, base[2]), (0.5, 0.5, 0.5), 20.0)
        ids.append(bid)
    return ids


def pyramid(session: Session, layers: int,
            pos: tuple[float, float, float] | None = None) -> list[str]:
    base = pos or (0, 0, 0)
    ids = []
    for layer in range(layers):
        count = layers - layer
        y = base[1] + 0.5 + layer * 1.0
        x_offset = base[0] - (count - 1) * 0.5
        for col in range(count):
            x = x_offset + col * 1.0
            bid = add_box(session, (x, y, base[2]), (0.5, 0.5, 0.5), 20.0)
            ids.append(bid)
    return ids


def row(session: Session, count: int,
        pos: tuple[float, float, float] | None = None) -> list[str]:
    base = pos or (0, 0, 0)
    ids = []
    for i in range(count):
        x = base[0] + i * 0.5
        bid = add_sphere(session, (x, base[1] + 0.2, base[2]), 0.2, 1.0)
        ids.append(bid)
    return ids


def grid(session: Session, rows: int, cols: int,
         pos: tuple[float, float, float] | None = None) -> list[str]:
    base = pos or (0, 0, 0)
    ids = []
    for r in range(rows):
        for c in range(cols):
            x = base[0] + c * 1.0
            z = base[2] + r * 1.0
            bid = add_box(session, (x, base[1] + 0.5, z), (0.5, 0.5, 0.5), 20.0)
            ids.append(bid)
    return ids


def random_spheres(session: Session, count: int,
                   seed: int | None = None) -> list[str]:
    rng = Random(seed)
    ids = []
    for _ in range(count):
        x = rng.uniform(-5.0, 5.0)
        y = rng.uniform(1.0, 10.0)
        z = rng.uniform(-5.0, 5.0)
        radius = rng.uniform(0.05, 0.5)
        mass = rng.uniform(0.1, 50.0)
        bid = add_sphere(session, (x, y, z), radius, mass)
        ids.append(bid)
    return ids


# ─── Steering ───────────────────────────────────────────────────────────────


def push(session: Session, body_id: str, direction: Direction,
         magnitude: float) -> pb.CommandAck:
    dx, dy, dz = direction.value
    return apply_impulse(session, body_id,
                         (dx * magnitude, dy * magnitude, dz * magnitude))


def launch(session: Session, body_id: str,
           target: tuple[float, float, float], speed: float) -> pb.CommandAck:
    state = get_state(session)
    if not state:
        raise RuntimeError("Cannot get simulation state for launch")
    body = None
    for b in state.bodies:
        if b.id == body_id:
            body = b
            break
    if body is None:
        raise RuntimeError(f"Body '{body_id}' not found in simulation state")
    dx = target[0] - body.position.x
    dy = target[1] - body.position.y
    dz = target[2] - body.position.z
    length = math.sqrt(dx * dx + dy * dy + dz * dz)
    if length < 0.001:
        raise RuntimeError("Launch target too close to body position")
    nx, ny, nz = dx / length, dy / length, dz / length
    return apply_impulse(session, body_id, (nx * speed, ny * speed, nz * speed))


# ─── Display Helpers ────────────────────────────────────────────────────────


def get_state(session: Session) -> pb.SimulationState | None:
    """Get current simulation state by merging TickState + PropertySnapshot."""
    try:
        # Get property snapshot (semi-static body properties, constraints, shapes)
        props_cache: dict[str, pb.BodyProperties] = {}
        constraints = []
        registered_shapes = []
        try:
            prop_stream = session.stub.StreamProperties(pb.StateRequest())
            evt = next(prop_stream)
            if evt.HasField("snapshot"):
                for bp in evt.snapshot.bodies:
                    props_cache[bp.id] = bp
                constraints = list(evt.snapshot.constraints)
                registered_shapes = list(evt.snapshot.registered_shapes)
        except Exception:
            pass

        # Get lean tick state (dynamic body poses + velocity)
        stream = session.stub.StreamState(pb.StateRequest())
        tick = next(stream)

        # Reconstruct full SimulationState
        state = pb.SimulationState()
        state.time = tick.time
        state.running = tick.running
        state.tick_ms = tick.tick_ms
        state.serialize_ms = tick.serialize_ms

        dynamic_ids = set()
        for pose in tick.bodies:
            dynamic_ids.add(pose.id)
            body = pb.Body()
            body.id = pose.id
            body.position.CopyFrom(pose.position)
            body.orientation.CopyFrom(pose.orientation)
            if pose.HasField("velocity"):
                body.velocity.CopyFrom(pose.velocity)
            if pose.HasField("angular_velocity"):
                body.angular_velocity.CopyFrom(pose.angular_velocity)
            # Merge semi-static properties from cache
            if pose.id in props_cache:
                p = props_cache[pose.id]
                if p.HasField("shape"):
                    body.shape.CopyFrom(p.shape)
                if p.HasField("color"):
                    body.color.CopyFrom(p.color)
                body.mass = p.mass
                body.is_static = p.is_static
                body.motion_type = p.motion_type
                body.collision_group = p.collision_group
                body.collision_mask = p.collision_mask
                if p.HasField("material"):
                    body.material.CopyFrom(p.material)
            state.bodies.append(body)

        # Add static bodies from property cache
        for bid, p in props_cache.items():
            if p.is_static and bid not in dynamic_ids:
                body = pb.Body()
                body.id = p.id
                body.is_static = True
                body.motion_type = p.motion_type
                body.mass = p.mass
                if p.HasField("shape"):
                    body.shape.CopyFrom(p.shape)
                if p.HasField("color"):
                    body.color.CopyFrom(p.color)
                if p.HasField("position"):
                    body.position.CopyFrom(p.position)
                if p.HasField("orientation"):
                    body.orientation.CopyFrom(p.orientation)
                if p.HasField("material"):
                    body.material.CopyFrom(p.material)
                body.collision_group = p.collision_group
                body.collision_mask = p.collision_mask
                state.bodies.append(body)

        for cs in constraints:
            state.constraints.append(cs)
        for rs in registered_shapes:
            state.registered_shapes.append(rs)
        for qr in tick.query_responses:
            state.query_responses.append(qr)

        return state
    except Exception:
        return None


def _shape_desc(body: pb.Body) -> str:
    shape = body.shape
    if shape.HasField("sphere"):
        return f"Sphere(r={shape.sphere.radius:.3f})"
    elif shape.HasField("box"):
        he = shape.box.half_extents
        return f"Box({he.x:.2f}x{he.y:.2f}x{he.z:.2f})"
    elif shape.HasField("plane"):
        return "Plane"
    return "Unknown"


def _vel_mag(v: pb.Vec3) -> float:
    return math.sqrt(v.x * v.x + v.y * v.y + v.z * v.z)


def list_bodies(session: Session) -> None:
    state = get_state(session)
    if not state:
        print("  No state available")
        return
    bodies = [b for b in state.bodies if not b.is_static]
    print(f"  Bodies: {len(bodies)}")
    print(f"  {'ID':<20} {'Shape':<25} {'Position':<30} {'Vel':>8}")
    print(f"  {'─'*20} {'─'*25} {'─'*30} {'─'*8}")
    for b in bodies[:20]:
        p = b.position
        pos_str = f"({p.x:7.2f}, {p.y:7.2f}, {p.z:7.2f})"
        vel = _vel_mag(b.velocity)
        print(f"  {b.id:<20} {_shape_desc(b):<25} {pos_str:<30} {vel:8.3f}")
    if len(bodies) > 20:
        print(f"  ... and {len(bodies) - 20} more")


def status(session: Session) -> None:
    state = get_state(session)
    if not state:
        print("  No state available")
        return
    dynamic = sum(1 for b in state.bodies if not b.is_static)
    static = sum(1 for b in state.bodies if b.is_static)
    print(f"  Status: time={state.time:.2f}s running={state.running} "
          f"bodies={dynamic} (+ {static} static) "
          f"tick={state.tick_ms:.1f}ms serialize={state.serialize_ms:.1f}ms")


# ─── Reset Simulation ──────────────────────────────────────────────────────


def reset_simulation(session: Session) -> None:
    pause(session)
    try:
        reset(session)
    except Exception as ex:
        print(f"  [RESET ERROR] {ex} — falling back to manual clear")
        clear_all(session)
    reset_ids()
    add_plane(session)
    set_gravity(session, (0.0, -9.81, 0.0))
    sleep(100)


# ─── Standalone Runner Helper ──────────────────────────────────────────────


# ─── Color Palette ─────────────────────────────────────────────────────────

PROJECTILE_COLOR = make_color(1.0, 0.2, 0.1)
TARGET_COLOR = make_color(0.3, 0.6, 1.0)
STRUCTURE_COLOR = make_color(0.7, 0.7, 0.7)
ACCENT_YELLOW = make_color(1.0, 0.8, 0.0)
ACCENT_GREEN = make_color(0.2, 0.8, 0.3)
ACCENT_PURPLE = make_color(0.8, 0.4, 1.0)
ACCENT_ORANGE = make_color(1.0, 0.5, 0.0)
KINEMATIC_COLOR = make_color(0.0, 1.0, 1.0)


# ─── Advanced Shape Commands ──────────────────────────────────────────────


def make_triangle_cmd(body_id: str, pos: tuple[float, float, float],
                      a: tuple[float, float, float],
                      b: tuple[float, float, float],
                      c: tuple[float, float, float],
                      mass: float) -> pb.SimulationCommand:
    return pb.SimulationCommand(add_body=pb.AddBody(
        id=body_id, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(triangle=pb.Triangle(
            a=to_vec3(*a), b=to_vec3(*b), c=to_vec3(*c)))))


def make_convex_hull_cmd(body_id: str, pos: tuple[float, float, float],
                         points: list[tuple[float, float, float]],
                         mass: float) -> pb.SimulationCommand:
    hull_points = [to_vec3(*p) for p in points]
    return pb.SimulationCommand(add_body=pb.AddBody(
        id=body_id, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(convex_hull=pb.ConvexHull(points=hull_points))))


def make_compound_cmd(body_id: str, pos: tuple[float, float, float],
                      children: list[tuple[pb.Shape, tuple[float, float, float]]],
                      mass: float) -> pb.SimulationCommand:
    compound_children = []
    for child_shape, local_pos in children:
        compound_children.append(pb.CompoundChild(
            shape=child_shape, local_position=to_vec3(*local_pos),
            local_orientation=pb.Vec4(x=0, y=0, z=0, w=1)))
    return pb.SimulationCommand(add_body=pb.AddBody(
        id=body_id, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(compound=pb.Compound(children=compound_children))))


def make_mesh_cmd(body_id: str, pos: tuple[float, float, float],
                   triangles: list[tuple[tuple[float, float, float],
                                         tuple[float, float, float],
                                         tuple[float, float, float]]],
                   mass: float) -> pb.SimulationCommand:
    mesh_tris = [pb.MeshTriangle(a=to_vec3(*a), b=to_vec3(*b), c=to_vec3(*c))
                 for a, b, c in triangles]
    return pb.SimulationCommand(add_body=pb.AddBody(
        id=body_id, position=to_vec3(*pos), mass=mass,
        shape=pb.Shape(mesh=pb.MeshShape(triangles=mesh_tris))))


# ─── Kinematic & Filter Helpers ────────────────────────────────────────────


def with_color_and_material(cmd: pb.SimulationCommand,
                            color: pb.Color | None = None,
                            material: pb.MaterialProperties | None = None) -> pb.SimulationCommand:
    if color:
        cmd.add_body.color.CopyFrom(color)
    if material:
        cmd.add_body.material.CopyFrom(material)
    return cmd


def with_motion_type(cmd: pb.SimulationCommand,
                     motion_type: int) -> pb.SimulationCommand:
    cmd.add_body.motion_type = motion_type
    return cmd


def with_collision_filter(cmd: pb.SimulationCommand,
                          group: int, mask: int) -> pb.SimulationCommand:
    cmd.add_body.collision_group = group
    cmd.add_body.collision_mask = mask
    return cmd


def make_kinematic_cmd(body_id: str, pos: tuple[float, float, float],
                       shape: pb.Shape) -> pb.SimulationCommand:
    return pb.SimulationCommand(add_body=pb.AddBody(
        id=body_id, position=to_vec3(*pos), mass=0.0,
        motion_type=1, shape=shape))  # 1 = KINEMATIC


def set_body_pose(session: Session, body_id: str,
                  pos: tuple[float, float, float]) -> pb.CommandAck:
    return _send(session, pb.SimulationCommand(
        set_body_pose=pb.SetBodyPose(
            body_id=body_id, position=to_vec3(*pos))))


# ─── Constraint Helpers ────────────────────────────────────────────────────


def make_ball_socket_cmd(constraint_id: str, body_a: str, body_b: str,
                         offset_a: tuple[float, float, float],
                         offset_b: tuple[float, float, float]) -> pb.SimulationCommand:
    ct = pb.ConstraintType(ball_socket=pb.BallSocketConstraint(
        local_offset_a=to_vec3(*offset_a), local_offset_b=to_vec3(*offset_b)))
    return pb.SimulationCommand(add_constraint=pb.AddConstraint(
        id=constraint_id, body_a=body_a, body_b=body_b, type=ct))


def make_hinge_cmd(constraint_id: str, body_a: str, body_b: str,
                   axis: tuple[float, float, float],
                   offset_a: tuple[float, float, float],
                   offset_b: tuple[float, float, float]) -> pb.SimulationCommand:
    ct = pb.ConstraintType(hinge=pb.HingeConstraint(
        local_hinge_axis_a=to_vec3(*axis), local_hinge_axis_b=to_vec3(*axis),
        local_offset_a=to_vec3(*offset_a), local_offset_b=to_vec3(*offset_b)))
    return pb.SimulationCommand(add_constraint=pb.AddConstraint(
        id=constraint_id, body_a=body_a, body_b=body_b, type=ct))


def make_weld_cmd(constraint_id: str, body_a: str,
                  body_b: str) -> pb.SimulationCommand:
    spring = pb.SpringSettings(frequency=30.0, damping_ratio=1.0)
    ct = pb.ConstraintType(weld=pb.WeldConstraint(
        local_offset=to_vec3(0, 0, 0),
        local_orientation=pb.Vec4(x=0, y=0, z=0, w=1), spring=spring))
    return pb.SimulationCommand(add_constraint=pb.AddConstraint(
        id=constraint_id, body_a=body_a, body_b=body_b, type=ct))


def make_distance_limit_cmd(constraint_id: str, body_a: str, body_b: str,
                            min_dist: float,
                            max_dist: float) -> pb.SimulationCommand:
    spring = pb.SpringSettings(frequency=30.0, damping_ratio=1.0)
    ct = pb.ConstraintType(distance_limit=pb.DistanceLimitConstraint(
        local_offset_a=to_vec3(0, 0, 0), local_offset_b=to_vec3(0, 0, 0),
        min_distance=min_dist, max_distance=max_dist, spring=spring))
    return pb.SimulationCommand(add_constraint=pb.AddConstraint(
        id=constraint_id, body_a=body_a, body_b=body_b, type=ct))


# ─── Query Helpers ─────────────────────────────────────────────────────────


def query_raycast(session: Session, origin: tuple[float, float, float],
                  direction: tuple[float, float, float],
                  max_distance: float = 1000.0) -> list[tuple[str, tuple, tuple, float]]:
    resp = session.stub.Raycast(pb.RaycastRequest(
        origin=to_vec3(*origin), direction=to_vec3(*direction),
        max_distance=max_distance, all_hits=False))
    if resp.hit:
        return [(h.body_id,
                 (h.position.x, h.position.y, h.position.z),
                 (h.normal.x, h.normal.y, h.normal.z),
                 h.distance) for h in resp.hits]
    return []


def query_overlap_sphere(session: Session, radius: float,
                         position: tuple[float, float, float]) -> list[str]:
    shape = pb.Shape(sphere=pb.Sphere(radius=radius))
    resp = session.stub.Overlap(pb.OverlapRequest(
        shape=shape, position=to_vec3(*position)))
    return list(resp.body_ids)


def query_sweep_sphere(session: Session, radius: float,
                       start: tuple[float, float, float],
                       direction: tuple[float, float, float],
                       max_distance: float) -> tuple | None:
    shape = pb.Shape(sphere=pb.Sphere(radius=radius))
    resp = session.stub.SweepCast(pb.SweepCastRequest(
        shape=shape, start_position=to_vec3(*start),
        direction=to_vec3(*direction), max_distance=max_distance))
    if resp.hit:
        h = resp.closest
        return (h.body_id,
                (h.position.x, h.position.y, h.position.z),
                (h.normal.x, h.normal.y, h.normal.z),
                h.distance)
    return None


# ─── Standalone Runner Helper ──────────────────────────────────────────────


def run_standalone(run_fn, name: str = "Demo") -> None:
    addr = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5180"
    print(f"Connecting to {addr}...")
    s = connect(addr)
    try:
        print(f"Running {name}...")
        run_fn(s)
        print(f"\n  ✓ {name} complete")
    except Exception as ex:
        print(f"\n  ✗ {name} FAILED: {ex}")
    finally:
        reset_simulation(s)
        disconnect(s)
