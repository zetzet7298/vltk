# Validation

## Proof Strategy

`scripts/verify-us-p0-005.sh` locks the evidence inputs and asserts the bounded native mapping while keeping global pause, local input, and timer semantics unresolved until runtime/scope proof exists.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Evidence anchors, hashes, and negative assertions. |
| Integration | Native/ISIL mapping and caller/callee traces. |
| E2E | Pending authorized runtime trace. |
| Platform | Android ARM64 artifact identity. |
| Performance | Not applicable. |
| Logs/Audit | Detailed Harness trace and failed-method boundary. |

## Fixtures

Hash-locked Android 1.304 native/metadata and generated ISIL inputs in the evidence card.

## Commands

```text
scripts/verify-us-p0-005.sh
scripts/bin/harness-cli story verify US-P0-005
scripts/bin/harness-cli story verify-all
scripts/bin/harness-cli audit
git diff --check
```

## Acceptance Evidence

- No global pause, input-lock, or timer parity claim is emitted without scope/runtime proof.
- `R-DHCD-003` remains unresolved while Quick/global/timer/input behavior is not closed.
