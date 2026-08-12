import urllib.request
import json
import time

url = "http://127.0.0.1:8080/api/instances"
print("Waiting for Unity instance to connect to MCP server...")
for i in range(20):
    try:
        with urllib.request.urlopen(url, timeout=2) as response:
            body = response.read().decode('utf-8')
            data = json.loads(body)
            instances = data.get("instances", [])
            if instances:
                print(f"Connected instances: {instances}")
                break
    except Exception as e:
        print("Error checking instances:", e)
    time.sleep(5)
else:
    print("Timed out waiting for Unity instance.")
