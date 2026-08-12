#!/usr/bin/env python3
"""Focused unittest suite for measure_unity_debug_compile.

Pure stdlib, no Unity, no network. Exercises trace parsing (incl. malformed
lines and missing Csc events), deterministic capture, median/p95/noise math,
metadata-mismatch rejection, N<5 rejection, and CLI plumbing.

Run: ``python3 -m unittest scripts.test_measure_unity_debug_compile``
"""

from __future__ import annotations

import io
import json
import sys
import tempfile
import unittest
from pathlib import Path

# Ensure we import the module under test from this directory.
sys.path.insert(0, str(Path(__file__).resolve().parent))

import measure_unity_debug_compile as m  # noqa: E402

MICROS = 1_000_000


def csc_line(dur_micros: int, detail: str = "x.dll") -> str:
    return (
        '{ "pid":1, "tid":0, "ts":0, "dur":%d, "ph":"X", '
        '"name":"Csc", "args": {"detail":"%s"}}' % (dur_micros, detail)
    )


def other_line(name: str, dur: int = 100) -> str:
    return '{ "pid":1, "tid":0, "ts":0, "dur":%d, "ph":"X", "name":"%s"}' % (
        dur,
        name,
    )


def write_trace(path: Path, lines: list[str]) -> None:
    # First line has no leading comma, the rest do (Chrome trace style).
    body = lines[0] + "\n" if lines else ""
    body += "".join("," + ln + "\n" for ln in lines[1:])
    path.write_text(body, encoding="utf-8")


def base_meta(**overrides) -> dict:
    meta = {
        "unity_version": "2022.3.20f1",
        "packages_lock_hash": "sha256:aaa",
        "project_settings_hash": "sha256:bbb",
        "debug_optimization": "Debug",
        "auto_refresh": True,
        "cache_protocol": "tundra-default",
        "invalidation_fixture": "touch-one-script",
    }
    meta.update(overrides)
    return meta


class TraceParsingTests(unittest.TestCase):
    def test_sums_csc_dur_micros_and_ignores_other_events(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d) / "t.traceevents"
            write_trace(p, [csc_line(1_000_000), csc_line(500_000), other_line("File")])
            events, micros, errors = m.parse_trace_file(p)
        self.assertEqual(events, 2)
        self.assertEqual(micros, 1_500_000)
        self.assertEqual(errors, 0)

    def test_malformed_lines_counted_not_fatal(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d) / "t.traceevents"
            write_trace(
                p,
                [csc_line(250_000), "{ not valid json", "", other_line("File")],
            )
            events, micros, errors = m.parse_trace_file(p)
        self.assertEqual(events, 1)
        self.assertEqual(micros, 250_000)
        self.assertEqual(errors, 1)

    def test_no_csc_events_yields_unavailable(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d) / "t.traceevents"
            write_trace(p, [other_line("File"), other_line("CopyFiles")])
            stats = m.collect_csc([p])
        self.assertFalse(stats["available"])
        self.assertEqual(stats["events"], 0)
        self.assertEqual(stats["total_seconds"], 0.0)


class StatsTests(unittest.TestCase):
    def test_median_odd_and_even(self):
        self.assertEqual(m.median([3.0, 1.0, 2.0]), 2.0)
        self.assertEqual(m.median([1.0, 2.0, 3.0, 4.0]), 2.5)

    def test_p95_nearest_rank_is_max_at_n5(self):
        # ceil(0.95*5) = 5 -> the sample max (conservative at min trial count)
        self.assertEqual(m.p95_nearest_rank([1.0, 2.0, 3.0, 4.0, 5.0]), 5.0)
        # n=20 -> rank 19
        vals = list(range(1, 21))
        self.assertEqual(m.p95_nearest_rank(vals), 19.0)


