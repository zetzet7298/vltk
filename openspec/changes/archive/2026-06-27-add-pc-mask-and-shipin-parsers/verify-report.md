# Verify Report — add-pc-mask-and-shipin-parsers

Status: PASS
Date: 2026-06-27
Implementation commit: `c05838469 Add PC mask and shipin item parsers`
Next recommended phase: sync

## Executive Summary

Verified completed OpenSpec change `add-pc-mask-and-shipin-parsers` against proposal/spec/design/tasks/apply-progress and the implementation files requested by the parent. No blockers found.

The implementation matches scope: it adds PC `mask.txt` and `shipin.txt` item-data parsers, wires them into `PcItemBatchLoader`, preserves mask `ParticularType=0`, keeps shipin repeated-zero row-index fallback, adds fast sample files, and adds Equipment-category tests. No UI/gameplay/icon-rendering scope creep was found.

## Structured Status / Action Context Findings

- Active change: `add-pc-mask-and-shipin-parsers` (explicit in parent task).
- Artifact store: OpenSpec files are present under `/var/www/vltk-mobile/openspec/changes/add-pc-mask-and-shipin-parsers/`.
- Workspace/action context: repo-local verification of implementation commit `c05838469`; target files are inside `/var/www/vltk-mobile`.
- Native inherited SDD status was non-authoritative/ambiguous, but the parent task explicitly selected this change and the OpenSpec artifacts were present.
- Implementation ownership proved by `git log -1 --oneline` showing `c05838469 Add PC mask and shipin item parsers` and `git show --name-only c05838469` including the requested implementation/test/artifact files.
- No staged files were present during verification (`git diff --cached --name-only` returned empty). One untracked harness log directory existed and was not part of implementation verification.

## Artifact Coverage

Read and verified:

- `openspec/changes/add-pc-mask-and-shipin-parsers/proposal.md`
- `openspec/changes/add-pc-mask-and-shipin-parsers/spec.md`
- `openspec/changes/add-pc-mask-and-shipin-parsers/design.md`
- `openspec/changes/add-pc-mask-and-shipin-parsers/tasks.md`
- `openspec/changes/add-pc-mask-and-shipin-parsers/apply-progress.md`
- `Assets/Scripts/Sandbox/ItemData/PcItemBatchLoader.cs`
- `Assets/Scripts/Sandbox/ItemData/PcMaskItemParser.cs`
- `Assets/Scripts/Sandbox/ItemData/PcShipinItemParser.cs`
- `Assets/Tests/EditMode/Sandbox/EquipmentBindingTests.cs`
- `Assets/Tests/EditMode/Sandbox/ItemData/PcItemBatchLoaderTests.cs`
- `Assets/StreamingAssets/Reference/PcItem/mask_sample.txt`
- `Assets/StreamingAssets/Reference/PcItem/shipin_sample.txt`

## Spec Coverage

| Spec requirement | Verification result | Evidence |
|---|---:|---|
| PC Mask Item Parser | PASS | `PcMaskItemParser.ParseRow` reads `cols[1]` into `itemGenre`, `cols[2]` into `detailType`, `cols[3]` into `particularType`, and `cols[4]` into `BuildIconSourceId(...)`. Test `PcMaskItemParser_ParseRow_ReadsDetailTypeAndPreservesZeroParticularType` asserts `0/11/0` and icon source. |
| PC Shipin Item Parser | PASS | `PcShipinItemParser.ParseRow` reads `cols[1]`, `cols[2]`, `cols[3]`, and `cols[4]`. Test `PcShipinItemParser_ParseRow_ReadsTrinketDetailType` asserts `itemGenre=0`, `detailType=14`, trinket mapping, and icon source. |
| Batch Loader Includes Mask and Shipin | PASS | `PcItemBatchLoader.CategoryStems` includes `("mask", "mask")` and `("shipin", "shipin")`; dispatch cases call `PcMaskItemParser.ParseFile(path)` and `PcShipinItemParser.ParseFile(path)`. Test `LoadAll_ReadsAllSixteenPcItemFiles` asserts 16 per-file entries and mask/shipin counts. |
| Mask ParticularType Zero Is Preserved | PASS | `ApplyCategoryIds` only row-indexes zero when `stem != "mask"`; test `PcItemBatchLoader_MaskParticularTypeZero_IsPreserved` asserts mask zero remains zero. |
| Shipin Rows Remain Importable Despite Repeated Zero | PASS | Same `ApplyCategoryIds` guard leaves row-index fallback active for `shipin`; test `PcItemBatchLoader_ShipinParticularTypeZero_FallsBackToUniqueRows` asserts values become 1 and 2. Test `ImportInto_IncludesMaskAndShipinItems` resolves `(0,14,1)`. |
| Equipment Test Categorization | PASS | `EquipmentBindingTests` and `PcItemBatchLoaderTests` both have `[TestFixture, Category("Equipment")]`. Parent evidence: `category_names=["Equipment"]` passed 33/33. |

