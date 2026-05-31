# 0007 — Obstacle Pack File + Test Assembly Definitions

**Status**: accepted
**Date**: 2026-05-31

## Context

The first attempt to actually compile and run the EditMode test suite exposed
three blocking problems that had previously hidden the fact that the M0/M1
tests had never run:

1. **57,808 loose JSON files** under `Assets/StreamingAssets/Obstacles/`
   (~235 MB on disk). Unity's AssetDatabase enumerates, hashes, and tracks a
   `.meta` for every file under `Assets/` (StreamingAssets is not exempt at
   import time). With tens of thousands of tiny files the editor stalls in
   "Initial Refresh" — observed CPU pinned high while artifact/meta counts
   stayed flat for ~30 minutes. A known Unity anti-pattern.
2. **No assembly definitions** in `Assets/Scripts/`. The test asmdef referenced
   named assemblies `VLTK.Core/Model/Sandbox/Resources` that did not exist, so
   scripts compiled into the default `Assembly-CSharp` and tests could never
   resolve them.
3. **`com.unity.modules.scenemanagement@1.0.0`** in `Packages/manifest.json`
   does not exist in Unity 6000.4 (folded into the core engine). Batch mode
   treats unresolved package dependencies as fatal.

## Decision

1. **Pack obstacle data into one binary file** `StreamingAssets/Obstacles.bin`
   (magic `VOBP`, version, count, index of fixed 24-byte records, then raw
   cell bytes). Packer: `vltktool/obstacle_pack.py`. The loose JSON tree is
   moved out of `Assets/` (regenerable from `vltktool/unpacked/maps_pak` via
   `obstacle_to_unity.py`; backup at `/var/www/vltk-obstacle-json-backup`).
   `ObstacleGridLoader` now memory-maps the pack once, caches the index, and
   slices per-region cells. Public API (`LoadFromStreamingAssets`,
   `LoadDefault`) is unchanged; added `SetPackPathForTesting`/`ResetCache`.
2. **Add five asmdefs** (`VLTK.Model`, `VLTK.Core`, `VLTK.Resources`,
   `VLTK.Sandbox`, `VLTK.Sprites`) forming a clean dependency DAG, and fix the
   test asmdef (drop duplicate `UnityEngine.TestRunner` reference, add
   `VLTK.Sprites`).
3. **Remove the invalid scenemanagement dependency** from the manifest to match
   `packages-lock.json`.

## Consequences

- Editor import is fast again; one pack file instead of 57,808.
- One file handle at runtime is also friendlier for Android/iOS.
- EditMode suite compiles and passes 167/167 (was: never compiled).
- Source `.dat` remains the source of truth; the pack is a build artifact and
  can be regenerated deterministically.

## Verification

`Unity -batchmode -runTests -testPlatform EditMode` → total=167 passed=167
failed=0 (results: /tmp/vltk-editmode-results.xml).
