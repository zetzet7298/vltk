---
name: jx-hud-port
description: >-
  Port, fix, or verify the VLTK-mobile HUD against JX PC UI evidence. Use for
  HP/MP/EXP/stamina bars, minimap, chat, toolbar, action buttons, quick slots,
  pointer blocking, localization, PC INI layout, or SPR-backed HUD art.
---

# JX HUD Port

Apply `jx-pc-port-rule` first. Use `jx-pc-resource-resolver` for every PC INI,
SPR, encoded path, UID, duplicate, and package-winner decision. Use
`unity-mcp-orchestrator` for Editor inspection, tests, Play Mode, console, and
screenshots.

## Scope

Use this skill for HUD presentation and input behavior. Use `jx-skill-ui-port`
for the skill-management panel and `jx-skill-port` for combat mechanics. Map,
enemy, and player rendering remain with their specialist skills.

## Evidence Workflow

1. Identify the exact PC UI definition and selected package/version. Read its
   layout, control type, state frames, clipping, anchors, and referenced paths.
2. Resolve and decode every selected SPR with `jx-pc-resource-resolver`; retain
   original path bytes, package winner, UID, byte count, and SHA-256. Never use a
   remembered hash or a static UID catalog.
3. Inspect the current Unity implementation with `srcwalk`. The current entry
   points include `Assets/UI/HUD/`, `Assets/Scripts/UI/GameHudController.cs`,
   and `Assets/Scripts/UI/CombatSkillSlotController.cs`; verify them before each
   edit instead of assuming an old method or field still exists.
4. Confirm the active loader before staging art. The current project has editor
   art under `Assets/UI/HUD/Art/` and runtime art under
   `Assets/StreamingAssets/UI/HUD/Art/`; keep both only where the current loader
   and tests require them.

## Port Rules

- Preserve PC geometry and control semantics. Bars clip a fixed-size fill when
  the PC control clips; toggle buttons use the PC up/down state frames; do not
  replace either behavior with a visually similar shortcut.
- Keep HUD identity keyed by stable control or skill IDs, not translated labels
  or visual row positions.
- Decode orientation from the selected SPR and screenshot evidence. Do not
  blanket-flip a batch of images.
- Use exact PC art. Do not generate, recolor, redraw, or substitute placeholder
  HUD art unless the user explicitly accepts a provisional replacement.
- Keep noninteractive containers from blocking gameplay input. Only real
  controls should receive pointer events; verify the joystick and map gestures.
- Localize user-facing text to Vietnamese while preserving the PC control and
  resource mapping. If text is embedded in art, decode and verify the selected
  Vietnamese asset rather than overlaying an invented translation by default.

## Proof

- Add or update targeted EditMode coverage for loader paths, control state,
  clipping, layout bounds, and pointer behavior.
- In Play Mode, verify bars, minimap, chat, toolbar, action states, quick slots,
  Vietnamese text, and unblocked gameplay input at the supported resolutions.
- Read the Unity console after import, compile, tests, and runtime interaction.
  Capture screenshots only while the verified UI is visible.
- Report PC definitions and selected asset provenance, Unity files/art changed,
  automated and runtime proof, and every unresolved language or pixel-parity gap.
