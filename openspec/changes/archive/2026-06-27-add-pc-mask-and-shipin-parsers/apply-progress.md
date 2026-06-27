# Apply Progress — add-pc-mask-and-shipin-parsers

## Summary

Implemented the parser integration follow-up for PC `mask.txt` and `shipin.txt` without UI or gameplay changes.

## Changes Applied

- Added `PcMaskItemParser` and `PcShipinItemParser` under `Assets/Scripts/Sandbox/ItemData/`.
- Added `mask` and `shipin` stems and parser dispatch to `PcItemBatchLoader`.
- Preserved `particularType=0` for mask rows in `ApplyCategoryIds` because PC mask uses zero as a valid key.
- Kept row-index fallback for shipin rows so repeated PC `ParticularType=0` rows remain importable by tuple.
- Added fast reference samples:
  - `Assets/StreamingAssets/Reference/PcItem/mask_sample.txt`
  - `Assets/StreamingAssets/Reference/PcItem/shipin_sample.txt`
- Updated `PcItemBatchLoaderTests` to expect 16 files and assert mask/shipin importability.
- Added Equipment-category parser/particularType guard tests to `EquipmentBindingTests`.

## TDD Evidence

- RED: first Unity compile after adding tests/new parser names exposed a real compile failure due existing `VLTK.Sandbox.PcMaskParser` / `PcShipinParser` classes. The new item-data parser classes were renamed to `PcMaskItemParser` / `PcShipinItemParser` to avoid ambiguity.
- GREEN: Unity compile passed after rename and dispatch/test updates.
- Focused regression: `run_tests(mode="EditMode", category_names=["Equipment"])` passed **33/33**, 0 failures, duration 15.164157s.

## Parent Verification Evidence

- Fresh read-only review (`blind-hunter`): **APPROVE**, no blockers. Reviewer checked name ambiguity, loader wiring, parser columns, mask/shipin particularType behavior, test coverage, and scope.
- Full EditMode pre-push gate via Unity MCP: **4102 tests executed**. Result state failed only because of the known baseline failures in Backend/Auth, Backend/Predict/Status, BaLang/CaiBang/CombatSkillSlot, InventoryService visual SPR, Mount/MalePlayerVisual, and PcWeaponThief source-path tests. Focused Equipment tests passed 33/33 and no item parser/equipment failures were reported.

## Remaining Gate

- [x] Full EditMode suite before commit/push (shared `PcItemBatchLoader` path changed).

## Non-goals Preserved

- No UI changes.
- No equip/unequip/socket gameplay.
- No sprite decoding/icon rendering changes.
