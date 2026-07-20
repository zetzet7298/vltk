# Validation

## Independent gates

`test_generate_emei_oracle.py` independently asserts EMei learned/display partition, 90 exclusion,
23-target closure, null UI order, generator determinism, and stale source/target slice rejection.

`test_audit_skill_coverage.py` independently recomputes every faction. It keeps EMei baseline
probe `gap=14`, then requires completed-wave exclusion before rank recomputes to TianRen `gap=12`.

## Commands

```bash
cd /var/www/vltk-mobile
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 77,79,80,81,82,83,84,85,86,87,88,89,91,92,93,252,282,328,332,380,385,712,1061,1062,1114 --output harness/docs/stories/SKL-EM-PROOF-001/PcEMeiSkills.txt --manifest harness/docs/stories/SKL-EM-PROOF-001/PcEMeiSkills.provenance.json --check
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 2,3,4,5,68,101,142,186,191,206,207,208,243,281,323,324,329,331,333,375,718,1089,1115 --output harness/docs/stories/SKL-EM-PROOF-001/PcEMeiRelationshipTargets.txt --manifest harness/docs/stories/SKL-EM-PROOF-001/PcEMeiRelationshipTargets.provenance.json --check
python3 -m py_compile scripts/generate_emei_oracle.py scripts/test_generate_emei_oracle.py scripts/audit_skill_coverage.py scripts/test_audit_skill_coverage.py
python3 -m pytest scripts/test_generate_emei_oracle.py scripts/test_audit_skill_coverage.py -q
python3 scripts/generate_emei_oracle.py --check
python3 scripts/audit_skill_coverage.py --check
```

## Limits

Static learned-membership only. No UI ordering, Unity factory/runtime, PC runtime golden, Android/device, visual/audio, child/event execution, or platform parity claim.
