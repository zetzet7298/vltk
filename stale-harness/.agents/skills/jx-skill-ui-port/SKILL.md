---
name: jx-skill-ui-port
description: Port, fix, or verify JX Online 1 / Vo Lam Truyen Ky skill UI and skill-management behavior in the VLTK-mobile Unity client. Use for skill panels, trees, icons, names, levels, descriptions, point spending, faction order, detail selection, and combat-slot integration. PC evidence is required; do not invent UI, assets, or rules.
---

# JX Skill UI Port

## Scope and rules

Use this skill for skill/võ công panels, skill trees, tooltips, icons, learned
levels, point spending, faction lists, and their combat-slot links.

- Apply `jx-pc-port-rule` before port work, `jx-pc-resource-resolver` for every
  PC asset or PAK lookup, and `unity-mcp-orchestrator` for Unity Editor work.
- Use `skillId` as the identity for name, icon, level, selected detail, upgrade,
  and combat-slot binding. A visual row index may position a slot only.
- Do not infer PC UI layout, faction order, descriptions, gates, names, or assets
  from the Unity client. Do not substitute generated, recolored, or screenshot art.
- If the PC source is absent or ambiguous, leave the behavior provisional and
  report the gap. Do not create a fallback rule.

## Evidence order

1. Locate the selected lowercase PC `Skills.txt` and, when the panel exposes
   combat behavior, `Missles.txt`. Establish package/version and the active
   load-order winner.
2. Read the relevant extracted C++ under
   `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/` for panel
   interaction, selection, ordering, and point-spend behavior.
3. Cross-check the selected runtime data and UI resources in
   `/var/www/jx-source/pak_unpacked/`. Resolve paths, encoding, UID, candidate
   winner, and decoded SPR through `jx-pc-resource-resolver`.
4. Record source path, package/version, SHA-256, row/line or function, and
   `skillId` before making an implementation claim.

`Skills.txt` is authoritative for skill identity and icon references; use
`Missles.txt` only for the combat-facing fields actually in scope.

## Unity source map

Start from the requested symptom, then use `srcwalk` to trace the current path:

- `Assets/Scripts/UI/PcSkillPanelService.cs`
- `Assets/Scripts/UI/SkillTreePanelService.cs`
- `Assets/Scripts/UI/PcSkillIconArtResolver.cs`
- `Assets/Scripts/UI/CombatSkillSlotController.cs`
- `Assets/Scripts/Sandbox/PlayerSkillPointService.cs`
- `Assets/Scripts/Sandbox/PcCombatCatalogFactory.cs`
- `Assets/Tests/EditMode/Sandbox/CaiBangSkillPanelTests.cs`
- `Assets/Tests/EditMode/Sandbox/PcSkillIconArtResolverTests.cs`

Confirm that visible data is keyed by `skillId`, then trace the requested
faction's catalog, progression state, selected detail, icon resolver, and slot
assignment. Treat these paths as implementation evidence, not PC proof.

## Workflow

1. Define the affected panel, faction, `skillId`, player level, points, and
   reproduction path. Resolve a Vietnamese name to PC identity before editing.
2. Build a compact evidence table: PC name, `skillId`, selected `Skills.txt`
   row, icon path/UID, relevant C++ behavior, Unity symbols, and proof status.
3. If an asset is involved, stage only the selected PC bytes with provenance;
   never guess a hash or filename.
4. Make the smallest change. Keep identity data separate from visual placement
   and preserve unrelated HUD, combat, map, and player behavior.
5. Add targeted tests for the reported mapping or gate. Test icon/name/level/
   selection/upgrade consistency by `skillId`.
6. For Unity changes, wait for compilation, inspect the console, run targeted
   EditMode tests with `run_tests`, and poll with `get_test_job`. In Play Mode,
   open the panel, select the skill, exercise the reported action, and capture a
   screenshot when the task is visual.

## Completion

Report the PC evidence and selected asset provenance, Unity files changed,
targeted test job/result, runtime observation, and any unresolved source or
human visual-acceptance gap. Claim PC parity only for evidence and behavior
actually verified.
