#!/usr/bin/env python3
"""Record and compare Unity Debug script-compile trials. Read-only.

This tool NEVER launches Unity, NEVER edits ProjectSettings, NEVER deletes
caches, and NEVER invents a baseline. It only reads already-produced artifacts:

  * wall time  -> supplied explicitly (the caller measures
    ``scripts/compile_scripts.py`` with an external clock and passes ``--wall-seconds``).
  * Bee traces -> ``Library/Bee/backend*.traceevents`` (Chrome Trace-Event
    format, newline-delimited JSON; ``Csc`` ``X`` events carry the csc dur in
    microseconds). Bee overwrites these files between runs, so capture must
    happen once per trial before the next compile.

Three subcommands:

  capture   read one completed trial (wall seconds + Bee traces) and write a
            compact, deterministic ``trial_*.json`` into an explicit out-dir.
  summary   median/p95 over the trials in a series dir.
  compare   two labeled series; reports an improvement verdict ONLY when
            metadata match, both sides have >=5 trials, and p95 is not worse.

A valid improvement claim also requires the median wall delta to exceed the
larger of 5% of the baseline median or 0.5s. The tool reports numbers and a
conservative boolean; it asserts no speed-up that fails these gates.

Run: ``python3 scripts/measure_unity_debug_compile.py <subcommand> -h``
"""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path
from typing import Any, Iterable

TRIAL_SCHEMA = "unity-debug-compile-trial/v1"
COMPARE_SCHEMA = "unity-debug-compile-compare/v1"
MIN_TRIALS = 5
# Metadata keys that MUST match before two series may be compared.
METADATA_KEYS = (
    "unity_version",
    "packages_lock_hash",
    "project_settings_hash",
    "debug_optimization",
    "auto_refresh",
    "cache_protocol",
    "invalidation_fixture",
)


# --------------------------------------------------------------------------- #
# Trace parsing
# --------------------------------------------------------------------------- #
def parse_trace_file(path: Path) -> tuple[int, int, int]:
    """Parse one Bee ``*.traceevents`` file.

    Returns ``(csc_events, csc_total_micros, parse_errors)``. Only complete
    ``X`` events named ``Csc`` with an integer/float ``dur`` are summed.
    Malformed lines are counted in ``parse_errors`` and skipped so a single bad
    line never aborts an otherwise valid capture.
    """
    events = 0
    total_micros = 0
    errors = 0
    with path.open("r", encoding="utf-8", errors="replace") as fh:
        for raw in fh:
            line = raw.strip()
            if not line:
                continue
            # Chrome trace dumps comma-prefix every record after the first.
            line = line.lstrip(",").strip()
            if not line:
                continue
            try:
                ev = json.loads(line)
            except (ValueError, TypeError):
                errors += 1
                continue
            if not isinstance(ev, dict):
                errors += 1
                continue
            if ev.get("ph") != "X" or ev.get("name") != "Csc":
                continue
            dur = ev.get("dur")
            if not isinstance(dur, (int, float)) or isinstance(dur, bool):
                continue
            events += 1
            total_micros += int(dur)
    return events, total_micros, errors


def collect_csc(trace_paths: Iterable[Path]) -> dict[str, Any]:
    """Aggregate ``Csc`` stats across one or more trace files."""
    sources: list[str] = []
    events = 0
    micros = 0
    errors = 0
    for p in trace_paths:
        e, m, err = parse_trace_file(p)
        sources.append(str(p))
        events += e
        micros += m
        errors += err
    return {
        "available": events > 0,
        "events": events,
        "total_micros": micros,
        "total_seconds": round(micros / 1_000_000, 6),
        "parse_errors": errors,
        "sources": sources,
    }


# --------------------------------------------------------------------------- #
# Stats
# --------------------------------------------------------------------------- #
def median(values: list[float]) -> float:
    s = sorted(values)
    n = len(s)
    mid = n // 2
    if n == 0:
        return float("nan")
    if n % 2 == 1:
        return float(s[mid])
    return (s[mid - 1] + s[mid]) / 2.0