class CaptureTests(unittest.TestCase):
    def test_capture_is_deterministic_and_readonly(self):
        with tempfile.TemporaryDirectory() as d:
            dpath = Path(d)
            trace = dpath / "b.traceevents"
            write_trace(trace, [csc_line(2_000_000)])
            before_size = trace.stat().st_size
            before_mtime = trace.stat().st_mtime_ns
            out = dpath / "series"

            rec = m.capture_trial(
                wall_seconds=12.5,
                trace_paths=[trace],
                metadata=base_meta(),
                out_dir=out,
                trial_id="0007",
                note="run-A",
            )
            # Trace file untouched (read-only).
            self.assertEqual(trace.stat().st_size, before_size)
            self.assertEqual(trace.stat().st_mtime_ns, before_mtime)
            # Record content.
            self.assertEqual(rec["trial_id"], "0007")
            self.assertEqual(rec["wall_seconds"], 12.5)
            self.assertTrue(rec["csc"]["available"])
            self.assertEqual(rec["csc"]["total_seconds"], 2.0)
            self.assertEqual(rec["metadata"]["cache_protocol"], "tundra-default")

            # Deterministic: identical bytes on a second write of same inputs.
            written = (out / "trial_0007.json").read_text(encoding="utf-8")
            m.capture_trial(
                wall_seconds=12.5,
                trace_paths=[trace],
                metadata=base_meta(),
                out_dir=out,
                trial_id="0007",
                note="run-A",
            )
            self.assertEqual((out / "trial_0007.json").read_text("utf-8"), written)

            # Round-trips through load_trials.
            loaded = m.load_trials(out)
            self.assertEqual(len(loaded), 1)
            self.assertEqual(loaded[0]["trial_id"], "0007")

    def test_capture_missing_metadata_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            trace = Path(d) / "b.traceevents"
            write_trace(trace, [csc_line(1)])
            bad = base_meta()
            del bad["unity_version"]
            with self.assertRaises(ValueError):
                m.capture_trial(1.0, [trace], bad, Path(d) / "o", trial_id="1")


class SummaryTests(unittest.TestCase):
    def _series(self, walls):
        trials = []
        for i, w in enumerate(walls):
            trials.append(
                {
                    "schema": m.TRIAL_SCHEMA,
                    "wall_seconds": w,
                    "csc": {"available": True, "total_seconds": w * 0.8},
                    "metadata": base_meta(),
                }
            )
        return trials

    def test_summary_median_p95_and_sufficient_flag(self):
        s = m.summarize_series(self._series([10.0, 10.0, 10.0, 10.0, 11.0]))
        self.assertTrue(s["sufficient"])
        self.assertEqual(s["wall_seconds"]["median"], 10.0)
        self.assertEqual(s["wall_seconds"]["p95"], 11.0)
        self.assertEqual(s["csc_seconds"]["median"], 8.0)

    def test_summary_n_below_five_marks_insufficient(self):
        s = m.summarize_series(self._series([10.0, 10.0, 10.0, 10.0]))
        self.assertFalse(s["sufficient"])
        self.assertEqual(s["count"], 4)


