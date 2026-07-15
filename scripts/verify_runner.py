import urllib.request
import urllib.error
import json
import time

def parse_mcp_response(response_text):
    for line in response_text.split('\n'):
        line = line.strip()
        if line.startswith("data:"):
            data_str = line[5:].strip()
            return json.loads(data_str)
    raise ValueError(f"Could not find data: line in response: {response_text}")

class HttpMcpClient:
    def __init__(self, base_url="http://127.0.0.1:8080"):
        self.base_url = f"{base_url}/mcp"
        self.session_id = None
        self.next_id = 1

    def connect(self):
        # 1. POST to /mcp with initialize request
        init_payload = {
            "jsonrpc": "2.0",
            "method": "initialize",
            "id": self.next_id,
            "params": {
                "protocolVersion": "2024-11-05",
                "capabilities": {},
                "clientInfo": {"name": "http-mcp-client", "version": "1.0.0"}
            }
        }
        self.next_id += 1
        
        req = urllib.request.Request(
            self.base_url,
            data=json.dumps(init_payload).encode('utf-8'),
            method="POST",
            headers={
                "Content-Type": "application/json",
                "Accept": "application/json, text/event-stream"
            }
        )
        try:
            with urllib.request.urlopen(req, timeout=10) as response:
                headers = dict(response.info())
                self.session_id = headers.get("mcp-session-id")
                if not self.session_id:
                    raise ValueError("mcp-session-id not found in response headers")
                body = response.read().decode('utf-8')
                result_json = parse_mcp_response(body)
                print(f"Connected. Session ID: {self.session_id}")
        except Exception as e:
            print("Initialization failed:", e)
            raise e

        # 2. POST initialized notification
        init_notif = {
            "jsonrpc": "2.0",
            "method": "notifications/initialized"
        }
        req_notif = urllib.request.Request(
            self.base_url,
            data=json.dumps(init_notif).encode('utf-8'),
            method="POST",
            headers={
                "Content-Type": "application/json",
                "Accept": "application/json, text/event-stream",
                "mcp-session-id": self.session_id
            }
        )
        with urllib.request.urlopen(req_notif, timeout=10) as response:
            response.read() # drain response

    def call_tool(self, tool_name, arguments=None):
        msg_id = self.next_id
        self.next_id += 1
        payload = {
            "jsonrpc": "2.0",
            "method": "tools/call",
            "id": msg_id,
            "params": {
                "name": tool_name,
                "arguments": arguments or {}
            }
        }
        req = urllib.request.Request(
            self.base_url,
            data=json.dumps(payload).encode('utf-8'),
            method="POST",
            headers={
                "Content-Type": "application/json",
                "Accept": "application/json, text/event-stream",
                "mcp-session-id": self.session_id
            }
        )
        with urllib.request.urlopen(req, timeout=60) as response:
            body = response.read().decode('utf-8')
            res_json = parse_mcp_response(body)
            if "error" in res_json:
                raise RuntimeError(f"Tool call error: {res_json['error']}")
            return res_json.get("result")

