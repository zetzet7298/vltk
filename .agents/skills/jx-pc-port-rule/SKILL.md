---
name: jx-pc-port-rule
description: Mandatory source-of-truth rule for porting any JX Online 1 / Võ Lâm Truyền Kỳ PC game feature into the VLTK-mobile Unity client. Use this skill before any porting work from PC to Unity, including skills, combat, NPCs, maps, UI, HUD, player visuals, items, effects, sprites, configs, Lua/C++ behavior, PAK/SPR assets, or parity fixes. It forces the agent to inspect the scoped PC source first and port logic/behavior/visuals 100% from PC instead of guessing or using out-of-scope PC sources.
---

# JX PC Port Rule

Use before starting any PC-to-Unity porting task in this repo. This is a short guardrail skill; after applying it, also load the more specific skill if the task matches one, such as `jx-map-port`, `jx-hud-port`, `jx-enemy-port`, `jx-player-visual`, `jx-skill-ui-port`, or `jx-skill-visual-port`.

## Source Of Truth

- Treat `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem` as the only PC game source of truth.
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

1. Locate the relevant file(s) under `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem` before editing Unity code.
2. Compare the PC source/data/asset against the current Unity implementation.
3. Implement the Unity port using PC values and assets directly where possible.
4. Verify with Unity compile/tests or runtime checks appropriate to the task.
5. Report which PC source files/assets were used.

## If Source Is Missing

- Say exactly what was searched under `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem`.
- Do not silently substitute another PC source.
- Make only clearly marked provisional changes if the user explicitly allows it.