class CompareTests(unittest.TestCase):
    def _trials(self, walls, **meta_overrides):
        meta = base_meta(**meta_overrides)
        return [
            {
                "schema": m.TRIAL_SCHEMA,
                "wall_seconds": w,
                "csc": {"available": True, "total_seconds": w * 0.8},
                "metadata": meta,
            }
            for w in walls
        ]

    def test_metadata_mismatch_rejected_exit1(self):
        base = self._trials([10.0] * 5)
        cand = self._trials([9.0] * 5, cache_protocol="tundra-v2")
        result, code = m.compare_series("before", base, "after", cand)
        self.assertEqual(code, 1)
        self.assertFalse(result["metadata_match"])
        self.assertIn("cache_protocol", result["metadata_diff"])

    def test_insufficient_trials_exit2(self):
        base = self._trials([10.0] * 5)
        cand = self._trials([9.0] * 4)  # N<5
        result, code = m.compare_series("before", base, "after", cand)
        self.assertEqual(code, 2)
        self.assertTrue(result["metadata_match"])

    def test_improvement_supported_when_delta_exceeds_noise(self):
        # baseline median 10, candidate median 9 -> delta 1.0 > 0.5 noise;
        # candidate p95 (10) <= baseline p95 (11).
        base = self._trials([10.0, 10.0, 10.0, 10.0, 11.0])
        cand = self._trials([9.0, 9.0, 9.0, 9.0, 10.0])
        result, code = m.compare_series("before", base, "after", cand)
        self.assertEqual(code, 0)
        self.assertTrue(result["wall"]["improvement_claim_supported"])
        self.assertTrue(result["wall"]["candidate_p95_not_worse"])
        self.assertIn("after faster by", result["verdict"])

    def test_below_noise_not_supported(self):
        # baseline median 10, candidate median 9.7 -> delta 0.3 < 0.5 noise.
        base = self._trials([10.0] * 5)
        cand = self._trials([9.7] * 5)
        result, code = m.compare_series("before", base, "after", cand)
        self.assertEqual(code, 0)
        self.assertFalse(result["wall"]["improvement_claim_supported"])

    def test_p95_worse_blocks_claim(self):
        base = self._trials([10.0, 10.0, 10.0, 10.0, 11.0])
        cand = self._trials([9.0, 9.0, 9.0, 9.0, 12.0])  # p95=12 > 11
        result, code = m.compare_series("before", base, "after", cand)
        self.assertEqual(code, 0)
        self.assertFalse(result["wall"]["candidate_p95_not_worse"])
        self.assertFalse(result["wall"]["improvement_claim_supported"])

    def test_percentage_noise_floor_uses_5pct_when_larger(self):
        # baseline median 20 -> 5% = 1.0s, larger than the 0.5s floor.
        base = self._trials([20.0] * 5)
        cand = self._trials([19.4] * 5)  # delta 0.6 > 0.5 but < 1.0
        result, _ = m.compare_series("before", base, "after", cand)
        self.assertEqual(result["wall"]["noise_seconds"], 1.0)
        self.assertFalse(result["wall"]["improvement_claim_supported"])

    def test_csc_skipped_when_unavailable(self):
        base = [
            {
                "schema": m.TRIAL_SCHEMA,
                "wall_seconds": 10.0,
                "csc": {"available": False, "total_seconds": 0.0},
                "metadata": base_meta(),
            }
        ] * 5
        cand = [
            {
                "schema": m.TRIAL_SCHEMA,
                "wall_seconds": 9.0,
                "csc": {"available": False, "total_seconds": 0.0},
                "metadata": base_meta(),
            }
        ] * 5
        result, code = m.compare_series("before", base, "after", cand)
        self.assertEqual(code, 0)
        self.assertIsNone(result["csc"])
        self.assertTrue(result["wall"]["improvement_claim_supported"])


class SeriesConsistencyTests(unittest.TestCase):
    """P1 regression: within-series metadata must be homogeneous before compare."""

    def _trials(self, walls, protocols):
        if isinstance(protocols, str):
            protocols = [protocols] * len(walls)
        out = []
        for w, proto in zip(walls, protocols):
            out.append(
                {
                    "schema": m.TRIAL_SCHEMA,
                    "wall_seconds": w,
                    "csc": {"available": True, "total_seconds": w * 0.8},
                    "metadata": base_meta(cache_protocol=proto),
                }
            )
        return out

    def test_exact_counterexample_baseline_mixed_now_rejected(self):
        # Audit counterexample: baseline A,B,B,B,B vs candidate A,A,A,A,A.
        # Old code only read trials[0] (A) and reported a supported claim.
        base = self._trials([10.0] * 5, ["A", "B", "B", "B", "B"])
        cand = self._trials([9.0] * 5, "A")
        result, code = m.compare_series("before", base, "after", cand)
        self.assertEqual(code, 1)
        self.assertEqual(result["kind"], "inconsistent_series")
        self.assertFalse(result["within_series_consistent"])
        self.assertIsNone(result["metadata_match"])
        labels = [e["label"] for e in result["inconsistent_series"]]
        self.assertIn("before", labels)
        before = next(
            e for e in result["inconsistent_series"] if e["label"] == "before"
        )
        self.assertIn("cache_protocol", before["inconsistent_keys"])
        # No verdict / improvement block computed on rejection.
        self.assertNotIn("wall", result)
        self.assertNotIn("verdict", result)

    def test_candidate_mixed_rejected(self):
        base = self._trials([10.0] * 5, "A")
        cand = self._trials([9.0] * 5, ["A", "B", "B", "B", "B"])
        result, code = m.compare_series("before", base, "after", cand)
        self.assertEqual(code, 1)
        self.assertEqual(result["kind"], "inconsistent_series")
        self.assertEqual(
            [e["label"] for e in result["inconsistent_series"]], ["after"]
        )

    def test_mismatch_after_first_trial_caught(self):
        # Old trials[0] check saw A==A and passed; trial[4] differs now.
        base = self._trials([10.0] * 5, "A")
        cand = self._trials([9.0] * 5, ["A", "A", "A", "A", "B"])
        result, code = m.compare_series("before", base, "after", cand)
        self.assertEqual(code, 1)
        self.assertEqual(result["kind"], "inconsistent_series")
        entry = result["inconsistent_series"][0]
        self.assertEqual(entry["label"], "after")
        self.assertIn("cache_protocol", entry["inconsistent_keys"])

    def test_valid_homogeneous_series_still_supported(self):
        base = self._trials([10.0, 10.0, 10.0, 10.0, 11.0], "A")
        cand = self._trials([9.0, 9.0, 9.0, 9.0, 10.0], "A")
        result, code = m.compare_series("before", base, "after", cand)
        self.assertEqual(code, 0)
        self.assertEqual(result["kind"], "compared")
        self.assertTrue(result["within_series_consistent"])
        self.assertTrue(result["metadata_match"])
        self.assertTrue(result["wall"]["improvement_claim_supported"])


