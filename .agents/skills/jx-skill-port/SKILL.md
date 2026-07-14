---
name: jx-skill-port
description: Port, fix, or verify complete JX Online 1 / Võ Lâm Truyền Kỳ PC combat skills in the VLTK-mobile Unity client with source-backed parity across skill identity and combat-slot binding, Skills.txt, Missles.txt, Lua/C++ behavior, targeting and homing, missile formations, damage and states, event/child skills, cooldown and cost, exact SPR/WAV assets, UI icons, tests, and Unity runtime behavior. Use this skill whenever the user asks to port a võ công/skill, make a named skill "giống PC" or "100%", debug Phi Long Tại Thiên/dragons/projectiles/missiles, fix an aura/passive/buff, repair combat-slot or icon integration, or verify any faction skill even if they mention only one visual symptom. Never infer PC behavior from current Unity code.
---

# JX Skill Port

Port one skill at a time from the current JX PC corpus into Unity Mobile. Treat parity as an evidence problem first and an implementation problem second.

## Required companions

Apply the current repo instructions and these skills when they exist:

- Apply `jx-pc-port-rule` before inspecting or editing any port.
- Apply `jx-pc-resource-resolver` for every SPR, WAV, INI, Lua, TXT, hash, duplicate asset, and PAK-winner decision.
- Apply `jx-skill-visual-port` for projectile, VFX, animation, pivot, sound, and flight mechanics. If it is unavailable, follow [unity-verification.md](references/unity-verification.md) directly.
- Apply `jx-skill-ui-port` when the skill panel, icon, level, tooltip, add-point flow, or combat-slot mapping is involved.
- Apply `unity-mcp-orchestrator` for Unity Editor state, compilation, tests, runtime setup, console inspection, and screenshots.
- Use `srcwalk` for repository navigation. Run `srcwalk guide`, then use `overview`, `discover`, `context`, `show`, `trace`, and `review` instead of broad grep-based code inference.

Do not treat Unity code, Unity tests, comments, screenshots, previous reports, generated reference files, or another port as PC proof. They are comparison evidence only.

## Inputs to establish

Identify these before editing:

- Vietnamese skill name and PC source name.
- `skillId`, requested level, faction, and whether the task targets player, NPC, or both.
- Active PC package/load-order context.
- Current combat slot and input path, when the skill is cast from HUD combat.
- Reproduction scenario: caster position, target identity and movement, obstacles, level, mount state, and expected result.
- User acceptance target: deterministic mechanics, visual parity, audio parity, or the complete skill.

If the user only gives a name, locate the ID in current PC data. Never bind by translated name or list index when an ID exists.

## Parity contract

Maintain a parity ledger using [parity-ledger.md](references/parity-ledger.md). Each important behavior needs:

1. Exact PC evidence with package, path, row/line, field, enum, or C++ function.
2. The PC value or runtime semantics.
3. The corresponding Unity symbol/file.
4. Automated or runtime proof.
5. A status that distinguishes source-proven, implemented, runtime-verified, and human-accepted.

Do not edit the behavior until the ledger proves at least the skill identity, selected package row, level-data source, skill-style dispatch, target rules, child/event graph, and relevant missile/state dispatch. Unresolved evidence remains an explicit gap; it is not permission to invent a fallback.

## Workflow

### 1. Preflight

1. Read the nearest `AGENTS.md` files and current skill instructions.
2. Inspect `git status` and keep unrelated dirty changes untouched.
3. Confirm the canonical PC roots from `jx-pc-port-rule`.
4. Research unfamiliar engine or Unity APIs using the repo-mandated research tools before implementation. If a mandatory service is unavailable, report that fact and use the documented fallback rather than guessing.
5. Check Unity MCP editor state before any Editor operation. Do not assume the Editor is connected or ready.

When agent delegation is available and allowed, split only independent, bounded work:

- A read-only PC evidence task may fill the source side of the ledger.
- A Unity trace task may map current runtime and tests.
- One implementation owner edits a coherent area.
- The root agent owns the ledger, reconciles contradictions, stages files, and reports final evidence.

