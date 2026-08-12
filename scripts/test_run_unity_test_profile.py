from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


MODULE_PATH = Path(__file__).with_name("run_unity_test_profile.py")
SPEC = importlib.util.spec_from_file_location("run_unity_test_profile", MODULE_PATH)
runner = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = runner
SPEC.loader.exec_module(runner)


def config():
    return runner._load_config(Path(__file__).with_name("unity_test_profiles.json"))


class SelectionTests(unittest.TestCase):
    def test_fast_tooling_change_stays_fast(self):
        plan = runner.select_plan(config(), [runner.Change("scripts/check_unity_assembly_boundaries.py")])
        self.assertEqual("fast", plan.profile)

    def test_portdata_selects_dedicated_focused_assembly(self):
        plan = runner.select_plan(config(), [runner.Change("Assets/Scripts/PortData/PcText.cs")])
        self.assertEqual("focused", plan.profile)
        self.assertEqual({"VLTK.Tests.PortData.EditMode"}, plan.editmode_assemblies)
        self.assertEqual({config()["focused_smoke"]}, plan.playmode_tests)

    def test_world_selects_world_assembly(self):
        plan = runner.select_plan(config(), [runner.Change("Assets/Scripts/World/PcMapListParser.cs")])
        self.assertEqual({"VLTK.Tests.World.EditMode"}, plan.editmode_assemblies)

    def test_combat_uses_broad_editmode_without_micro_assembly(self):
        plan = runner.select_plan(config(), [runner.Change("Assets/Scripts/Combat/PcSkillFullParser.cs")])
        self.assertEqual({"VLTK.Tests.EditMode"}, plan.editmode_assemblies)

    def test_asmdef_is_boundary(self):
        plan = runner.select_plan(config(), [runner.Change("Assets/Scripts/World/VLTK.World.asmdef")])
        self.assertEqual("boundary", plan.profile)

    def test_meta_move_is_boundary(self):
        plan = runner.select_plan(config(), [runner.Change("Assets/Foo.cs.meta", "R")])
        self.assertEqual("boundary", plan.profile)

    def test_added_asset_is_boundary(self):
        plan = runner.select_plan(config(), [runner.Change("Assets/Scripts/World/New.cs", "A")])
        self.assertEqual("boundary", plan.profile)

    def test_scene_project_and_package_changes_are_boundary(self):
        for path in (
            "Assets/Scenes/Sandbox.unity",
            "ProjectSettings/ProjectSettings.asset",
            "Packages/manifest.json",
        ):
            with self.subTest(path=path):
                self.assertEqual(
                    "boundary", runner.select_plan(config(), [runner.Change(path)]).profile
                )

    def test_unknown_asset_fails_closed(self):
        plan = runner.select_plan(config(), [runner.Change("Assets/Unknown/Foo.txt")])
        self.assertEqual("boundary", plan.profile)

    def test_unknown_asset_inside_mapped_prefix_fails_closed(self):
        plan = runner.select_plan(
            config(), [runner.Change("Assets/Scripts/World/Unknown.json")]
        )
        self.assertEqual("boundary", plan.profile)

    def test_verification_policy_change_is_boundary(self):
        for path in runner.POLICY_PATHS:
            with self.subTest(path=path):
                self.assertEqual(
                    "boundary", runner.select_plan(config(), [runner.Change(path)]).profile
                )

    def test_all_structural_statuses_are_boundary(self):
        for status in runner.STRUCTURAL_STATUSES:
            with self.subTest(status=status):
                plan = runner.select_plan(
                    config(), [runner.Change("Assets/Scripts/World/PcMapListParser.cs", status)]
                )
                self.assertEqual("boundary", plan.profile)

    def test_all_structural_suffixes_are_boundary(self):
        for suffix in runner.STRUCTURAL_SUFFIXES:
            with self.subTest(suffix=suffix):
                plan = runner.select_plan(
                    config(), [runner.Change(f"Assets/Scripts/World/Thing{suffix}")]
                )
                self.assertEqual("boundary", plan.profile)

    def test_boundary_keeps_owning_editmode_assembly(self):
        plan = runner.select_plan(
            config(), [runner.Change("Assets/Scripts/World/NewParser.cs", "A")]
        )
        rendered = runner.render_plan(config(), plan)
        self.assertEqual(["full_compile", "run_tests", "run_tests"], [
            step["action"] for step in rendered["unity_steps"]
        ])
        self.assertEqual("EditMode", rendered["unity_steps"][1]["mode"])
        self.assertEqual(
            ["VLTK.Tests.World.EditMode"],
            rendered["unity_steps"][1]["assembly_names"],
        )

    def test_mixed_profiles_escalate_to_highest(self):
        plan = runner.select_plan(
            config(),
            [
                runner.Change("scripts/check_unity_assembly_boundaries.py"),
                runner.Change("Assets/Scripts/World/PcMapListParser.cs"),
                runner.Change("Assets/Scenes/Sandbox.unity"),
            ],
        )
        self.assertEqual("boundary", plan.profile)
        self.assertEqual({config()["boundary_smoke"]}, plan.playmode_tests)

    def test_explicit_profile_cannot_weaken_required_proof(self):
        with self.assertRaisesRegex(ValueError, "would weaken"):
            runner.select_plan(
                config(),
                [runner.Change("Assets/Scenes/Sandbox.unity")],
                explicit_profile="fast",
            )

    def test_explicit_profile_can_escalate(self):
        plan = runner.select_plan(
            config(),
            [runner.Change("scripts/check_unity_assembly_boundaries.py")],
            explicit_profile="boundary",
        )
        self.assertEqual("boundary", plan.profile)

    def test_explicit_tests_are_sorted_and_deduplicated(self):
        plan = runner.select_plan(
            config(),
            [runner.Change("harness/docs/x.md")],
            explicit_profile="focused",
            extra_editmode_tests=["B", "A", "A"],
            extra_playmode_tests=["Z", "Z"],
        )
        rendered = runner.render_plan(config(), plan)
        self.assertEqual(["A", "B"], rendered["unity_steps"][0]["test_names"])
        self.assertEqual(["Z"], rendered["unity_steps"][1]["test_names"])

    def test_changes_are_deterministically_sorted(self):
        plan = runner.select_plan(
            config(),
            [runner.Change("harness/z"), runner.Change("harness/a"), runner.Change("harness/a")],
        )
        rendered = runner.render_plan(config(), plan)
        self.assertEqual(["harness/a", "harness/z"], [row["path"] for row in rendered["changes"]])


