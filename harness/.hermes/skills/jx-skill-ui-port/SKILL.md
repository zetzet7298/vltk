---
name: jx-skill-ui-port
description: >-
  Port, fix, or verify JX Online 1 / Võ Lâm Truyền Kỳ skill/võ công UI and
  skill-management logic in VLTK-mobile from PC jxwin source/assets. Use for
  bảng kỹ năng, võ công, skill grid/list, detail popup/tooltip, icons/names/levels,
  tăng điểm kỹ năng, conditions, remaining points, faction skill mapping,
  Cái Bang/Thiếu Lâm panels, Skills.txt, UiSkills*.ini, skill SPR icons,
  icon-name mismatch, missing/wrong icons, stale level text, click icon vs plus
  behavior, or any skill-management visual/logic bug. Enforce strict PC
  provenance: never invent UI, sprites, logic, descriptions, or skill lists; prove
  icon-name-level mapping before editing.
---

# JX Skill UI / Võ Công Panel Port

Use for Unity work involving skill/võ công UI, skill data mapping, upgrade logic, skill detail popups, or visual bugs in skill management. Goal: PC-parity, source-backed, no guessed skill UI.

## Hard rules

Strict PC provenance: never invent skill list, order, Vietnamese names, icons, sprites,
descriptions, upgrade gates, or UI layout. Prove the icon ↔ name ↔ level ↔ detail mapping by
`skillId` (never by grid row/index) before editing. PC `jxwin` `Skills.txt` + `UiSkills*.ini`
+ real SPR are the source of truth; current Unity code is implementation, not proof. The full
rule list:

## Resource/hash guard learned from combat visual port

Before concluding that any PC SPR/icon/effect/NPC/HUD asset is missing, apply `jx-pc-port-rule` → **PC resource resolution doctrine**:

- Read PC TXT/INI tables with the correct encoding. Paths with Chinese resource folders are usually GB2312/GBK; mojibake paths hash to fake UIDs.
- PAK entries named `unknown/<uid>.spr` are valid extracted PC assets, not garbage.
- For PAK lookup use PC signed-byte FileNameHash, not an unsigned-byte/private runtime hash.
- Copy exact PC assets into `Assets/StreamingAssets/...`; never load directly from `/var/www/jx-source` at runtime.
- Verify with real file existence/decode/render evidence before claiming parity or missing source.

- Do not invent skill list, order, Vietnamese names, icons, sprites, descriptions, upgrade gates, or UI layout.
- Treat PC `jxwin` data/assets as source of truth. Current Unity code is implementation, not proof.
- Map by `skillId`, never by grid row/index alone. Grid index can place slot; `skillId` must drive icon/name/level/detail/add-point behavior.
- If user names a Vietnamese skill, find PC Chinese/source equivalent before changing code. If not found, say so and keep source-backed mapping.
- Use real PC SPR/PAK assets. Generated/generic/recolored/screenshot-baked icons are not acceptable final art.
- Localize Chinese UI to Vietnamese in Unity, but preserve PC IDs/paths in code comments/tests/docs for traceability.
- Preserve existing player visual/HUD/map behavior unless task explicitly includes it.

## Source map

Start here for current project:

- Unity runtime/data:
  - `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs`
  - `Assets/Scripts/UI/CaiBangSkillPanelService.cs`
  - `Assets/Scripts/UI/GameHudController.cs`
  - `Assets/Scripts/UI/PcHudVietnameseTextOverlay.cs`
  - `Assets/Scripts/Sandbox/PlayerProgressionService.cs`
  - `Assets/Scripts/Model/SkillDefinition.cs`
- Unity tests:
  - `Assets/Tests/EditMode/Sandbox/CaiBangSkillPanelTests.cs`
  - `Assets/Tests/EditMode/Sandbox/CaiBangCombatParityTests.cs`
- Unity art/provenance:
  - `Assets/UI/HUD/Art/Generated/PC_SOURCE.txt`
  - `Assets/UI/HUD/Art/Generated/cai_bang_skill_*.png`
  - `Assets/UI/HUD/Art/技能*.png`