class CLITests(unittest.TestCase):
    def test_help_exits_zero(self):
        for argv in (["-h"], ["capture", "-h"], ["summary", "-h"], ["compare", "-h"]):
            with self.assertRaises(SystemExit) as cm:
                m.main(argv)
            self.assertEqual(cm.exception.code, 0, msg=str(argv))

    def test_capture_then_summary_then_compare_via_main(self):
        with tempfile.TemporaryDirectory() as d:
            dpath = Path(d)
            trace = dpath / "b.traceevents"
            write_trace(trace, [csc_line(1_000_000)])
            before = dpath / "before"
            after = dpath / "after"

            meta_args = [
                "--unity-version", "2022.3.20f1",
                "--packages-lock-hash", "sha256:aaa",
                "--project-settings-hash", "sha256:bbb",
                "--debug-optimization", "Debug",
                "--auto-refresh", "true",
                "--cache-protocol", "tundra-default",
                "--invalidation-fixture", "touch-one-script",
            ]
            for i in range(5):
                m.main(
                    ["capture", "--wall-seconds", "10.0", "--trace", str(trace),
                     "--out-dir", str(before), "--trial-id", f"{i:04d}", *meta_args]
                )
                m.main(
                    ["capture", "--wall-seconds", "9.0", "--trace", str(trace),
                     "--out-dir", str(after), "--trial-id", f"{i:04d}", *meta_args]
                )

            buf = io.StringIO()
            old = sys.stdout
            sys.stdout = buf
            try:
                rc = m.main(["summary", "--series", str(before)])
            finally:
                sys.stdout = old
            self.assertEqual(rc, 0)
            summ = json.loads(buf.getvalue())
            self.assertTrue(summ["sufficient"])
            self.assertEqual(summ["wall_seconds"]["median"], 10.0)

            buf = io.StringIO()
            sys.stdout = buf
            try:
                rc = m.main(
                    ["compare", "--baseline", f"before:{before}",
                     "--candidate", f"after:{after}"]
                )
            finally:
                sys.stdout = old
            self.assertEqual(rc, 0)
            res = json.loads(buf.getvalue())
            self.assertTrue(res["wall"]["improvement_claim_supported"])

    def test_compare_mismatch_via_main_exit1(self):
        with tempfile.TemporaryDirectory() as d:
            dpath = Path(d)
            a = dpath / "a"
            b = dpath / "b"
            a.mkdir()
            b.mkdir()
            for wall, out in ((10.0, a), (9.0, b)):
                (out / "trial_0000.json").write_text(
                    json.dumps(
                        {
                            "schema": m.TRIAL_SCHEMA,
                            "wall_seconds": wall,
                            "csc": {"available": True, "total_seconds": wall},
                            "metadata": base_meta(
                                cache_protocol="X" if out is a else "Y"
                            ),
                        }
                    ),
                    encoding="utf-8",
                )
            rc = m.main(
                ["compare", "--baseline", f"a:{a}", "--candidate", f"b:{b}"]
            )
            self.assertEqual(rc, 1)


if __name__ == "__main__":
    unittest.main()
