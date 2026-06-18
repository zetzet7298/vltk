import asyncio
import sys
from mcp_client import SimpleMcpClient

async def main():
    client = SimpleMcpClient()
    await client.connect()
    
    try:
        # 1. Load sandbox scene
        print("Loading Sandbox scene...")
        res = await client.call_tool("manage_scene", {
            "action": "load",
            "scene_path": "Assets/Scenes/Sandbox.unity"
        })
        print("Scene load result:", res)
        
        # 2. Enter Play Mode
        print("Entering Play Mode...")
        res = await client.call_tool("manage_editor", {
            "action": "play"
        })
        print("Play Mode result:", res)
        
        # Wait for compilation/playmode transition to settle
        print("Waiting 10 seconds for Play Mode to stabilize and load map...")
        await asyncio.sleep(10)
        
        # 3. Read verify_player.cs script content
        print("Reading verify_player.cs C# script...")
        with open("/var/www/vltk-mobile/bak/skills/jx-player-visual/scripts/verify_player.cs", "r") as f:
            code_content = f.read()
            
        # 4. Execute C# code
        print("Executing verification code in Unity...")
        res = await client.call_tool("execute_code", {
            "code": code_content
        })
        print("\n=== RUNTIME VERIFICATION REPORT ===")
        # The result from execute_code usually has 'content' or is a dict with details
        if isinstance(res, dict) and "content" in res:
            for item in res["content"]:
                if item.get("type") == "text":
                    print(item.get("text"))
        else:
            print(res)
        print("===================================\n")
        
        # 5. Stop Play Mode
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
