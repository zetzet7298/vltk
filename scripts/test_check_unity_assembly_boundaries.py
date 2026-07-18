#!/usr/bin/env python3
"""Focused unittest suite for check_unity_assembly_boundaries.

Builds minimal throwaway Unity projects in tmp dirs to exercise each rule
deterministically: success, cycle, forbidden edge, direction violation,
unresolved reference, missing meta, orphan meta, malformed GUID, duplicate
GUID, and the forbidden-target pattern set.

Run: ``python3 -m unittest scripts.test_check_unity_assembly_boundaries``
"""

from __future__ import annotations

import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

# Make the sibling guard module importable both when this file is run directly
# from scripts/ and when collected as scripts.test_* from the repo root.
sys.path.insert(0, str(Path(__file__).resolve().parent))

import check_unity_assembly_boundaries as guard  # noqa: E402

GUIDS = [
    "00000000000000000000000000000001",
    "00000000000000000000000000000002",
    "00000000000000000000000000000003",
    "00000000000000000000000000000004",
    "00000000000000000000000000000005",
    "00000000000000000000000000000006",
    "00000000000000000000000000000007",
    "00000000000000000000000000000008",
    "00000000000000000000000000000009",
    "0000000000000000000000000000000a",
    "0000000000000000000000000000000b",
    "0000000000000000000000000000000c",
]


def asmdef(name: str, refs=None) -> str:
    import json
    return json.dumps(
        {
            "name": name,
            "rootNamespace": name,
            "references": list(refs or []),
            "includePlatforms": [],
            "excludePlatforms": [],
            "allowUnsafeCode": False,
            "overrideReferences": False,
            "precompiledReferences": [],
            "autoReferenced": True,
            "defineConstraints": [],
            "versionDefines": [],
            "noEngineReferences": False,
        },
        indent=4,
    )


def meta(guid: str) -> str:
    return "fileFormatVersion: 2\nguid: " + guid + "\n"


class FakeProject:
    """Minimal Assets/Scripts tree builder. Each asmdef dir gets a .cs + meta."""

    def __init__(self) -> None:
        self.root = Path(tempfile.mkdtemp(prefix="unity-bound-"))
        self.scripts = self.root / "Assets" / "Scripts"
        self.scripts.mkdir(parents=True)
        self._guid_idx = 0

    def close(self) -> None:
        shutil.rmtree(self.root, ignore_errors=True)

    def _next_guid(self) -> str:
        g = GUIDS[self._guid_idx % len(GUIDS)]
        self._guid_idx += 1
        return g

    def add_asmdef(self, name: str, refs=None, *, dir_name: str | None = None,
                   asmdef_guid: str | None = None,
                   reuse_guid: str | None = None) -> Path:
        d = self.scripts / (dir_name or name.replace("VLTK.", "").replace(".", "/"))
        d.mkdir(parents=True, exist_ok=True)
        (d / f"{name}.asmdef").write_text(asmdef(name, refs), encoding="utf-8")
        # asmdef_guid pins the asmdef .meta GUID (for GUID-reference tests);
        # reuse_guid forces BOTH asmdef + Entry.cs metas to collide (dup test).
        asm_g = asmdef_guid or reuse_guid or self._next_guid()
        (d / f"{name}.asmdef.meta").write_text(meta(asm_g), encoding="utf-8")
        # one .cs so the dir is a real script root with valid meta pairing
        cs = d / "Entry.cs"
        cs.write_text(f"namespace {name} {{}}\n", encoding="utf-8")
        cg = reuse_guid or self._next_guid()
        (cs.with_name(cs.name + ".meta")).write_text(meta(cg), encoding="utf-8")
        return d

    def add_raw_asmdef(self, rel_path: str, content: str) -> Path:
        """Write an asmdef file with arbitrary (possibly invalid) content."""
        p = self.scripts / rel_path
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(content, encoding="utf-8")
        return p

    def add_cs(self, rel_path: str, *, guid: str | None = None,
               no_meta: bool = False) -> Path:
        p = self.scripts / rel_path
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text("// stub\n", encoding="utf-8")
        if not no_meta:
            (p.with_name(p.name + ".meta")).write_text(
                meta(guid or self._next_guid()), encoding="utf-8"
            )
        return p

    def findings(self) -> guard.Findings:
        return guard.run(self.root)


