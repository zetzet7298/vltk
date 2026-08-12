# Validation

## Proof Strategy

`scripts/verify-us-p0-008.sh` checks the blocked evidence card, canonical inputs, and candidate-only wording. It fails if a reward constant is presented as recovered.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Candidate hashes, evidence anchors, and blocked assertions. |
| Integration | VFS/key/parser and native `AddExp`/`TestRate` mapping. |
| E2E | Pending authorized selected-bundle runtime capture. |
| Platform | Android ARM64 AssetBundle path. |
| Performance | Not applicable. |
| Logs/Audit | Detailed trace and failed-decoder output. |

## Fixtures

The listed candidate files and index artifacts from `r-dhcd-006-drop-xp.md`.

## Commands

```text
scripts/verify-us-p0-008.sh
scripts/bin/harness-cli story verify US-P0-008
scripts/bin/harness-cli story verify-all
scripts/bin/harness-cli audit
git diff --check
```

## Acceptance Evidence

- Candidate files remain explicitly non-active.
- No XP/drop constant or formula is approved without selected bytes and native/runtime proof.