- PC skill data:
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Client/Settings/Skills.txt`
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Settings/Skills.txt`
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/Script/skill/**`
- PC skill UI:
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Client/Ui/Ui3/UiSkillsSheet.ini`
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Client/Ui/Ui3/UiSkillsFightSub.ini`
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Client/Ui/Ui3/UiSkillsLive.ini`
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/SwordOnline/Sources/S3Client/Ui/UiCase/UiSkills.cpp`
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/SwordOnline/Sources/S3Client/Ui/UiCase/UiSkillTree.cpp`
- PC skill logic:
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/SwordOnline/Sources/Core/Src/CoreShell.cpp`
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/SwordOnline/Sources/Core/Src/KSkillList.cpp`
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/SwordOnline/Sources/Core/Src/KPlayer.cpp`
  - `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/SwordOnline/Sources/Core/Src/KSkills.cpp`
- PC asset source:
  - Canonical unpacked PAK tree: `/var/www/jx-source/pak_unpacked` (skill SPR icons live under the SPR trees here; `\spr\Ui\技能图标\...` group). Manifest `_unpack_summary.json`.
  - `/var/www/vltktool/` helpers for PAK/SPR lookup/extraction (read its README first; do not write ad-hoc scanners).

Important PC asset caveat: Chinese PAK paths may require signed-byte hashing. Unsigned-byte lookup can fail even for correct paths.

## Workflow

### 1) Confirm scope + evidence

- Restate target faction/panel/skill IDs and bug symptoms.
- Use `git status`, `git diff`, and recent commits to understand pending work before editing.
- Use `srcwalk` (the repo's required code-navigation tool) to find Unity symbols/files/flows; use `rg` for exact PC names, IDs, paths. Prefer `srcwalk`/`rg`/`fd` over raw grep/find per AGENTS.md.
- For named skills, build a mini evidence table:

| Vietnamese | PC Chinese | SkillId | PC icon path | Unity PNG/path | Notes |
|---|---:|---:|---|---|---|

If any cell lacks proof, inspect PC data before coding.

### 2) Scan PC source/assets

- Read `Skills.txt` row(s): `SkillName`, `SkillId`, `SkillStyle`, `SkillIcon`, `ReqLevel`, `MaxLevel`, `LvlSetScript`, `LvlSetting*`, `LvlData*`, `SkillDesc`.
- Read relevant Lua level scripts for formulas/descriptions where needed.
- Read PC UI INI for dimensions/slot positions/text offsets. For fight skills, preserve grid structure unless PC source proves another layout.
- Read PC UI/C++ behavior for interactions:
  - opening/closing panel;
  - icon click/hover/detail behavior;
  - add-point button behavior;
  - remaining point display;
  - sort/list generation.
- Locate exact SPR icon path from `Skills.txt`; extract/decode from PAK if missing in Unity.

### 3) Cross-check Unity mapping

- Verify every visible slot uses `skillId` for:
  - displayed Vietnamese name;
  - PC Chinese raw name;
  - icon texture path/PNG;
  - learned level;
  - selected detail popup;
  - add-point target.
- Look for row-index drift: arrays/dictionaries keyed by list index, parallel icon arrays, or label/icon loaded before reorder.
- Check visual overlap bugs: stale screenshot backgrounds, slot not cleared before redraw, level text drawn twice, selected detail stale after upgrade.
- Check upgrade logic against PC gates: remaining points, max level, required player level, known skill/faction, and any PC-specific constraints.

### 4) Edit minimally

- Keep UI grid PC-like: slot placement can use row-major index, but all data must come from `skillId`.
- Add or update provenance docs when adding assets (`PC_SOURCE.txt` or story docs). Include exact PC path, PAK source, hash/orientation notes if relevant.
- Prefer small service-level fixes over scattering logic in UI rendering.
- Add tests before/with fixes for any mapping bug the user reported, especially sensitive skills and icon-name mismatches.

### 5) Verify

Run relevant checks:

- `git diff --check`
- Unity compile/console error check via Unity MCP if available.
- Targeted EditMode tests:
  - `CaiBangSkillPanelTests`
  - `CaiBangCombatParityTests`
  - player visual tests if HUD overlay/player visibility may be affected.
- Full EditMode when skill UI/data code changed broadly.
- Runtime probe: open skill panel, select affected skill, upgrade with plus, confirm points/level/selected detail update.
- Screenshot proof for visual tasks. Compare against PC target/source screenshot if user supplied one.
- Harness trace/story update when task changes story behavior.

### 6) Final audit before response

Audit requirement-by-requirement:

- Correct PC skill IDs/list/order?
- Icon ↔ name ↔ level/detail all keyed by `skillId`?
- Exact PC SPR/PAK assets used, no fabricated art?
- Vietnamese UI text present, with PC source names retained in code/tests/provenance?
- Add-point logic matches PC gates?
- Visual grid still grid, no long-list regression?
- No stale/baked level text or overlapping redraw?
- Tests/build/screenshot evidence recorded?

Report concise Vietnamese summary: changed files, PC sources used, tests/job IDs, screenshot path, any source-backed caveat (for example: “skill name requested not found in PC `Skills.txt`”).

## Common pitfalls

- `Thiên Hạ Vô Cẩu` is PC `天下无狗`, skill `125`, icon `\spr\Ui\技能图标\icon_sk_gb_31.spr`. Do not let it inherit `Đả Cẩu Trận`/`打狗阵` icon `icon_sk_gb_23.spr`.
- `Đả Cẩu Trận` is PC `打狗阵`, skill `124`, icon `icon_sk_gb_23.spr`.
- `Kháng Long Hữu Hối` is PC `亢龙有悔`, skill `128`, icon `icon_sk_gb_41.spr` in current PC `Skills.txt`.
- Names like “Phi Long Tại Thiên” or “Bổng Đả Ác Cẩu” must be proven in current PC skill data before adding to skill panel. Do not import mobile/private-server naming unless user explicitly changes source of truth.
- PC detail popup is tied to skill object/id (`UOC_SKILL*` flow), not to row index.
- PC skill level text often draws only when level != 0; avoid baked screenshot levels.
