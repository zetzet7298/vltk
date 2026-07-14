---
name: jx-pc-port-rule
description: Mandatory source-of-truth rule for porting any JX Online 1 / Võ Lâm Truyền Kỳ PC game feature into the VLTK-mobile Unity client. Use this skill before any porting work from PC to Unity, including skills, combat, NPCs, maps, UI, HUD, player visuals, items, effects, sprites, configs, Lua/C++ behavior, PAK/SPR assets, or parity fixes. It forces the agent to inspect the scoped PC source first and port logic/behavior/visuals from the current audited PC corpus instead of guessing or using stale PC source paths.
---

# JX PC Port Rule

Use before starting any PC-to-Unity porting task in this repo. This is a short guardrail skill; after applying it, also load the more specific skill if the task matches one, such as `jx-map-port`, `jx-hud-port`, `jx-enemy-port`, `jx-player-visual`, `jx-skill-ui-port`, `jx-skill-visual-port`, or the PC-side `jx-pc-resource-resolver` skill for asset/hash/PAK resolution.

## Source Of Truth

- Treat `/var/www/jx-source/pak_unpacked/` as the canonical newest-VNG PC data root.
- Inspect both loose PC trees and unpacked PAK runtime data before porting:
  - Canonical unpacked PAK tree: `/var/www/jx-source/pak_unpacked/`
- Current audited PC port-doc entry points are `/var/www/jx-source/docs/SOURCE_INDEX.md` and `/var/www/jx-source/docs/SCAN_REPORT_TINH_KIEM.md`; rich audited docs live under `docs/backend_port/` and `docs/client_port/`.
- Do not use stale source paths such as old extracted trees, generated Unity reference dumps, screenshots, or previous guesses as proof.
- Unity code, generated assets, old extracted files, screenshots, and previous guesses are implementation clues only; they are not proof.
- For every behavior, visual, coordinate, timing, skill formula, asset path, NPC/object definition, or UI layout, find the matching PC source/data/asset first.

## Current PAK / Asset Rule

- Current audit snapshot: all 46 real source `.pak` files are accounted for; `403,560 / 403,560` entries are exported and present on disk, `failed=0`, `100.00%` coverage.
- PAK files are not image-only bundles. They may contain SPR assets, Lua, TXT/INI config, maps, UI data, and other runtime data.
- For every PC asset path, Hash_UID, and PAK load-order decision, use the PC-side `jx-pc-resource-resolver` workflow against `/var/www/jx-source/pak_unpacked/`.
- PAK load-order winners must be determined from the active client package order, not guessed from whichever duplicate file is easiest to find.

## Porting Rule

- Port PC logic, behavior, timing, data, and visuals as faithfully as possible into Unity mobile.
- Preserve PC IDs, file paths, Hash_UIDs, PAK origins, and source references in concise comments or docs when useful for traceability.
- Do not invent fallback behavior, sprites, names, formulas, animation frames, or coordinates when PC source can answer it.
- If PC source uses Chinese or legacy encoded text, localize user-facing Unity text to Vietnamese while preserving the original source mapping.
- Preserve native PC file encodings when reading evidence; do not re-encode `.lua`, `.txt`, `.ini`, or `.cfg` sources.
- Prefer the smallest Unity change that makes the feature match PC behavior.

## Required Workflow

1. Locate the relevant PC file(s) under `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/` or `pak_unpacked/` before editing Unity code.
2. For assets, resolve the exact path/Hash_UID/PAK winner using `jx-pc-resource-resolver` and verify the file under `pak_unpacked/`.
3. Compare the PC source/data/asset against the current Unity implementation.
4. Implement the Unity port using PC values and assets directly where possible.
5. Verify with Unity compile/tests or runtime checks appropriate to the task.
6. Report which PC source files/assets, Hash_UIDs, PAK origins, and mechanism references were used.

## If Source Is Missing Or Ambiguous

- Do not silently substitute another PC source.
- Make only clearly marked provisional changes if the user explicitly allows it.
