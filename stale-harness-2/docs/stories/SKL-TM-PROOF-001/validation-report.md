# Validation Report

Date: 2026-07-17

## Scope

`SKL-TM-PROOF-001` and blocker `SKL-TM-CATALOG-001`, against base revision
`ddf637352aa1b0d00c6ceeb2d48e36e2127d23b4` plus the current dirty worktree.
This report proves bounded static membership/catalog parity only.

## Commands Run

```text
python3 /home/zet/Projects/vltktool/extract_table_slice.py --input /var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt --key-column SkillId --ids <32-direct-target-ids> --output Assets/StreamingAssets/Reference/PcTangMenRelationshipTargets.txt --manifest Assets/StreamingAssets/Reference/PcTangMenRelationshipTargets.provenance.json --check
python3 -m py_compile scripts/generate_tangmen_oracle.py scripts/test_generate_tangmen_oracle.py
python3 -m pytest scripts/test_generate_tangmen_oracle.py -q
python3 scripts/generate_tangmen_oracle.py --check
python3 scripts/compile_scripts.py
python3 scripts/run_sandbox_tests.py VLTK.Tests.Sandbox.TangMenCanonicalOracleParityTests VLTK.Tests.Sandbox.TangMenSkillPanelTests VLTK.Tests.Sandbox.AllFactionsCombatParityTests VLTK.Tests.Sandbox.CombatSkillSlotTests VLTK.Tests.Sandbox.SkillLevelUpgradeServiceTests
python3 scripts/run_sandbox_tests.py VLTK.Tests.Sandbox.TangMenCanonicalOracleParityTests VLTK.Tests.Sandbox.PcSkillCatalogParityTests VLTK.Tests.Sandbox.CombatSkillSlotTests VLTK.Tests.Sandbox.SkillCatalogTests
```

## Results

| Check | Result | Notes |
| --- | --- | --- |
| vltktool target slice | pass | 32 IDs; byte-exact `--check` |
| Python compile/unit | pass | 14 passed |
| Oracle check | pass | 23 learned, 32 targets |
| Unity compile | pass | Existing unrelated warnings only |
| Final progression/catalog selection | pass | Job `e2b85da623ca409c83cf036638dd273e`: 94 passed, 0 failed, 0 skipped |
| Catalog-wide consumer selection | pass | Job `2fe2fd54e8d841569965672516f1fbfe`: 43 passed, 0 failed, 0 skipped |
| Independent proof review | pass after fix | Initial NO-GO found `51/55/57` in learned state; final code/tests remove that promotion |

## Evidence

- Learned slice SHA-256:
  `e4a6657ccfd87be51e5404143df81ce60a022fbbd17303cb9c9c1c59841108ad`
- Relationship-target slice SHA-256:
  `888c93cde48ec22160e12386580bca3aafc2b74d5bc16ba21b70c06a9a8007ba`
- Oracle SHA-256:
  `e4270bd12a534b229c962c3fc322a9271aaefc6b99d062e3df0711a5b0f84f89`
- Membership: 23 PC-learned IDs; `51,55,57` unresolved/display-only.
- UI order: deliberately unproven; oracle value remains null.

## Gaps

- The sixteen new learned definitions and 32 support targets have static/catalog
  proof only; runtime magic, projectile timing and behavior remain separate work.
- `missileForm=13`, `Series.Wood`, target-chain behavior beginning `58 -> 227`,
  UI ordering, assets/audio and Android/device smoke are not proven here.
