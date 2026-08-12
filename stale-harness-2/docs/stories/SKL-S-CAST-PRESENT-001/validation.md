# Validation

## Focused proof

`ShaolinCastPresentationParityTests.Catalog_ShaolinCastPresentation_MatchesPinnedSlice`
asserts exact `CharAnimId`, `WaitTime`, canonical male cast SFX, empty/`0` female cast
SFX, and signed GB2312 PreCast SPR UIDs for all existing factory rows.

`ShaolinCastPresentationParityTests.Catalog_ShaolinLearnedOnlyRoots_RemainUnregisteredUntilCatalogEvidenceExists`
asserts fail-closed behavior for learned-only roots outside current factory coverage.

## Check commands

```bash
cd /var/www/vltk-mobile
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 3,4,6,8,9,10,11,12,13,14,15,16,17,18,19,20,21,271,273,318,319,321,709,1055,1056,1057 --output harness/docs/stories/SKL-S-PROOF-001/PcShaolinSkills.txt --manifest harness/docs/stories/SKL-S-PROOF-001/PcShaolinSkills.provenance.json --check
# Unity EditMode filter: ShaolinCastPresentationParityTests
```

## Limits

No CLIENTACTION weapon/blend mapping, packaged SPR availability, device validation, PC runtime golden, runtime formula, or visual/audio parity claim.
