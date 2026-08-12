# Validation

```bash
cd /var/www/vltk-mobile
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 23,24,26,29,30,31,32,33,34,35,36,37,40,41,42,322,323,324,325,708,1058,1059,1060 --output harness/docs/stories/SKL-TW-PROOF-001/PcTianWangSkills.txt --manifest harness/docs/stories/SKL-TW-PROOF-001/PcTianWangSkills.provenance.json --check
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 219,220,221,222,224,225,326,327,404,405,406,407,408,1084,1087,1088 --output harness/docs/stories/SKL-TW-PROOF-001/PcTianWangRelationshipTargets.txt --manifest harness/docs/stories/SKL-TW-PROOF-001/PcTianWangRelationshipTargets.provenance.json --check
python3 -m pytest scripts/test_generate_tianwang_oracle.py scripts/test_audit_skill_coverage.py -q
python3 scripts/generate_tianwang_oracle.py --check
python3 scripts/audit_skill_coverage.py --check
```

Static membership only. No UI order, runtime/device, PC golden, formula, child/event execution, visual, or audio parity claim.