Do not use a review agent as a substitute for the user's visual acceptance. Respect any user instruction that forbids subagents.

### 2. Generate a PC reconnaissance packet

Run the bundled read-only helper from this skill directory:

```bash
python3 scripts/audit_pc_skill.py --skill-id 357 --package slistcache --level 20
```

The JSON packet exposes the selected `Skills.txt` row, child missile or child skill row, enum hints, Lua table excerpts, source line numbers, hashes, and resource paths. It is reconnaissance, not final proof:

- `--package slistcache` selects one extracted package tree only.
- It does not determine the active PAK winner.
- It does not execute Lua interpolation or C++ dispatch.
- It does not recursively prove dynamic event skills.

Resolve those gaps from source before editing. Read [pc-source-map.md](references/pc-source-map.md) for the evidence order and field map.

### 3. Reconstruct the complete PC behavior graph

Trace the root skill and every reachable child or event:

- Root `Skills.txt` row and all non-empty `LvlSettingN` / `LvlDataN` bindings.
- Lua level table at the exact requested level, including discontinuities and duplicate level anchors.
- `SkillStyle`, `MisslesForm`, `BaseSkill`, `ChildSkillId`, `ChildSkillLevel`, `ChildSkillNum`, `MslsGenerate`, `Param1`, and `Param2`.
- Static and Lua-driven start, show, fly, collide, vanish, state, add-skill-damage, auto-skill, and response-skill references.
- `Missles.txt` movement, identity-following, collision, lifetime, speed, Z movement, animation slots, and sound slots.
- C++ dispatch and update order for the selected enums. Numeric values alone are insufficient.
- Cost, cooldown, horse restrictions, action animation, targeting flags, damage timing, state priority, aura ownership, and duration.

Represent the result as a graph. Recurse until each reachable node is proven or explicitly out of scope.

### 4. Resolve exact resources

For every resource path:

1. Preserve the original PC path bytes and decoded path.
2. Use `jx-pc-resource-resolver` to calculate the JX hash and inspect duplicate candidates.
3. Determine the active PAK winner from package order, not the first matching file.
4. Cross-check `_labels.json` and decode the SPR with `~/Projects/vltktool/`.
5. Verify Vietnamese artwork when text is embedded in the sprite.
6. Record flight, impact, precast, state-loop, and sound slots separately.

Never invent or silently substitute a generic sprite, recolor, generated icon, or similarly named sound.

### 5. Trace the current Unity path

Use `srcwalk` from intent to exact symbols:

```bash
srcwalk overview --scope Assets/Scripts
srcwalk discover '357,Phi Long Tại Thiên' --match any --as text --scope Assets
srcwalk context <candidate-symbol> --scope Assets
srcwalk trace callers <cast-symbol> --scope Assets/Scripts
srcwalk trace callees <cast-symbol> --detailed --scope Assets/Scripts
```

Trace all relevant layers:

- Catalog/parser and level tuning.
- Skill acquisition and learned level.
- Combat-slot assignment, icon, tap/input handler, and selected skill ID.
- Target selection and stable actor identity.
- Cast gates, cost, cooldown, action state, and damage/state timing.
- Projectile or state simulation.
- Collision/event callback and exactly-once guards.
- Visual renderer, direction/frame selection, pivot, sorting, and cleanup.
- Audio routing.
- Tests and runtime scene setup.

Do not patch only the renderer when the missing behavior originates in target identity, simulation, event dispatch, or slot binding.

### 6. Implement the smallest source-backed slice

- Preserve PC IDs, tick units, coordinates, formulas, enum meanings, event order, and asset provenance.
- Keep target identity separate from a sampled target position for homing behavior.
- Simulate PC-tick mechanics on a fixed accumulator. Rendering may interpolate, but must not change simulation results.
- Use swept collision when a missile can cross a target between ticks.
- Fire each collision/event once and stop or transition the individual missile according to PC flags.
- Drive sprite direction from simulation direction, not the original cast vector.
- Keep each missile's lifecycle independent; one callback or UI exception must not prevent later missiles from updating.
- Key UI and combat slots by `skillId`, never by row or translated name.
- Localize user-facing Chinese text to Vietnamese while retaining PC names/paths in provenance.
- Avoid unrelated refactors. Shared runtime changes require broader regression coverage.

