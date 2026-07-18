# Validation

## Proof Strategy

Static catalog equality is a prerequisite, not runtime proof. Each corrected case
must assert independently sourced cast gates and an ordered runtime outcome:
resource/cooldown, projectile/event identity, damage/state applications, and final
actor state. Visual/audio claims additionally require exact package-winner assets
and runtime captures.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | PC formula interpolation, event-level selection, idempotency, source/provenance drift |
| Integration | Cast through direct/missile/trap lifecycle and compare ordered reports/state |
| E2E | Fresh Sandbox Play for representative Cái Bang and Đường Môn active/passive cases |
| Platform | Unity compile; Android/device remains required before epic closure |
| Performance | No duplicate event damage; bounded projectile/event fan-out |
| Logs/Audit | Harness trace plus independent proof-auditor verdict |

## Fixtures

- Pinned Cái Bang and Đường Môn static oracles and vltktool slices.
- Canonical Lua/C++ source references for each selected runtime case.
- Deterministic caster/target state, level, position, relation, seed, and tick.

## Commands

```text
python3 scripts/generate_caibang_oracle.py --check
python3 scripts/generate_tangmen_oracle.py --check
bash scripts/run_caibang_parity_tests.sh
bash scripts/run_tangmen_parity_tests.sh
```

## Acceptance Evidence

Executed on 2026-07-17:

- `python3 scripts/compile_scripts.py`: pass; only pre-existing Unity warnings.
- Cái Bang focused runner (including the new fire-pool fixture), job
  `3d38c34ffcb647db9bc253232fdd8747`: 148/148 pass. It now covers separate
  `AddFireMagicV`/`AddFireDamageV`, fail-closed 117, additive PC accumulator
  semantics, and `CreateCombatActor` normal player initialization for learned
  skill 116. The focused fixture plus controller regression job
  `53458a4803b1407290102f6360b843d8` passed 39/39.
- Đường Môn focused EditMode job `ddaa321adb194948a9eff63d1055becc`:
  4/4 legacy static tests plus the new level-data parity cases; combined
  Cái Bang/Đường Môn direct run passed 10/10. Canonical Lua data is pinned by
  SHA-256 `dea854244a292b9f9c96b0fcb31ad0a0e25165cfa4e2252c9141f1c9042d46d8`.
- PlayMode `VLTK.Tests.PlayMode.E2EGameplayLoopTests`, job
  `6507f1c8af1f4344ad084442c73210d7`: 8/8 pass.
- Fresh five-second Play smoke: no compile/runtime errors; warnings remain for
  duplicate skill 1539, unstaged horse SPR, and memory above the 200 MB budget.
- Follow-up Đường Môn lifecycle runner, job
  `e2953417c0d146f1a1fbafd005316724`: 13/13 pass. The added cases execute
  Lua-gated collide `1069→1097`, full runtime fly `1070→1098`, and visual-to-runtime
  vanish `1110→1113`; lifecycle child IDs are recorded in order and vanish is
  idempotent per projectile. Post-change Cái Bang regression job
  `f1c5a0cb8169486da1be59e509250617` remains 148/148 pass. Unity compilation
  succeeds with only the pre-existing warnings.
- Cái Bang return-damage follow-up, job
  `4520dd418f3544b8a55f2ccdb7e85912`: 149/149 pass. Runtime now consumes
  `meleedamagereturn_p`/`rangedamagereturn_p` as the defender's PC percent
  return pools and applies the attacker's `returnres_p` afterwards, matching
  `KNpc.cpp:2665-2678`. The integration case casts the real skill-129 and
  skill-130 buffs before a deterministic melee probe. Đường Môn regression job
  `cbab3714db4e4e78965f14cdac2914a7` remains 13/13 pass.
- Proof-auditor P1 follow-up, Cái Bang job
  `8159805218ee4c759c6c23a8f8482c9e`: 152/152 pass. Skill 714 now uses the
  authoritative server Lua encoded rate points L1/L15/L20=`1/5/6`, decodes
  child level and the 216-tick delay, and preserves the PC strict boundary
  (`nextCastTime < currentTime`, so t+216 remains blocked and t+217 is eligible).
  Player-state writeback removes learned passive contributions before persistence,
  so skill 116 is materialized exactly once on the next load while a same-pool
  active skill-130 contribution survives. Đường Môn runner job
  `31a823c62dcb40539621da0bdfef82c9` now includes the previously omitted
  `TangMenSkillLevelDataParityTests` and passes 17/17. Generator checks pass:
    12/12 Python tests and level-data SHA-256
    `dea854244a292b9f9c96b0fcb31ad0a0e25165cfa4e2252c9141f1c9042d46d8`.
