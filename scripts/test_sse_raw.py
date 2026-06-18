import asyncio
import httpx
import json

async def main():
    async with httpx.AsyncClient(timeout=60) as client:
        # 1. POST /mcp to initialize
        url = "http://127.0.0.1:8080/mcp"
        init_payload = {
            "jsonrpc": "2.0",
            "method": "initialize",
            "id": 1,
            "params": {
                "protocolVersion": "2024-11-05",
                "capabilities": {},
                "clientInfo": {"name": "test", "version": "1.0"}
            }
        }
        headers = {
            "Content-Type": "application/json",
            "Accept": "application/json, text/event-stream"
        }
        res = await client.post(url, json=init_payload, headers=headers)
        session_id = res.headers.get("mcp-session-id")
        print("Session ID:", session_id)
        
        # Start reading SSE stream in a background task
        async def read_sse():
            sse_headers = {
                "Accept": "text/event-stream",
                "mcp-session-id": session_id
            }
            async with client.stream("GET", url, headers=sse_headers) as response:
                print("SSE stream opened. Code:", response.status_code)
                async for line in response.aiter_lines():
                    print(f"SSE LINE: {line}")
        
        sse_task = asyncio.create_task(read_sse())
        await asyncio.sleep(1.0)
        
        # 2. POST initialized notification
        init_notif = {
            "jsonrpc": "2.0",
            "method": "notifications/initialized"
        }
        headers = {
            "mcp-session-id": session_id,
            "Content-Type": "application/json",
            "Accept": "application/json, text/event-stream"
        }
        await client.post(url, json=init_notif, headers=headers)
        print("Sent initialized notification.")
        await asyncio.sleep(1.0)
        
        # 3. Call read_console tool
        tool_call = {
            "jsonrpc": "2.0",
            "method": "tools/call",
            "id": 2,
            "params": {
                "name": "read_console",
                "arguments": {"action": "get", "count": "2"}
            }
        }
        print("POSTing tool call...")
        tool_res = await client.post(url, json=tool_call, headers=headers)
        print("POST tool call response code:", tool_res.status_code)
        print("POST tool call response body:", tool_res.text)
        
        # Wait a bit to see if SSE receives the response
        await asyncio.sleep(5.0)
        sse_task.cancel()

if __name__ == "__main__":
    asyncio.run(main())
