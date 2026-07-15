import asyncio
import json
from verify_runner import HttpMcpClient

async def main():
    client = HttpMcpClient()
    client.connect()
    
    # Read verify_player.cs content
    with open("/var/www/vltk-mobile/harness/.agents/skills/jx-player-visual/scripts/verify_player.cs", "r") as f:
        code_content = f.read()
        
    print("Executing verify_player.cs...")
    res = client.call_tool("execute_code", {
        "action": "execute",
        "code": code_content
    })
    print("Response:")
    print(json.dumps(res, indent=2))

if __name__ == "__main__":
    asyncio.run(main())