def p95_nearest_rank(values: list[float]) -> float:
    """Nearest-rank 95th percentile (1-indexed ``ceil(0.95*n)``, clamped).

    Reproducible without numpy; for n=5 it equals the sample max, which is the
    conservative reading of "p95 not worse" at the minimum trial count.
    """
    s = sorted(values)
    n = len(s)
    if n == 0:
        return float("nan")
    rank = max(1, min(n, math.ceil(0.95 * n)))
    return float(s[rank - 1])


def _series_stats(trials: list[dict], key: str) -> dict[str, Any] | None:
    """Median/p95 for a numeric trial key, or None if any value is missing."""
    vals = [t.get(key) for t in trials]
    if any(not isinstance(v, (int, float)) or isinstance(v, bool) for v in vals):
        return None
    fvals = [float(v) for v in vals]
    return {
        "count": len(fvals),
        "median": round(median(fvals), 6),
        "p95": round(p95_nearest_rank(fvals), 6),
    }


# --------------------------------------------------------------------------- #
# Trial capture / load
# --------------------------------------------------------------------------- #
def _next_trial_id(out_dir: Path) -> str:
    existing = [p.stem for p in out_dir.glob("trial_*.json")]
    nums = []
    for name in existing:
        suffix = name[len("trial_"):]
        if suffix.isdigit():
            nums.append(int(suffix))
    return str(max(nums) + 1).zfill(4) if nums else "0001"


def capture_trial(
    wall_seconds: float,
    trace_paths: list[Path],
    metadata: dict[str, Any],
    out_dir: Path,
    trial_id: str | None = None,
    note: str | None = None,
) -> dict[str, Any]:
    """Build a trial record, write ``trial_<id>.json`` into ``out_dir``.

    The record is fully deterministic given the inputs (no clock). The out-dir
    is the only thing this command writes; trace files are read-only.
    """
    if not isinstance(wall_seconds, (int, float)) or isinstance(wall_seconds, bool):
        raise TypeError("--wall-seconds must be a number")
    if wall_seconds < 0:
        raise ValueError("--wall-seconds must be >= 0")
    missing = [k for k in METADATA_KEYS if metadata.get(k) is None]
    if missing:
        raise ValueError("missing required metadata: " + ", ".join(missing))
    if not trace_paths:
        raise ValueError("at least one --trace file is required")

    out_dir.mkdir(parents=True, exist_ok=True)
    tid = trial_id or _next_trial_id(out_dir)
    record = {
        "schema": TRIAL_SCHEMA,
        "trial_id": tid,
        "wall_seconds": round(float(wall_seconds), 6),
        "csc": collect_csc(trace_paths),
        "metadata": {k: metadata[k] for k in METADATA_KEYS},
        "note": note,
    }
    (out_dir / f"trial_{tid}.json").write_text(
        json.dumps(record, indent=2, sort_keys=True) + "\n", encoding="utf-8"
    )
    return record


