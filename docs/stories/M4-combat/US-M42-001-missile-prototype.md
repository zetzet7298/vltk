# US-M42-001 M4.2 Missile/Projectile Prototype

## Status

implemented

## Lane

normal

## Intake

Intake #18 (spec_slice, normal). Flags: Existing behavior.

## Product Contract

Missile/projectile visuals spawn from skill data so PC projectile behavior can be
mapped: a missile skill cast spawns a projectile/effect placeholder, a decoded
effect sprite plays when available, and a cast that is out of range or targets a
blocked cell is rejected with a diagnostic reason.

## Relevant Product Docs

- `docs/spec.md` — "M4.2 — Missile/Projectile Prototype"
- PC source: `jxwin-kinnox/.../Core/Src/KSkills.cpp` (m_MissleAttribs: speed/lifetime/range)

## Acceptance Criteria

- AC1: Skill has missile/effect ref; cast in sandbox; projectile/effect placeholder
  spawns.
- AC2: Effect sprite available; cast in sandbox; decoded sprite effect plays.
- AC3: Target blocked/out of range; cast requested; cast is rejected with a
  diagnostic reason.

## Design Notes

- `ProjectileInstance` (origin/target/speed/effectClipRef, `Step` toward target) +
  `ProjectileService` (pure C#): `Cast` checks range (attackRadius * RangeWorldPerUnit)
  and blocked target via `ObstacleQueryService`, spawns for missile skills, instant
  success for non-missile, `Step` advances/retires projectiles.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: spawn missile, instant skill, step+arrive, effect resolved/missing, out-of-range/blocked/null rejection |
| Integration | Obstacle query reused for blocked-target (unit-covered) |
| E2E | Live projectile sprite in Play Mode (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Projectile prototype over M4.1 skills + M1.5 obstacle query.

## Evidence

EditMode 317/317 pass (docs/evidence/editmode-results-2026-05-31-m4-combat.json).
`ProjectileInstance` + `ProjectileService` + `CastResult`/`CastRejectReason`. Suite
`VLTK.Tests.Sandbox.ProjectileServiceTests` (9 tests) covers AC1–AC3.
