# Design — add-pc-mask-and-shipin-parsers

## Approach

Use the existing PC item parser pattern rather than introducing a generic parser abstraction. The repository already has one parser class per equipment file (`PcPendantParser`, `PcRingParser`, `PcAmuletParser`, etc.), all using the same 46-column model. Adding `PcMaskItemParser` and `PcShipinItemParser` is the smallest consistent change and avoids refactoring a shared loader path during a focused follow-up.

## Data Model

Both files use the standard 46-column equipment layout:

| Column | Meaning |
|---:|---|
| 0 | name |
| 1 | itemGenre |
| 2 | detailType |
| 3 | particularType |
| 4 | SPR icon/path |
| 13..33 | 7 base stat triples |
| 34..45 | 6 requirement pairs |

`PcItemCommon.BuildStatDeltas` and `PcItemCommon.BuildIconSourceId` are reused.

## Parser Classes

Add:

- `Assets/Scripts/Sandbox/ItemData/PcMaskItemParser.cs`
- `Assets/Scripts/Sandbox/ItemData/PcShipinItemParser.cs`

Each class mirrors `PcPendantParser` but must explicitly populate:

- `itemGenre = PcItemCommon.Int(cols, 1)`
- `detailType = PcItemCommon.Int(cols, 2)`
- `particularType = PcItemCommon.Int(cols, 3)`

This is important because `mask` has valid `particularType=0` and `shipin` uses `detailType=14`.

## Batch Loader Changes

Update `PcItemBatchLoader`:

- Add constants `MaskFile` and `ShipinFile`.
- Append category stems after existing 14 stems:
  - `("mask", "mask")`
  - `("shipin", "shipin")`
- Dispatch parser cases:
  - `mask -> PcMaskItemParser.ParseFile(path)`
  - `shipin -> PcShipinItemParser.ParseFile(path)`
- Update comments from 14-item/12-file wording to include mask/shipin.
- Add fallback detail comments for mask D11 and shipin D14 if parser ever leaves genre/detail zero.

## ParticularType Rule

Current `ApplyCategoryIds` rewrites `particularType=0` to one-based row index. That is safe for many item files and still useful for `shipin`, whose early rows repeat zero. It is unsafe for mask because PC mask uses zero as a valid unique particular type followed by 1, 2, 3...

Rule:

```csharp
if (it.particularType == 0 && stem != "mask")
    it.particularType = i + 1;
```

This preserves mask zero while retaining importer uniqueness for shipin.

## Reference Samples

Add fast sample files under `Assets/StreamingAssets/Reference/PcItem/`:

- `mask_sample.txt`
- `shipin_sample.txt`

Use header + first 5 PC rows from `/var/www/jx-source/Server 6.0/server/home_jxser/server1/settings/item/004/`.

## Tests

Update/add tests under `Assets/Tests/EditMode/Sandbox/` with `[Category("Equipment")]` for new coverage:

- `PcMaskItemParser_ParseFile_ReadsDetailTypeAndPreservesZeroParticularType`
- `PcShipinItemParser_ParseFile_ReadsTrinketDetailType`
- `PcItemBatchLoader_LoadAll_ReadsAllSixteenPcItemFiles`
- `PcItemBatchLoader_MaskParticularTypeZero_IsPreserved`
- `PcItemBatchLoader_ShipinParticularTypeZero_FallsBackToUniqueRows`
- `PcItemBatchLoader_ImportInto_IncludesMaskAndShipinItems`

## Validation

- Focused dev loop: Unity EditMode `category_names=["Equipment"]`.
- Pre-push gate: full EditMode suite; baseline failures outside Equipment/CharacterInfo/item parser paths are acceptable if unchanged.

## Non-goals

- No UI changes.
- No equip/unequip/socket gameplay.
- No SPR decoding or icon rendering.
- No generic parser refactor.
