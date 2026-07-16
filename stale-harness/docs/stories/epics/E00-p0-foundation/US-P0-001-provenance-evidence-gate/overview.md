# Overview

## Current Behavior

Intake `#9` classifies this as normal documentation reconciliation for the
high-risk DHCD x JX port initiative. `US-P0-001` is admitted and implemented
as the Harness packet contract for `REQ-P0-001`; it does not implement a
runtime feature or evidence selection. The durable Harness graph records the
direct blocker edges `US-P0-001 -> US-P0-002`, `US-P0-001 -> US-P0-003`,
`US-P0-001 -> US-P0-004`, `US-P0-001 -> US-P0-005`,
`US-P0-001 -> US-P0-006`, `US-P0-001 -> US-P0-007`, and
`US-P0-001 -> US-P0-008`. The packet retains the fail-closed boundaries for
`DOC-GOV-02`, `DOC-JX-05`, `DOC-JX-08`, `B-EVIDENCE-001`, and
`B-LEGAL-001`; it does not assert their resolution.

## Target Behavior

`US-P0-001` supplies the admitted, high-risk contract for documenting and
reviewing selected evidence. A future selected JX PAK/hashed asset record must
contain all fields in the schema in `design.md`, have an owner/reviewer/timestamp,
and fail closed when evidence is incomplete. It is the direct blocker for
`US-P0-002` through `US-P0-008` in the durable Harness graph. The P0 pilot
remains internal-only; public distribution is prohibited unless the separate
legal clearance gate is cleared.

## Affected Users

- Evidence owner preparing auditable provenance records.
- JX reviewer verifying PC source and resolver evidence.
- Legal reviewer enforcing internal-only and public-distribution gates.
- Technical reviewer checking that later port stories do not substitute guesses
  for source evidence.

## Affected Product Docs

- `specs/dhcd-jx-port/01-governance/traceability.md` (`REQ-P0-001`,
  `OBJ-P0-02`, `OBJ-P0-04`).
- `specs/dhcd-jx-port/01-governance/evidence-register.md` (`DOC-GOV-02`).
- `specs/dhcd-jx-port/05-jx-parity/assets.md` (`DOC-JX-05`).
- `specs/dhcd-jx-port/05-jx-parity/spr-vfx-wav-manifest.md` (`DOC-JX-08`).

## Non-Goals

- No runtime port or Unity implementation.
- No vendoring or copying selected bytes.
- No guessed candidate, logical path, mapping, name, UID, encoding, or decode.
- No legal-clearance, internal-approval, or public-distribution claim.