## Task Completion Status

All implementation tasks T1-T11 are checked in `tasks.md`.

Unchecked implementation task markers matching `^\s*- \[ \]`: none found.

Task map:

- T1 RED tests/evidence: PASS — apply-progress records RED compile failure caused by parser-name ambiguity, then rename to `PcMaskItemParser` / `PcShipinItemParser`.
- T2 mask parser: PASS — `PcMaskItemParser.cs` present and implemented.
- T3 shipin parser: PASS — `PcShipinItemParser.cs` present and implemented.
- T4 loader constants/stems/dispatch: PASS — `MaskFile`, `ShipinFile`, stems and switch cases present.
- T5 particularType behavior: PASS — mask zero preserved; shipin fallback unchanged.
- T6 samples: PASS — `mask_sample.txt` and `shipin_sample.txt` present with header + five PC rows.
- T7 batch loader tests: PASS — tests updated from 14 to 16 and assert importability.
- T8 direct parser/ApplyCategoryIds tests: PASS — direct parser and fallback tests in Equipment category.
- T9 focused regression: PASS — parent evidence Equipment 33/33 passed.
- T10 full gate: PASS with baseline disclaimer — parent evidence full EditMode 4102 executed; only known baseline failures, 0 new item parser/equipment failures.
- T11 documentation: PASS — apply-progress and task checkboxes updated.

## Implementation Findings

### Parser classes

PASS:

- `Assets/Scripts/Sandbox/ItemData/PcMaskItemParser.cs`
  - Uses 46-column format (`MinColumns = 46`).
  - Skips header and parses tab-delimited rows.
  - Reads identity columns exactly as designed:
    - `itemGenre = PcItemCommon.Int(cols, 1)`
    - `detailType = PcItemCommon.Int(cols, 2)`
    - `particularType = PcItemCommon.Int(cols, 3)`
  - Reads icon path from column 4 via `PcItemCommon.BuildIconSourceId(PcItemCommon.Str(cols, 4), EvidenceNote)`.
  - Reuses standard stat parsing: `BuildStatDeltas(cols, 13, 34, StatCount, ReqCount, item.refineLevel)`.

- `Assets/Scripts/Sandbox/ItemData/PcShipinItemParser.cs`
  - Same 46-column parser structure.
  - Reads columns 1/2/3/4 as required.
  - Uses separate evidence note `pc_item_004_shipin`.

### Batch loader

PASS:

- `PcItemBatchLoader` declares `MaskFile = "mask.txt"` and `ShipinFile = "shipin.txt"`.
- `CategoryStems` appends mask/shipin after the previous 14 stems.
- `ParseForStem` dispatches:
  - `case "mask": return PcMaskItemParser.ParseFile(path);`
  - `case "shipin": return PcShipinItemParser.ParseFile(path);`
- `ApplyCategoryIds` preserves mask zero and keeps fallback elsewhere:
  - `if (it.particularType == 0 && stem != "mask") it.particularType = i + 1;`
- Detail fallback switch includes `mask => 11` and `shipin => 14` for parser fallback safety.

### Samples

PASS:

- `mask_sample.txt` contains header + five rows; rows show `ItemGenre=0`, `DetailType=11`, and `ParticularType` values `0..4` with mask SPR path `\spr\item\equip\mask\yrmj_01.spr`.
- `shipin_sample.txt` contains header + five rows; rows show `ItemGenre=0`, `DetailType=14`, and repeated `ParticularType=0` with shipin SPR path `\spr\mel\trangsuctanthu.spr`.
- Direct text display through the verification reader shows mojibake/replacement glyphs due reader encoding, but parent Equipment run includes `LoadAll_ItemsHavePositiveItemIdAndNonEmptyNames` and passed, including the `nameRaw.Contains("�")` guard.

### Tests

PASS:

- `EquipmentBindingTests.cs`
  - Class-level `[TestFixture, Category("Equipment")]` present.
  - Direct parser tests cover identity columns and icon source.
  - Direct `ApplyCategoryIds` tests cover mask zero preservation and shipin row-index fallback.
