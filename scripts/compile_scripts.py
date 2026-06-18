import urllib.request
import urllib.error
import json
import time

def parse_mcp_response(response_text):
    for line in response_text.split('\n'):
        line = line.strip()
        if line.startswith("data:"):
            data_str = line[5:].strip()
            return json.loads(data_str)
    raise ValueError(f"Could not find data: line in response: {response_text}")

class HttpMcpClient:
    def __init__(self, base_url="http://127.0.0.1:8080"):
        self.base_url = f"{base_url}/mcp"
        self.session_id = None
        self.next_id = 1

    def connect(self):
        init_payload = {
            "jsonrpc": "2.0",
            "method": "initialize",
            "id": self.next_id,
            "params": {
                "protocolVersion": "2024-11-05",
                "capabilities": {},
                "clientInfo": {"name": "http-mcp-client", "version": "1.0.0"}
            }
        }
        self.next_id += 1
        
        req = urllib.request.Request(
            self.base_url,
            data=json.dumps(init_payload).encode('utf-8'),
            method="POST",
            headers={
                "Content-Type": "application/json",
                "Accept": "application/json, text/event-stream"
            }
        )
        try:
            with urllib.request.urlopen(req, timeout=10) as response:
                headers = dict(response.info())
                self.session_id = headers.get("mcp-session-id")
                if not self.session_id:
                    raise ValueError("mcp-session-id not found in response headers")
                body = response.read().decode('utf-8')
                parse_mcp_response(body)
                print(f"Connected. Session ID: {self.session_id}")
        except Exception as e:
            print("Initialization failed:", e)
            raise e

        init_notif = {
            "jsonrpc": "2.0",
            "method": "notifications/initialized"
        }
        req_notif = urllib.request.Request(
            self.base_url,
            data=json.dumps(init_notif).encode('utf-8'),
            method="POST",
            headers={
                "Content-Type": "application/json",
                "Accept": "application/json, text/event-stream",
                "mcp-session-id": self.session_id
            }
        )
        with urllib.request.urlopen(req_notif, timeout=10) as response:
            response.read()

    def call_tool(self, tool_name, arguments=None):
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
        req = urllib.request.Request(
            self.base_url,
            data=json.dumps(payload).encode('utf-8'),
            method="POST",
            headers={
                "Content-Type": "application/json",
                "Accept": "application/json, text/event-stream",
                "mcp-session-id": self.session_id
            }
        )
        with urllib.request.urlopen(req, timeout=60) as response:
            body = response.read().decode('utf-8')
            res_json = parse_mcp_response(body)
            if "error" in res_json:
                raise RuntimeError(f"Tool call error: {res_json['error']}")
            return res_json.get("result")

def run():
    client = HttpMcpClient()
    client.connect()

    print("\n--- Requesting Unity Asset/Script Refresh & Compilation ---")
    refresh_res = client.call_tool("refresh_unity", {
        "compile": "request",
        "mode": "force",
        "scope": "all",
        "wait_for_ready": True
    })
    print("Refresh response:", json.dumps(refresh_res, indent=2))
    
    print("\n--- Reading Console Logs ---")
    console_res = client.call_tool("read_console", {
        "action": "get",
        "count": 20
    })
    
    logs = ""
    if "content" in console_res:
        for c in console_res["content"]:
            if c.get("type") == "text":
                logs += c.get("text")
    print(logs or json.dumps(console_res, indent=2))

if __name__ == "__main__":
    run()
