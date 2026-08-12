# Validation

```bash
cd /var/www/vltk-mobile
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 151,152,153,154,155,156,157,158,159,160,161,162,163,164,165,166,267,365,368,716,1078,1079 --output harness/docs/stories/SKL-WD-PROOF-001/PcWuDangSkills.txt --manifest harness/docs/stories/SKL-WD-PROOF-001/PcWuDangSkills.provenance.json --check
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids 24,25,26,28,29,110,173,175,211,274,340,341,371,738,1107 --output harness/docs/stories/SKL-WD-PROOF-001/PcWuDangRelationshipTargets.txt --manifest harness/docs/stories/SKL-WD-PROOF-001/PcWuDangRelationshipTargets.provenance.json --check
python3 -m pytest scripts/test_generate_wudang_oracle.py scripts/test_audit_skill_coverage.py -q
python3 scripts/generate_wudang_oracle.py --check
python3 scripts/audit_skill_coverage.py --check
```

Static membership only. No UI order, runtime/device, PC golden, formula, child/event execution, visual, or audio parity claim.
