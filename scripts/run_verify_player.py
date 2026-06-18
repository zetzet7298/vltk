import asyncio
from mcp import ClientSession
from mcp.client.sse import sse_client

async def main():
    headers = {"Accept": "application/json, text/event-stream"}
    print("Connecting to http://127.0.0.1:8080/mcp with headers...")
    async with sse_client("http://127.0.0.1:8080/mcp", headers=headers) as (read_stream, write_stream):
        print("Connected! Initializing session...")
        async with ClientSession(read_stream, write_stream) as session:
            await session.initialize()
            print("Session initialized successfully!")
            
            # Read script content
            with open("/var/www/vltk-mobile/bak/skills/jx-player-visual/scripts/verify_player.cs", "r") as f:
                code_content = f.read()
            
            # We want to execute this code.
            # Let's call execute_code tool.
            # According to execute_code schema, it usually takes:
            # "code": code string
            # Let's verify what the arguments are by listing the tools first, or just calling it.
            print("Executing verify_player.cs via execute_code tool...")
            res = await session.call_tool("execute_code", {
                "code": code_content
            })
            print("Execution Result:")
            print(res)

try:
    asyncio.run(main())
except Exception as e:
    import traceback
    traceback.print_exc()
