"""MCP Tool Test Runner — tests all PhysicsSandbox MCP tools and generates a report."""
import json, urllib.request, threading, queue, time, sys
from datetime import datetime

MCP_URL = "http://localhost:5199"

class McpClient:
    def __init__(self, url):
        self.url = url
        self.msg_q = queue.Queue()
        self._id = 0
        eq = queue.Queue()
        def sse():
            req = urllib.request.Request(f"{url}/sse", headers={"Accept":"text/event-stream"})
            resp = urllib.request.urlopen(req, timeout=120)
            buf, et = b"", None
            while True:
                c = resp.read(1)
                if not c: break
                buf += c
                if buf.endswith(b"\n\n"):
                    for l in buf.decode().strip().split("\n"):
                        if l.startswith("event:"): et = l[6:].strip()
                        elif l.startswith("data:"):
                            d = l[5:].strip()
                            if et == "endpoint": eq.put(d)
                            elif et == "message":
                                try: self.msg_q.put(json.loads(d))
                                except: pass
                    buf, et = b"", None
        threading.Thread(target=sse, daemon=True).start()
        ep = eq.get(timeout=10)
        self.msg_url = f"{url}{ep}" if ep.startswith("/") else ep
        self._send("initialize", {"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1"}})
        self._post({"jsonrpc":"2.0","method":"notifications/initialized"})
        time.sleep(0.3)

    def _post(self, payload):
        urllib.request.urlopen(urllib.request.Request(self.msg_url, json.dumps(payload).encode(),
            headers={"Content-Type":"application/json"}), timeout=15).read()

    def _send(self, method, params=None):
        self._id += 1
        p = {"jsonrpc":"2.0","method":method,"id":self._id}
        if params: p["params"] = params
        self._post(p)
        return self.msg_q.get(timeout=15)

    def list_tools(self):
        return self._send("tools/list", {}).get("result",{}).get("tools",[])

    def call(self, name, args=None):
        p = {"name": name}
        if args: p["arguments"] = args
        return self._send("tools/call", p)

def test(client, name, args=None):
    t0 = time.time()
    try:
        r = client.call(name, args)
        ms = int((time.time()-t0)*1000)
        if "error" in r:
            return {"tool":name,"status":"RPC_ERROR","ms":ms,"msg":str(r["error"].get("message",""))[:300]}
        res = r.get("result",{})
        txt = (res.get("content",[{}])[0].get("text","") if res.get("content") else "")[:300]
        if res.get("isError"):
            return {"tool":name,"status":"TOOL_ERROR","ms":ms,"msg":txt}
        return {"tool":name,"status":"OK","ms":ms,"msg":txt}
    except Exception as e:
        return {"tool":name,"status":"EXCEPTION","ms":int((time.time()-t0)*1000),"msg":str(e)[:200]}

