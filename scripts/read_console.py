import asyncio
import json
from mcp_client import SimpleMcpClient

async def main():
    client = SimpleMcpClient()
    await client.connect()
    try:
        # Read the console
        res = await client.call_tool("read_console", {"action": "get", "count": 20})
        print(json.dumps(res, indent=2))
    except Exception as e:
        print("Error:", e)
    finally:
        await client.close()

if __name__ == "__main__":
    asyncio.run(main())