def _lines(f: guard.Findings) -> list[str]:
    return f.lines


class ForbiddenReasonTests(unittest.TestCase):
    def test_ui_editor_test_scene_patterns(self):
        self.assertEqual(guard.forbidden_reason("VLTK.UI"), "UI composition")
        self.assertEqual(guard.forbidden_reason("Foo.Editor"), "Editor assembly")
        self.assertEqual(guard.forbidden_reason("VLTK.Tests.Foo"), "Test assembly")
        self.assertEqual(guard.forbidden_reason("X.Test"), "Test assembly")
        self.assertEqual(guard.forbidden_reason("Game.Scenes"), "Scene composition")
        self.assertIsNone(guard.forbidden_reason("VLTK.Model"))
        self.assertIsNone(guard.forbidden_reason("VLTK.Core"))
        self.assertIsNone(guard.forbidden_reason("Newtonsoft.Json"))


class SuccessTests(unittest.TestCase):
    def test_clean_layered_project_passes(self):
        p = FakeProject()
        try:
            # Model < Gameplay.Domain < {PortData} < Sandbox.Runtime < UI,
            # plus an exempt Core boundary and an external (non-VLTK) ref.
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.Gameplay.Domain", ["VLTK.Core", "VLTK.Model"])
            p.add_asmdef("VLTK.Core", ["VLTK.Model"])
            p.add_asmdef("VLTK.PortData", ["VLTK.Core", "VLTK.Model"])
            p.add_asmdef("VLTK.Sandbox.Runtime",
                         ["VLTK.Core", "VLTK.Model", "VLTK.Gameplay.Domain",
                          "VLTK.PortData", "Unity.InputSystem"])
            p.add_asmdef("VLTK.UI",
                         ["VLTK.Core", "VLTK.Model", "VLTK.Sandbox.Runtime",
                          "Newtonsoft.Json"])
            f = p.findings()
            self.assertEqual(_lines(f), [], msg="\n".join(_lines(f)))
        finally:
            p.close()

    def test_exempt_boundary_may_depend_on_higher_without_direction_error(self):
        # Backend is exempt from rank; referencing higher layers is allowed as
        # long as no cycle and no forbidden target.
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.Backend", ["VLTK.Model"])
            p.add_asmdef("VLTK.UI", ["VLTK.Backend", "VLTK.Model"])
            self.assertEqual(_lines(p.findings()), [])
        finally:
            p.close()