- `PcItemBatchLoaderTests.cs`
  - Class-level `[TestFixture, Category("Equipment")]` present.
  - `LoadAll_ReadsAllSixteenPcItemFiles` asserts 16 per-file keys including mask/shipin and at least five rows each.
  - `ImportInto_IncludesMaskAndShipinItems` asserts runtime importer can resolve mask `(0,11,0)` and shipin `(0,14,1)`.

Assertion quality: adequate. Tests assert concrete parsed fields, tuple importability, category count, row counts, and distinct particularType behavior; no tautological/ghost-loop/type-only/smoke-only assertions found in the changed tests.

## Scope / Review Workload Findings

- Scope matches proposal/spec/design.
- No UI changes found in the requested changed files.
- No equip/unequip/socket gameplay changes found.
- No sprite decoding or item icon rendering changes found; icon work is limited to storing `iconSourceId` and `iconResolved=false` like existing parsers.
- No generic parser refactor or broader architecture churn found.
- `tasks.md` does not include a Review Workload Forecast / chain strategy section; implementation is a single focused parser-integration PR and appears within the declared scope.

## Test / Validation Evidence

Commands/evidence consumed or run during verification:

1. `read openspec/changes/add-pc-mask-and-shipin-parsers/{proposal.md,spec.md,design.md,tasks.md,apply-progress.md}` — passed; artifacts present and coherent.
2. `read Assets/Scripts/Sandbox/ItemData/{PcItemBatchLoader.cs,PcMaskItemParser.cs,PcShipinItemParser.cs}` — passed; code matches design.
3. `read Assets/Tests/EditMode/Sandbox/EquipmentBindingTests.cs` and `read Assets/Tests/EditMode/Sandbox/ItemData/PcItemBatchLoaderTests.cs` — passed; tests cover required behavior and are Equipment-category runnable.
4. `read Assets/StreamingAssets/Reference/PcItem/{mask_sample.txt,shipin_sample.txt}` — passed; sample shape matches required rows.
5. `bash: cd /var/www/vltk-mobile && git status --short && git log -1 --oneline` — passed; latest commit is `c05838469`; only untracked harness log dir seen.
6. `bash: cd /var/www/vltk-mobile && git diff --cached --name-only && git diff --name-only HEAD -- <verified files>` — passed; no staged files and no implementation drift from HEAD for verified files.
7. `bash: static verification script checking parser columns, loader stems/dispatch, mask guard, Equipment categories, test names, and unchecked tasks` — passed all checks.

Parent-collected validation evidence accepted:

- Unity compile: passed after rename from ambiguous `PcMaskParser` / `PcShipinParser` to `PcMaskItemParser` / `PcShipinItemParser`; 0 console errors after rename.
- Focused Unity EditMode Equipment category: 33/33 passed, duration 15.164157s.
- Full Unity EditMode: 4102 tests executed; failures are known baseline areas only, with 0 new item parser/equipment failures.
- Fresh blind-hunter review: APPROVE, no blockers.

This verify executor did not rerun Unity tests because Unity MCP tools were not available in the executor toolset; verification relies on parent-provided Unity evidence plus direct static/file inspection.

## Strict TDD Compliance

Strict TDD is not configured in `openspec/config.yaml` and was not explicitly activated by the parent task. `apply-progress.md` includes TDD evidence: RED compile failure from name ambiguity, GREEN compile after rename, and focused Equipment tests passing 33/33. No strict-TDD blocker applies.

## Baseline Failure Disclaimer

Full EditMode did not produce a clean all-green result, but the reported failures are known baseline/non-scope areas: Backend/Auth, Backend/Predict/Status, BaLang/CaiBang/CombatSkillSlot, InventoryService visual SPR, Mount/MalePlayerVisual, and PcWeaponThief source-path tests. The focused Equipment category passed 33/33 and there is no evidence of new item parser/equipment regression.

## Blockers

None.

## Residual Risks

- Full suite remains red due known baseline failures outside this change; future CI/archive decisions should continue to distinguish those baseline failures from parser/equipment regressions.
- Sample-file text appears mojibake when read with a plain UTF-8-oriented reader, but parser tests passed via project decoding path. If future tooling edits these samples, preserve the original PC/server encoding behavior.
- Existing `VLTK.Sandbox.PcMaskParser` / `PcShipinParser` service parsers share conceptual names with the new item-data parsers; the new `PcMaskItemParser` / `PcShipinItemParser` names avoid the current compile ambiguity, but future contributors should keep the distinction.

## Final Verdict

PASS. The change is verified and ready for the SDD sync phase.
