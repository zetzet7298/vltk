---
name: jx-enemy-port
description: >-
  Port, fix, or verify JX Online 1 / Vo Lam Truyen Ky map enemies, NPCs, and
  trainer/object spawns in the VLTK-mobile Unity client. Use for Region_S.dat,
  NpcS.txt, PC spawn coordinates, NPC SPRs, 8-way animation, shadows, HP bars,
  nameplates, or PC-parity enemy behavior.
---

# JX Enemy And Spawn Port

Apply `jx-pc-port-rule` first. Use `jx-pc-resource-resolver` for every PC
config or asset lookup and `unity-mcp-orchestrator` for Editor work. Those
skills own source authority, package winners, hashes, encodings, provenance,
and resource-first MCP checks.

## PC Evidence

1. Locate the selected map's server data under
   `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/maps/`
   and enumerate its `*_Region_S.dat` candidates. `Region_C` is client map
   decoration and is not evidence for server enemy spawns.
2. Parse the `Region_S` NPC section using the matching C++ definitions under
   `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/`. Preserve
   template ID, MPS X/Y, kind, level, facing, camp, series, raw GBK name, and
   script data.
3. Resolve each template against
   `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/Settings/NpcS.txt`.
   Verify names against the spawn record; use its stats, AI fields, and
   `NpcResType` only after the row is confirmed.
4. Resolve every selected SPR through `jx-pc-resource-resolver` in
   `/var/www/jx-pc/pak_unpacked/`. If no exact visual is available, retain
   the authoritative spawn as a non-rendered marker or report it; do not ship a
   placeholder.

## Unity Port

1. Keep the spawn root at the PC-derived world position. Confirm the map's MPS
   conversion against its renderer and a known coordinate before applying it.
2. Attach the visual, AI, health, and UI below that root. Use decoded PC SPR
   directions and frame counts; do not assume a fixed frame count or direction
   order.
3. Render the body with the existing PC NPC visual path. Reuse the player-style
   SPR shadow as a child layer below the body; frame offsets must not move the
   spawn root.
4. Anchor a compact PC-style nameplate and thin HP bar to actual sprite bounds.
   Preserve localized display mapping while retaining the PC identity in data.
5. Cover all accepted `Region_S` templates, including town NPCs, trainers, and
   training objects when the requested scope includes them.

## Proof

- Parser tests prove section bounds and decoded spawn/template fields.
- Data checks prove the selected `NpcS.txt` rows and resolved visual resources.
- Runtime checks prove exact spawn coordinates, visual count, 8-way animation,
  shadow below body, and nameplate/HP anchoring.
- In Play Mode, inspect the scene and console through the resource-first Unity
  MCP workflow. Capture a screenshot only after the map and sprites are loaded.

Report the selected `Region_S`, `NpcS.txt`, C++ format reference, and each
rendered asset provenance. Mark missing or ambiguous PC evidence blocked rather
than inventing behavior or visuals.
