# Validation

```bash
cd /var/www/vltk-mobile
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 95,97,99,100,101,102,103,105,108,109,111,113,114,269,336,337,713,1063,1065 --output harness/docs/stories/SKL-CY-PROOF-001/PcCuiYanSkills.txt --manifest harness/docs/stories/SKL-CY-PROOF-001/PcCuiYanSkills.provenance.json --check
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 6,7,8,9,10,12,111,112,146,147,326,327,338,398,1064,1093,1102 --output harness/docs/stories/SKL-CY-PROOF-001/PcCuiYanRelationshipTargets.txt --manifest harness/docs/stories/SKL-CY-PROOF-001/PcCuiYanRelationshipTargets.provenance.json --check
python3 -m pytest scripts/test_generate_cuiyan_oracle.py scripts/test_audit_skill_coverage.py -q
python3 scripts/generate_cuiyan_oracle.py --check
python3 scripts/audit_skill_coverage.py --check
git diff --check
```

Static membership only. No UI order, runtime/device, PC golden, formula, child/event execution, visual, audio, factory, or platform parity claim.
