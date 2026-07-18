#!/usr/bin/env python3
"""Select and optionally execute the smallest safe Unity verification profile.

The selector is intentionally conservative. Structural changes and unknown production
paths escalate to ``boundary``. A dry run is the default so CI and developers can inspect
the exact proof before allowing Unity mutations.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import time
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Iterable, Sequence


SCHEMA = "vltk-unity-test-plan/v1"
PROFILE_RANK = {"fast": 0, "focused": 1, "boundary": 2}
SUCCESS_TEST_STATES = {"completed", "success", "succeeded"}
TERMINAL_TEST_STATES = SUCCESS_TEST_STATES | {
    "cancelled",
    "canceled",
    "error",
    "failed",
    "timed_out",
    "timeout",
}
STRUCTURAL_SUFFIXES = {".asmdef", ".asmref", ".meta", ".unity", ".prefab", ".asset"}
STRUCTURAL_PREFIXES = ("Packages/", "ProjectSettings/", "Assets/Scenes/")
STRUCTURAL_STATUSES = {"A", "D", "R", "C", "T", "U"}
POLICY_PATHS = {
    "scripts/run_unity_test_profile.py",
    "scripts/unity_test_profiles.json",
}
RESULT_SAVE_RE = re.compile(r"Saving results to:\s+.+(?:/|\\)TestResults\.xml\s*")


@dataclass(frozen=True)
class Change:
    path: str
    status: str = "M"


@dataclass
class Plan:
    profile: str = "fast"
    changes: list[Change] = field(default_factory=list)
    reasons: set[str] = field(default_factory=set)
    editmode_assemblies: set[str] = field(default_factory=set)
    editmode_tests: set[str] = field(default_factory=set)
    playmode_tests: set[str] = field(default_factory=set)

    def escalate(self, profile: str, reason: str) -> None:
        if PROFILE_RANK[profile] > PROFILE_RANK[self.profile]:
            self.profile = profile
        self.reasons.add(reason)


def _normalize_path(raw: str) -> str:
    value = raw.strip().replace("\\", "/")
    while value.startswith("./"):
        value = value[2:]
    return value


def _load_config(path: Path) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict) or data.get("schema") != "vltk-unity-test-profiles/v1":
        raise ValueError(f"invalid profile config schema: {path}")
    for key in ("fast_commands", "focused_smoke", "boundary_smoke", "path_rules", "fast_paths"):
        if key not in data:
            raise ValueError(f"profile config missing {key}: {path}")
    return data


def _parse_explicit_change(raw: str) -> Change:
    if ":" in raw and raw.split(":", 1)[0].upper() in STRUCTURAL_STATUSES | {"M"}:
        status, path = raw.split(":", 1)
        return Change(_normalize_path(path), status.upper())
    # Without a status we cannot distinguish a method-body edit from an add,
    # delete, or rename. Fail closed; callers can opt into focused routing with
    # an explicit M:path.
    return Change(_normalize_path(raw), "U")


def _git_changes(root: Path, base: str | None) -> list[Change]:
    if base:
        cmd = ["git", "diff", "--name-status", "-z", f"{base}...HEAD"]
    else:
        cmd = ["git", "status", "--porcelain=v1", "-z"]
    proc = subprocess.run(cmd, cwd=root, capture_output=True, check=True)
    fields = proc.stdout.decode("utf-8", errors="surrogateescape").split("\0")
    changes: list[Change] = []
    if base:
        index = 0
        while index < len(fields) and fields[index]:
            status = fields[index].split("\t", 1)[0]
            if "\t" in fields[index]:
                _, path = fields[index].split("\t", 1)
                index += 1
            else:
                index += 1
                path = fields[index] if index < len(fields) else ""
                index += 1
            if status.startswith(("R", "C")) and index < len(fields):
                original_path = path
                path = fields[index]
                index += 1
                if original_path:
                    changes.append(Change(_normalize_path(original_path), status[:1]))
            if path:
                changes.append(Change(_normalize_path(path), status[:1]))
        return changes + _git_changes(root, None)

    index = 0
    while index < len(fields):
        entry = fields[index]
        index += 1
        if not entry:
            continue
        raw_status = entry[:2]
        status_chars = [value for value in raw_status if value != " "]
        status = status_chars[0] if status_chars else "M"
        path = entry[3:]
        if raw_status == "??":
            status = "A"
        elif any(value in {"R", "C"} for value in status_chars):
            # Porcelain v1 -z emits the destination in this entry and the original
            # path as the following NUL field. Consume the original so it cannot be
            # misclassified, but retain both owners for boundary test selection.
            status = next(value for value in status_chars if value in {"R", "C"})
            if index < len(fields) and fields[index]:
                original_path = fields[index]
                index += 1
                changes.append(Change(_normalize_path(original_path), status))
        else:
            structural = next(
                (value for value in status_chars if value in STRUCTURAL_STATUSES), None
            )
            if structural:
                status = structural
        changes.append(Change(_normalize_path(path), status[:1]))
    return changes


def _is_structural(change: Change) -> bool:
    path = change.path
    return (
        change.status in STRUCTURAL_STATUSES
        or path.startswith(STRUCTURAL_PREFIXES)
        or Path(path).suffix.lower() in STRUCTURAL_SUFFIXES
    )


def select_plan(
    config: dict[str, Any],
    changes: Sequence[Change],
    explicit_profile: str = "auto",
    extra_editmode_tests: Iterable[str] = (),
    extra_playmode_tests: Iterable[str] = (),
) -> Plan:
    plan = Plan(changes=sorted(set(changes), key=lambda item: (item.path, item.status)))
    fast_paths = set(config["fast_paths"])
    rules = config["path_rules"]

    if not plan.changes:
        plan.escalate("fast", "no changed paths; local policy checks only")

    for change in plan.changes:
        path = change.path
        matched_rule = next(
            (rule for rule in rules if path.startswith(rule["prefix"])), None
        )
        if matched_rule:
            plan.editmode_assemblies.update(matched_rule.get("editmode_assemblies", []))
            plan.editmode_tests.update(matched_rule.get("editmode_tests", []))
            plan.playmode_tests.update(matched_rule.get("playmode_tests", []))

        if path in POLICY_PATHS:
            plan.escalate("boundary", f"verification policy change: {change.status}:{path}")
            continue
        if _is_structural(change):
            plan.escalate("boundary", f"structural change: {change.status}:{path}")
            continue
        if (
            path.startswith(("Assets/Scripts/", "Assets/Tests/"))
            and Path(path).suffix.lower() != ".cs"
        ):
            plan.escalate("boundary", f"non-C# path under code/test tree: {path}")
            continue
        if path in fast_paths or path.startswith("harness/"):
            plan.escalate("fast", f"local tooling/docs: {path}")
            continue

        if matched_rule:
            plan.escalate(
                matched_rule["profile"],
                f"{matched_rule['profile']} rule {matched_rule['prefix']}: {path}",
            )
            continue
        if path.startswith("Assets/") or path.startswith("scripts/"):
            plan.escalate("boundary", f"unmapped code or asset path: {path}")
        else:
            plan.escalate("fast", f"non-runtime path: {path}")

    plan.editmode_tests.update(value for value in extra_editmode_tests if value)
    plan.playmode_tests.update(value for value in extra_playmode_tests if value)
    if plan.editmode_tests or plan.playmode_tests:
        plan.escalate("focused", "explicit focused test selection")

    if explicit_profile != "auto":
        if PROFILE_RANK[explicit_profile] < PROFILE_RANK[plan.profile]:
            raise ValueError(
                f"explicit profile {explicit_profile} would weaken required {plan.profile} proof"
            )
        plan.escalate(explicit_profile, f"explicit profile override: {explicit_profile}")

    if plan.profile == "focused" and not plan.playmode_tests:
        plan.playmode_tests.add(config["focused_smoke"])
    if plan.profile == "boundary":
        plan.playmode_tests = {config["boundary_smoke"]}
    return plan


def render_plan(config: dict[str, Any], plan: Plan) -> dict[str, Any]:
    unity_steps: list[dict[str, Any]] = []
    if plan.profile == "focused":
        if plan.editmode_assemblies or plan.editmode_tests:
            unity_steps.append(
                {
                    "action": "run_tests",
                    "mode": "EditMode",
                    "assembly_names": sorted(plan.editmode_assemblies),
                    "test_names": sorted(plan.editmode_tests),
                }
            )
        if plan.playmode_tests:
            unity_steps.append(
                {
                    "action": "run_tests",
                    "mode": "PlayMode",
                    "test_names": sorted(plan.playmode_tests),
                }
            )
    elif plan.profile == "boundary":
        unity_steps.append({"action": "full_compile", "mode": "Debug"})
        if plan.editmode_assemblies or plan.editmode_tests:
            unity_steps.append(
                {
                    "action": "run_tests",
                    "mode": "EditMode",
                    "assembly_names": sorted(plan.editmode_assemblies),
                    "test_names": sorted(plan.editmode_tests),
                }
            )
        unity_steps.append(
            {
                "action": "run_tests",
                "mode": "PlayMode",
                "test_names": sorted(plan.playmode_tests),
            }
        )

    return {
        "schema": SCHEMA,
        "profile": plan.profile,
        "changes": [
            {"path": item.path, "status": item.status} for item in plan.changes
        ],
        "reasons": sorted(plan.reasons),
        "local_commands": config["fast_commands"],
        "unity_steps": unity_steps,
        "safety": {
            "dry_run_default": True,
            "no_test_weakening": True,
            "unknown_assets_escalate_boundary": True,
        },
    }


def _tool_payload(result: Any) -> dict[str, Any]:
    if isinstance(result, dict) and isinstance(result.get("content"), list):
        for item in result["content"]:
            if item.get("type") == "text":
                try:
                    value = json.loads(item.get("text", ""))
                except json.JSONDecodeError:
                    continue
                if isinstance(value, dict):
                    return value
    return result if isinstance(result, dict) else {}


def _require_tool_success(payload: dict[str, Any], operation: str) -> None:
    if (
        payload.get("success") is not True
        or payload.get("isError") is True
        or payload.get("error")
    ):
        raise RuntimeError(f"Unity MCP {operation} failed: {payload}")


def _run_local(commands: Sequence[Sequence[str]], root: Path) -> None:
    for command in commands:
        subprocess.run(list(command), cwd=root, check=True)


def _wait_test_job(
    client: Any, job_id: str, timeout_seconds: float = 1800.0
) -> dict[str, Any]:
    deadline = time.monotonic() + timeout_seconds
    while True:
        raw = client.call_tool(
            "get_test_job",
            {"job_id": job_id, "wait_timeout": 45, "include_failed_tests": True},
        )
        payload = _tool_payload(raw)
        _require_tool_success(payload, "get_test_job")
        data = payload.get("data", payload)
        if not isinstance(data, dict):
            raise RuntimeError(f"Unity test job {job_id} returned invalid data: {payload}")
        status = str(data.get("status", "")).lower()
        if status in TERMINAL_TEST_STATES:
            result = data.get("result") or {}
            summary = result.get("summary") or {}
            result_state = str(summary.get("resultState", "")).lower()
            total = int(summary.get("total", 0))
            passed = int(summary.get("passed", 0))
            failed = int(summary.get("failed", 0))
            skipped = int(summary.get("skipped", 0))
            result_state_ok = result_state in {"passed", "success", "succeeded"} or (
                result_state.startswith("skipped:") and passed > 0
            )
            if (
                status not in SUCCESS_TEST_STATES
                or not result_state_ok
                or failed > 0
                or failed < 0
                or skipped < 0
                or passed <= 0
                or total <= 0
                or passed + failed + skipped != total
            ):
                raise RuntimeError(f"Unity test job {job_id} failed: {summary}")
            return data
        if time.monotonic() >= deadline:
            raise TimeoutError(
                f"Unity test job {job_id} did not finish within {timeout_seconds:g}s"
            )
        time.sleep(1)


def _console_messages(payload: dict[str, Any]) -> list[str]:
    data = payload.get("data", payload)
    if isinstance(data, dict):
        items = data.get("items")
    elif isinstance(data, list):
        items = data
    else:
        items = None
    if not isinstance(items, list):
        raise RuntimeError(f"Unity MCP read_console returned invalid data: {payload}")
    return [
        str(item.get("message", ""))
        for item in items
        if isinstance(item, dict)
    ]


def _new_console_errors(before: Sequence[str], after: Sequence[str]) -> list[str]:
    before_counts = Counter(message for message in before if not RESULT_SAVE_RE.fullmatch(message))
    after_counts = Counter(message for message in after if not RESULT_SAVE_RE.fullmatch(message))
    delta = after_counts - before_counts
    return [message for message, count in delta.items() for _ in range(count)]


def _run_unity_step(client: Any, step: dict[str, Any]) -> None:
    if step["action"] == "full_compile":
        before = _tool_payload(
            client.call_tool(
                "read_console", {"action": "get", "types": ["error"], "count": 100}
            )
        )
        _require_tool_success(before, "read_console before full compile")
        baseline = _console_messages(before)
        refresh = _tool_payload(
            client.call_tool(
                "refresh_unity",
                {
                    "compile": "request",
                    "mode": "force",
                    "scope": "all",
                    "wait_for_ready": True,
                },
            )
        )
        _require_tool_success(refresh, "refresh_unity")
        after = _tool_payload(
            client.call_tool(
                "read_console", {"action": "get", "types": ["error"], "count": 100}
            )
        )
        _require_tool_success(after, "read_console after full compile")
        messages = _console_messages(after)
        new_errors = _new_console_errors(baseline, messages)
        cs_errors = [message for message in messages if "error CS" in message]
        if cs_errors or new_errors:
            raise RuntimeError(
                "full compile produced errors: " + " | ".join(cs_errors + new_errors)
            )
        print("PASS full Debug compile (no new Console errors)", file=sys.stderr)
        return

    before = _tool_payload(
        client.call_tool("read_console", {"action": "get", "types": ["error"], "count": 100})
    )
    _require_tool_success(before, "read_console before tests")
    baseline = _console_messages(before)
    arguments = {
        "mode": step["mode"],
        "include_failed_tests": True,
        "init_timeout": 120000,
    }
    if step.get("assembly_names"):
        arguments["assembly_names"] = step["assembly_names"]
    if step.get("test_names"):
        arguments["test_names"] = step["test_names"]
    payload = _tool_payload(client.call_tool("run_tests", arguments))
    _require_tool_success(payload, "run_tests")
    data = payload.get("data", payload)
    if not isinstance(data, dict):
        raise RuntimeError(f"Unity run_tests returned invalid data: {payload}")
    job_id = data.get("job_id")
    if not job_id:
        raise RuntimeError(f"Unity run_tests returned no job_id: {payload}")
    completed = _wait_test_job(client, str(job_id))
    summary = (completed.get("result") or {}).get("summary") or {}
    after = _tool_payload(
        client.call_tool("read_console", {"action": "get", "types": ["error"], "count": 100})
    )
    _require_tool_success(after, "read_console after tests")
    new_errors = _new_console_errors(baseline, _console_messages(after))
    if new_errors:
        raise RuntimeError("Unity tests produced Console errors: " + " | ".join(new_errors))
    print(
        "PASS {mode}: {passed}/{total} passed, {skipped} skipped, {duration:.3f}s".format(
            mode=step["mode"],
            passed=int(summary.get("passed", 0)),
            total=int(summary.get("total", 0)),
            skipped=int(summary.get("skipped", 0)),
            duration=float(summary.get("durationSeconds", 0.0)),
        ),
        file=sys.stderr,
    )


def execute_plan(plan_data: dict[str, Any], root: Path) -> None:
    _run_local(plan_data["local_commands"], root)
    if not plan_data["unity_steps"]:
        return
    try:
        from compile_scripts import HttpMcpClient
    except ImportError as exc:  # pragma: no cover - protected by CLI layout
        raise RuntimeError("run from the repository root so scripts/ is importable") from exc
    client = HttpMcpClient(tool_timeout=240)
    client.connect()
    for step in plan_data["unity_steps"]:
        _run_unity_step(client, step)


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument(
        "--config", type=Path, default=Path(__file__).with_name("unity_test_profiles.json")
    )
    parser.add_argument("--profile", choices=["auto", "fast", "focused", "boundary"], default="auto")
    parser.add_argument(
        "--path",
        action="append",
        default=[],
        help="STATUS:path (plain paths fail closed to boundary; use M:path for focused edits)",
    )
    parser.add_argument("--changed-from", help="git merge-base/base revision for name-status input")
    parser.add_argument("--editmode-test", action="append", default=[])
    parser.add_argument("--playmode-test", action="append", default=[])
    parser.add_argument("--execute", action="store_true", help="execute the rendered plan")
    parser.add_argument("--json", action="store_true", help="emit JSON only")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    root = args.root.resolve()
    config = _load_config(args.config.resolve())
    changes = (
        [_parse_explicit_change(value) for value in args.path]
        if args.path
        else _git_changes(root, args.changed_from)
    )
    try:
        plan = select_plan(
            config,
            changes,
            explicit_profile=args.profile,
            extra_editmode_tests=args.editmode_test,
            extra_playmode_tests=args.playmode_test,
        )
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 2
    rendered = render_plan(config, plan)
    print(json.dumps(rendered, indent=2, sort_keys=True))
    if args.execute:
        execute_plan(rendered, root)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
