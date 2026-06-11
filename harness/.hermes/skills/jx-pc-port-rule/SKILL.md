---
name: jx-pc-port-rule
description: Mandatory source-of-truth rule for porting any JX Online 1 / Võ Lâm Truyền Kỳ PC game feature into the VLTK-mobile Unity client. Use this skill before any porting work from PC to Unity, including skills, combat, NPCs, maps, UI, HUD, player visuals, items, effects, sprites, configs, Lua/C++ behavior, PAK/SPR assets, or parity fixes. It forces the agent to inspect the scoped PC source first and port logic/behavior/visuals 100% from PC instead of guessing or using out-of-scope PC sources.
---

# JX PC Port Rule

Use before starting any PC-to-Unity porting task in this repo. This is a short guardrail skill; after applying it, also load the more specific skill if the task matches one, such as `jx-map-port`, `jx-hud-port`, `jx-enemy-port`, `jx-player-visual`, or `jx-skill-ui-port`.

## Source Of Truth




## When to escalate to `reverse-engineering`

Load/use the `reverse-engineering` skill when the port is blocked by **engine/format/binary behavior**, not ordinary TXT/INI/Lua data:

- A PC table references a path, but hash lookup and `pak_unpacked` lookup disagree.
- The asset exists only as `unknown/<uid>.spr` and path resolution is the blocker.
- UTF-8/Vietnamese-friendly parsing produces mojibake for GB2312/GBK paths.
- Need to understand or prove PAK/SPR/WOR/Region binary layout, UID hashing, compression flags, frame offsets, palette/frame order, direction buckets, or engine draw semantics.
- Need to inspect client/engine binaries (`engine.dll`, client exe, native DLL/SO) to recover an algorithm such as `g_FileName2Id`.
- PC runtime visibly does something but source TXT/Lua/C# does not explain it.

Do **not** escalate for routine data ports where the source table and assets already resolve; use the domain skill directly.

### Reverse-engineering workflow for VLTK resource blockers

1. Pick a small evidence pair: known PC path/loose file/screenshot ↔ candidate `unknown/<uid>.spr` or runtime output.
2. Prove binary/content equivalence when possible (`cmp`, size/header/frame count, decoded preview), not just similar names.
3. Test encoding/path/hash variants with a script and record the expected UID/file path.
4. Validate the hypothesis against at least 2–3 different paths from different folders/PAKs.
5. Patch the Unity/tool resolver only after the verifier passes, then copy exact assets into `Assets/StreamingAssets/...`.
6. Update the relevant port skill/status with the new invariant so the mistake is not repeated.

## PC resource resolution doctrine (PAK/SPR/TXT) — mandatory

Apply this before any asset/resource conclusion (map art, NPC/player SPR, HUD, skill icon, missile/effect SPR):

1. **Use canonical unpack first.** Search `/var/www/vltksource_new/vl_update_27/pak_unpacked` and cite the real file/manifest. Do not re-unpack a full PAK unless repairing a proven gap.
2. **`unknown/<uid>.spr` is valid PC source.** It means the PAK entry was extracted by UID but the human path was not resolved. Do not conclude “missing” just because a named path was not resolved.
3. **Decode PC tables with the encoding required by the table.** Missile/skill/resource paths containing Chinese must be read as GB2312/GBK. A Vietnamese-friendly or UTF-8 fallback can mojibake paths such as `\spr\skill\丐帮\...`, producing wrong hashes and fake missing assets.
4. **Use the PC signed-byte FileNameHash for PAK UID lookup.** Normalize slashes to `\`, encode path as GB2312/GBK, lowercase ASCII A-Z only, then treat bytes >=128 as signed (`b-256`) before the `0x8000000B / 0xFFFFFFEF / xor 0x12345678` hash. Evidence: `\spr\Ui\技能图标\icon_sk_ty_at.spr` hashes to `c4454165` (PC) while the old unsigned hash `bedc5b69` misses.
5. **Runtime must not point outside `vltk-mobile`.** If a PC asset is needed, copy the exact SPR/PNG/data file into `Assets/StreamingAssets/...` or another project asset folder, then load that project-local copy.
6. **If signed hash still misses, verify all likely PAK roots before declaring missing.** For combat effects, inspect `data/skills/unknown`; for HUD, inspect `data/1024`, `data/800`, `data/updatejx*`; for generic SPR, inspect `data/spr/unknown` plus loose `Client 6.0/spr`.
7. **Never assign purpose to unidentified hash-only SPR by guess.** Use path evidence, table reference, binary/content match, or visual preview evidence.

- Treat `/var/www/vltksource_new/vl_update_27` as the only PC game source of truth.
- For **asset SPR** (sprite, effect, icon, NPC visual, HUD, item art), the authoritative index is:
  - `/var/www/vltksource_new/docs/port_docs/18_spr_asset_index.md` (source of truth + tool tra cứu)
  - `/var/www/vltksource_new/docs/port_docs/19_pak_spr_taxonomy.md` (phân loại pak/SPR chi tiết)
  - Canonical unpack root: `/var/www/vltksource_new/vl_update_27/pak_unpacked` (75,928 `.spr` files from all source PAKs)
  - Manifest: `/var/www/vltksource_new/vl_update_27/pak_unpacked/_unpack_summary.json`
  - Optional label map: `/var/www/vltksource_new/vl_update_27/pak_unpacked/_labels.json` only if rebuilt for the canonical root; otherwise use the tree + vltktool API/rebuild index.
  - Tra cứu tool/API: `http://localhost:8081/` (`/api/spr`, `/api/categories`)
- Use the API/label map only if it has been rebuilt for the canonical root; otherwise inspect the canonical unpack tree and cite exact paths. Never guess sprite filenames.
- **Trust protocol:** exact file paths in `pak_unpacked` + manifest evidence are primary. If a rebuilt label map exists, its `confidence`/`pak_origin` fields are secondary provenance; do not fabricate names or purposes for unresolved `unknown/<hash>.spr` entries.
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

1. Locate the relevant file(s) under both loose PC source and `/var/www/vltksource_new/vl_update_27/pak_unpacked` before editing Unity code.
   - For SPR assets: inspect the canonical unpacked tree first; query the SPR tool/API or rebuilt `_labels.json` (see 18_spr_asset_index.md) only as helper evidence — do not guess sprite filenames.
2. Compare the PC source/data/asset against the current Unity implementation.
3. Implement the Unity port using PC values and assets directly where possible.
4. Verify with Unity compile/tests or runtime checks appropriate to the task.
5. Report which PC source files/assets were used.

## If Source Is Missing

- Say exactly what was searched under `/var/www/vltksource_new/vl_update_27`.
- Do not silently substitute another PC source.
- Make only clearly marked provisional changes if the user explicitly allows it.
