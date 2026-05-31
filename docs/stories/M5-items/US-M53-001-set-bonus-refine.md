# US-M53-001 M5.3 Set Bonus and Refine Rules

## Status

implemented

## Lane

normal

## Intake

Intake #22 (spec_slice, normal). Flags: Weak proof, Existing behavior.

## Product Contract

Set bonus/refine rules are validated against golden cases so item parity is
measurable: golden replay cases match expected stat outcomes, stubbed rules are
reported by the quality gate, and changing equipment recalculates set/refine
effects.

## Relevant Product Docs

- `docs/spec.md` — "M5.3 — Set Bonus and Refine Rules"
- vltktool: `item_set_bonus_contract.json` (SET_COUNT_EQUIPPED_PIECES /
  SET_HIDDEN_MAGIC_ACTIVATION), `item_refine_formula_contract.json` (REFINE_GEN_NORMAL),
  `run_item_golden_replay.py`

## Acceptance Criteria

- AC1: Golden replay cases exist; tests run; expected stat outcomes match.
- AC2: Rule is stubbed; quality gate runs; stub status is reported.
- AC3: GM changes equipment; preview updates; set/refine effects recalculate.

## Design Notes

- `SetBonusRule` (piece-count threshold activation), `RefineRule` (per-level flat
  bonus), `SetRefineGoldenCase`/`GoldenReplayResult`.
- `SetBonusRefineService` (pure C#): `ComputeTotals` (base + refine*level + active
  set bonuses, recalculated on every call → AC#3), `ReplayGolden` matches expected
  totals (AC#1), `QualityGate` tallies stub/approx/implemented rule statuses (AC#2).

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: base totals, refine per-level, set activation/threshold, equipment-change recompute, golden match/mismatch, quality gate stub/no-stub |
| Integration | Totals drive golden replay (unit-covered) |
| E2E | GM equipment change preview in Play Mode (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Closes Phase M5: set/refine parity primitive with golden-replay proof, anchored to
vltktool set/refine contracts.

## Evidence

EditMode 345/345 pass (docs/evidence/editmode-results-2026-05-31-m5-items.json).
`SetBonusRefineService` + rule/golden types. Suite
`VLTK.Tests.Sandbox.SetBonusRefineServiceTests` (9 tests) covers AC1–AC3.