def run():
    client = HttpMcpClient()
    client.connect()

    # Step 0: Ensure Play Mode is stopped
    print("\n--- Ensuring Play Mode is Stopped ---")
    stop_init = client.call_tool("manage_editor", {"action": "stop"})
    print("Stop play mode response:", json.dumps(stop_init, indent=2))

    # Step 1: Run EditMode tests
    print("\n--- Running EditMode Tests ---")
    test_res = client.call_tool("run_tests", {
        "mode": "EditMode",
        "test_names": [
            "VLTK.Tests.Sandbox.MalePlayerVisualTests",
            "VLTK.Tests.Sandbox.FemalePlayerVisualTests"
        ]
    })
    print("run_tests response:", json.dumps(test_res, indent=2))
    
    # Extract job_id
    job_id = None
    if "content" in test_res:
        for c in test_res["content"]:
            if c.get("type") == "text":
                try:
                    text_data = json.loads(c.get("text"))
                    if "data" in text_data and isinstance(text_data["data"], dict):
                        job_id = text_data["data"].get("job_id")
                    else:
                        job_id = text_data.get("job_id")
                except Exception:
                    pass
    if not job_id:
        job_id = test_res.get("job_id")
        
    if not job_id:
        print("Error: Could not extract job_id from test response.")
        return

    print(f"Polling test job {job_id}...")
    while True:
        poll_res = client.call_tool("get_test_job", {"job_id": job_id, "wait_timeout": 10})
        # Extract status
        status = None
        results_str = ""
        error_msg = None
        if "content" in poll_res:
            for c in poll_res["content"]:
                if c.get("type") == "text":
                    results_str += c.get("text")
                    try:
                        text_data = json.loads(c.get("text"))
                        if "data" in text_data and isinstance(text_data["data"], dict):
                            status = text_data["data"].get("status")
                            error_msg = text_data["data"].get("error")
                        else:
                            status = text_data.get("status")
                            error_msg = text_data.get("error")
                    except Exception:
                        pass
        if not status:
            status = poll_res.get("status")
            
        print(f"Job Status: {status}")
        if status and status.lower() in ["completed", "success", "failed", "succeeded"]:
            print("Test results detail:")
            print(results_str or json.dumps(poll_res, indent=2))
            if error_msg:
                print(f"Job Error Message: {error_msg}")
            break
        elif not status:
            # Check if there is details in results_str
            if "passed" in results_str.lower() or "failed" in results_str.lower():
                print("Test results detail:")
                print(results_str)
                break
        time.sleep(2)

    # Step 2: Load Sandbox Scene
    print("\n--- Loading Sandbox Scene ---")
    scene_res = client.call_tool("manage_scene", {
        "action": "load",
        "path": "Assets/Scenes/Sandbox.unity"
    })
    print("Scene load result:", json.dumps(scene_res, indent=2))

    # Step 3: Enter Play Mode
    print("\n--- Entering Play Mode ---")
    play_res = client.call_tool("manage_editor", {
        "action": "play"
    })
    print("Play Mode result:", json.dumps(play_res, indent=2))

    # Wait for Play Mode to settle
    print("Waiting 10 seconds for Play Mode to stabilize and load map...")
    time.sleep(10)

    # Step 4: Execute C# verify_player.cs code
    print("\n--- Reading verify_player.cs ---")
    with open("/var/www/vltk-mobile/harness/.agents/skills/jx-player-visual/scripts/verify_player.cs", "r") as f:
        code_content = f.read()

    print("\n--- Executing verify_player.cs for MalePlayer ---")
    exec_res = client.call_tool("execute_code", {
        "action": "execute",
        "code": code_content
    })
    print("\n=== RUNTIME VERIFICATION REPORT (MalePlayer) ===")
    if "content" in exec_res:
        for c in exec_res["content"]:
            if c.get("type") == "text":
                print(c.get("text"))
    else:
        print(json.dumps(exec_res, indent=2))
    print("================================================\n")

    # Step 5: Execute custom C# script to spawn and check FemalePlayer
    print("\n--- Spawning and verifying FemalePlayer ---")
    female_verify_code = """
    var sb = new System.Text.StringBuilder();
    var manager = VLTK.Sandbox.SandboxManager.Instance;
    if (manager == null) return "SandboxManager.Instance not found";
    
    var femaleVisual = manager.SpawnFemaleVisual();
    if (femaleVisual == null) return "Failed to spawn FemalePlayerVisual";
    
    var player = GameObject.Find("FemalePlayer");
    if (player == null) return "FemalePlayer GameObject not found after spawn";
    
    var srs = player.GetComponentsInChildren<SpriteRenderer>(true);
    int withSprite = 0, minP = int.MaxValue, maxP = int.MinValue;
    foreach (var sr in srs) {
        if (sr.sprite != null) {
            withSprite++;
            if (sr.sortingOrder < minP) minP = sr.sortingOrder;
            if (sr.sortingOrder > maxP) maxP = sr.sortingOrder;
        }
    }
    sb.AppendLine($"CHECK 1 parts: {withSprite}/5 with sprite");
    sb.AppendLine($"  pos={player.transform.position} sortingOrder=[{minP}..{maxP}]");
    
    var mr = GameObject.Find("MapRenderer");
    int mapMax = int.MinValue;
    if (mr != null) {
        foreach (var sr in mr.GetComponentsInChildren<SpriteRenderer>(true)) {
            if (sr.sprite != null && sr.sortingOrder > mapMax) mapMax = sr.sortingOrder;
        }
    }
    if (mapMax == int.MinValue) mapMax = 32000;
    sb.AppendLine($"CHECK 2 vs map: player min {minP} vs map max {mapMax} -> {(minP > mapMax ? "ABOVE ok" : "UNDER")}");
    
    // CHECK 3: A/B occlusion diff
    var cam = GameObject.FindObjectsOfType<Camera>()[0];
    cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -100f);
    cam.orthographic = true; cam.orthographicSize = 120f;
    int W = 256, H = 256;
    System.Func<Texture2D> grab = () => {
        var rt = new RenderTexture(W, H, 24); var prev = cam.targetTexture;
        cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
        var t = new Texture2D(W, H, TextureFormat.RGB24, false);
        t.ReadPixels(new Rect(0, 0, W, H), 0, 0); t.Apply();
        RenderTexture.active = null; cam.targetTexture = prev;
        UnityEngine.Object.DestroyImmediate(rt); return t;
    };
    foreach (var sr in srs) sr.enabled = true;  var a = grab();
    foreach (var sr in srs) sr.enabled = false; var b = grab();
    foreach (var sr in srs) sr.enabled = true;
    int diff = 0;
    for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) {
        var ca = a.GetPixel(x, y); var cb = b.GetPixel(x, y);
        if (Mathf.Abs(ca.r - cb.r) + Mathf.Abs(ca.g - cb.g) + Mathf.Abs(ca.b - cb.b) > 0.08f) diff++;
    }
    UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
    sb.AppendLine($"CHECK 3 visible (A/B diff): {diff} px on top -> {(diff > 50 ? "VISIBLE ok" : "OCCLUDED/empty")}");
    
    // CHECK 4: FemalePlayer direction and action states
    var vis = player.GetComponent<VLTK.Sandbox.FemalePlayerVisual>();
    if (vis != null) {
        vis.SetDirection(0);
        bool okDir0 = (vis.direction == 0);
        sb.AppendLine($"CHECK 4 female direct direction check: {(okDir0 ? "PASS" : "FAIL")}");
    } else {
        sb.AppendLine("CHECK 4 female visual component not found");
    }
    
    return sb.ToString();
    """
    exec_res_female = client.call_tool("execute_code", {
        "action": "execute",
        "code": female_verify_code
    })
    print("\n=== RUNTIME VERIFICATION REPORT (FemalePlayer) ===")
    if "content" in exec_res_female:
        for c in exec_res_female["content"]:
            if c.get("type") == "text":
                print(c.get("text"))
    else:
        print(json.dumps(exec_res_female, indent=2))
    print("==================================================\n")

    # Step 6: Exit Play Mode
    print("\n--- Stopping Play Mode ---")
    stop_res = client.call_tool("manage_editor", {
        "action": "stop"
    })
    print("Stop Play Mode result:", json.dumps(stop_res, indent=2))

if __name__ == "__main__":
    run()
