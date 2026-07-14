# Design: Cai Bang Skill PC Parity

## Overview
This change ports Cai Bang skills by making PC source data the authoritative input and treating mobile code as a deterministic Unity runtime implementation of those semantics. The design intentionally separates evidence extraction, gameplay logic, visual/SFX resource resolution, and verification so each slice remains reviewable.

## PC Source Mapping

### Data sources
- Skill base rows: `Assets/StreamingAssets/Reference/PcSkill/skills.txt`
- Missile rows: `Assets/StreamingAssets/Reference/PcAttrib/missles.txt`
- Lua level formulas: `Assets/StreamingAssets/Reference/gaibang.lua`
- PC canonical scripts:
  - `/var/www/jx-source/Client 6.0/file/skill/gaibang.lua`
  - `/var/www/jx-source/Client 6.0/file/skill/gaibang/gaibang-zhangfa.lua`
  - `/var/www/jx-source/Server 6.0/server/home_jxser/server1/script/skill/gaibang.lua`
  - `/var/www/jx-source/pak_unpacked/*/script/skill/gaibang.lua`

### Evidence matrix
Create a checked-in audit artifact under the change directory, for example `openspec/changes/port-caibang-skill-pc-parity/evidence/caibang-skill-matrix.md`, with one row per skill. Required columns:
- Skill id and Vietnamese name
- PC Lua symbol
- Current mobile catalog entry
- `skills.txt` evidence columns
- Missile ids and `missles.txt` evidence columns
- Visual resources: icon, pre-cast/cast, missile anims, impact anims
- SFX resources
- Current mobile parity status
- Tests covering the skill

## Mobile Architecture Mapping

### `PcCaiBangLuaLevelService`
Role: parse/evaluate `gaibang.lua` level data and expose typed accessors.

Needed work:
- Ensure all Cai Bang skill ids map to PC Lua symbols, including known missing mappings for `127 -> huabu_liushou` and `130 -> zuidie_kuangwu` if still absent.
- Expose typed accessors for missile count/form/params, skill cost, buff attributes, durations, and damage modifiers.
- Keep formula evaluation deterministic and unit-tested.

### `PcCombatCatalogFactory`
Role: build combat catalog entries from PC-derived data.

Needed work:
- Ensure Cai Bang catalog entries are generated from PC data rather than hand approximations.
- Keep manual overrides only when backed by explicit PC evidence.

### `CombatRuntimeService`
Role: apply skill casts, states, damage, and active effects.

Needed work:
- Apply buff state values/durations from `PcCaiBangLuaLevelService`.
- Tick and expire active states or integrate existing `BuffStateService` if compatible.
- Populate defender stats from active states before damage calculation.
- Preserve scope: avoid broad faction regressions by adding tests before shared changes.

### `SkillEffectVisualService`
Role: configure live skill visuals and missile trajectories.

Needed work:
- For Cai Bang multi-missile skills, read `skill_misslesform_v` and route to the correct trajectory setup.
- `Phi Long Tại Thiên`: level-derived count, live target tracking, stable parallel lane offsets.
- `Kháng Long Hữu Hối`: fan/radial spread for PC missile form `2`, using `skill_param1_v` as angle step.

### `SkillEffectRenderer` and `SkillEffectWorldOverlay`
Role: render missile frames/directions.

Needed work:
- Use an index-specific target resolver for each missile when selecting PC missile frames.
- Do not orient all dragon sprites toward a single global target center when the PC effect requires lane-specific targets.

### Visual/SFX resource pipeline
Role: resolve, decode/import, and use PC resources.

Process:
1. Read resource path from PC config/script.
2. Normalize with leading backslash and lowercase/backslashes.
3. Encode using GBK/CP1258/latin1/utf-8 as appropriate.
4. Compute JX Pack Hash UID exactly as documented in `jx-pc-resource-resolver`.
5. Search under `/var/www/jx-source/pak_unpacked/`.
6. Cross-check `_labels.json`, `label_map_raw.json`, or decoded SPR frames when resource language/visual ambiguity exists.
7. Import into mobile with deterministic dimensions/frame metadata.

## Homing Projectile Strategy
`Phi Long Tại Thiên` should not rely on Unity physics for core homing logic. Use deterministic vector updates driven by fixed delta in the combat/visual runtime:
- Store live target accessor (`getCurrentTargetPos`) when a target exists.
- For each missile, resolve target as `liveTarget + laneOffset`.
- Move missile toward that resolved target each update.
- Keep lane offsets stable so four dragons remain parallel rather than collapsing.
- Use tests to simulate target movement and assert missile target positions change after cast.

Unity physics queries may still be used for collision/overlap when needed, but deterministic tests should avoid relying on non-isolated physics scene timing.

## Test Strategy
- Every slice starts with a failing EditMode test in `Assets/Tests/EditMode/Sandbox/*CaiBang*.cs`.
- Add `[TestFixture, Category("CaiBang")]` for new test fixtures.
- Use category-filtered Unity MCP tests during development:
  - `unityMCP_run_tests(mode="EditMode", category_names=["CaiBang"])`
  - use `category_names=["!Slow"]` when visual decode is not under test.
- Add lower-level tests for formula accessors and runtime tests for homing/buff expiration.

## Implementation Slices
1. Evidence matrix and PC data audit.
2. Phi Long homing and four-dragon lane parity.
3. Kháng Long missile form/fan spread parity.
4. Buff skill mappings and runtime state application/expiration.
5. Defender stats and damage integration for active attacks.
6. Visual/SFX resource parity for affected skills.
7. Final review, screenshot/visual smoke, and filtered/full validation as appropriate.

## Risks and Mitigations
- **Shared runtime risk**: `CombatRuntimeService` changes may affect all factions. Mitigate with focused tests and small slices.
- **Resource ambiguity**: multiple PC assets may exist across packages. Mitigate with JX hash workflow and Vietnamese label/decoded frame checks.
- **Scope creep**: all Cai Bang skills is large. Mitigate by treating Phi Long as first acceptance slice and keeping other skills behind evidence matrix tasks.
