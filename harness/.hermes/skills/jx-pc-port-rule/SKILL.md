---
name: jx-pc-port-rule
description: Mandatory source-of-truth rule for porting any JX Online 1 / Võ Lâm Truyền Kỳ PC game feature into the VLTK-mobile Unity client. Use this skill before any porting work from PC to Unity, including skills, combat, NPCs, maps, UI, HUD, player visuals, items, effects, sprites, configs, Lua/C++ behavior, PAK/SPR assets, or parity fixes. It forces the agent to inspect the scoped PC source first and port logic/behavior/visuals 100% from PC instead of guessing or using out-of-scope PC sources.
---

# JX PC Port Rule

Use before starting any PC-to-Unity porting task in this repo. This is a short guardrail skill; after applying it, also load the more specific skill if the task matches one, such as `jx-map-port`, `jx-hud-port`, `jx-enemy-port`, `jx-player-visual`, or `jx-skill-ui-port`.

## Source Of Truth

- Treat `/var/www/vltksource_new/vl_update_27` as the only PC game source of truth.
- For **asset SPR** (sprite, effect, icon, NPC visual, HUD, item art), the authoritative index is:
  - `/var/www/vltksource_new/docs/port_docs/18_spr_asset_index.md` (source of truth + tool tra cứu)
  - `/var/www/vltksource_new/docs/port_docs/19_pak_spr_taxonomy.md` (phân loại pak/SPR chi tiết)
  - Label map: `/var/www/vltktool/vltksource_new_unpaked/_labels.json` (path → tên Việt + nhóm, 62,953 SPR)
  - Tra cứu tool/API: `http://localhost:8081/` (`/api/spr`, `/api/categories`)
- All 62,953 unpacked SPR are Vietnamized + classified. Use the API/label map to find the exact `path`, never guess sprite filenames.
- **Trust protocol (per label `confidence` field) — this guarantees ≥99% reliability:**
  - `confidence: high` (39,500 file ≈ 62.7%): real engine path PROVEN. `resolve_method`: `npcres-table` (26,504 — hash-matched from `settings/npcres/*.txt` NPC animation tables: Stand/Walk/Run/Die/Attack/Magic/Ride columns), `part-enum` (8,542), `named` (2,561), `code-ref` (746), `hash-reverse`/`hash-xprod` (1,140). The `path`/`name_vi` is the actual asset — safe to port directly.
  - `confidence: unidentified` (23,449 file): hash-only `unknown/<hash>.spr` with NO resolved path. Purpose is UNKNOWN — category is `Chưa định danh`, with only measurable metadata (pak origin, WxH, frame count). NEVER assign a purpose (Icon/Visual/Object...) to these — doing so is fabrication (e.g. a vase/table/chair would be mislabeled "button"). To know what one is, open it in the preview tool. Never invent a filename or purpose.
  - `pak_origin` (ALWAYS known, hard fact from the pak index): which pak the engine loaded the file from. Cite this for traceability.
- Player/NPC visuals are split SPR parts (body/head/limbs/weapon, multi-frame, 8-direction). To build a complete character you must combine the part SPR in the same `spr/npcres/<family>/` group — the "Visual nhân vật/NPC" category collects these.
- Equipment exists in two forms: an **icon** SPR (inventory/UI, small, few frames) and a **runtime visual** (worn on the body, part of the player visual set). Match the form the task needs.
- Do not read or trust other PC source trees for port decisions unless the user explicitly expands scope.
- Unity code, generated assets, old extracted files, screenshots, and previous guesses are implementation clues only; they are not proof.
- For every behavior, visual, coordinate, timing, skill formula, asset path, NPC/object definition, or UI layout, find the matching PC source/data/asset first.

## Porting Rule

- Port PC logic, behavior, timing, data, and visuals as faithfully as possible into Unity mobile.
- Preserve PC IDs, file paths, and source references in concise comments or docs when useful for traceability.
- Do not invent fallback behavior, sprites, names, formulas, animation frames, or coordinates when PC source can answer it.
- If PC source uses Chinese text, localize user-facing Unity text to Vietnamese while preserving the original source mapping.
- Prefer the smallest Unity change that makes the feature match PC behavior.

## Required Workflow

1. Locate the relevant file(s) under `/var/www/vltksource_new/vl_update_27` before editing Unity code.
   - For SPR assets: query the SPR tool/API or `_labels.json` (see 18_spr_asset_index.md) to get the exact `path` — do not guess sprite filenames.
2. Compare the PC source/data/asset against the current Unity implementation.
3. Implement the Unity port using PC values and assets directly where possible.
4. Verify with Unity compile/tests or runtime checks appropriate to the task.
5. Report which PC source files/assets were used.

## If Source Is Missing

- Say exactly what was searched under `/var/www/vltksource_new/vl_update_27`.
- Do not silently substitute another PC source.
- Make only clearly marked provisional changes if the user explicitly allows it.
