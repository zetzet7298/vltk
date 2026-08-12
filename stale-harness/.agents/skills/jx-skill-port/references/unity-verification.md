# Unity Skill Verification

Use this after the PC side of the parity ledger is proven.

## Editor Preflight

Use the live Unity MCP catalog:

1. Read `mcpforunity://instances` and select the intended editor if needed.
2. Read `mcpforunity://editor/state`; fields are under `data`.
3. Wait until `data.advice.ready_for_tools` is true and compilation, domain
   reload, asset refresh, tests, and Play Mode transitions are idle.
4. Read current errors and warnings.
5. Inspect the active scene and locate the player, targets, combat HUD, and
   relevant controllers before mutating anything.

If Unity MCP is unavailable, report runtime verification as blocked. Static or
CLI tests do not substitute for an observed combat-slot cast.

## Targeted Tests

Activate the live `testing` tool group if necessary. Re-list the tool schema
before invocation. The current server accepts snake_case parameters:

```json
{
  "mode": "EditMode",
  "category_names": ["FactionName"],
  "include_failed_tests": true
}
```

Alternatively use `test_names`, `group_names`, or `assembly_names`. Do not run
the full EditMode suite in the inner loop.

`run_tests` returns `job_id`. Poll with:

```json
{
  "job_id": "<job-id>",
  "wait_timeout": 60,
  "include_failed_tests": true
}
```

Keep category names and test filters aligned with the current repository. Run
the broader required gate only when shared combat code, asmdefs, or project
policy requires it.

## Deterministic Matrix

- Correct `skillId`, PC/raw name, Vietnamese display name, faction, player/NPC variant.
- Learned level and max level.
- Actual combat slot resolves the ID, icon, level, and input handler.
- Tap/click casts exactly one skill through the normal target-selection path.

- Target flags and no-target behavior.
- Range and obstacle behavior.
- Mana/resource cost.
- Cooldown and horse/action restrictions.
- Cast animation and damage timing.

- Test every breakpoint around the requested level.
- Test duplicate Lua anchors at the switch level.
- Test missing-table behavior when PC returns no attributes.
- Test child/event skill level inheritance.

### Projectile skills

- Missile count and per-missile origin.
- Formation orientation relative to cast direction.
- Speed per PC tick, lifetime ticks, Z motion, and delayed generation.
- Stable target identity and moving-target retarget cadence.
- Simulation direction drives sprite direction.
- Swept collision prevents tunneling on a large frame delta.
- Each missile collides/vanishes once and stops independently.
- Start/fly/collide/vanish event order and gates.
- Distinct precast, flight, loop, impact sprites and sounds.

### State, aura, and passive skills

- State owner and legal targets.
- Magnitude, duration, priority, stacking/replacement, dispel/death cleanup.
- Aura enter, remain, leave, recast, source removal, and mount offsets.
- Passive trigger condition, probability, internal cooldown, destination skill, and whether the edge modifies damage or casts a skill.

### Robustness

- No exception in one missile callback prevents later missiles from updating.
- No duplicate damage, damage popup, audio, impact, or cleanup.
- Destroyed/missing target follows PC fallback behavior.
- Scene unload and actor death clean active effects.

## Runtime scenario matrix

Exercise the actual combat slot in Play Mode:

| Scenario | Purpose |
|---|---|
| Stationary target in range | Baseline cast, count, formation, damage, impact |
| Moving target after launch | Homing identity and retarget timing |
| Target near maximum range | Range and lifetime behavior |
| Target crossed in one large update | Swept collision/tunneling |
| Two targets close together | Target selection and range damage |
| Target dies or despawns mid-flight | Follow/vanish fallback |
| Mounted and unmounted | Restrictions, offsets, animation |
| Repeated cast after cooldown | Cleanup, no stale state, no duplicate handlers |

For multi-projectile skills, observe every projectile until impact or vanish. Clear/read the console around the cast; an exception after projectile 1 can make projectiles 2-4 appear to have incorrect mechanics.

## Visual and audio capture

- Capture precast, early flight, retarget/turn, collision, and cleanup phases.
- Use screenshots to inspect sprite, direction, pivot, scale, layer, and per-projectile impact.
- Screenshots cannot prove full movement cadence or audio. Use runtime observation and explicit user acceptance.
- Verify SFX phase and multiplicity: once per cast versus once per missile.
- Compare with PC source footage or a reproducible PC client scenario when available.

## Phi Long Tai Thien acceptance

For skill `357` at level 20:

- Casting from the assigned combat slot spawns four missile `166` dragons.
- Origins are a wall at offsets `-64, -32, 0, 32` for a horizontal cast.
- All four initially face the cast direction and follow the same live target identity.
- Speed is `24` PC pixels per tick, lifetime is `24` ticks, simulation is `18 Hz`.
- Moving the target after launch leaves direction unchanged for updates 1-8 and changes it on update 9.
- The four lanes converge toward the target instead of pursuing four offset points.
- A large update still records each collision through swept collision.
- Every dragon generates one impact/collide callback, stops, and does not damage twice.
- Level 20 root damage is deferred until the individual collisions.
- Flight and impact sprites/sounds remain distinct.
- No `TextMesh`, damage-popup, audio, or renderer exception aborts dragons 2-4.

Relevant current Unity acceptance tests include:

```text
Assets/Tests/EditMode/Sandbox/CaiBangPhiLongCollisionAcceptanceTests.cs
Assets/Tests/EditMode/Sandbox/CaiBangPhiLongSpreadTests.cs
Assets/Tests/EditMode/Sandbox/CaiBangCombatParityTests.cs
```

These tests verify Unity behavior; they do not replace the PC citations in the ledger.

## Completion language

Use exact wording:

- `PC source-backed`: source ledger complete.
- `Targeted tests passed`: include category/job/result.
- `Unity runtime verified`: include scenario and console state.
- `Waiting for human visual/audio acceptance`: use until the user confirms.
- `100% parity`: only after all in-scope deterministic rows pass and the user accepts the subjective result.
