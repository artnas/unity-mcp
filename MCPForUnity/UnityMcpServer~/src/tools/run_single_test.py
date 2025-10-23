from typing import Annotated, Literal, Any

from mcp.server.fastmcp import Context
from pydantic import BaseModel, Field

from models import MCPResponse
from registry import mcp_for_unity_tool
from unity_connection import async_send_command_with_retry


class RunSingleTestSummary(BaseModel):
    total: int
    passed: int
    failed: int
    skipped: int
    durationSeconds: float
    resultState: str


class RunSingleTestTestResult(BaseModel):
    name: str
    fullName: str
    state: str
    durationSeconds: float
    message: str | None = None
    stackTrace: str | None = None
    output: str | None = None


class RunSingleTestResult(BaseModel):
    mode: str
    testName: str
    summary: RunSingleTestSummary
    results: list[RunSingleTestTestResult]


class RunSingleTestResponse(MCPResponse):
    data: RunSingleTestResult | None = None


@mcp_for_unity_tool(description="Runs a specific Unity test by name")
async def run_single_test(
        ctx: Context,
        test_name: Annotated[str, Field(
            description="Full name of the test to run (e.g., 'BeetleSpawnTest')")],
        mode: Annotated[Literal["edit", "play"], Field(
            description="Unity test mode to run")] = "edit",
        timeout_seconds: Annotated[int, Field(
            description="Optional timeout in seconds for the Unity test run")] | None = None,
) -> RunSingleTestResponse:
    await ctx.info(f"Processing run_single_test: test_name={test_name}, mode={mode}")

    params: dict[str, Any] = {"test_name": test_name, "mode": mode}
    if timeout_seconds is not None:
        params["timeout_seconds"] = timeout_seconds

    # Mirror run_tests.py: snake_case command name and info log for response
    response = await async_send_command_with_retry("run_single_test", params)
    await ctx.info(f"Response {response}")

    return RunSingleTestResponse(**response) if isinstance(response, dict) else response
