# US-M43-001 M4.3 Damage Formula Port

## Status

implemented

## Lane

normal

## Intake

Intake #19 (spec_slice, normal). Flags: Weak proof, Existing behavior.

## Product Contract

Damage formulas mapped from PC logic make future combat parity testable: unit
tests cover representative fixtures, a GM stat edit updates a deterministic damage
preview matching the formula, and any source-evidence gap is recorded before
implementation.

## Relevant Product Docs

- `docs/spec.md` — "M4.3 — Damage Formula Port"
- PC source: `jxwin-kinnox/.../Core/Src/KNpc.cpp:2125` `KNpc::CalcDamage`;
  `GameDataDef.h:128` `#define MAX_RESIST 95`

## Acceptance Criteria

- AC1: Formula source evidence exists; port implemented; unit tests cover
  representative fixtures.
- AC2: GM edits stats; skill damage preview updates; preview matches formula output.
- AC3: Formula source is unclear; work starts; source evidence gap is recorded
  before implementation.

## Design Notes

- `DamageFormulaService` (pure C#) ports the PC pipeline exactly:
  `nDamage = nMin + rand(nMax-nMin)` → typed armor absorption → mana shield
  (`dmg*manaShield/100` capped at currentMana) → resist `dmg*(100-nRes)/100` with
  nRes capped at resistMax then MAX_RESIST (95). Random roll injected for
  determinism; `PreviewDamage` for the GM stat preview.
- `SourceEvidence.Record` anchors the formula claim to KNpc::CalcDamage (AC#3).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: no-mitigation, armor leak/full, resist scale + caps (resistMax & MAX_RESIST=95), mana shield, full pipeline, zero early-out, roll provider, preview, evidence record |
| Integration | Pipeline order asserted in full-pipeline fixture |
| E2E | GM damage preview in Play Mode (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

First combat-parity primitive with explicit PC source evidence; reusable by future
skill/combat parity work. Records source evidence per AC#3.

## Evidence

EditMode 317/317 pass (docs/evidence/editmode-results-2026-05-31-m4-combat.json).
`DamageFormulaService` + `DamageType`/`AttackerStats`/`DefenderStats`/`DamageResult`.
Suite `VLTK.Tests.Sandbox.DamageFormulaServiceTests` (12 tests) covers AC1–AC3.
MAX_RESIST verified as 95 from `GameDataDef.h:128` (not assumed).
