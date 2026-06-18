import asyncio
from mcp import ClientSession
from mcp.client.sse import sse_client

async def main():
    print("Connecting to http://127.0.0.1:8080/mcp ...")
    async with sse_client("http://127.0.0.1:8080/mcp") as (read_stream, write_stream):
        print("Connected! Initializing session...")
        async with ClientSession(read_stream, write_stream) as session:
            await session.initialize()
            print("Session initialized successfully!")
            
            # List tools
            tools = await session.list_tools()
            print(f"Registered tools count: {len(tools.tools)}")
            for t in tools.tools:
                print(f" - {t.name}: {t.description}")
            
            # Let's run tests
            print("Running EditMode tests in Assets/Tests/EditMode/Sandbox/ ...")
            # The worker used run_tests with:
            # { "mode": "EditMode", "test_names": ["VLTK.Tests.Sandbox.MalePlayerVisualTests", "VLTK.Tests.Sandbox.FemalePlayerVisualTests"] }
            # Wait, let's check what arguments run_tests expects by looking at its schema or calling it.
            # Let's call run_tests!
            res = await session.call_tool("run_tests", {
                "mode": "EditMode",
                "test_names": [
                    "VLTK.Tests.Sandbox.MalePlayerVisualTests",
                    "VLTK.Tests.Sandbox.FemalePlayerVisualTests"
                ]
            })
            print("Result of run_tests:")
            print(res)

try:
    asyncio.run(main())
except Exception as e:
    import traceback
    traceback.print_exc()
