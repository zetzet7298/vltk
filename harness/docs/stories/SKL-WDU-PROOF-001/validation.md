# Validation

```bash
cd /var/www/vltk-mobile
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 60,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,353,355,356,384,390,711,1066,1067 --output harness/docs/stories/SKL-WDU-PROOF-001/PcWuDuSkills.txt --manifest harness/docs/stories/SKL-WDU-PROOF-001/PcWuDuSkills.provenance.json --check
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 20,30,31,32,33,34,163,165,190,203,328,329,354,383,1094,1095 --output harness/docs/stories/SKL-WDU-PROOF-001/PcWuDuRelationshipTargets.txt --manifest harness/docs/stories/SKL-WDU-PROOF-001/PcWuDuRelationshipTargets.provenance.json --check
python3 -m pytest scripts/test_generate_wudu_oracle.py scripts/test_audit_skill_coverage.py -q
python3 scripts/generate_wudu_oracle.py --check
python3 scripts/audit_skill_coverage.py --check
```

Static membership only. No UI order, runtime/device, PC golden, formula, child/event execution, visual, or audio parity claim.
