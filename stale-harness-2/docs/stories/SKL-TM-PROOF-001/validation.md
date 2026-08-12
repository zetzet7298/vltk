# Validation

## Proof Strategy

The generator must be independent of Unity code and deterministic. Before
catalog comparison, it must reproduce the reviewed 23-ID progression/skillbook
union, 16 PC-only IDs and Unity-only unresolved `51,55,57` from canonical
sources, without converting missing source cells to defaults.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Oracle determinism, source/oracle hash, membership diff and populated fields |
| Integration | Catalog match, child/event edges, `58 -> 227` |
| E2E | Not in scope for static proof |
| Platform | Android smoke is a later gate |
| Performance | Not in scope |
| Logs/Audit | Harness evidence and independent review |

## Fixtures

`membership-classification.json`, exact TangMen `skills.txt` rows, progression
and skillbook grants, and the canonical TangMen Lua relationship slice; no
implementation-derived expected fixture.

The static fixture and provenance are:

- `Assets/StreamingAssets/Reference/PcTangMenSkills.txt`
- `Assets/StreamingAssets/Reference/PcTangMenSkills.provenance.json`
- `Assets/StreamingAssets/Reference/PcTangMenRelationshipTargets.txt`
- `Assets/StreamingAssets/Reference/PcTangMenRelationshipTargets.provenance.json`
- `Assets/StreamingAssets/Reference/PcTangMenOracle.json`

## Commands

```text
python3 -m pytest scripts/test_generate_tangmen_oracle.py -q
python3 scripts/generate_tangmen_oracle.py --check
python3 scripts/compile_scripts.py
bash scripts/run_tangmen_parity_tests.sh
```

## Acceptance Evidence

Required before completion: reviewed membership/classification, source/oracle
hashes, catalog comparison pass, relationship pass, existing fixture regression
pass, and independent proof review. This does not establish universal parity.

Completed evidence is recorded in `validation-report.md`: 14 Python tests pass;
oracle and Unity compile checks pass; the final TangMen/progression regression
selection passes 94/94 with zero skipped; the catalog-wide selection passes
43/43. Oracle SHA-256 is
`e4270bd12a534b229c962c3fc322a9271aaefc6b99d062e3df0711a5b0f84f89`.