class GraphViolationTests(unittest.TestCase):
    def test_cycle_detected(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model", ["VLTK.Gameplay.Domain"])
            p.add_asmdef("VLTK.Gameplay.Domain", ["VLTK.Model"])
            lines = _lines(p.findings())
            self.assertTrue(any(l.startswith("[cycle]") for l in lines),
                            msg=str(lines))
        finally:
            p.close()

    def test_forbidden_ui_dependency(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.UI", ["VLTK.Model"])
            p.add_asmdef("VLTK.Sandbox.Runtime", ["VLTK.Model", "VLTK.UI"])
            lines = _lines(p.findings())
            self.assertTrue(
                any("[forbidden]" in l and "VLTK.UI" in l for l in lines),
                msg=str(lines),
            )
        finally:
            p.close()

    def test_forbidden_editor_and_test_targets(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.UI", ["VLTK.Model", "Acme.Editor", "Acme.Tests"])
            lines = _lines(p.findings())
            self.assertTrue(any("Editor assembly" in l for l in lines), msg=str(lines))
            self.assertTrue(any("Test assembly" in l for l in lines), msg=str(lines))
        finally:
            p.close()

    def test_direction_violation_lower_depends_on_higher(self):
        # Model (rank 0) depending on Sandbox.Runtime (rank 3) is illegal.
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Sandbox.Runtime", ["VLTK.Model"])
            p.add_asmdef("VLTK.Model", ["VLTK.Sandbox.Runtime"])
            lines = _lines(p.findings())
            self.assertTrue(
                any(l.startswith("[direction]") and "VLTK.Model" in l
                    and "VLTK.Sandbox.Runtime" in l for l in lines),
                msg=str(lines),
            )
        finally:
            p.close()

    def test_unresolved_vltk_reference(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.UI", ["VLTK.Model", "VLTK.Ghost"])
            lines = _lines(p.findings())
            self.assertTrue(
                any("[unresolved]" in l and "VLTK.Ghost" in l for l in lines),
                msg=str(lines),
            )
        finally:
            p.close()

    def test_duplicate_assembly_name(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.UI", ["VLTK.Model"], dir_name="UI")
            p.add_asmdef("VLTK.UI", ["VLTK.Model"], dir_name="UI2")
            lines = _lines(p.findings())
            self.assertTrue(any("[asmdef] duplicate" in l for l in lines),
                            msg=str(lines))
        finally:
            p.close()


class MetaIntegrityTests(unittest.TestCase):
    def test_missing_meta(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_cs("Loose/NoMeta.cs", no_meta=True)
            lines = _lines(p.findings())
            self.assertTrue(
                any("[meta] missing .meta" in l and "NoMeta.cs" in l for l in lines),
                msg=str(lines),
            )
        finally:
            p.close()

    def test_orphan_cs_meta(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            meta_path = p.scripts / "Loose" / "Orphan.cs.meta"
            meta_path.parent.mkdir(parents=True, exist_ok=True)
            meta_path.write_text(meta("1234567890abcdef1234567890abcdef"),
                                 encoding="utf-8")
            lines = _lines(p.findings())
            self.assertTrue(any("[meta] orphan" in l for l in lines),
                            msg=str(lines))
        finally:
            p.close()

    def test_malformed_guid(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_cs("Loose/Bad.cs", guid="1234567890abcdef")
            lines = _lines(p.findings())
            self.assertTrue(any("[guid] malformed" in l for l in lines),
                            msg=str(lines))
        finally:
            p.close()

    def test_empty_guid(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            cs = p.scripts / "Loose" / "Empty.cs"
            cs.parent.mkdir(parents=True, exist_ok=True)
            cs.write_text("// x\n", encoding="utf-8")
            (cs.with_name(cs.name + ".meta")).write_text(
                "fileFormatVersion: 2\n", encoding="utf-8"
            )
            lines = _lines(p.findings())
            self.assertTrue(any("[guid] empty/missing" in l for l in lines),
                            msg=str(lines))
        finally:
            p.close()

    def test_duplicate_guid(self):
        p = FakeProject()
        try:
            g = "deadbeefdeadbeefdeadbeefdeadbeef"
            p.add_asmdef("VLTK.Model", reuse_guid=g)
            # second .cs with the SAME guid -> collision
            p.add_cs("Extra/Dup.cs", guid=g)
            lines = _lines(p.findings())
            self.assertTrue(any("[guid] duplicate" in l and g in l for l in lines),
                            msg=str(lines))
        finally:
            p.close()


class MissingRootTests(unittest.TestCase):
    def test_missing_scripts_dir_reports_config_error(self):
        p = FakeProject()
        try:
            shutil.rmtree(p.scripts)
            lines = _lines(p.findings())
            self.assertTrue(any("[config]" in l for l in lines), msg=str(lines))
        finally:
            p.close()


class ExitCodeTests(unittest.TestCase):
    def test_main_clean_returns_zero(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            rc = guard.main(["--root", str(p.root)])
            self.assertEqual(rc, 0)
        finally:
            p.close()

    def test_main_violation_returns_one(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.UI", ["VLTK.Model"])
            p.add_asmdef("VLTK.Sandbox.Runtime", ["VLTK.UI"])
            rc = guard.main(["--root", str(p.root)])
            self.assertEqual(rc, 1)
        finally:
            p.close()


# --- Reviewer-driven regression coverage ------------------------------------


class GuidReferenceTests(unittest.TestCase):
    """P1: internal GUID:* refs must flow through all policy checks."""

    def test_internal_guid_to_ui_is_forbidden(self):
        p = FakeProject()
        try:
            ui_guid = "aaaa1111aaaa1111aaaa1111aaaa1111"
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.UI", ["VLTK.Model"], asmdef_guid=ui_guid)
            p.add_asmdef(
                "VLTK.Sandbox.Runtime",
                ["VLTK.Model", f"GUID:{ui_guid}"],
            )
            lines = _lines(p.findings())
            self.assertTrue(
                any("[forbidden]" in l and "VLTK.UI" in l for l in lines),
                msg=str(lines),
            )
        finally:
            p.close()

    def test_guid_cycle_detected(self):
        # Same rank (PortData/Combat) so no direction violation masks the cycle.
        p = FakeProject()
        try:
            gp = "bbbb2222bbbb2222bbbb2222bbbb2222"
            gc = "cccc3333cccc3333cccc3333cccc3333"
            p.add_asmdef("VLTK.PortData", [f"GUID:{gc}"], asmdef_guid=gp)
            p.add_asmdef("VLTK.Combat", [f"GUID:{gp}"], asmdef_guid=gc)
            lines = _lines(p.findings())
            self.assertTrue(any(l.startswith("[cycle]") for l in lines),
                            msg=str(lines))
        finally:
            p.close()

    def test_unknown_guid_is_external_not_an_error(self):
        # A GUID that matches no internal asmdef is an external package ref:
        # it must NOT be reported (no mislabelling of package refs).
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef(
                "VLTK.UI",
                ["VLTK.Model", "GUID:ffffffffffffffffffffffffffffffff"],
            )
            self.assertEqual(_lines(p.findings()), [])
        finally:
            p.close()


class MalformedGuidRefTests(unittest.TestCase):
    """P3: malformed GUID: refs are rejected; valid unknown GUIDs stay clean."""

    def test_non_hex_guid_ref_rejected(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.UI", ["VLTK.Model", "GUID:not-a-guid"])
            lines = _lines(p.findings())
            self.assertTrue(
                any("[guid-ref]" in l and "not-a-guid" in l for l in lines),
                msg=str(lines),
            )
            self.assertEqual(guard.main(["--root", str(p.root)]), 1)
        finally:
            p.close()

    def test_empty_guid_ref_rejected(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.UI", ["VLTK.Model", "GUID:"])
            lines = _lines(p.findings())
            self.assertTrue(
                any("[guid-ref]" in l and "malformed" in l for l in lines),
                msg=str(lines),
            )
        finally:
            p.close()

    def test_wrong_length_guid_ref_rejected(self):
        # 31 hex digits (one short) and 33 (one long) are both malformed.
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef(
                "VLTK.UI",
                ["VLTK.Model",
                 "GUID:1111111111111111111111111111111",   # 31
                 "GUID:222222222222222222222222222222222"],  # 33
            )
            lines = _lines(p.findings())
            self.assertEqual(
                sum(1 for l in lines if l.startswith("[guid-ref]")), 2,
                msg=str(lines),
            )
        finally:
            p.close()

    def test_valid_unknown_guid_remains_clean(self):
        # A well-formed 32-hex GUID that matches no internal asmdef is an
        # external package ref: must NOT be flagged.
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef(
                "VLTK.UI",
                ["VLTK.Model", "GUID:0123456789abcdef0123456789abcdef"],
            )
            self.assertEqual(_lines(p.findings()), [])
        finally:
            p.close()


class UnclassifiedAssemblyTests(unittest.TestCase):
    """P1: every VLTK.* assembly must be in LAYER_RANKS or EXEMPT_BOUNDARIES."""

    def test_unclassified_vltk_assembly_rejected(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.Whatever", ["VLTK.Model"])
            lines = _lines(p.findings())
            self.assertTrue(
                any("[unclassified]" in l and "VLTK.Whatever" in l for l in lines),
                msg=str(lines),
            )
        finally:
            p.close()

    def test_exempt_and_ranked_are_not_flagged(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model")
            p.add_asmdef("VLTK.Core", ["VLTK.Model"])  # exempt
            self.assertEqual(_lines(p.findings()), [])
        finally:
            p.close()


class SelfCycleTests(unittest.TestCase):
    """P2: an assembly referencing itself is a cycle."""

    def test_named_self_reference_is_cycle(self):
        p = FakeProject()
        try:
            p.add_asmdef("VLTK.Model", ["VLTK.Model"])
            lines = _lines(p.findings())
            self.assertTrue(
                any(l.startswith("[cycle]") and "VLTK.Model -> VLTK.Model" in l
                    for l in lines),
                msg=str(lines),
            )
        finally:
            p.close()

    def test_guid_self_reference_is_cycle(self):
        p = FakeProject()
        try:
            g = "dddd4444dddd4444dddd4444dddd4444"
            p.add_asmdef("VLTK.Model", [f"GUID:{g}"], asmdef_guid=g)
            lines = _lines(p.findings())
            self.assertTrue(
                any(l.startswith("[cycle]") and "VLTK.Model -> VLTK.Model" in l
                    for l in lines),
                msg=str(lines),
            )
        finally:
            p.close()


class AsmdefSchemaTests(unittest.TestCase):
    """P2: malformed asmdef JSON -> deterministic finding, exit 1, no crash."""

    def test_non_object_json_root(self):
        p = FakeProject()
        try:
            p.add_raw_asmdef("Bad/X.asmdef", "[]\n")
            lines = _lines(p.findings())
            self.assertTrue(any("[asmdef]" in l and "Bad/X.asmdef" in l
                                for l in lines), msg=str(lines))
            self.assertEqual(guard.main(["--root", str(p.root)]), 1)
        finally:
            p.close()

    def test_invalid_references_type(self):
        p = FakeProject()
        try:
            import json
            p.add_raw_asmdef(
                "Bad/Y.asmdef",
                json.dumps({"name": "VLTK.Model", "references": "oops"}),
            )
            lines = _lines(p.findings())
            self.assertTrue(any("[asmdef]" in l and "references" in l
                                for l in lines), msg=str(lines))
            self.assertEqual(guard.main(["--root", str(p.root)]), 1)
        finally:
            p.close()

    def test_references_non_string_entry(self):
        p = FakeProject()
        try:
            import json
            p.add_raw_asmdef(
                "Bad/Z.asmdef",
                json.dumps({"name": "VLTK.Model", "references": ["ok", 5]}),
            )
            lines = _lines(p.findings())
            self.assertTrue(any("[asmdef]" in l and "non-string" in l
                                for l in lines), msg=str(lines))
        finally:
            p.close()


class RepoRootInvocationTests(unittest.TestCase):
    """P3: the documented unittest command works from the repo root."""

    def test_documented_unittest_command_from_repo_root(self):
        repo_root = Path(__file__).resolve().parents[1]
        proc = subprocess.run(
            [sys.executable, "-m", "unittest", "-q",
             "scripts.test_check_unity_assembly_boundaries.ForbiddenReasonTests"],
            cwd=repo_root,
            capture_output=True,
            text=True,
            timeout=60,
        )
        self.assertEqual(
            proc.returncode, 0,
            msg=proc.stdout + proc.stderr,
        )


if __name__ == "__main__":
    unittest.main()
