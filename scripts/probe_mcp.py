import urllib.request
import json

url = "http://127.0.0.1:8080/api/instances"
try:
    with urllib.request.urlopen(url, timeout=2) as response:
        body = response.read().decode('utf-8')
        print("GET /api/instances ->", response.getcode())
        print(body)
except Exception as e:
    print("Exception:", e)
