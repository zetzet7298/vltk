# Validation

## Proof Strategy

Membership expected phải được recompute từ canonical progression + skillbook trước
khi đọc Unity catalog. Static expected chỉ đến từ byte-preserving `vltktool` slices.
Oracle phải pin hashes, giữ `uiOrder = null`, resolve direct targets và fail khi
artifact/source drift.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Pinned source/slice/provenance hashes; exact 24 learned; 13/11/5 partition; deterministic oracle |
| Integration | Populated static fields; direct relationship targets resolve; no support/residual target promoted; MaxAll không chạm skill ngoại phái |
| E2E | Không trong proof phase |
| Platform | Unity compile/EditMode; Android smoke để epic residual |
| Performance | Không áp dụng |
| Logs/Audit | Detailed Harness trace + independent proof-auditor |

## Fixtures

- `membership-classification.json` trong story folder.
- `Assets/StreamingAssets/Reference/PcKunLunSkills.txt` + provenance.
- `Assets/StreamingAssets/Reference/PcKunLunOracle.json` + pinned SHA.
- Relationship-target slice/provenance cho 17 direct target IDs.

## Commands

```text
python3 -m pytest scripts/test_audit_skill_coverage.py scripts/test_generate_kunlun_oracle.py -q
python3 scripts/audit_skill_coverage.py --check
python3 scripts/generate_kunlun_oracle.py --check
python3 scripts/compile_scripts.py
bash scripts/run_kunlun_parity_tests.sh
```

## Acceptance Evidence

Static/catalog proof đã pass: exact vltktool `--check` cho learned/target slices;
35/35 Python inventory/oracle tests; deterministic inventory/oracle checks; Unity compile;
focused EditMode job `cd28be5d69d34946bd74617567f31ac2` đạt 138/138; deterministic oracle SHA-256
`3be6712946489b82d2595eae77894bcf022f0b6cd4d43977850572c700be399f`;
independent proof-auditor GO và không tìm thấy circular membership/relationship
promotion. Xem `validation-report.md`.

Acceptance của static catalog/progression-selection boundary đã đủ. Runtime formula,
projectile, visual/audio, Android/device và PC runtime golden vẫn là residual của epic,
không được suy diễn là đã parity từ story này.
