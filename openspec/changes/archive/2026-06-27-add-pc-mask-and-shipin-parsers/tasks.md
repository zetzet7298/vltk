# Tasks — add-pc-mask-and-shipin-parsers

## PR-1 — Parser integration

- [x] **T1 (RED)** — Add Equipment-category tests proving current batch loader lacks `mask` and `shipin`, parser classes are absent, and mask `particularType=0` must be preserved.
- [x] **T2 (GREEN)** — Add `PcMaskItemParser` mirroring standard 46-column equipment parser and preserving `itemGenre`, `detailType`, and `particularType` from columns 1/2/3.
- [x] **T3 (GREEN)** — Add `PcShipinItemParser` mirroring standard 46-column equipment parser and preserving `itemGenre`, `detailType`, and `particularType` from columns 1/2/3.
- [x] **T4 (GREEN)** — Add `mask` and `shipin` constants, stems, and parser dispatch to `PcItemBatchLoader`.
- [x] **T5 (GREEN)** — Change `ApplyCategoryIds` so `particularType=0` is preserved for stem `mask`, while existing row-index fallback remains for other stems including `shipin`.
- [x] **T6 (SAMPLES)** — Add `mask_sample.txt` and `shipin_sample.txt` reference files with header + first five PC rows from `settings/item/004`.
- [x] **T7 (TESTS)** — Update `PcItemBatchLoaderTests` from 14 to 16 files and add assertions for mask/shipin counts/importability.
- [x] **T8 (TESTS)** — Add direct parser/ApplyCategoryIds tests to `EquipmentBindingTests` or item-data tests with `[Category("Equipment")]` coverage.
- [x] **T9 (REGRESSION)** — Run Unity EditMode `category_names=["Equipment"]` and record pass/fail evidence.
- [x] **T10 (GATE)** — Run full EditMode suite before push; record baseline failures only, 0 new item parser/equipment failures.
- [x] **T11 (DOC)** — Update apply progress / task checkboxes and note no UI/gameplay changes.
