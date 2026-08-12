import urllib.request
import urllib.error

url = "http://127.0.0.1:8080/mcp"
req = urllib.request.Request(
    url, 
    method="GET", 
    headers={
        "Accept": "text/event-stream"
    }
)
try:
    with urllib.request.urlopen(req, timeout=5) as response:
        code = response.getcode()
        headers = dict(response.info())
        print(f"GET {url} -> {code}")
        print("Headers:", headers)
        
        # Read first few lines of the stream
        for i in range(5):
            line = response.readline().decode('utf-8')
            if not line:
                break
            print(f"Line {i}: {line.strip()}")
except urllib.error.HTTPError as e:
    print(f"HTTPError {e.code}: {e.reason}")
    print("Response body:", e.read().decode('utf-8', errors='ignore'))
except Exception as e:
    print(f"Exception:", e)