def load_trials(series_dir: Path) -> list[dict[str, Any]]:
    if not series_dir.is_dir():
        raise FileNotFoundError(f"series dir not found: {series_dir}")
    trials = []
    for p in sorted(series_dir.glob("trial_*.json")):
        try:
            data = json.loads(p.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            continue
        if isinstance(data, dict) and data.get("schema") == TRIAL_SCHEMA:
            trials.append(data)
    return trials


def summarize_series(trials: list[dict[str, Any]]) -> dict[str, Any]:
    wall = _series_stats(trials, "wall_seconds")
    csc_vals = [
        float(t["csc"]["total_seconds"])
        for t in trials
        if isinstance(t.get("csc"), dict) and t["csc"].get("available")
    ]
    csc = None
    if len(csc_vals) == len(trials) and csc_vals:
        csc = {
            "count": len(csc_vals),
            "median": round(median(csc_vals), 6),
            "p95": round(p95_nearest_rank(csc_vals), 6),
        }
    return {
        "count": len(trials),
        "sufficient": len(trials) >= MIN_TRIALS,
        "wall_seconds": wall,
        "csc_seconds": csc,
    }


# --------------------------------------------------------------------------- #
# Compare
# --------------------------------------------------------------------------- #
def metadata_matches(
    a: dict[str, Any], b: dict[str, Any]
) -> tuple[bool, list[str]]:
    diffs = [k for k in METADATA_KEYS if a.get(k) != b.get(k)]
    return (not diffs, diffs)


def _hashable(value: Any) -> Any:
    try:
        hash(value)
        return value
    except TypeError:
        return json.dumps(value, sort_keys=True)


def series_inconsistent_keys(trials: list[dict[str, Any]]) -> list[str]:
    """METADATA_KEYS whose values differ WITHIN one series (empty = homogeneous).

    Guards the P1 finding: a mixed series must be rejected even when
    ``trials[0]`` happens to match the other series. After this passes,
    ``_side`` may safely treat ``trials[0]`` as representative.
    """
    keys: list[str] = []
    for k in METADATA_KEYS:
        seen: set[Any] = set()
        for t in trials:
            meta = t.get("metadata") or {}
            seen.add(_hashable(meta.get(k)))
            if len(seen) > 1:
                keys.append(k)
                break
    return keys


def _side(label: str, trials: list[dict[str, Any]]) -> dict[str, Any]:
    summary = summarize_series(trials)
    meta = trials[0]["metadata"] if trials else {}
    csc_avail = all(
        isinstance(t.get("csc"), dict) and t["csc"].get("available") for t in trials
    ) and bool(trials)
    return {
        "label": label,
        "count": summary["count"],
        "sufficient": summary["sufficient"],
        "median_wall_seconds": summary["wall_seconds"]["median"]
        if summary["wall_seconds"]
        else None,
        "p95_wall_seconds": summary["wall_seconds"]["p95"]
        if summary["wall_seconds"]
        else None,
        "median_csc_seconds": summary["csc_seconds"]["median"]
        if summary["csc_seconds"]
        else None,
        "p95_csc_seconds": summary["csc_seconds"]["p95"]
        if summary["csc_seconds"]
        else None,
        "csc_available": csc_avail,
        "metadata": {k: meta.get(k) for k in METADATA_KEYS},
    }


def _metric_block(
    baseline: dict[str, Any], candidate: dict[str, Any], median_key: str, p95_key: str
) -> dict[str, Any]:
    bm = baseline[median_key]
    cm = candidate[median_key]
    bp95 = baseline[p95_key]
    cp95 = candidate[p95_key]
    if bm is None or cm is None or bp95 is None or cp95 is None:
        return {
            "baseline_median": bm,
            "candidate_median": cm,
            "median_delta_seconds": None,
            "median_delta_pct": None,
            "candidate_p95_not_worse": None,
            "improvement_claim_supported": False,
        }
    delta = bm - cm  # positive => candidate faster
    noise = max(0.05 * bm, 0.5)
    not_worse = cp95 <= bp95
    return {
        "baseline_median": round(bm, 6),
        "candidate_median": round(cm, 6),
        "median_delta_seconds": round(delta, 6),
        "median_delta_pct": round((delta / bm) * 100.0, 3) if bm else None,
        "noise_seconds": round(noise, 6),
        "candidate_p95_not_worse": not_worse,
        "improvement_claim_supported": (delta > noise) and not_worse,
    }


def compare_series(
    baseline_label: str,
    baseline_trials: list[dict[str, Any]],
    candidate_label: str,
    candidate_trials: list[dict[str, Any]],
) -> tuple[dict[str, Any], int]:
    """Compare two labeled series.

    Returns ``(result, exit_code)``. exit_code:
      1 = metadata integrity failure (within-series inconsistency or
          cross-series mismatch),
      2 = empty series or insufficient trials (<5 on a side),
      0 = comparison performed (verdict may still be "no improvement").

    Order of gates: empty -> within-series homogeneity -> cross-series
    match -> trial count. Within-series is checked before ``_side`` so a
    series like [A,B,B,B,B] is rejected even though trial[0] (A) matches.
    """
    if not baseline_trials or not candidate_trials:
        return (
            {
                "schema": COMPARE_SCHEMA,
                "rejected": True,
                "kind": "empty_series",
                "within_series_consistent": None,
                "metadata_match": None,
                "error": "one or both series are empty",
            },
            2,
        )

    inconsistent = []
    for label, trials in (
        (baseline_label, baseline_trials),
        (candidate_label, candidate_trials),
    ):
        keys = series_inconsistent_keys(trials)
        if keys:
            inconsistent.append({"label": label, "inconsistent_keys": keys})
    if inconsistent:
        parts = [
            f"{e['label']} ({', '.join(e['inconsistent_keys'])})"
            for e in inconsistent
        ]
        return (
            {
                "schema": COMPARE_SCHEMA,
                "rejected": True,
                "kind": "inconsistent_series",
                "within_series_consistent": False,
                "metadata_match": None,
                "metadata_diff": [],
                "inconsistent_series": inconsistent,
                "error": "inconsistent metadata within series: " + "; ".join(parts),
            },
            1,
        )

    base = _side(baseline_label, baseline_trials)
    cand = _side(candidate_label, candidate_trials)

    match, diffs = metadata_matches(base["metadata"], cand["metadata"])
    if not match:
        return (
            {
                "schema": COMPARE_SCHEMA,
                "baseline": base,
                "candidate": cand,
                "rejected": True,
                "kind": "metadata_mismatch",
                "within_series_consistent": True,
                "metadata_match": False,
                "metadata_diff": diffs,
                "error": "metadata mismatch; refusing to compare: " + ", ".join(diffs),
            },
            1,
        )

    if not (base["sufficient"] and cand["sufficient"]):
        return (
            {
                "schema": COMPARE_SCHEMA,
                "baseline": base,
                "candidate": cand,
                "rejected": True,
                "kind": "insufficient_trials",
                "within_series_consistent": True,
                "metadata_match": True,
                "metadata_diff": [],
                "error": (
                    f"need >= {MIN_TRIALS} trials per side "
                    f"(baseline={base['count']}, candidate={cand['count']})"
                ),
            },
            2,
        )

    wall = _metric_block(base, cand, "median_wall_seconds", "p95_wall_seconds")
    csc: dict[str, Any] | None = None
    if base["csc_available"] and cand["csc_available"]:
        csc = _metric_block(base, cand, "median_csc_seconds", "p95_csc_seconds")

    dw = wall["median_delta_seconds"]
    verdict_bits = []
    if wall["improvement_claim_supported"] and dw is not None:
        verdict_bits.append(
            f"{candidate_label} faster by {dw:.3f}s ({wall['median_delta_pct']:.1f}%) "
            f"on wall time; p95 not worse"
        )
    else:
        verdict_bits.append("no supported wall-time improvement claim")
    if csc and csc.get("improvement_claim_supported"):
        verdict_bits.append(
            f"{candidate_label} faster by {csc['median_delta_seconds']:.3f}s on csc time; p95 not worse"
        )
    elif csc:
        verdict_bits.append("no supported csc-time improvement claim")

    return (
        {
            "schema": COMPARE_SCHEMA,
            "baseline": base,
            "candidate": cand,
            "rejected": False,
            "kind": "compared",
            "within_series_consistent": True,
            "metadata_match": True,
            "metadata_diff": [],
            "wall": wall,
            "csc": csc,
            "verdict": "; ".join(verdict_bits),
        },
        0,
    )


# --------------------------------------------------------------------------- #
# CLI
# --------------------------------------------------------------------------- #
def _str2bool(v: str) -> bool:
    if v.lower() in ("true", "1", "yes", "y"):
        return True
    if v.lower() in ("false", "0", "no", "n"):
        return False
    raise argparse.ArgumentTypeError("expected true or false")


def _add_metadata_args(p: argparse.ArgumentParser) -> None:
    p.add_argument("--unity-version", required=True)
    p.add_argument("--packages-lock-hash", required=True)
    p.add_argument("--project-settings-hash", required=True)
    p.add_argument("--debug-optimization", required=True)
    p.add_argument("--auto-refresh", type=_str2bool, required=True)
    p.add_argument("--cache-protocol", required=True)
    p.add_argument("--invalidation-fixture", required=True)


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="measure_unity_debug_compile",
        description="Record/compare Unity Debug compile trials (read-only).",
    )
    sub = parser.add_subparsers(dest="command", required=True)

    pc = sub.add_parser(
        "capture", help="record one completed trial into an out-dir"
    )
    pc.add_argument("--wall-seconds", type=float, required=True)
    pc.add_argument(
        "--trace",
        action="append",
        required=True,
        metavar="PATH",
        help="Bee *.traceevents file (repeatable)",
    )
    pc.add_argument("--out-dir", required=True)
    pc.add_argument("--trial-id", default=None)
    pc.add_argument("--note", default=None)
    _add_metadata_args(pc)

    ps = sub.add_parser("summary", help="median/p95 over a series dir")
    ps.add_argument("--series", required=True, help="dir of trial_*.json files")

    pcm = sub.add_parser(
        "compare", help="compare two labeled series (metadata must match)"
    )
    pcm.add_argument(
        "--baseline",
        required=True,
        metavar="LABEL:DIR",
        help='e.g. before:/path/to/series',
    )
    pcm.add_argument(
        "--candidate",
        required=True,
        metavar="LABEL:DIR",
        help='e.g. after:/path/to/series',
    )
    return parser