class ParsingTests(unittest.TestCase):
    def test_explicit_status_path(self):
        self.assertEqual(
            runner.Change("Assets/Foo.cs.meta", "R"), runner._parse_explicit_change("R:Assets/Foo.cs.meta")
        )

    def test_plain_path_without_status_fails_closed(self):
        self.assertEqual(runner.Change("Assets/Foo.cs", "U"), runner._parse_explicit_change("./Assets/Foo.cs"))

    def test_invalid_config_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "config.json"
            path.write_text(json.dumps({"schema": "wrong"}), encoding="utf-8")
            with self.assertRaises(ValueError):
                runner._load_config(path)

    def test_porcelain_rename_consumes_original_path(self):
        payload = (
            b"R  Assets/Tests/World/EditMode/A.cs\0"
            b"Assets/Tests/EditMode/Sandbox/A.cs\0"
        )
        completed = subprocess.CompletedProcess(["git"], 0, stdout=payload, stderr=b"")
        with mock.patch.object(subprocess, "run", return_value=completed):
            changes = runner._git_changes(Path("."), None)
        self.assertEqual(
            [
                runner.Change("Assets/Tests/EditMode/Sandbox/A.cs", "R"),
                runner.Change("Assets/Tests/World/EditMode/A.cs", "R"),
            ],
            changes,
        )

    def test_porcelain_compound_status_uses_structural_column(self):
        for raw_status, expected in (("MD", "D"), ("MT", "T"), ("AM", "A")):
            with self.subTest(raw_status=raw_status):
                completed = subprocess.CompletedProcess(
                    ["git"],
                    0,
                    stdout=f"{raw_status} Assets/Scripts/World/Foo.cs\0".encode(),
                    stderr=b"",
                )
                with mock.patch.object(subprocess, "run", return_value=completed):
                    changes = runner._git_changes(Path("."), None)
                self.assertEqual(
                    [runner.Change("Assets/Scripts/World/Foo.cs", expected)], changes
                )
                self.assertEqual("boundary", runner.select_plan(config(), changes).profile)

    def test_untracked_asset_is_treated_as_added(self):
        completed = subprocess.CompletedProcess(
            ["git"], 0, stdout=b"?? Assets/Scripts/World/New.cs\0", stderr=b""
        )
        with mock.patch.object(subprocess, "run", return_value=completed):
            changes = runner._git_changes(Path("."), None)
        self.assertEqual([runner.Change("Assets/Scripts/World/New.cs", "A")], changes)
        self.assertEqual("boundary", runner.select_plan(config(), changes).profile)

    def test_changed_from_unions_dirty_worktree(self):
        committed = subprocess.CompletedProcess(
            ["git"], 0, stdout=b"M\0Assets/Scripts/World/PcMapListParser.cs\0", stderr=b""
        )
        dirty = subprocess.CompletedProcess(
            ["git"], 0, stdout=b"?? Assets/Scripts/World/New.asmdef\0", stderr=b""
        )
        with mock.patch.object(subprocess, "run", side_effect=[committed, dirty]):
            changes = runner._git_changes(Path("."), "HEAD~1")
        self.assertEqual(
            {
                runner.Change("Assets/Scripts/World/PcMapListParser.cs", "M"),
                runner.Change("Assets/Scripts/World/New.asmdef", "A"),
            },
            set(changes),
        )
        self.assertEqual("boundary", runner.select_plan(config(), changes).profile)