- Resistance-cap and Cái Bang FlyEvent follow-up, Cái Bang job
  `0e988cc09a9c4e5bba32f3a0868366cf`: 153/153 pass. Player fire and
  physical resistance caps now start at the canonical 75; the skill-720
  `fireresmax_p` debuff changes an actual fire-damage outcome. Runtime now
  dispatches `1073→1103` from the parent missile event point, preserves the
  parent L20 event level, and rejects duplicate projectile/ordinal callbacks;
  the visual service now arms the existing controller callback for that row.
  Post-change Đường Môn regression job
  `85226b94252d4b79a020ebab33bbccf5` remains 17/17 pass; generator tests
  remain 12/12 and `git diff --check` passes.
- Đường Môn gate follow-up, job `9c78b933698e4e719f46d08be18b89dd`:
  18/18 pass. Skill 339 now consumes the pinned Lua duplicate-breakpoint gate,
  suppressing child 340 through L10 and dispatching it once per projectile from
  L11. An immediately preceding job failed to initialize with zero tests run;
  the clean rerun distinguishes that editor timeout from a gameplay assertion.
- Đường Môn temporal-override follow-up, job
  `5ad82ad4f2bd47aaa184b550400dac2c`: 20/20 pass. Pinned Lua
  `missle_lifetime_v` now overrides raw missile lifetime for 302/1070, and
  `missle_speed_v` overrides raw speed for 58/1069/1071. The stationary
  lifecycle checks expiry before FlyEvent on the terminal tick, matching the
  inspected `KMissle` order. The shared Lua parser now evaluates bounded
  literal multiplication/division such as `18*2`; a parser regression test is
  included. Cái Bang regression job `068a6d04b6004ead87f77641d7c05146`
  passes 156/156 and now proves the canonical L20 `1073→1103` fan-out of three
  child missiles per FlyEvent rather than the former one-child assumption.

## Proven and Unproven

Proven for this slice: parser/catalog separation of the two Cái Bang fire
attributes; PC-style endpoint-segment extrapolation for generated Đường Môn
level data; deterministic EditMode and PlayMode regressions; and clean script
compilation.

Not proven and intentionally open: Cái Bang vanish events and source-aware
stacking/expiry for simultaneous positive and negative `rangedamagereturn_p`;
unconsumed state attributes (`fatallystrike_p`, `anti_do_hurt_p`,
  series conversion and block-rate fields); remaining Đường Môn trap placement,
  target reacquisition and untested nested lifecycle chains beyond the bounded
  302/339/1069/1070/1110 temporal/event cases; PC runtime golden
comparison; Android/device capture; and exact runtime asset/audio parity.

The global skill-coverage audit is also stale: recomputed inventory is 242
versus committed 245, with provenance-request drift. This is recorded as a
repository residual rather than hidden by narrowing tests. Herdr writer lanes
also edited outside their declared write scopes; the files were reviewed and
retained because the edits are part of this bounded slice, with scope
enforcement recorded as Harness friction.

Therefore the story outcome is `partial`; it must not be marked `PARITY_DONE`.

## Independent Review Gate

The proof auditor blocked approval pending two follow-ups: (1) the runner fix
above (now applied and rerun at 144/144), and (2) an integration test through
normal player passive-state materialization for skill 116. The auditor also
noted that `PcModSkillParser` does not map the new fire attribute names; the
current claim is limited to the `PcConfigParser`/Cái Bang factory path until
that importer is audited or covered. These findings keep the story partial.

The follow-ups are now addressed for Cái Bang: `PcModSkillParser` maps both
fire-pool names, `CreateCombatActor` materializes learned passives, and the
tests exercise the production path plus PC additive accumulation from
`KNpcAttribModify::AddFireMagicV`. The remaining Cái Bang blockers are
FlyEvent/VanishEvent, unconsumed state attributes, and PC/mobile golden proof.

The independent audit resolved the child provenance for 301 and 1098 against
the pinned `slistcache` relationship slice and canonical server Lua
(`PcTangMenSkillLevelData.lua` SHA-256
`3f2e7c2aba8329508adab3a6293f41be29a0d68a8d63ac5a34903bece0578c90`). It also
confirmed both children are PC `SkillStyle=0` Missiles rows, with 301's
`ByMissle=0` direct-damage behavior explicitly preserved.

The first Đường Môn lifecycle slice is now implemented: FlyEvent `302→301`
uses the Lua duplicate-breakpoint semantics (level 10 remains disabled; level
11 enables), interval 30, event-level lookup, per-projectile/interval
idempotency, and visual callbacks during stationary or moving effects. Runner
job `e601ec01a1e44f88be92e31b872a30ea` passed 10/10, including three runtime
FlyEvent tests. The reusable `1070→1098` gate is proven in the service but has
not yet received a full visual/runtime integration case.

Đường Môn collide routing now uses `PcTangMenLuaLevelService` rather than the
Cái Bang service, and the visual lifecycle calls runtime vanish dispatch once
per projectile. These bounded chains do not prove every Đường Môn skill.

Still open: Cái Bang Fly/Vanish events, remaining unconsumed state attributes, remaining
Đường Môn trap/homing/lifecycle coverage, exact PC runtime golden comparison,
Android/device capture, and the global 242-versus-245 inventory drift.
