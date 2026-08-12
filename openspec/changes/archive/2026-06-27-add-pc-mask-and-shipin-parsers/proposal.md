# Proposal — add-pc-mask-and-shipin-parsers

## Summary

Add runtime PC item-data parsing for `mask.txt` and `shipin.txt` so the accessory equipment slots introduced by `bind-accessory-equipment-slots` can be populated from PC source data.

This is a technical follow-up to the archived `bind-accessory-equipment-slots` change. That change added canonical `EquipSlot.Mask`, `EquipSlot.Trinket`, and related UI binding, but deliberately left `PcMaskItemParser` / `PcShipinItemParser` out of scope. Without this follow-up, Character Info can render mask/trinket slots but the runtime item DB has no parsed mask/shipin items to equip later.

## Motivation

- PC source has mask accessories in `settings/item/004/mask.txt` with `DetailType=11`.
- PC source has shipin/trinket accessories in `settings/item/004/shipin.txt` with `DetailType=14`.
- Canonical equipment-binding spec already maps `11 -> PcItemCategory.Mask` and `14 -> PcItemCategory.Trinket`.
- `PcItemBatchLoader` currently loads 14 stems and omits `mask` / `shipin`.

## Grounding

- Existing loader: `Assets/Scripts/Sandbox/ItemData/PcItemBatchLoader.cs`
- Existing similar parsers: `PcPendantParser`, `PcRingParser`, `PcAmuletParser`
- PC source:
  - `/var/www/jx-pc/Server 6.0/server/home_jxser/server1/settings/item/004/mask.txt`
  - `/var/www/jx-pc/Server 6.0/server/home_jxser/server1/settings/item/004/shipin.txt`
- Scout report: `harness/harness/planning/scout-mask-shipin-parsers.md`
- Exa research: Unity/CSV parser practice supports header-aware tab-delimited parsing and integration tests.
- DeepWiki repo query was unavailable because the GitHub repo is not indexed.

## Scope

### In Scope

- Add mask/shipin parser support using the established 46-column PC item parser pattern.
- Add `mask` and `shipin` to `PcItemBatchLoader` category stems and dispatch.
- Ensure `mask` preserves valid `ParticularType=0` rows instead of overriding them to row index.
- Add reference sample files for fast EditMode tests.
- Add focused EditMode tests under category `Equipment`.

### Out of Scope

- No UI changes.
- No equip/unequip/socket gameplay.
- No sprite decoding or resource resolver changes.
- No item icon rendering changes.
- No changes to archived `bind-accessory-equipment-slots` artifacts.

## Risks

- `mask.txt` uses `ParticularType=0` as a valid key. The current fallback behavior in `ApplyCategoryIds` overrides zero to row index, which would collide with the second mask row (`ParticularType=1`). This change must preserve mask zero.
- `shipin.txt` sample rows have repeated `ParticularType=0`; the current fallback behavior is useful there to keep importer keys unique.
- Full EditMode has known baseline failures unrelated to item parser changes; focused `Equipment` tests are the green gate, with full suite used as pre-push regression evidence.
