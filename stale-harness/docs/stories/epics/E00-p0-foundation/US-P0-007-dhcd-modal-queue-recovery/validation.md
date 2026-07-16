# Validation

## Proof Strategy

`scripts/verify-us-p0-007.sh` checks the fail-closed modal evidence and canonical native mapper inputs; it never approves FIFO, input lock, or pause by name alone.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Evidence anchors, hashes, and explicit non-claims. |
| Integration | Native branch and player-data response mapping. |
| E2E | Pending authorized modal round trip. |
| Platform | Android ARM64 and client UI artifacts. |
| Performance | Not applicable. |
| Logs/Audit | Trace, dependency edge, and failed-method log. |

## Fixtures

Hash-locked controller, metadata, declarations, and reconstructed client evidence.

## Commands

```text
scripts/verify-us-p0-007.sh
scripts/bin/harness-cli story verify US-P0-007
scripts/bin/harness-cli story verify-all
scripts/bin/harness-cli audit
git diff --check
```

## Acceptance Evidence

- Role-keyed branch ordering is bounded and reproducible.
- FIFO, input lock, pause, and cross-player serialization remain unresolved without stronger proof.
