# Exec Plan

## Goal

Admit and define the P0 provenance/evidence gate required by `REQ-P0-001` so
later JX asset and configuration work has a fail-closed, auditable record
format. This story creates the contract only; it does not perform a PC-to-Unity
port.

## Scope

In scope:

- Define the required provenance fields and review gate for selected JX
  PAK/SPR/VFX/WAV/map/UI/config evidence.
- Define negative checks that reject incomplete, guessed, unreviewed, or
  public-distribution evidence.
- Preserve the internal-only pilot boundary and public-distribution prohibition.

Out of scope:

- Runtime Unity, server, map, NPC, item, skill, UI, or asset changes.
- Selecting a candidate, resolving a resource, decoding an asset, or vendoring
  bytes.
- Claiming legal clearance, an approved internal build, or public distribution.

## Risk Classification

Risk flags:

- Audit/security: provenance and release evidence are safety-critical records.
- External systems: canonical PC corpus and resolver toolchain are external to
  this repository.
- Public contracts: the packet establishes a gate used by P0 asset work.
- Multi-domain: evidence, legal policy, JX parity, and pilot release policy
  intersect.

Hard gates:

- Legal clearance cannot be inferred from source discovery or a manifest row.
- `B-EVIDENCE-001` and `B-LEGAL-001` remain active until their owners provide
  the required evidence and approval records.

## Work Phases

1. Use intake `#9` and `REQ-P0-001` as the reconciliation authority for the
   admitted, implemented packet contract.
2. Record the evidence contract from `DOC-GOV-02`, `DOC-JX-05`, and
   `DOC-JX-08`.
3. Define fail-closed review, stop, and negative-validation conditions.
4. Reconcile the durable high-risk Harness story registration without a fake
   verifier and without inferring any graph edge beyond the recorded direct
   downstream blocker edges.
5. Confirm the durable Harness graph contains exactly the direct edges
   `US-P0-001 -> US-P0-002`, `US-P0-001 -> US-P0-003`,
   `US-P0-001 -> US-P0-004`, `US-P0-001 -> US-P0-005`,
   `US-P0-001 -> US-P0-006`, `US-P0-001 -> US-P0-007`, and
   `US-P0-001 -> US-P0-008`.
6. Validate packet content and Harness registration without treating the
   contract or dependency edges as runtime, asset-selection, legal-clearance,
   or parity evidence.

## Stop Conditions

Pause for human confirmation if:

- A candidate must be selected without full candidate enumeration, package
  version, and active load-order evidence.
- A logical path, UID, encoding, label, decode result, or legal state is
  unavailable, ambiguous, or conflicting.
- Work would vendor bytes, modify `/var/www/jx-pc`, alter runtime behavior,
  or enable any pilot/public distribution.
- A legal approval, owner, expiry, or distribution boundary must be interpreted
  rather than supplied as evidence.
- The durable graph lacks, adds to, or changes any required direct
  `US-P0-001 -> US-P0-002..008` blocker edge without an authorized graph
  update and corresponding packet reconciliation.
