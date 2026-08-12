# Validation

## Proof Strategy

The story is complete when the high-risk packet and durable Harness story record
exist, identify `REQ-P0-001`, and state a complete, fail-closed evidence schema.
`scripts/verify-us-p0-001.sh` is packet-contract proof: it checks the four
packet files for required literal anchors. `harness-cli query dependencies` is
graph proof for the durable dependency edges. It cannot prove a selected asset,
runtime port, legal clearance, or pilot release.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Not applicable: no runtime code is introduced. |
| Integration | Harness story registration shows `US-P0-001` with high-risk lane and its `REQ-P0-001` contract, plus exactly the direct blocker edges to `US-P0-002`, `US-P0-003`, `US-P0-004`, `US-P0-005`, `US-P0-006`, `US-P0-007`, and `US-P0-008`. |
| E2E | Not applicable: no runtime, pilot, or distribution flow is introduced. |
| Platform | Confirm packet explicitly declares no Unity/server/platform change. |
| Performance | Not applicable: no executable behavior is introduced. |
| Logs/Audit | Confirm schema includes source path, pack/load order, UID, encoding/path bytes, byte count, SHA-256, decode, `name_vi`, reviewer, and timestamp. |

## Negative Validation

- Reject a record that selects a candidate without complete candidate enumeration.
- Reject a record that chooses a winner from mtime without equivalent
  package-version and active-load-order evidence.
- Reject missing or guessed `hash_uid`, `encoding`, normalized path bytes,
  `name_vi` cross-check, decode result, byte count, SHA-256, reviewer, or
  timestamp when the field applies.
- Reject an asset record that claims `verified` with an unresolved conflict,
  absent resolver evidence, or absent decode evidence.
- Reject vendoring bytes before selection and actual use.
- Reject any claim that an internal evidence record clears public distribution.
- Reject runtime-port, legal-clearance, or selected-candidate claims from this
  story's evidence alone.

## Fixtures

No asset fixture is introduced. Future evidence work must use exact canonical
source paths and exact resolver/decode outputs; fabricated paths, hashes, UIDs,
labels, or bytes are forbidden.

## Commands

```text
scripts/verify-us-p0-001.sh
HARNESS_DB_PATH=/var/www/vltk-mobile/harness/harness.db harness/scripts/bin/harness-cli query dependencies --json
scripts/bin/harness-cli story verify US-P0-001
```

## Acceptance Evidence

- Packet documents the `REQ-P0-001` contract, `OBJ-P0-02/04`, `DOC-GOV-02`,
  `DOC-JX-05`, `DOC-JX-08`, `B-EVIDENCE-001`, and `B-LEGAL-001`.
- Packet contains the exact required provenance/review fields and stop
  conditions.
- Harness contains `US-P0-001` as a high-risk story whose verifier is
  `scripts/verify-us-p0-001.sh`.
- Harness contains exactly these direct downstream blocker edges:
  `US-P0-001 -> US-P0-002`, `US-P0-001 -> US-P0-003`,
  `US-P0-001 -> US-P0-004`, `US-P0-001 -> US-P0-005`,
  `US-P0-001 -> US-P0-006`, `US-P0-001 -> US-P0-007`, and
  `US-P0-001 -> US-P0-008`.
- The packet contract and dependency evidence do not prove runtime
  implementation, selected assets, legal clearance, or JX/DHCD parity.
