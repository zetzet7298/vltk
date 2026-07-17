# Validation

## Proof Strategy

The frozen oracle stays independent of Unity. Production must first equal the
23-ID learned set; only then may static fields and direct relationships be
compared. `51,55,57` remain excluded, `uiOrder` remains null, and all 32 direct
targets must resolve without becoming learned skills.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Oracle/slice hashes, 23-ID exact membership, populated fields |
| Integration | Direct relationships, target closure, `58 -> 227` |
| E2E | Not in scope |
| Platform | Compile/EditMode only; Android smoke is later |
| Performance | Not in scope |
| Logs/Audit | Detailed Harness trace and independent review |

## Fixtures

- `Assets/StreamingAssets/Reference/PcTangMenOracle.json`
- `Assets/StreamingAssets/Reference/PcTangMenSkills.txt`
- `Assets/StreamingAssets/Reference/PcTangMenRelationshipTargets.txt`

## Commands

```text
python3 -m pytest scripts/test_generate_tangmen_oracle.py -q
python3 scripts/generate_tangmen_oracle.py --check
python3 scripts/compile_scripts.py
bash scripts/run_tangmen_parity_tests.sh
```

## Acceptance Evidence

Required before completion: oracle generator checks pass; Unity compiles; all
four `TangMenCanonicalOracleParityTests` pass without `Assume` skips; the exact
learned set is 23 IDs; `51,55,57` remain excluded; all populated fields and all
32 direct targets match/resolve; independent review confirms no Unity-derived
expected values.

Completed: see `../SKL-TM-PROOF-001/validation-report.md`. The final production
state separates the 23 learned IDs from legacy display-only `51,55,57`; the
three residuals are absent from `knownSkills`, `skillLevels`, upgrade and
`MaxAll` state while remaining visible in the explicit ten-row panel contract.
