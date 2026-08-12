import asyncio
import json
from verify_runner import HttpMcpClient

async def main():
    client = HttpMcpClient()
    client.connect()
    # Stop play mode first just in case
    client.call_tool("manage_editor", {"action": "stop"})
    res = client.call_tool("manage_scene", {
        "action": "load",
        "path": "Assets/Scenes/Sandbox.unity"
    })
    print("Load scene response:", json.dumps(res, indent=2))

if __name__ == "__main__":
    asyncio.run(main())
