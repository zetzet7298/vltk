import asyncio
import json
from verify_runner import HttpMcpClient

async def main():
    client = HttpMcpClient()
    client.connect()
    job_id = "de87311d00fb43fe893dd873cd231835"
    res = client.call_tool("get_test_job", {"job_id": job_id, "include_failed_tests": False, "include_details": True})
    print(json.dumps(res, indent=2))

if __name__ == "__main__":
    asyncio.run(main())
