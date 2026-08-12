# Validation

## Contract

`membership-classification.json` and `static-catalog-proof.json` are pinned. Generator
recomputes learned/display sets independently, verifies source/proof/membership hashes and
schemas, runs vltktool `--check` over exact 26-row slice/provenance, validates partition and
relationship-target classification, and requires `ui_order = null`.

## Command

```bash
cd /var/www/vltk-mobile
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 3,4,6,8,9,10,11,12,13,14,15,16,17,18,19,20,21,271,273,318,319,321,709,1055,1056,1057 --output harness/docs/stories/SKL-S-PROOF-001/PcShaolinSkills.txt --manifest harness/docs/stories/SKL-S-PROOF-001/PcShaolinSkills.provenance.json --check
python3 -m py_compile scripts/audit_skill_coverage.py scripts/test_audit_skill_coverage.py
python3 -m pytest scripts/test_audit_skill_coverage.py -q
python3 scripts/audit_skill_coverage.py --check
```

## Scope Caveat

Proof establishes static canonical learned-membership and observed display set only. It does
not prove Unity slot order, runtime formula/projectile behavior, visual/audio behavior,
Android/device, or PC runtime golden parity.
