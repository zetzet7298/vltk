import asyncio
import json
from verify_runner import HttpMcpClient

async def main():
    client = HttpMcpClient()
    client.connect()
    # Ensure playmode is stopped first
    client.call_tool("manage_editor", {"action": "stop"})
    
    print("Refreshing Unity assets...")
    res = client.call_tool("refresh_unity", {
        "compile": "none",
        "mode": "force",
        "scope": "assets",
        "wait_for_ready": True
    })
    print("Refresh result:")
    print(json.dumps(res, indent=2))

if __name__ == "__main__":
    asyncio.run(main())