def main():
    print(f"Connecting to {MCP_URL}...")
    c = McpClient(MCP_URL)
    tools = c.list_tools()
    print(f"Found {len(tools)} tools\n")
    R = []

    # QUERY & STATE
    R.append(test(c, "get_status"))
    R.append(test(c, "get_state"))

    # SIMULATION CORE
    R.append(test(c, "restart_simulation"))
    R.append(test(c, "set_gravity", {"x":0,"y":-9.81,"z":0}))
    R.append(test(c, "add_body", {"shape":"sphere","radius":0.5,"x":0,"y":5,"z":0,"mass":2}))
    R.append(test(c, "add_body", {"shape":"box","half_extents_x":0.5,"half_extents_y":0.5,"half_extents_z":0.5,"x":3,"y":5,"z":0,"mass":5}))
    R.append(test(c, "apply_force", {"body_id":"sphere-1","x":10,"y":0,"z":0}))
    R.append(test(c, "apply_impulse", {"body_id":"sphere-1","x":0,"y":5,"z":0}))
    R.append(test(c, "apply_torque", {"body_id":"box-1","x":0,"y":1,"z":0}))
    R.append(test(c, "step"))
    R.append(test(c, "play"))
    time.sleep(0.5)
    R.append(test(c, "pause"))
    R.append(test(c, "clear_forces", {"body_id":"sphere-1"}))

    # CONSTRAINTS
    R.append(test(c, "add_constraint", {"constraint_type":"ball_socket","body_a":"sphere-1","body_b":"box-1",
        "offset_ax":0.5,"offset_ay":0,"offset_az":0,"offset_bx":-0.5,"offset_by":0,"offset_bz":0}))
    R.append(test(c, "remove_constraint", {"constraint_id":"constraint-1"}))
    R.append(test(c, "remove_body", {"body_id":"box-1"}))

    # SHAPE & COLLISION
    R.append(test(c, "register_shape", {"shape_handle":"test-shape","shape":"sphere","radius":0.3}))
    R.append(test(c, "unregister_shape", {"shape_handle":"test-shape"}))
    R.append(test(c, "set_collision_filter", {"body_id":"sphere-1","collision_group":1,"collision_mask":4294967295}))
    R.append(test(c, "set_body_pose", {"body_id":"sphere-1","x":0,"y":3,"z":0}))
    R.append(test(c, "raycast", {"origin_x":0,"origin_y":10,"origin_z":0,"direction_x":0,"direction_y":-1,"direction_z":0,"max_distance":20}))
    R.append(test(c, "sweep_cast", {"shape":"sphere","radius":0.2,"start_x":5,"start_y":5,"start_z":0,
        "direction_x":-1,"direction_y":0,"direction_z":0,"max_distance":20}))
    R.append(test(c, "overlap", {"shape":"sphere","radius":2,"x":0,"y":3,"z":0}))

    # PRESETS
    for p in ["add_marble","add_bowling_ball","add_beach_ball","add_crate","add_brick","add_boulder","add_die"]:
        R.append(test(c, p, {"x":0,"y":8,"z":0,"mass":1,"id":""}))

    # GENERATORS
    R.append(test(c, "generate_random_bodies", {"count":5}))
    R.append(test(c, "generate_stack", {"count":3,"x":5,"y":0.5,"z":5}))
    R.append(test(c, "generate_row", {"count":4,"x":-5,"y":1,"z":0}))
    R.append(test(c, "generate_grid", {"rows":2,"cols":2,"x":-5,"y":0.5,"z":-5}))
    R.append(test(c, "generate_pyramid", {"layers":3,"x":0,"y":0.5,"z":-5}))

    # STEERING
    R.append(test(c, "push_body", {"body_id":"sphere-1","direction":"up","strength":10}))
    R.append(test(c, "launch_body", {"body_id":"sphere-1","target_x":5,"target_y":5,"target_z":0,"speed":10}))
    R.append(test(c, "spin_body", {"body_id":"sphere-1","axis":"y","strength":5}))
    R.append(test(c, "stop_body", {"body_id":"sphere-1"}))

    # VIEW
    R.append(test(c, "set_camera", {"pos_x":10,"pos_y":10,"pos_z":10,"target_x":0,"target_y":0,"target_z":0}))
    R.append(test(c, "set_zoom", {"level":1.5}))
    R.append(test(c, "toggle_wireframe"))

    # BATCH
    R.append(test(c, "batch_commands", {"commands":json.dumps([
        {"type":"add_body","id":"batch-1","shape":"sphere","radius":0.3,"x":1,"y":5,"z":1,"mass":1},
        {"type":"step"}])}))
    R.append(test(c, "batch_view_commands", {"commands":json.dumps([
        {"type":"set_camera","pos_x":5,"pos_y":5,"pos_z":5,"target_x":0,"target_y":0,"target_z":0}])}))

    # RECORDING
    R.append(test(c, "recording_status"))
    R.append(test(c, "start_recording", {"label":"test-session"}))
    # Run sim briefly for recording content
    test(c, "play"); time.sleep(1); test(c, "pause")
    R.append(test(c, "stop_recording"))
    R.append(test(c, "list_sessions"))

    # Get session ID from list
    sess_resp = c.call("list_sessions")
    sess_text = sess_resp.get("result",{}).get("content",[{}])[0].get("text","")
    sid = None
    for line in sess_text.split("\n"):
        for word in line.split():
            w = word.strip("(),:")
            if len(w) >= 20 and "-" in w and not w.startswith("http"):
                sid = w
                break
        if sid: break

    if sid:
        R.append(test(c, "query_summary", {"session_id":sid}))
        R.append(test(c, "query_snapshots", {"session_id":sid}))
        R.append(test(c, "query_events", {"session_id":sid}))
        R.append(test(c, "query_body_trajectory", {"body_id":"sphere-1","session_id":sid}))
        R.append(test(c, "query_mesh_fetches", {"session_id":sid}))
        R.append(test(c, "delete_session", {"session_id":sid}))
    else:
        for t in ["query_summary","query_snapshots","query_events","query_body_trajectory","query_mesh_fetches","delete_session"]:
            R.append({"tool":t,"status":"SKIPPED","ms":0,"msg":"No session ID found"})

    # METRICS & AUDIT
    R.append(test(c, "get_metrics"))
    R.append(test(c, "get_diagnostics"))
    R.append(test(c, "get_command_log", {"count":10}))

    # STRESS TEST
    st = test(c, "start_stress_test", {"scenario":"command-throughput","duration_seconds":2})
    R.append(st)
    # Extract test_id
    test_id = None
    if st["status"] == "OK":
        for word in st["msg"].split():
            w = word.strip("(),:")
            if len(w) >= 20 and "-" in w:
                test_id = w
                break
    time.sleep(4)
    R.append(test(c, "get_stress_test_status", {"test_id": test_id or "unknown"}))

    # COMPARISON
    comp = test(c, "start_comparison_test", {"body_count":5,"step_count":3})
    R.append(comp)
    comp_id = None
    if comp["status"] == "OK":
        for word in comp["msg"].split():
            w = word.strip("(),:")
            if len(w) >= 20 and "-" in w:
                comp_id = w
                break
    time.sleep(4)
    r2 = test(c, "get_stress_test_status", {"test_id": comp_id or "unknown"})
    r2["tool"] = "comparison_test_result"
    R.append(r2)

    # Toggle wireframe back
    test(c, "toggle_wireframe")

    # RESULTS
    ok = sum(1 for r in R if r["status"]=="OK")
    errs = sum(1 for r in R if r["status"] in ("RPC_ERROR","TOOL_ERROR","EXCEPTION"))
    skip = sum(1 for r in R if r["status"]=="SKIPPED")
    total = len(R)

    print(f"\n{'='*60}")
    print(f"RESULTS: {ok}/{total} OK, {errs} errors, {skip} skipped")
    print(f"{'='*60}")
    for r in R:
        sym = "✓" if r["status"]=="OK" else ("⊘" if r["status"]=="SKIPPED" else "✗")
        print(f"  {sym} {r['tool']:30s} [{r['status']:10s}] {r['ms']:4d}ms  {r['msg'][:80]}")

    with open("/home/developer/projects/PhysicsSandbox/reports/mcp_tool_results.json","w") as f:
        json.dump({"timestamp":datetime.now().isoformat(),"total":total,"ok":ok,"errors":errs,
                    "skipped":skip,"tools_available":len(tools),"results":R},f,indent=2)
    print(f"\nSaved to reports/mcp_tool_results.json")
    return 0 if errs == 0 else 1

if __name__ == "__main__":
    sys.exit(main())
