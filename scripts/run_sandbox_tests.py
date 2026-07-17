import urllib.request
import urllib.error
import json
import time
import sys

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
                result_json = parse_mcp_response(body)
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

    print("\n--- Triggering Sandbox EditMode Tests ---")
    
    # We want to run all VLTK.Tests.Sandbox tests.
    test_args = {
        "mode": "EditMode",
        "include_details": True,
        "include_failed_tests": True,
        "test_names": [
            "VLTK.Tests.Sandbox.PlayerEquipmentChangeTests",
            "VLTK.Tests.Sandbox.MountVisualTests",
            "VLTK.Tests.Sandbox.InventoryServiceTests"
        ]
    }
    
    if len(sys.argv) > 1:
        test_args["test_names"] = sys.argv[1:]
        
    while True:
        test_res = client.call_tool("run_tests", test_args)
        print("run_tests response:", json.dumps(test_res, indent=2))
        
        # Check if busy
        is_busy = False
        if "content" in test_res:
            for c in test_res["content"]:
                if c.get("type") == "text":
                    try:
                        text_data = json.loads(c.get("text"))
                        if text_data.get("error") == "busy" or text_data.get("message") == "tests_running":
                            is_busy = True
                    except Exception:
                        pass
        
        if is_busy:
            print("Unity is busy running tests. Retrying in 5 seconds...")
            time.sleep(5)
            continue
            
        job_id = None
        if "content" in test_res:
            for c in test_res["content"]:
                if c.get("type") == "text":
                    try:
                        text_data = json.loads(c.get("text"))
                        if "data" in text_data and isinstance(text_data["data"], dict):
                            job_id = text_data["data"].get("job_id")
                        else:
                            job_id = text_data.get("job_id")
                    except Exception:
                        pass
        if not job_id:
            job_id = test_res.get("job_id")
            
        if not job_id:
            print("Error: Could not extract job_id from test response.")
            return
        break


    print(f"Polling test job {job_id}...")
    while True:
        poll_res = client.call_tool("get_test_job", {"job_id": job_id, "include_details": True, "wait_timeout": 15})
        status = None
        results_str = ""
        if "content" in poll_res:
            for c in poll_res["content"]:
                if c.get("type") == "text":
                    results_str += c.get("text")
                    try:
                        text_data = json.loads(c.get("text"))
                        if "data" in text_data and isinstance(text_data["data"], dict):
                            status = text_data["data"].get("status")
                        else:
                            status = text_data.get("status")
                    except Exception:
                        pass
        if not status:
            status = poll_res.get("status")
            
        if status and status.lower() in ["completed", "success", "succeeded", "failed"]:
            print("\n=== TEST RESULTS DETAIL ===")
            print(results_str or json.dumps(poll_res, indent=2))
            if status.lower() == "failed":
                raise SystemExit(1)
            break
        elif not status:
            if "passed" in results_str.lower() or "failed" in results_str.lower():
                print("\n=== TEST RESULTS DETAIL ===")
                print(results_str)
                break
        time.sleep(2)


if __name__ == "__main__":
    run()
