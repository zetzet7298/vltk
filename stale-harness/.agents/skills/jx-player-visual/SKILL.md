---
name: jx-player-visual
description: >-
  Build, fix, or verify player avatars and player-like NPC visuals in the
  VLTK-mobile Unity client using PC layered SPR parts, actions, draw order,
  equipment, and mounts. Use for male/female visuals, MA_/WO_ assets, animation,
  layering, invisibility, or player spawn presentation.
---

# JX Player Visual

Apply `jx-pc-port-rule` first. Use `jx-pc-resource-resolver` for every PC
definition, draw-order table, and SPR candidate, and use
`unity-mcp-orchestrator` for resource-first Editor and runtime validation.

## PC Evidence And Model

1. Read the selected player definition and draw-order data under
   `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/Settings/NpcRes/`.
   Resolve the corresponding `spr\npcres\man` or `spr\npcres\woman` parts from
   `/var/www/jx-source/pak_unpacked/`.
2. A character is layered SPR parts, not one sprite. Each action chooses the
   appropriate frame per direction; per-frame offsets share a reference pixel,
   and the PC draw-order table controls layer order.
3. Derive direction and frames-per-direction from decoded SPR headers. Do not
   assume a fixed frame count, action set, part list, or mount variant.
4. Keep root position independent from child frame offsets. Reset static decoded
   clip caches across Play Mode reloads, and keep player layers above the map
   according to the current renderer's documented range.

## Port Workflow

1. Resolve and stage only the selected exact SPR bytes to
   `Assets/StreamingAssets/Sprites/{runtime-uid}.spr`; preserve the runtime
   catalog path spelling and manifest provenance.
2. Extend the existing male/female catalog and visual implementation rather than
   cloning it. Reuse the PC draw-order mapping and add tests for each supported
   action, direction, and equipment/mount set.
3. For mounts, verify all required front/middle/rear parts and rider action
   assets before enabling the variant. Missing parts are a blocked asset set,
   not permission to substitute art.
4. Use the repository-root utilities when applicable:
   `/var/www/vltk-mobile/scripts/extract_horse_019.py`,
   `/var/www/vltk-mobile/scripts/stage_all_sandbox_sprites.py`, and
   `/var/www/vltk-mobile/scripts/verify_runner.py`.

## Proof

- Compile and run targeted EditMode coverage for catalog lookup, direction,
  actions, and layer ordering.
- In Play Mode, use `scripts/verify_player.cs` through the bare live
  `execute_code` tool after the player and map are loaded. It checks loaded
  parts, map ordering, visible pixels, and 8-way movement.
- Inspect console and a Play Mode screenshot through the resource-first MCP
  workflow. Report selected PC definitions, draw-order evidence, and resolved
  asset provenance.
