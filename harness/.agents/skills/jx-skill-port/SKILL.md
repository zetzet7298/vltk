---
name: jx-skill-port
description: >-
  Port, fix, or verify complete JX Online 1 / Vo Lam Truyen Ky PC combat skills
  in the VLTK-mobile Unity client. Use for skill identity, Skills.txt,
  Missles.txt, Lua/C++ behavior, targeting, missiles, damage, states, child
  events, cooldown/cost, exact SPR/WAV assets, combat slots, tests, and runtime
  parity. Never infer PC behavior from current Unity code.
---

# JX Skill Port

Port one skill at a time. Apply `jx-pc-port-rule` first,
`jx-pc-resource-resolver` for every PC resource, `jx-skill-ui-port` for panel or
combat-slot UI, and `unity-mcp-orchestrator` for Editor work. Unity code,
comments, tests, screenshots, and previous ports are comparison evidence, not
PC proof.

## Evidence Gate

Create a task-owned ledger from
[parity-ledger.md](references/parity-ledger.md). Before behavioral edits, prove:

- PC/raw name, Vietnamese name, `skillId`, faction, requested level, and
  player/NPC scope.
- Active package/load-order winner for `Skills.txt`, `Missles.txt`, Lua, and
  every referenced asset.
- Level bindings and the C++ consumer that defines interpolation, rounding,
  duplicate anchors, and missing data.
- `SkillStyle`, target rules, child/event graph, missile formation/movement or
  state/aura dispatch, cost, cooldown, action restrictions, and timing.
- Exact precast, flight, loop, impact, icon, and WAV resources used by the
  in-scope path.

Missing or conflicting evidence is `blocked`; it is not a fallback-design
opportunity.

## Workflow

1. Use `srcwalk` to locate the current Unity catalog, acquisition, combat-slot,
   targeting, simulation, renderer, audio, and test paths.
2. Optionally run `scripts/audit_pc_skill.py` for a reconnaissance packet. Its
   selected package is not proof of the active PAK winner, and its output does
   not replace Lua/C++ dispatch analysis.
3. Reconstruct the root skill and every reachable child, start, fly, collide,
   vanish, response, auto-skill, and state edge. Recurse until each node is
   proven or explicitly out of scope.
4. Resolve every resource through `jx-pc-resource-resolver`; preserve original
   path bytes, encoding, UID, package winner, byte count, SHA-256, and decoded
   visual/audio evidence.
5. Implement the smallest source-backed slice. Preserve IDs, PC tick units,
   formulas, enum meanings, event order, target identity, collision semantics,
   independent projectile lifecycles, and `skillId`-based UI binding.
6. Add deterministic regression tests for every automatable ledger row:
   identity, level data, cast gates, formation, movement, collision, event
   order, damage/state timing, assets, slot binding, and the reported failure.
7. Follow [unity-verification.md](references/unity-verification.md). Run narrow
   tests first, cast through the actual combat slot in Play Mode, observe the
   full lifecycle, inspect the console, and verify audio separately.

## Completion Claims

- `source-backed`: the in-scope PC graph and resources are proven.
- `automated-verified`: deterministic tests pass.
- `runtime-verified`: the real Unity cast path was observed with a clean
  console.
- `100% parity`: reserve for complete in-scope evidence plus explicit human
  visual/audio acceptance.

Report PC evidence, Unity changes, automated/runtime proof, and remaining gaps.
Do not include unrelated dirty files.
