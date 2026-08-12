# Validation Report

Date: 2026-07-17

## Scope

Static learned-membership classification, vltktool slices/provenance,
relationship-target closure, deterministic oracle, production catalog overlay và
progression-selection boundary của `SKL-KL-PROOF-001`. Runtime combat và platform
golden vẫn ngoài report này.

## Commands Run

```text
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids <24-learned-ids> --output Assets/StreamingAssets/Reference/PcKunLunSkills.txt --manifest Assets/StreamingAssets/Reference/PcKunLunSkills.provenance.json --check
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids <17-relationship-target-ids> --output Assets/StreamingAssets/Reference/PcKunLunRelationshipTargets.txt --manifest Assets/StreamingAssets/Reference/PcKunLunRelationshipTargets.provenance.json --check
python3 -m py_compile scripts/generate_kunlun_oracle.py scripts/test_generate_kunlun_oracle.py
python3 -m pytest scripts/test_audit_skill_coverage.py scripts/test_generate_kunlun_oracle.py -q
python3 scripts/audit_skill_coverage.py --check
python3 scripts/generate_kunlun_oracle.py --check
python3 scripts/compile_scripts.py
bash scripts/run_kunlun_parity_tests.sh
```

## Results

| Check | Result | Notes |
| --- | --- | --- |
| vltktool learned slice | pass | 24 IDs; byte-exact provenance/check |
| vltktool relationship slice | pass | 17 targets; 3 learned overlaps + 14 support-only |
| Python inventory/oracle unit | pass | 35 passed |
| Inventory check | pass | union 245; current pre-exclusion winner Côn Luân; SHA `175704e0...` |
| Oracle check | pass | deterministic SHA pinned |
| Byte identity | pass | second oracle write unchanged |
| Independent static audit | pass | no circular expected values or target promotion |
| Unity compile | pass | exit 0; only pre-existing warnings |
| Focused Unity EditMode | pass | job `cd28be5d69d34946bd74617567f31ac2`; 138 passed, 0 failed, 0 skipped |
| Independent final proof audit | GO | static/catalog boundary; no P0/P1 finding |

## Evidence

- Learned IDs (24): `90,167,168,169,171,172,173,174,175,176,178,179,181,182,275,372,375,392,393,394,630,717,1080,1081`.
- Partition: 13 shared, 11 PC-only, 5 Unity-only unresolved (`170,177,180,183,184`).
- Relationship targets (17): `14,15,16,17,18,19,20,21,22,178,181,290,342,372,387,399,1109`.
- Learned overlaps: `178,181,372`; each có progression/skillbook evidence riêng.
- Support-only targets: 14 IDs còn lại; không được promote vào learned membership.
- Learned slice SHA-256: `34f7aef196656c44e9461d5e75960bb940c8be7c4e68ce12af644c289247236c`.
- Target slice SHA-256: `d136e0be557a5055aa27163b26842166dc097f2d903a6e0911ae055d22b79e3b`.
- Oracle SHA-256: `3be6712946489b82d2595eae77894bcf022f0b6cd4d43977850572c700be399f`.
- `uiOrder = null`; encoded `skills.txt` chỉ được evidence qua vltktool provenance.
- Production learned membership đúng exact 24 roots; năm residual chỉ phục vụ observed
  display set và không được Grant/MaxAll/upgrade promote.
- Cả 17 relationship target resolve; 14 support-only không bị promote, ba overlap
  `178,181,372` vẫn là learned roots.
- Canonical predicate được dùng bởi cả Grant và MaxAll; MaxAll chỉ max faction hiện tại
  cộng universal action, test trực tiếp giữ nguyên Đường Môn skill 54 ngoại phái.
- Frozen quick wins: Côn Luân 172 radius `360`, start skill `399`; Đường Môn 54
  missile form `6` (`Stance`).

## Gaps

- Runtime formula/projectile/visual/audio, Android/device và PC runtime golden ngoài
  static-proof scope; không có parity claim cho các lane này.
- `uiOrder` vẫn null, do đó chỉ chứng minh observed display set 18 rows, không chứng
  minh PC slot/UI ordering.
