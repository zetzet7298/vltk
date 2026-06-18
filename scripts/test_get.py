import urllib.request
import urllib.error

url = "http://127.0.0.1:8080/mcp"
req = urllib.request.Request(
    url, 
    method="GET", 
    headers={
        "Accept": "application/json, text/event-stream"
    }
)
try:
    with urllib.request.urlopen(req, timeout=5) as response:
        code = response.getcode()
        headers = dict(response.info())
        body = response.read().decode('utf-8')
        print(f"GET {url} -> {code}")
        print("Headers:", headers)
        print("Body:", body)
except urllib.error.HTTPError as e:
    print(f"HTTPError {e.code}: {e.reason}")
    print("Response body:", e.read().decode('utf-8', errors='ignore'))
except Exception as e:
    print(f"Exception:", e)
