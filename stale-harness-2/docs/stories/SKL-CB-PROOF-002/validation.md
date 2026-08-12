# Validation

## Acceptance

- Oracle generation deterministic và `--check` pass.
- Slice SHA-256 và oracle SHA-256 được pin trong EditMode test.
- Root order đúng player panel, đủ 26/26, không lẫn support/NPC variants.
- Production catalog match mọi populated canonical static field và hai Lua collide relationships.
- Existing Cái Bang suite tiếp tục pass.
- Independent proof-auditor không tìm thấy circular expected derivation.

## Commands

```text
cd /var/www/vltk-mobile
python3 scripts/generate_caibang_oracle.py --check
python3 scripts/compile_scripts.py
bash scripts/run_caibang_parity_tests.sh
```

## Evidence

- `python3 scripts/generate_caibang_oracle.py --check`: pass; canonical PC source hashes verified, 26 roots, oracle SHA-256 `91d3251aef30f755f3480a2104a48227eaffd8e7ea8fbf495d189dd185ed84da`.
- `python3 scripts/compile_scripts.py`: pass; only pre-existing unrelated CS1998 warning remains.
- `bash scripts/run_caibang_parity_tests.sh`: pass; `138 total, 138 passed, 0 failed, 0 skipped`.
- Oracle test also pins `Resources/Reference/PcCaiBangSkills.bytes` to the same slice SHA and checks SkillTree order equals player-panel order.
- Herdr proof-auditor: no circular oracle finding; fixed/covered canonical source hash binding, packaged slice hash, explicit `missileForm=0` fallback for 357/359, and stale SkillTree Cái Bang order.

## Residual Gap

Oracle này chứng minh static catalog shape + relationship anchors. Runtime curves, projectile lifecycle, visual/audio asset bytes và PC runtime golden tiếp tục do fixtures/story khác chứng minh; không tự nâng toàn bộ Cái Bang lên universal `PARITY_DONE`.
