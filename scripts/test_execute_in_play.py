import asyncio
import json
import time
from verify_runner import HttpMcpClient

async def main():
    client = HttpMcpClient()
    client.connect()
    
    print("Stopping Play Mode...")
    client.call_tool("manage_editor", {"action": "stop"})
    
    print("Loading Sandbox scene...")
    client.call_tool("manage_scene", {"action": "load", "path": "Assets/Scenes/Sandbox.unity"})
    
    print("Entering Play Mode...")
    client.call_tool("manage_editor", {"action": "play"})
    
    print("Waiting 10 seconds for Play Mode...")
    time.sleep(10)
    
    # Read verify_player.cs content
    with open("/var/www/vltk-mobile/harness/.agents/skills/jx-player-visual/scripts/verify_player.cs", "r") as f:
        code_content = f.read()
        
    print("Executing verify_player.cs in Play Mode...")
    res = client.call_tool("execute_code", {
        "action": "execute",
        "code": code_content
    })
    
    print("Response:")
    print(json.dumps(res, indent=2))
    
    print("Stopping Play Mode...")
    client.call_tool("manage_editor", {"action": "stop"})

if __name__ == "__main__":
    asyncio.run(main())
