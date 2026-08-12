from __future__ import annotations

import json
import tempfile
from pathlib import Path

from scripts.map_runtime import builder
from scripts.map_runtime.verify import VerifyError, verify_dir


def write_runtime(tmp: Path) -> dict[str, str]:
    return builder.write_all(tmp)


def load(tmp: Path, name: str) -> dict:
    return json.loads((tmp / name).read_text(encoding="utf-8"))


def dump(tmp: Path, name: str, payload: dict) -> None:
    (tmp / name).write_bytes(builder.canonical_bytes(payload))


def assert_rejected(tmp: Path, needle: str) -> None:
    try:
        verify_dir(tmp)
    except VerifyError as ex:
        assert needle in str(ex)
    else:
        raise AssertionError("verification unexpectedly passed")


def test_generate_twice_is_byte_equal_and_digests_verify():
    with tempfile.TemporaryDirectory() as a, tempfile.TemporaryDirectory() as b:
        ha = write_runtime(Path(a))
        hb = write_runtime(Path(b))
        assert ha == hb
        for name in ha:
            assert (Path(a) / name).read_bytes() == (Path(b) / name).read_bytes()
        result = verify_dir(Path(a))
        assert result["artifactSha256"] == ha["map-runtime.v1.json"]
        assert result["productionSignatureVerified"] is False


def test_schema_invariants_and_spawn_bounds_walkable():
    with tempfile.TemporaryDirectory() as d:
        tmp = Path(d)
        write_runtime(tmp)
        artifact = load(tmp, "map-runtime.v1.json")
        assert artifact["schema"] == "map-runtime.v1"
        assert artifact["mapId"] == 53
        assert artifact["canonicalIdentity"]["nameVi"] == "Ba Lăng huyện"
        assert artifact["canonicalIdentity"]["geometryKey"] == "g_1bbe240c72569d69"
        assert artifact["movement"]["rules"]["allowMapIds"] == [53]
        assert artifact["movement"]["rules"]["rejectMapIds"] == [79]
        b = artifact["bounds"]["world"]
        s = artifact["spawn"]["world"]
        assert b["x"] <= s["x"] <= b["x"] + b["width"]
        assert b["y"] <= s["y"] <= b["y"] + b["height"]
        assert artifact["spawn"]["regionCell"] in artifact["walkability"]["walkableRegionCells"]


def test_forbidden_fallback_policy_rejected():
    for key in ["filesystemFallbackAllowed", "testDataAllowed", "loosePcFolderFallbackAllowed", "aliasRemapAllowed", "absoluteRuntimePathsAllowed"]:
        with tempfile.TemporaryDirectory() as d:
            tmp = Path(d)
            write_runtime(tmp)
            artifact = load(tmp, "map-runtime.v1.json")
            artifact["movement"]["rules"][key] = True
            dump(tmp, "map-runtime.v1.json", artifact)
            assert_rejected(tmp, "must be false")


def test_map79_alias_testdata_absolute_and_loose_paths_rejected():
    cases = [
        ("allowMapIds", [53, 79], "map 79"),
        ("artifactToken", "TestData", "forbidden runtime fallback token"),
        ("artifactToken", "/var/www/jx-pc", "forbidden runtime fallback token"),
    ]
    for field, value, message in cases:
        with tempfile.TemporaryDirectory() as d:
            tmp = Path(d)
            write_runtime(tmp)
            artifact = load(tmp, "map-runtime.v1.json")
            if field == "allowMapIds":
                artifact["movement"]["rules"][field] = value
            else:
                artifact["movement"]["forbiddenProbe"] = value
            dump(tmp, "map-runtime.v1.json", artifact)
            assert_rejected(tmp, message)


def test_tampering_and_missing_provenance_rejected():
    with tempfile.TemporaryDirectory() as d:
        tmp = Path(d)
        write_runtime(tmp)
        artifact = load(tmp, "map-runtime.v1.json")
        artifact["bounds"]["world"]["width"] += 1
        dump(tmp, "map-runtime.v1.json", artifact)
        assert_rejected(tmp, "signature artifact digest mismatch")

    with tempfile.TemporaryDirectory() as d:
        tmp = Path(d)
        write_runtime(tmp)
        (tmp / "map-runtime.v1.provenance.json").unlink()
        assert_rejected(tmp, "missing map-runtime files")


def test_signature_fails_closed_and_require_prod_rejects():
    with tempfile.TemporaryDirectory() as d:
        tmp = Path(d)
        write_runtime(tmp)
        assert_rejected_prod = False
        try:
            verify_dir(tmp, require_production_signature=True)
        except VerifyError as ex:
            assert "production map-runtime signature unavailable" in str(ex)
            assert_rejected_prod = True
        assert assert_rejected_prod

        sig = load(tmp, "map-runtime.v1.signature.json")
        sig["verification"]["productionSignatureVerified"] = True
        dump(tmp, "map-runtime.v1.signature.json", sig)
        catalog = load(tmp, "map-runtime.catalog.v1.json")
        for item in catalog["artifacts"]:
            if item["logicalPath"] == "map-runtime.v1.signature.json":
                item["sha256"] = builder.sha256((tmp / "map-runtime.v1.signature.json").read_bytes())
        dump(tmp, "map-runtime.catalog.v1.json", catalog)
        assert_rejected(tmp, "signature status")
