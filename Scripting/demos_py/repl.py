"""Python REPL for PhysicsSandbox.

Launch: python3 -i -m Scripting.demos_py.repl

Drops you into an interactive Python session with all prelude functions
(connect, play, pause, add_sphere, marble, list_bodies, etc.) available.
"""

from Scripting.demos_py.prelude import *  # noqa: F401,F403

print("PhysicsSandbox Python REPL loaded.")
print('Use s = connect("http://localhost:5180") to begin.')
