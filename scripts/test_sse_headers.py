import urllib.request
import urllib.error
import json

# 1. POST /mcp to create session
url = "http://127.0.0.1:8080/mcp"
req = urllib.request.Request(
    url, 
    method="POST", 
    headers={
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream"
    }
)
try:
    init_msg = {
        "jsonrpc": "2.0",
        "method": "initialize",
        "id": 1,
        "params": {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {"name": "test-client", "version": "1.0.0"}
        }
    }
    data = json.dumps(init_msg).encode('utf-8')
    with urllib.request.urlopen(req, data=data, timeout=5) as response:
        headers = dict(response.info())
        session_id = headers.get("mcp-session-id")
        print("Created session ID:", session_id)
        
        # 2. Try GET with session ID in various headers or cookie
        header_variants = [
            {"mcp-session-id": session_id},
            {"session-id": session_id},
            {"session_id": session_id},
            {"x-session-id": session_id},
            {"Cookie": f"mcp-session-id={session_id}"},
            {"Cookie": f"session_id={session_id}"},
            # also let's check query params with exact name matching what might be expected:
            # maybe it's sessionId but case-sensitive?
            # Wait, let's try a GET with session ID in headers:
        ]
        
        for h in header_variants:
            print(f"Trying GET with headers: {h} ...")
            get_req = urllib.request.Request(
                "http://127.0.0.1:8080/mcp",
                method="GET",
                headers={
                    "Accept": "text/event-stream",
                    **h
                }
            )
            try:
                with urllib.request.urlopen(get_req, timeout=2) as get_res:
                    print(f"SUCCESS! Code: {get_res.getcode()}")
                    break
            except urllib.error.HTTPError as e:
                print(f"Failed: {e.code} {e.reason}")
                print("Error body:", e.read().decode('utf-8'))
except Exception as e:
    print("Exception:", e)
