# Validation

```bash
cd /var/www/vltk-mobile
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 131,132,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,361,362,364,391,715,1075,1076 --output harness/docs/stories/SKL-TR-PROOF-001/PcTianRenSkills.txt --manifest harness/docs/stories/SKL-TR-PROOF-001/PcTianRenSkills.provenance.json --check
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 20,54,55,56,57,58,69,169,171,192,337,363,366,723,1131 --output harness/docs/stories/SKL-TR-PROOF-001/PcTianRenRelationshipTargets.txt --manifest harness/docs/stories/SKL-TR-PROOF-001/PcTianRenRelationshipTargets.provenance.json --check
python3 -m pytest scripts/test_generate_tianren_oracle.py scripts/test_audit_skill_coverage.py -q
python3 scripts/generate_tianren_oracle.py --check
python3 scripts/audit_skill_coverage.py --check
```

Static membership only. No UI order, runtime/device, PC golden, formula, child/event execution, visual, or audio parity claim.
