import urllib.request
import urllib.error
import json

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
            headers={"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        )
        with urllib.request.urlopen(req, timeout=10) as response:
            headers = dict(response.info())
            self.session_id = headers.get("mcp-session-id")
            body = response.read().decode('utf-8')
            parse_mcp_response(body)

        init_notif = {
            "jsonrpc": "2.0",
            "method": "notifications/initialized"
        }
        req_notif = urllib.request.Request(
            self.base_url,
            data=json.dumps(init_notif).encode('utf-8'),
            method="POST",
            headers={"Content-Type": "application/json", "Accept": "application/json, text/event-stream", "mcp-session-id": self.session_id}
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
            headers={"Content-Type": "application/json", "Accept": "application/json, text/event-stream", "mcp-session-id": self.session_id}
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
    
    code = """
    var go = new GameObject("Test");
    var mv = go.AddComponent<VLTK.Sandbox.MalePlayerVisual>();
    mv.spritesRootOverride = VLTK.Tests.Sandbox.MalePlayerSprStaging.StageForTests();
    mv.RefreshActionParts(force: true);
    mv.SetWeapon(VLTK.Sandbox.PcWeaponType.EmptyHand);
    mv.SetAction(VLTK.Sandbox.PlayerVisualAction.Idle);
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine($"Loaded parts count: {mv.LoadedPartCount}");
    sb.AppendLine($"HasAllRequiredParts: {mv.HasAllRequiredParts}");
    foreach (var p in mv.LastMissingRequiredParts) {
        sb.AppendLine($"Missing: {p}");
    }
    VLTK.Tests.Sandbox.MalePlayerSprStaging.CleanupTempDir(mv.spritesRootOverride);
    UnityEngine.Object.DestroyImmediate(go);
    return sb.ToString();
    """
    
    res = client.call_tool("execute_code", {
        "action": "execute",
        "code": code
    })
    print(json.dumps(res, indent=2))

if __name__ == "__main__":
    run()
