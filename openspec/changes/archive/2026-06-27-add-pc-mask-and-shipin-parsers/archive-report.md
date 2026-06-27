# Archive Report — add-pc-mask-and-shipin-parsers

## Status: ARCHIVED

## Change

`add-pc-mask-and-shipin-parsers` — runtime parsers for PC `mask.txt` and `shipin.txt` so the Mask and Trinket accessory slots have parsed `ItemDefinition` data.

## Lifecycle

- proposal → spec → design → tasks → apply → verify (PASS) → sync (SYNCED) → archive

## Commits

- Implementation: `c05838469 Add PC mask and shipin item parsers`
- Verify: `64a5b0188 Verify mask and shipin parser change`
- Sync: `dd45a0f6b Sync mask and shipin parser change`

## Canonical Spec

Merged into `openspec/specs/equipment-binding/spec.md`:
- Requirement: PC Mask Item Parser
- Requirement: PC Shipin Item Parser
- Requirement: Batch Loader Includes Mask and Shipin
- Requirement: Mask ParticularType Zero Is Preserved
- Requirement: Shipin Rows Remain Importable Despite Repeated Zero
- Requirement: Test Categorization and Run Discipline

## Verification Evidence

- Unity compile: 0 errors after rename to `PcMaskItemParser` / `PcShipinItemParser`.
- Equipment category: 33/33 passed.
- Full EditMode: 4102 executed; only known baseline failures, 0 new item parser/equipment failures.
- Fresh review (blind-hunter): APPROVE, no blockers.

## Archive Location

`openspec/changes/archive/2026-06-27-add-pc-mask-and-shipin-parsers/`
