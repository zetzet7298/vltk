#!/usr/bin/env python3
"""Deterministic read-only guard for Unity production assembly boundaries.

Stdlib-only. Accepts a project root and inspects production asmdefs under
``<root>/Assets/Scripts``. Validates:

  1. Classification — every discovered ``VLTK.*`` assembly must be declared
     in ``LAYER_RANKS`` or ``EXEMPT_BOUNDARIES`` (no untyped assemblies).
  2. Schema — each asmdef is a JSON object with a non-empty string ``name``
     and a ``references`` list of strings; bad JSON/type becomes a finding,
     never an unhandled exception.
  3. Resolved references — ``VLTK.*`` name references and internal
     ``GUID:<guid>`` references (resolved via the asmdef's own ``.meta``)
     must point to a discovered production asmdef. ``GUID:`` refs whose GUID
     is not an internal asmdef are treated as external package refs (not
     errors), so external packages are never mislabelled. A ``GUID:`` ref
     whose payload is not a valid 32-hex GUID is rejected as malformed.
  4. Acyclicity — the resolved dependency graph has no cycles, including
     self-cycles (an assembly referencing itself).
  5. Direction — dependencies obey the layering

         Model < Gameplay.Domain < {PortData,Combat,World}
                                   < Sandbox.Runtime < UI

      Generated protobuf assemblies are the lowest production seam, followed
      by SkillPort and the production networking/world/UI composition layers.
      Core/Resources/Sprites/Backend are *deliberate boundaries*: they take
     part in cycle detection but are exempt from the rank rule, so existing
     intentional edges to/from them never produce false positives.
  6. Forbidden inward dependencies — no production assembly may reference a
     UI / Editor / Test / scene-composition target (leaf consumers must stay
     at the top / outside the reusable core). Applies to name refs AND
     internal GUID refs after resolution.
  7. Script/meta integrity — every ``.cs`` has a sibling ``.cs.meta``, every
     meta GUID is nonempty and well-formed, and GUIDs are unique across
     ``Assets/Scripts``.

Exit code is ``0`` when clean, ``1`` when any violation is found. Output is
deterministic (sorted, one finding per line).
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Set, Tuple

# --- Policy -----------------------------------------------------------------

# Layer ranks: lower = depended upon more. Higher layers may depend on lower.
# Keyed by the asmdef ``name`` field (NOT the directory or filename, which can
# differ — e.g. VLTK.Sandbox.asmdef declares name "VLTK.Sandbox.Runtime").
LAYER_RANKS: Dict[str, int] = {
    "VLTK.Generated.GameV1": 0,
    "VLTK.Generated.ContentV1": 0,
    "VLTK.Model": 0,
    "VLTK.Gameplay.Domain": 1,
    "VLTK.SkillPort": 1,
    "VLTK.PortData": 2,
    "VLTK.Combat": 2,
    "VLTK.World": 2,
    "VLTK.Production.Networking": 2,
    "VLTK.Production.World.Unity": 2,
    "VLTK.Sandbox.Runtime": 3,
    "VLTK.Production.UI.Runtime": 3,
    "VLTK.UI": 4,
    "VLTK.Production.App": 4,
}

# Deliberate boundaries: participate in cycle detection but are exempt from the
# rank/direction rule. Existing edges to/from these are intentional.
EXEMPT_BOUNDARIES: Set[str] = {
    "VLTK.Core",
    "VLTK.Resources",
    "VLTK.Sprites",
    "VLTK.Backend",
}

VLTK_PREFIX = "VLTK."
GUID_LINE_RE = re.compile(r"^guid:\s*([0-9a-fA-F]+)\s*$", re.MULTILINE)
VALID_GUID_RE = re.compile(r"^[0-9a-fA-F]{32}$")


def forbidden_reason(name: str) -> Optional[str]:
    """Return a human reason if ``name`` is a forbidden dependency target.

    Leaf consumers (UI/Editor/Test/scene composition) must never be depended
    on by production runtime assemblies. Name-pattern based so it also catches
    targets that are not part of the discovered production set.
    """
    if name.endswith(".UI"):
        return "UI composition"
    if ".Editor" in name:
        return "Editor assembly"
    if ".Test" in name or ".Tests" in name:
        return "Test assembly"
    if name.endswith((".SceneComposition", ".Scenes", ".Bootstrap")):
        return "Scene composition"
    return None


# --- Discovery / parsing ----------------------------------------------------

def discover_asmdefs(*roots: Path) -> List[Path]:
    """All production asmdefs below the supplied roots, sorted deterministically."""
    return sorted(path for root in roots for path in root.rglob("*.asmdef"))


def parse_asmdef(path: Path) -> Tuple[str, List[str]]:
    """Validate and return (declared name, references) for an asmdef file.

    Raises ``ValueError`` (deterministic, caught by the caller) for any schema
    problem: unparseable JSON, a non-object root, a missing/non-string name,
    or a ``references`` field that is absent-wrong-type or holds non-string
    entries. Never raises ``AttributeError``.
    """
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise ValueError(f"invalid JSON ({exc.msg})") from exc
    if not isinstance(data, dict):
        raise ValueError(
            f"root is {type(data).__name__}, expected a JSON object"
        )
    name = data.get("name")
    if not isinstance(name, str) or not name.strip():
        raise ValueError("'name' missing or not a non-empty string")
    refs = data.get("references", [])
    if refs is None:
        refs = []
    if not isinstance(refs, list):
        raise ValueError(
            f"'references' is {type(refs).__name__}, expected a list"
        )
    out: List[str] = []
    for r in refs:
        if not isinstance(r, str) or not r:
            raise ValueError(
                f"'references' contains non-string/empty entry: {r!r}"
            )
        out.append(r)
    return name, out


def build_guid_map(asmdefs: List[Tuple[Path, str, List[str]]]) -> Dict[str, str]:
    """Map each asmdef's meta GUID -> declared name (internal-assembly index).

    Used to resolve internal ``GUID:<guid>`` references. An asmdef whose
    ``.meta`` is missing or has a malformed GUID is simply absent from the
    map (its own meta problems are reported by ``check_meta_integrity``).
    """
    guid_map: Dict[str, str] = {}
    for path, name, _refs in asmdefs:
        meta_path = path.with_name(path.name + ".meta")
        if not meta_path.is_file():
            continue
        guid = read_guid(meta_path)
        if guid and VALID_GUID_RE.match(guid):
            guid_map[guid] = name
    return guid_map


def read_guid(meta_path: Path) -> Optional[str]:
    """Return the GUID string from a .meta file, or None if absent/malformed."""
    m = GUID_LINE_RE.search(meta_path.read_text(encoding="utf-8", errors="replace"))
    return m.group(1) if m else None


# --- Checks -----------------------------------------------------------------

class Findings:
    """Accumulates deterministic violation messages."""

    def __init__(self) -> None:
        self._lines: List[str] = []

    def add(self, msg: str) -> None:
        self._lines.append(msg)

    def extend(self, msgs: Iterable[str]) -> None:
        self._lines.extend(msgs)

    @property
    def lines(self) -> List[str]:
        return sorted(self._lines)

    def has_errors(self) -> bool:
        return bool(self._lines)


def check_assembly_graph(
    asmdefs: List[Tuple[Path, str, List[str]]], findings: Findings
) -> None:
    names_to_path: Dict[str, Path] = {}
    for path, name, _refs in asmdefs:
        if name in names_to_path:
            findings.add(
                f"[asmdef] duplicate assembly name '{name}': "
                f"{names_to_path[name]} and {path}"
            )
        names_to_path[name] = path

    name_set = set(names_to_path)

    # Classification gate: every VLTK.* assembly must be explicitly typed.
    for name in sorted(names_to_path):
        if (
            name.startswith(VLTK_PREFIX)
            and name not in LAYER_RANKS
            and name not in EXEMPT_BOUNDARIES
        ):
            findings.add(
                f"[unclassified] '{name}' is a VLTK assembly not declared in "
                f"LAYER_RANKS or EXEMPT_BOUNDARIES ({names_to_path[name]})"
            )

    # Internal-asmdef GUID index, for resolving GUID: references.
    guid_map = build_guid_map(asmdefs)

    # Build the resolved dependency graph.
    # edges[a] = sorted unique list of internal targets a depends on.
    edges: Dict[str, List[str]] = {name: [] for _, name, _ in asmdefs}
    for _path, src, refs in asmdefs:
        for raw in refs:
            if raw.startswith("GUID:"):
                payload = raw[len("GUID:"):]
                if not VALID_GUID_RE.match(payload):
                    # Malformed/empty/wrong-length GUID ref is a real error:
                    # Unity requires 32 hex digits. Distinct label so it is
                    # never conflated with a valid-but-unknown external GUID.
                    findings.add(
                        f"[guid-ref] {src} references malformed GUID "
                        f"'{payload}' (expected 32 hex digits)"
                    )
                    continue
                target = guid_map.get(payload)
                if target is None:
                    # Valid 32-hex GUID that is not an internal asmdef ->
                    # external package ref: cannot classify, not an error.
                    continue
            else:
                target = raw
            # Self-cycle is the most fundamental violation; report it first.
            if target == src:
                findings.add(f"[cycle] {src} -> {src} (self-reference)")
                continue
            # Forbidden leaf consumers apply to ALL resolved targets — name
            # refs and internal GUID refs alike. A third-party Editor/Test
            # dependency is just as bad as an internal one.
            reason = forbidden_reason(target)
            if reason:
                findings.add(
                    f"[forbidden] {src} depends on {target} ({reason}) — "
                    f"production may not reference leaf consumers"
                )
                continue
            if not target.startswith(VLTK_PREFIX):
                # Non-VLTK name ref (Newtonsoft.Json, Unity.InputSystem,
                # ...). Intentionally ignored: out of scope.
                continue
            if target not in name_set:
                findings.add(
                    f"[unresolved] {src} references unknown VLTK assembly "
                    f"'{target}'"
                )
                continue
            edges[src].append(target)

    for src in edges:
        edges[src] = sorted(set(edges[src]))

    # Direction check (ranked assemblies only; exempt boundaries skipped).
    for src in sorted(edges):
        if src not in LAYER_RANKS:
            continue
        for dst in edges[src]:
            if dst in LAYER_RANKS and LAYER_RANKS[src] < LAYER_RANKS[dst]:
                findings.add(
                    f"[direction] {src} (rank {LAYER_RANKS[src]}) -> "
                    f"{dst} (rank {LAYER_RANKS[dst]}): lower layer depends on "
                    f"higher layer"
                )

    # Cycle detection (white/gray/black DFS over the resolved graph).
    WHITE, GRAY, BLACK = 0, 1, 2
    color = {n: WHITE for n in edges}
    stack: List[str] = []

    def dfs(node: str) -> None:
        color[node] = GRAY
        stack.append(node)
        for nxt in edges[node]:
            if color.get(nxt) == GRAY:
                idx = stack.index(nxt)
                cycle = stack[idx:] + [nxt]
                findings.add("[cycle] " + " -> ".join(cycle))
            elif color.get(nxt) == WHITE:
                dfs(nxt)
        stack.pop()
        color[node] = BLACK

    for node in sorted(edges):
        if color[node] == WHITE:
            dfs(node)


def check_meta_integrity(scripts_dir: Path, findings: Findings) -> None:
    cs_files = sorted(p for p in scripts_dir.rglob("*.cs"))
    for cs in cs_files:
        meta = cs.with_name(cs.name + ".meta")
        if not meta.is_file():
            findings.add(f"[meta] missing .meta for {cs.relative_to(scripts_dir)}")

    # Orphan .cs.meta (no sibling .cs).
    for meta in sorted(scripts_dir.rglob("*.cs.meta")):
        if not meta.with_name(meta.name[:-5]).is_file():
            findings.add(
                f"[meta] orphan .cs.meta without source: "
                f"{meta.relative_to(scripts_dir)}"
            )

    # GUID: nonempty, well-formed, unique — across every .meta in the tree
    # (covers .cs.meta, .asmdef.meta, and folder .meta alike, matching Unity's
    # own project-wide uniqueness rule).
    seen: Dict[str, Path] = {}
    for meta in sorted(scripts_dir.rglob("*.meta")):
        guid = read_guid(meta)
        rel = meta.relative_to(scripts_dir)
        if not guid:
            findings.add(f"[guid] empty/missing guid in {rel}")
            continue
        if not VALID_GUID_RE.match(guid):
            findings.add(f"[guid] malformed guid '{guid}' in {rel}")
            continue
        if guid in seen:
            findings.add(
                f"[guid] duplicate guid {guid}: {seen[guid]} and {rel}"
            )
        else:
            seen[guid] = rel


# --- Entry point ------------------------------------------------------------

def run(root: Path) -> Findings:
    scripts_dir = root / "Assets" / "Scripts"
    findings = Findings()
    if not scripts_dir.is_dir():
        findings.add(f"[config] {scripts_dir} does not exist")
        return findings

    # Generated protobuf assemblies live outside Assets/Scripts but are part of
    # the production dependency graph. Including their asmdefs resolves those
    # references without weakening the unknown-VLTK fail-closed rule.
    asmdef_paths = discover_asmdefs(scripts_dir, root / "Assets" / "Generated")
    parsed: List[Tuple[Path, str, List[str]]] = []
    for p in asmdef_paths:
        try:
            parsed.append((p, *parse_asmdef(p)))
        except (ValueError, json.JSONDecodeError, OSError) as exc:
            findings.add(f"[asmdef] failed to parse {p}: {exc}")
    if parsed:
        check_assembly_graph(parsed, findings)
    check_meta_integrity(scripts_dir, findings)
    return findings


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        description="Validate Unity production assembly boundaries and meta GUIDs."
    )
    parser.add_argument(
        "--root",
        type=Path,
        default=Path.cwd(),
        help="Unity project root containing Assets/Scripts (default: cwd)",
    )
    args = parser.parse_args(argv)

    findings = run(args.root.resolve())
    for line in findings.lines:
        print(line)
    if findings.has_errors():
        print(f"\nBOUNDARIES: {len(findings.lines)} violation(s) — FAILED")
        return 1
    print("BOUNDARIES: clean — OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
