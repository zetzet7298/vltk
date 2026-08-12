import asyncio
import json
from verify_runner import HttpMcpClient

async def main():
    client = HttpMcpClient()
    client.connect()
    job_id = "a8714d947e6943c1878990f06eb330f5"
    res = client.call_tool("get_test_job", {"job_id": job_id, "include_failed_tests": True, "include_details": True})
    print(json.dumps(res, indent=2))

if __name__ == "__main__":
    asyncio.run(main())
