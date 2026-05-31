# US-M51-001 M5.1 Item Contract Import

## Status

implemented

## Lane

normal

## Intake

Intake #20 (spec_slice, normal). Flags: Existing behavior, External artifact.

## Product Contract

Item contract outputs from `/var/www/vltktool` import into Unity so item data
starts from existing validated artifacts. The import creates/updates item
definitions, surfaces a quality-gate status in the GM Tools tab, and respects
strict mode for stubbed contract rules.

## Relevant Product Docs

- `docs/spec.md` — "M5.1 — Item Contract Import"
- vltktool: `generate_item_contract_bundle.py`, `generated/item_stat_contract.json`,
  `item_set_bonus_contract.json`, `item_refine_formula_contract.json`

## Acceptance Criteria

- AC1: Contract bundle generated; Unity import runs; item definitions created/updated.
- AC2: Quality gate report exists; import completes; gate status is visible in GM Tools tab.
- AC3: Contract has stubbed rules; strict mode enabled; import fails or marks warning
  according to config.

## Design Notes

- `ItemDefinition` model grounded in the contract (stat deltas by attr_code + stage,
  setId, refineLevel, icon ref) + `ContractRule`/`ItemQualityGateReport`.
- `ItemContractImporter` (pure C#): upsert items (AC#1), tally rule statuses into a
  gate report (AC#2), strict mode fails on stubs (AC#3), `ResolveIcons` via registry.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode tests: create/update, null bundle, sum-attr, rule tally, strict/non-strict stub, icon resolve |
| Integration | Icon resolution via AssetRegistry (unit-covered) |
| E2E | Live bundle import + GM Tools gate (documented; not automated in EditMode) |
| Platform | N/A |
| Release | N/A |

## Harness Delta

Item data primitive consumed by M5.2 inventory + M5.3 set/refine.

## Evidence

EditMode 345/345 pass (docs/evidence/editmode-results-2026-05-31-m5-items.json).
`ItemDefinition` + `ItemContractImporter`. Suite
`VLTK.Tests.Sandbox.ItemContractImporterTests` (8 tests) covers AC1–AC3.