class ExecutionTests(unittest.TestCase):
    @staticmethod
    def success_job(passed=1, skipped=0, total=None, result_state="Passed"):
        total = passed + skipped if total is None else total
        return {
            "success": True,
            "data": {
                "status": "succeeded",
                "result": {
                    "summary": {
                        "failed": 0,
                        "passed": passed,
                        "skipped": skipped,
                        "total": total,
                        "durationSeconds": 0.1,
                        "resultState": result_state,
                    }
                },
            },
        }

    def test_local_failure_propagates(self):
        with mock.patch.object(
            subprocess, "run", side_effect=subprocess.CalledProcessError(1, ["x"])
        ):
            with self.assertRaises(subprocess.CalledProcessError):
                runner._run_local([["x"]], Path("."))

    def test_wait_job_failure_propagates(self):
        client = mock.Mock()
        client.call_tool.return_value = {
            "content": [
                {
                    "type": "text",
                    "text": json.dumps(
                        {
                            "success": True,
                            "data": {
                                "status": "failed",
                                "result": {"summary": {"failed": 1}},
                            },
                        }
                    ),
                }
            ]
        }
        with self.assertRaises(RuntimeError):
            runner._wait_test_job(client, "job")
        self.assertEqual(45, client.call_tool.call_args.args[1]["wait_timeout"])

    def test_wait_job_cancelled_is_not_success(self):
        client = mock.Mock()
        payload = self.success_job()
        payload["data"]["status"] = "cancelled"
        payload["data"]["result"]["summary"]["resultState"] = "Cancelled"
        client.call_tool.return_value = payload
        with self.assertRaises(RuntimeError):
            runner._wait_test_job(client, "job")

    def test_wait_job_zero_tests_is_not_success(self):
        client = mock.Mock()
        client.call_tool.return_value = self.success_job(passed=0, total=0)
        with self.assertRaises(RuntimeError):
            runner._wait_test_job(client, "job")

    def test_wait_job_zero_pass_all_ignored_is_not_success(self):
        client = mock.Mock()
        client.call_tool.return_value = self.success_job(passed=0, skipped=2, total=2)
        with self.assertRaises(RuntimeError):
            runner._wait_test_job(client, "job")

    def test_wait_job_incoherent_counters_are_not_success(self):
        client = mock.Mock()
        client.call_tool.return_value = self.success_job(passed=1, total=2)
        with self.assertRaises(RuntimeError):
            runner._wait_test_job(client, "job")

    def test_wait_job_negative_counters_are_not_success(self):
        client = mock.Mock()
        payload = self.success_job(passed=2, total=1)
        payload["data"]["result"]["summary"]["failed"] = -1
        client.call_tool.return_value = payload
        with self.assertRaises(RuntimeError):
            runner._wait_test_job(client, "job")

    def test_wait_job_coherent_success_returns(self):
        client = mock.Mock()
        payload = self.success_job(passed=2, skipped=1)
        client.call_tool.return_value = payload
        self.assertEqual(payload["data"], runner._wait_test_job(client, "job"))

    def test_wait_job_partial_ignored_with_real_unity_state_returns(self):
        client = mock.Mock()
        payload = self.success_job(
            passed=95, skipped=4, result_state="Skipped:Ignored"
        )
        client.call_tool.return_value = payload
        self.assertEqual(payload["data"], runner._wait_test_job(client, "job"))

    def test_wait_job_times_out(self):
        client = mock.Mock()
        client.call_tool.return_value = {
            "success": True,
            "data": {"status": "running"},
        }
        with mock.patch.object(runner.time, "monotonic", side_effect=[0.0, 2.0]):
            with self.assertRaises(TimeoutError):
                runner._wait_test_job(client, "job", timeout_seconds=1.0)

    def test_wait_job_mcp_failure_propagates_immediately(self):
        client = mock.Mock()
        for payload in (
            {"success": False, "error": "disconnected"},
            {"isError": True},
            {},
        ):
            with self.subTest(payload=payload):
                client.call_tool.return_value = payload
                with self.assertRaisesRegex(RuntimeError, "get_test_job"):
                    runner._wait_test_job(client, "job")

    def test_full_compile_accepts_real_console_response_shape(self):
        client = mock.Mock()
        existing = {"type": "Error", "message": "pre-existing"}
        client.call_tool.side_effect = [
            {"success": True, "data": {"items": [existing]}},
            {"success": True},
            {"success": True, "data": {"items": [existing]}},
        ]
        runner._run_unity_step(client, {"action": "full_compile", "mode": "Debug"})
        self.assertEqual("refresh_unity", client.call_tool.call_args_list[1].args[0])

    def test_full_compile_rejects_new_console_error(self):
        client = mock.Mock()
        client.call_tool.side_effect = [
            {"success": True, "data": {"items": []}},
            {"success": True},
            {
                "success": True,
                "data": {"items": [{"type": "Error", "message": "error CS1002"}]},
            },
        ]
        with self.assertRaisesRegex(RuntimeError, "error CS1002"):
            runner._run_unity_step(client, {"action": "full_compile", "mode": "Debug"})

    def test_full_compile_rejects_refresh_failure(self):
        client = mock.Mock()
        client.call_tool.side_effect = [
            {"success": True, "data": {"items": []}},
            {"success": False, "error": "compile request rejected"},
        ]
        with self.assertRaisesRegex(RuntimeError, "refresh_unity"):
            runner._run_unity_step(client, {"action": "full_compile", "mode": "Debug"})

    def test_console_delta_counts_duplicate_messages(self):
        self.assertEqual(
            ["RepeatedError"],
            runner._new_console_errors(
                ["RepeatedError"], ["RepeatedError", "RepeatedError"]
            ),
        )

    def test_console_delta_ignores_only_exact_result_save_message(self):
        valid = "Saving results to: /tmp/TestResults.xml\n"
        spoof = "Saving results to: /tmp/TestResults.xml; unexpected failure"
        self.assertEqual([], runner._new_console_errors([], [valid]))
        self.assertEqual([spoof], runner._new_console_errors([], [spoof]))

    def test_run_tests_requires_job_id(self):
        client = mock.Mock()
        client.call_tool.side_effect = [
            {"success": True, "data": {"items": []}},
            {"success": True, "data": {}},
        ]
        with self.assertRaisesRegex(RuntimeError, "no job_id"):
            runner._run_unity_step(client, {"action": "run_tests", "mode": "EditMode"})

    def test_run_tests_ignores_only_result_save_bookkeeping(self):
        client = mock.Mock()
        client.call_tool.side_effect = [
            {"success": True, "data": {"items": []}},
            {"success": True, "data": {"job_id": "job"}},
            self.success_job(),
            {
                "success": True,
                "data": {"items": [{"message": "Saving results to: /tmp/TestResults.xml"}]},
            },
        ]
        runner._run_unity_step(client, {"action": "run_tests", "mode": "EditMode"})

    def test_run_tests_rejects_new_console_error(self):
        client = mock.Mock()
        client.call_tool.side_effect = [
            {"success": True, "data": {"items": []}},
            {"success": True, "data": {"job_id": "job"}},
            self.success_job(),
            {
                "success": True,
                "data": {"items": [{"message": "NullReferenceException"}]},
            },
        ]
        with self.assertRaisesRegex(RuntimeError, "NullReferenceException"):
            runner._run_unity_step(client, {"action": "run_tests", "mode": "EditMode"})


class CliTests(unittest.TestCase):
    def test_dry_run_is_default_and_machine_readable(self):
        proc = subprocess.run(
            [
                sys.executable,
                str(MODULE_PATH),
                  "--path",
                  "M:Assets/Scripts/World/PcMapListParser.cs",
                "--json",
            ],
            capture_output=True,
            text=True,
            check=True,
        )
        data = json.loads(proc.stdout)
        self.assertEqual("focused", data["profile"])
        self.assertTrue(data["safety"]["dry_run_default"])

    def test_cli_rejects_false_fast_override(self):
        proc = subprocess.run(
            [
                sys.executable,
                str(MODULE_PATH),
                  "--path",
                  "M:Assets/Scenes/Sandbox.unity",
                "--profile",
                "fast",
            ],
            capture_output=True,
            text=True,
        )
        self.assertEqual(2, proc.returncode)
        self.assertIn("would weaken", proc.stderr)


if __name__ == "__main__":
    unittest.main()
