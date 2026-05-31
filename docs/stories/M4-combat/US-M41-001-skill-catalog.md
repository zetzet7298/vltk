# US-M41-001 M4.1 Skill Catalog

## Status

implemented

## Lane

normal

## Intake

Intake #17 (spec_slice, normal). Flags: Existing behavior.

## Product Contract

A skill catalog mapped from PC config (KSkill) provides SkillDefinition entries so
skills can be selected in the sandbox. Skill icon/effect/missile references are
validated against the asset registry, and the GM UI shows selected-skill details.

## Relevant Product Docs

- `docs/spec.md` — "M4.1 — Skill Catalog"
- PC source: `jxwin-kinnox/.../Core/Src/KSkills.h` / `KSkills.cpp` (KSkill fields)

## Acceptance Criteria

- AC1: Skill config exists; converter runs; SkillDefinition entries are generated.
- AC2: Skill references icon/effect; registry resolves; asset links are validated.
- AC3: GM selects skill; UI updates; selected skill details are shown.

## Design Notes

- `SkillDefinition` model grounded in KSkill (m_nId, m_szName, m_usReqLevel,
  m_szSkillIcon, m_szPreCastEffectFile, m_nCost, m_nAttackRadius, m_bIsPhysical,
  m_eMisslesForm) + per-level `SkillDamageLevel`.
- `SkillCatalog` (pure C#): register/resolve, `ValidateAssets` (icon required,
  effect optional, missile required only when HasMissile), `Select`/`SelectedDetails`.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: register, level lookup, asset validation (icon/effect/missile), select details |
| Integration | Asset links via AssetRegistry (unit-covered) |
| E2E | GM skill select in Play Mode (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Skill primitive consumed by M4.2 projectile + M4.3 damage.

## Evidence

EditMode 317/317 pass (docs/evidence/editmode-results-2026-05-31-m4-combat.json).
`SkillDefinition` + `SkillCatalog`. Suite `VLTK.Tests.Sandbox.SkillCatalogTests`
(9 tests) covers AC1–AC3.
