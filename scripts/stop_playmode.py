import asyncio
import json
from verify_runner import HttpMcpClient

async def main():
    client = HttpMcpClient()
    client.connect()
    res = client.call_tool("manage_editor", {"action": "stop"})
    print("Stop play mode response:", json.dumps(res, indent=2))

if __name__ == "__main__":
    asyncio.run(main())
