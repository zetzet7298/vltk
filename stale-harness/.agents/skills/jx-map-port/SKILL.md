---
name: jx-map-port
description: >-
  Port or verify JX Online 1 / Vo Lam Truyen Ky PC maps in the VLTK-mobile Unity
  client using PC Region_C geometry, SPR art, projection, sorting, minimap, and
  preview evidence. Use for mapId, .wor, Region_C.dat, terrain, builtins,
  projection/layering defects, or map HUD parity.
---

# JX Map Port

Apply `jx-pc-port-rule` first. Use `jx-pc-resource-resolver` for logical
resource paths, encodings, UIDs, and package/load-order winners. Use
`unity-mcp-orchestrator` for resource-first Editor inspection, compilation,
tests, Play Mode, console, and screenshots.

## Canonical Inputs

- Source and loose map/config data:
  `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/`
- Runtime/extracted package data:
  `/var/www/jx-pc/pak_unpacked/`
- C++ format and renderer evidence:
  `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/`

Use the exact `MapList.ini` entry, `.wor` bounds, and `Region_C` cells selected
from those roots. Resolve every referenced SPR before staging it. Do not
reconstruct pack hashes or select duplicate assets in this skill; defer that to
the resolver workflow.

## Workflow

1. Identify the logical map name and bounds with
   `scripts/list_maps.py`, whose default source root is the canonical loose
   tree. MapList indices and Unity `Map_{id}_C` IDs can differ; verify the Unity
   catalog before extraction.
2. Use `scripts/jx_map_port.py` only with canonical inputs. It consumes original
   package files when available; otherwise resolve already-unpacked selected
   bytes through the resource workflow before staging. It writes only the
   selected Region_C cells, manifest/image list, and runtime-named SPRs.
3. Preserve PC region geometry and projection. Verify the current C++ transform
   before implementation; the audited corpus currently uses the builtin
   Z-screen term `sceneY / 2 - sceneZ * (887 / 1024)`.
4. Keep ground, cover, builtins, and player in distinct sort layers. Do not
   replace the authored builtin order with simple Y sorting.
5. When the request includes map HUD, validate full active bounds, minimap
   conversions, preview aspect, click-to-move, and PC-derived labels.

## Outputs And Proof

Expected Unity outputs are:

- `Assets/StreamingAssets/TestData/Regions/Map_{id}_C/`
- `Assets/StreamingAssets/Sprites/{runtime-uid}.spr`

Verify parser/renderer tests, zero unexpected missing sprites, correct tall
structure projection, cover below builtins, authored multipart ordering, and
non-duplicated player rendering. For minimap work, prove coordinate inverse
mapping and Play Mode interaction. Record the selected PC map/config/assets and
their resolver provenance; fail closed on unresolved geometry or art.
