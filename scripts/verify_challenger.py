import asyncio
import json
import sys
from mcp_client import SimpleMcpClient

async def main():
    client = SimpleMcpClient()
    await client.connect()
    
    try:
        # 1. Run EditMode tests
        print("Starting EditMode tests...")
        test_args = {
            "mode": "EditMode",
            "test_names": [
                "VLTK.Tests.Sandbox.MalePlayerVisualTests",
                "VLTK.Tests.Sandbox.FemalePlayerVisualTests"
            ]
        }
        res = await client.call_tool("run_tests", test_args)
        print("run_tests response:", json.dumps(res, indent=2))
        
        job_id = None
        # Parse job_id from response
        if isinstance(res, dict):
            # Sometimes result is wrapped in 'content' from MCP protocol
            if "content" in res:
                for item in res["content"]:
                    if item.get("type") == "text":
                        try:
                            data = json.loads(item.get("text"))
                            job_id = data.get("job_id")
                        except Exception:
                            # Try parsing as simple text containing job_id
                            text = item.get("text")
                            print("Text output:", text)
            else:
                job_id = res.get("job_id")
                
        if not job_id:
            # Let's extract job_id from string if it was returned as text
            print("Could not directly parse job_id from dict, trying to find in output...")
            # If the tool returned string, let's check res
            res_str = str(res)
            import re
            match = re.search(r"job_id['\s:]+([a-f0-9\-]+)", res_str, re.IGNORECASE)
            if match:
                job_id = match.group(1)
            else:
                # Let's print everything and exit
                print("Failed to find job_id in:", res_str)
                return

        print(f"Polling job {job_id}...")
        while True:
            job_res = await client.call_tool("get_test_job", {"job_id": job_id, "wait_timeout": 10})
            print("get_test_job response:", json.dumps(job_res, indent=2))
            
            # Check if job is finished
            # Let's check status field. We can search for 'status' or 'state' or 'completed'
            job_str = json.dumps(job_res).lower()
            if "completed" in job_str or "finished" in job_str or "success" in job_str or "failed" in job_str:
                if "running" not in job_str:
                    break
            await asyncio.sleep(2)

        # 2. Load Sandbox scene
        print("Loading Sandbox scene...")
        res = await client.call_tool("manage_scene", {
            "action": "load",
            "scene_path": "Assets/Scenes/Sandbox.unity"
        })
        print("Scene load result:", res)
        
        # 3. Enter Play Mode
        print("Entering Play Mode...")
        res = await client.call_tool("manage_editor", {
            "action": "play"
        })
        print("Play Mode result:", res)
        
        # Wait for compilation/playmode transition to settle
        print("Waiting 10 seconds for Play Mode to stabilize and load map...")
        await asyncio.sleep(10)
        
        # 4. Read verify_player.cs script content
        print("Reading verify_player.cs C# script...")
        with open("/var/www/vltk-mobile/harness/.agents/skills/jx-player-visual/scripts/verify_player.cs", "r") as f:
            code_content = f.read()
            
        # 5. Execute C# code for MalePlayer
        print("Executing verification code in Unity for MalePlayer...")
        res = await client.call_tool("execute_code", {
            "code": code_content
        })
        print("\n=== RUNTIME VERIFICATION REPORT (MalePlayer) ===")
        print(json.dumps(res, indent=2))
        print("===================================\n")
        
        # 6. Execute C# code for FemalePlayer
        # We need to spawn FemalePlayer first using SandboxManager.Instance.SpawnFemaleVisual()
        # and then run verify_player.cs adapted for "FemalePlayer".
        spawn_and_verify_female_code = """
        var sb = new System.Text.StringBuilder();
        var manager = GameObject.Find("SandboxManager")?.GetComponent<VLTK.Sandbox.SandboxManager>();
        if (manager == null) return "SandboxManager not found";
        
        var femaleVisual = manager.SpawnFemaleVisual();
        if (femaleVisual == null) return "Failed to spawn FemalePlayerVisual";
        
        // Wait a frame or process updates? Since we are executing in editor, let's run the check directly.
        var player = GameObject.Find("FemalePlayer");
        if (player == null) return "FemalePlayer GameObject not found after spawn";
        
        var srs = player.GetComponentsInChildren<SpriteRenderer>(true);
        int withSprite = 0, minP = int.MaxValue, maxP = int.MinValue;
        foreach (var sr in srs) {
            if (sr.sprite != null) withSprite++;
            if (sr.sortingOrder < minP) minP = sr.sortingOrder;
            if (sr.sortingOrder > maxP) maxP = sr.sortingOrder;
        }
        sb.AppendLine($"CHECK 1 parts: {withSprite}/{srs.Length} with sprite");
        sb.AppendLine($"  pos={player.transform.position} sortingOrder=[{minP}..{maxP}]");
        
        var mr = GameObject.Find("MapRenderer");
        int mapMax = int.MinValue;
        if (mr != null) foreach (var sr in mr.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.sortingOrder > mapMax) mapMax = sr.sortingOrder;
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
        
        // CHECK 4: Female player direction and move states
        var ctrl = player.GetComponent<VLTK.Sandbox.SandboxPlayerController>(); // may not have controller since it's just visual, let's check
        var vis = player.GetComponent<VLTK.Sandbox.FemalePlayerVisual>();
        if (vis != null) {
            // Test action states & 8 directions directly on the visual since there is no controller on FemalePlayer (or it might have one if added)
            // Actually, the female player has FemalePlayerVisual. Let's see if we can check directions.
            vis.SetDirection(0);
            bool okDir0 = vis.direction == 0;
            sb.AppendLine($"CHECK 4 female direct direction check: {(okDir0 ? "PASS" : "FAIL")}");
        } else {
            sb.AppendLine("CHECK 4 female visual component not found");
        }
        
        return sb.ToString();
        """
        
        print("Executing verification code in Unity for FemalePlayer...")
        res_female = await client.call_tool("execute_code", {
            "code": spawn_and_verify_female_code
        })
        print("\n=== RUNTIME VERIFICATION REPORT (FemalePlayer) ===")
        print(json.dumps(res_female, indent=2))
        print("===================================\n")
        
        # 7. Stop Play Mode
        print("Exiting Play Mode...")
        res = await client.call_tool("manage_editor", {
            "action": "stop"
        })
        print("Stop Play Mode result:", res)

    except Exception as e:
        print("Exception during execution:", e)
    finally:
        await client.close()

if __name__ == "__main__":
    asyncio.run(main())