def _split_label_dir(spec: str) -> tuple[str, Path]:
    if ":" not in spec:
        raise argparse.ArgumentTypeError(
            f"expected LABEL:DIR, got {spec!r}"
        )
    label, path = spec.split(":", 1)
    if not label or not path:
        raise argparse.ArgumentTypeError(f"expected LABEL:DIR, got {spec!r}")
    return label, Path(path)


def main(argv: list[str] | None = None) -> int:
    parser = _build_parser()
    args = parser.parse_args(argv)
    out = sys.stdout

    if args.command == "capture":
        metadata = {k: getattr(args, k.replace("-", "_")) for k in METADATA_KEYS}
        try:
            record = capture_trial(
                wall_seconds=args.wall_seconds,
                trace_paths=[Path(t) for t in args.trace],
                metadata=metadata,
                out_dir=Path(args.out_dir),
                trial_id=args.trial_id,
                note=args.note,
            )
        except (OSError, ValueError, TypeError) as exc:
            print(f"capture failed: {exc}", file=sys.stderr)
            return 1
        json.dump(record, out, indent=2, sort_keys=True)
        out.write("\n")
        return 0

    if args.command == "summary":
        trials = load_trials(Path(args.series))
        result = summarize_series(trials)
        result["series"] = str(args.series)
        json.dump(result, out, indent=2, sort_keys=True)
        out.write("\n")
        return 0

    if args.command == "compare":
        blabel, bdir = _split_label_dir(args.baseline)
        clabel, cdir = _split_label_dir(args.candidate)
        try:
            base_trials = load_trials(bdir)
            cand_trials = load_trials(cdir)
        except FileNotFoundError as exc:
            print(str(exc), file=sys.stderr)
            return 2
        result, code = compare_series(blabel, base_trials, clabel, cand_trials)
        json.dump(result, out, indent=2, sort_keys=True)
        out.write("\n")
        return code

    parser.error("unknown command")  # pragma: no cover
    return 2


if __name__ == "__main__":
    sys.exit(main())
