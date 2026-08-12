# Validation

## Proof Strategy

`scripts/verify-us-p0-002.sh` proves only that the US-P0-002 packet and arena
audit contain the required candidate structure, fail-closed terms, and explicit
unresolved markers. It cannot accept a winner, prove a selected source asset,
prove a Region_C/Region_S decode, or authorize a runtime map port.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Packet/audit verifier checks all four story files and required audit anchors. |
| Integration | Harness records high-risk US-P0-002 with dependency on implemented US-P0-001. |
| E2E | Not applicable: no Unity/runtime work is in scope. |
| Platform | Not applicable: no device/editor scene is changed. |
| Logs/Audit | Audit retains exact evidence values or explicit `unresolved` markers. |

## Negative Validation

- Fail if the candidate order or any named candidate is absent.
- Fail if an audit row says selected/winner/pilot without complete package,
  Region_C, Region_S, hash, resolver, and decode evidence.
- Fail if unresolved evidence is treated as an acceptance condition.
- Fail if textual IDs/names/hashes alone are described as identity proof.
- Fail if the story claims Unity/runtime work, byte vendoring, or legal/public
  distribution clearance.

## Fixtures

No map files or source bytes are copied into this repository. Canonical paths,
commands, and hashes are recorded only after read-only resolver/decode proof.

## Commands

```text
scripts/verify-us-p0-002.sh
scripts/bin/harness-cli story verify US-P0-002
```

## Acceptance Evidence

- The audit records all candidate names/IDs and their source-proven values or
  explicit unresolved fields.
- Completion requires one full package/load-order winner plus Region_C,
  Region_S, hash, and decode evidence.
- If those conditions are absent, US-P0-002 remains `in_progress`.
