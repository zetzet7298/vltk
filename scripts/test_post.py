import urllib.request
import urllib.error
import json

url = "http://127.0.0.1:8080/mcp"
init_msg = {
    "jsonrpc": "2.0",
    "method": "initialize",
    "id": 1,
    "params": {
        "protocolVersion": "2024-11-05",
        "capabilities": {},
        "clientInfo": {
            "name": "test-client",
            "version": "1.0.0"
        }
    }
}
data = json.dumps(init_msg).encode('utf-8')
req = urllib.request.Request(
    url, 
    method="POST", 
    headers={
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream"
    }
)
try:
    with urllib.request.urlopen(req, data=data, timeout=5) as response:
        code = response.getcode()
        headers = dict(response.info())
        body = response.read().decode('utf-8')
        print(f"POST {url} -> {code}")
        print("Headers:", headers)
        print("Body:", body)
except urllib.error.HTTPError as e:
    print(f"HTTPError {e.code}: {e.reason}")
    print("Response body:", e.read().decode('utf-8', errors='ignore'))
except Exception as e:
    print(f"Exception:", e)
