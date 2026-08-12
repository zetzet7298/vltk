import asyncio
import json
import httpx
import sys

class SimpleMcpClient:
    def __init__(self, base_url="http://127.0.0.1:8080"):
        self.base_url = base_url
        self.client = httpx.AsyncClient(timeout=60)
        self.session_id = None
        self.sse_task = None
        self.futures = {}
        self.next_id = 2

    async def connect(self):
        # 1. POST /mcp with initialize request
        url = f"{self.base_url}/mcp"
        init_payload = {
            "jsonrpc": "2.0",
            "method": "initialize",
            "id": 1,
            "params": {
                "protocolVersion": "2024-11-05",
                "capabilities": {},
                "clientInfo": {"name": "python-simple-client", "version": "1.0.0"}
            }
        }
        
        headers = {
            "Content-Type": "application/json",
            "Accept": "application/json, text/event-stream"
        }
        
        print(f"POSTing initialize to {url} ...")
        response = await self.client.post(url, json=init_payload, headers=headers)
        response.raise_for_status()
        
        self.session_id = response.headers.get("mcp-session-id")
        if not self.session_id:
            raise ValueError("No mcp-session-id header in initialize response")
        
        print(f"Session established: {self.session_id}")
        
        # Start SSE background reader
        self.sse_task = asyncio.create_task(self._read_sse())
        
        # Wait a small bit for SSE stream to start
        await asyncio.sleep(0.5)
        
        # 2. POST initialized notification
        initialized_payload = {
            "jsonrpc": "2.0",
            "method": "notifications/initialized"
        }
        await self._send_post(initialized_payload)
        print("Sent initialized notification.")

    async def _send_post(self, payload):
        url = f"{self.base_url}/mcp"
        headers = {
            "Content-Type": "application/json",
            "mcp-session-id": self.session_id,
            "Accept": "application/json, text/event-stream"
        }
        response = await self.client.post(url, json=payload, headers=headers)
        response.raise_for_status()
        return response

    async def _read_sse(self):
        url = f"{self.base_url}/mcp"
        headers = {
            "Accept": "text/event-stream",
            "mcp-session-id": self.session_id
        }
        
        print("Starting SSE reader task...")
        try:
            async with self.client.stream("GET", url, headers=headers) as response:
                response.raise_for_status()
                current_event = None
                async for line in response.aiter_lines():
                    line = line.strip()
                    if not line:
                        continue
                    if line.startswith("event:"):
                        current_event = line[6:].strip()
                    elif line.startswith("data:"):
                        data_str = line[5:].strip()
                        if current_event == "message":
                            msg = json.loads(data_str)
                            self._handle_message(msg)
        except asyncio.CancelledError:
            print("SSE reader task cancelled.")
        except Exception as e:
            print("Error in SSE reader:", e)

    def _handle_message(self, msg):
        msg_id = msg.get("id")
        if msg_id in self.futures:
            fut = self.futures.pop(msg_id)
            if not fut.done():
                fut.set_result(msg)
        else:
            # Maybe it's a notification or log
            method = msg.get("method")
            if method == "notifications/message":
                print(f"[Server Notification]: {msg.get('params')}")
            else:
                print(f"[Unrecognized Message]: {msg}")

    async def call_tool(self, tool_name: str, arguments: dict = None):
        msg_id = self.next_id
        self.next_id += 1
        
        payload = {
            "jsonrpc": "2.0",
            "method": "tools/call",
            "id": msg_id,
            "params": {
                "name": tool_name,
                "arguments": arguments or {}
            }
        }
        
        fut = asyncio.get_running_loop().create_future()
        self.futures[msg_id] = fut
        
        await self._send_post(payload)
        
        # Wait for the response on the SSE stream
        res_msg = await fut
        
        # Check for error
        if "error" in res_msg:
            raise RuntimeError(f"Tool call error: {res_msg['error']}")
        
        return res_msg.get("result")

    async def close(self):
        if self.sse_task:
            self.sse_task.cancel()
            try:
                await self.sse_task
            except asyncio.CancelledError:
                pass
        await self.client.aclose()
        print("Client closed.")
