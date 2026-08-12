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
    
    time.sleep(5)
    
    # Get active scene hierarchy
    res = client.call_tool("manage_scene", {
        "action": "get_hierarchy"
    })
    
    print("\n=== SCENE HIERARCHY ===")
    print(json.dumps(res, indent=2))
    print("========================\n")
    
    client.call_tool("manage_editor", {"action": "stop"})

if __name__ == "__main__":
    asyncio.run(main())
