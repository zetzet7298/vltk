# Parity Ledger

Use this ledger before editing a JX combat skill. Keep it in task notes, a ticket, or a task-owned document. Preserve durable source anchors in tests/comments/provenance where they prevent future regressions.

## Header

```text
Skill VI:
Skill PC/raw:
SkillId:
Requested level:
Faction:
Player/NPC scope:
Combat slot:
Selected PC package:
Active PAK order proof:
Unity scene/reproduction:
```

## Status vocabulary

Use only these statuses:

- `unknown`: no direct evidence yet.
- `pc-proven`: exact PC data/source establishes the value or behavior.
- `implemented`: Unity has a mapped implementation, not yet verified.
- `automated-verified`: deterministic tests prove the Unity behavior.
- `runtime-verified`: observed in the real Unity cast path with a clean console.
- `human-accepted`: the user accepted subjective visual/audio parity.
- `out-of-scope`: intentionally excluded and reported.
- `blocked`: source or runtime evidence is unavailable; no substitute was invented.

## Evidence standard

A useful PC citation contains:

- Package or source tree.
- Exact path.
- Row ID and column for tables, or current line/function for code/Lua.
- Decoded value plus raw/original path when encoding matters.
- Enum definition when a numeric field controls dispatch.
- PAK winner and hash for assets.

A useful Unity citation contains:

- Exact symbol/file.
- Mapping from the PC value to Unity data/state.
- Test name or runtime observation.
- Any conversion, such as PC tick to seconds or PC pixels to Unity units.

Screenshots and existing Unity comments are not PC evidence.

## Root ledger

| Area | PC evidence | PC value/semantics | Unity location | Verification | Status |
|---|---|---|---|---|---|
| Identity/name/ID |  |  |  |  | unknown |
| Player or NPC variant |  |  |  |  | unknown |
| Learned level/max level |  |  |  |  | unknown |
| Combat slot/input |  |  |  |  | unknown |
| Skill icon |  |  |  |  | unknown |
| Skill style/dispatch |  |  |  |  | unknown |
| Target flags/identity |  |  |  |  | unknown |
| Cast range |  |  |  |  | unknown |
| Cost/resource |  |  |  |  | unknown |
| Cooldown/cast time |  |  |  |  | unknown |
| Horse/action restrictions |  |  |  |  | unknown |
| Character animation |  |  |  |  | unknown |
| Precast visual/sound |  |  |  |  | unknown |
| Formation/origins |  |  |  |  | unknown |
| Missile/state count |  |  |  |  | unknown |
| Movement/tick cadence |  |  |  |  | unknown |
| Homing/retarget |  |  |  |  | unknown |
| Lifetime/vanish |  |  |  |  | unknown |
| Collision/range |  |  |  |  | unknown |
| Damage timing/formula |  |  |  |  | unknown |
| State/aura behavior |  |  |  |  | unknown |
| Start/fly/collide/vanish events |  |  |  |  | unknown |
| Flight/loop/impact SPR |  |  |  |  | unknown |
| Flight/impact SFX |  |  |  |  | unknown |
| Direction/pivot/sorting |  |  |  |  | unknown |
| Cleanup/exactly-once |  |  |  |  | unknown |

Delete irrelevant rows only after proving that the PC skill does not use that mechanism.

## Level ledger

Record every breakpoint that affects the requested level. Duplicate anchors matter because JX Lua tables can deliberately switch behavior at the same level.

| Setting | Lua data key | Anchors around level | Evaluator/C++ consumer | Resolved value | Test | Status |
|---|---|---|---|---|---|---|
| Damage |  |  |  |  |  | unknown |
| Cost |  |  |  |  |  | unknown |
| Range |  |  |  |  |  | unknown |
| Missile count |  |  |  |  |  | unknown |
| Missile speed |  |  |  |  |  | unknown |
| Formation |  |  |  |  |  | unknown |
| Event flags/skill level |  |  |  |  |  | unknown |
| State magnitude/duration |  |  |  |  |  | unknown |

Do not assume linear interpolation, rounding, `Conic`, missing-table, or duplicate-anchor semantics. Read the PC evaluator/consumer.

## Child and event graph

Add one row for every reachable node.

| From | Edge | Gate/timing | To ID/type | Level rule | PC evidence | Unity callback | Status |
|---|---|---|---|---|---|---|---|
| root | child missile/skill |  |  |  |  |  | unknown |
| root | start event |  |  |  |  |  | unknown |
| root | fly event |  |  |  |  |  | unknown |
| root | collide event |  |  |  |  |  | unknown |
| root | vanish event |  |  |  |  |  | unknown |
| root | show/response skill |  |  |  |  |  | unknown |
| root | add-skill-damage/auto-skill |  |  |  |  |  | unknown |

Recurse when the destination is another skill. Distinguish "adds damage to skill X" from "casts skill X"; the ID pair alone does not establish event semantics.

## Asset ledger

| Phase | Original PC path | Hash UID | PAK winner | Decoded evidence | Unity key/path | Status |
|---|---|---|---|---|---|---|
| Icon |  |  |  |  |  | unknown |
| Precast |  |  |  |  |  | unknown |
| Flight |  |  |  |  |  | unknown |
| Loop/state |  |  |  |  |  | unknown |
| Collision/impact |  |  |  |  |  | unknown |
| Cast SFX |  |  |  |  |  | unknown |
| Flight SFX |  |  |  |  |  | unknown |
| Impact SFX |  |  |  |  |  | unknown |

## Edit gate

Do not begin behavioral edits until these are `pc-proven`:

- Root identity and selected package row.
- Requested level-data source.
- `SkillStyle` and its C++ dispatch.
- Target flags and target identity behavior.
- Child/event graph for the in-scope path.
- `MisslesForm` and `MoveKind`, or state/aura dispatch for non-missile skills.
- Exact assets needed by the slice being changed.

If two current PC sources disagree, resolve active package/load order first. Do not average values or choose the implementation that looks better.

## Completion gate

The work is complete only when:

- No in-scope row remains `unknown`.
- Deterministic rows are `automated-verified`.
- The actual combat-slot scenario is `runtime-verified`.
- Visual/audio rows are `human-accepted` before claiming `100% parity`.
- Blocked or out-of-scope rows are explicitly reported.