### 7. Add deterministic tests

Write tests before or with the fix for every ledger row that can be automated:

- Identity, ID, level, slot, icon, cost, cooldown, and target flags.
- Level breakpoints and formulas.
- Missile count, origins, formation, speed, lifetime, and tick timing.
- Moving-target retarget cadence and target identity.
- Swept collision, exactly-once impact, event order, and stopped/vanished state.
- Damage/state timing and child/event skill level.
- Distinct precast, flight, impact, loop, and sound resources.
- Regression for the reported failure, including exceptions that abort a multi-projectile loop.

Follow the repo test policy:

- Add `[TestFixture, Category("<Faction>")]` at class level.
- Add `Category("Slow")` for sprite decode or other slow visual tests.
- Run the faction/category or namespace filter during development; never run the full EditMode suite in the inner loop.
- Use `TestCatalogCache` only for tests that do not mutate the catalog.
- Run the full required gate only when repo policy requires it before push or shared code changed.

### 8. Verify in Unity MCP

Follow [unity-verification.md](references/unity-verification.md):

1. Wait for compilation and domain reload.
2. Read errors and warnings before Play Mode.
3. Run targeted EditMode tests and retain the job/result evidence.
4. Build a controlled runtime scenario with a stationary and then moving target.
5. Cast from the actual assigned combat slot, not a direct debug method only.
6. Observe all projectiles through impact/vanish, inspect the console, and capture screenshots at useful phases.
7. Verify audio separately when screenshots cannot prove it.

Automated tests can prove deterministic mechanics. They cannot alone prove that timing, layering, animation, sound, and feel are visually identical to PC.

### 9. Close the ledger and report

Before claiming completion:

- Every in-scope ledger row has PC evidence and Unity evidence.
- Targeted tests pass and the Unity console is clean for the scenario.
- Exact resources and PAK provenance are recorded.
- The actual combat slot casts the intended `skillId`.
- Runtime behavior was observed against the stated reproduction.
- The user has visually accepted subjective parity.

Use these claims precisely:

- `source-backed`: PC evidence is complete.
- `automated verified`: deterministic tests pass.
- `runtime verified`: observed in Unity with a clean console.
- `100% parity`: reserve this for complete in-scope evidence plus explicit human visual/audio acceptance.

Report in Vietnamese with four compact parts:

1. PC evidence used.
2. Unity behavior/files changed.
3. Tests and runtime verification.
4. Remaining gaps or human acceptance still required.

Stage and commit only task-owned changes. Do not include unrelated dirty files.

## Common failure: Phi Long Tại Thiên

Use this as a diagnostic example, not as a template to hardcode every skill:

- Skill `357`, level 20, references child missile `166`.
- `MisslesForm=0` is `SKILL_MF_Wall`, not a guessed single/fan form.
- `Param1=32`; PC `CastWall` starts at `-Param1 * count / 2`, producing origins `-64, -32, 0, 32` for four missiles.
- Lua level 20 yields four missiles and speed `24`, overriding the raw missile speed `30`.
- Missile `166` has `MoveKind=5` (`MISSLE_MMK_Follow`) and `LifeTime=24`.
- Every missile stores the same target NPC index and stable NPC ID.
- PC follow direction refreshes when the counter reaches the ninth update.
- The mobile simulation uses fixed `18 Hz`; the renderer follows simulation direction.
- Swept collision prevents tunneling; each missile collides once, fires its impact/collide event, and stops independently.
- A duplicate `TextMesh` exception previously aborted the loop after the first dragon. A visual/UI exception is therefore part of combat acceptance when it interrupts remaining missiles.

The direct PC evidence and Unity acceptance checklist are recorded in the bundled references.

## References

- [parity-ledger.md](references/parity-ledger.md): evidence/status template and edit gate.
- [pc-source-map.md](references/pc-source-map.md): canonical PC evidence order, field groups, enums, and Phi Long source anchors.
- [unity-verification.md](references/unity-verification.md): targeted tests, MCP runtime matrix, and human acceptance.
