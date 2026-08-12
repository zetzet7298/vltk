import asyncio
import json
from verify_runner import HttpMcpClient

async def main():
    client = HttpMcpClient()
    client.connect()
    res = client.call_tool("read_console", {"action": "get", "count": 20})
    # Print it
    if "content" in res:
        for c in res["content"]:
            if c.get("type") == "text":
                print(c.get("text"))
    else:
        print(json.dumps(res, indent=2))

if __name__ == "__main__":
    asyncio.run(main())
