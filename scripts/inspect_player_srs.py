import asyncio
import json
import time
from verify_runner import HttpMcpClient

async def main():
    client = HttpMcpClient()
    client.connect()
    
    client.call_tool("manage_editor", {"action": "stop"})
    client.call_tool("manage_scene", {"action": "load", "path": "Assets/Scenes/Sandbox.unity"})
    client.call_tool("manage_editor", {"action": "play"})
    
    time.sleep(5) # wait a bit less
    
    inspect_code = """
    var player = GameObject.Find("MalePlayer");
    if (player == null) return "player not found";
    
    var sb = new System.Text.StringBuilder();
    var srs = player.GetComponentsInChildren<SpriteRenderer>(true);
    sb.AppendLine($"Total SpriteRenderers: {srs.Length}");
    foreach (var sr in srs) {
        sb.AppendLine($"GoName: {sr.gameObject.name}, Sprite: {(sr.sprite != null ? sr.sprite.name : "null")}, SortingOrder: {sr.sortingOrder}, Enabled: {sr.enabled}, Layer: {sr.gameObject.layer}");
    }
    return sb.ToString();
    """
    
    res = client.call_tool("execute_code", {
        "action": "execute",
        "code": inspect_code
    })
    
    print("\n=== PLAYER SR INSPECTION ===")
    if "content" in res:
        for c in res["content"]:
            if c.get("type") == "text":
                print(c.get("text"))
    else:
        print(json.dumps(res, indent=2))
    print("============================\n")
    
    client.call_tool("manage_editor", {"action": "stop"})

if __name__ == "__main__":
    asyncio.run(main())
