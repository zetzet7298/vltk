# Exec Plan

## Goal

Resolve package provenance for the P0 arena candidate queue:
`yanwuchang` IDs `209/210/211`, `jingjichang` ID `975`, and `shiliantang` ID
`925`. Record only source-supported facts in the arena audit. This story does
not select or enable a pilot arena unless all required package and geometry
evidence exists.

## Scope

In scope:

- Read-only investigation of canonical JX loose/runtime data using
  `/home/zet/Projects/vltktool`.
- Candidate enumeration, logical path/UID/encoding evidence, package version,
  active load-order evidence, source file bytes/hash, label cross-check, and
  map-region decode evidence when actually available.
- Updating the arena candidate audit with facts and explicit unresolved fields.

Out of scope:

- Unity, server, scene, asset, runtime, or feature-flag changes.
- Copying/vendoring bytes or modifying `/var/www/jx-pc`.
- Selecting, enabling, or claiming a pilot arena from textual IDs, names, or
  hashes alone.
- Legal clearance or any distribution claim.

## Risk Classification

Risk flags:

- External systems: canonical PC corpus and vltktool resolver/decode tools.
- Existing behavior: map identity/collision source evidence can affect later
  runtime behavior.
- Cross-platform: later map conversion must serve portrait Unity runtime.
- Weak proof: current candidates are textual/script-only and `DOC-JX-01` is
  blocked.

Hard gates:

- `US-P0-001` provenance gate must remain implemented.
- No winner without package/version/load-order proof and decoded Region_C plus
  Region_S proof.
- No legal or pilot-distribution claim under `B-LEGAL-001`.

## Work Phases

1. Admit the high-risk packet and dependency `US-P0-001 -> US-P0-002`.
2. Resolve each named candidate in required queue order without guessing paths
   or encodings.
3. Record complete candidate/provenance output or an explicit unresolved reason.
4. Verify only that the packet/audit remains fail-closed.
5. Mark complete only if a full winner and Region_C/Region_S/hash/decode
   evidence is recorded; otherwise retain `in_progress`.

## Stop Conditions

Pause and retain unresolved status if:

- Resolver output, package/version, or active load order is unavailable or
  conflicts.
- A candidate cannot be tied to a logical map path and exact source bytes.
- Region_C, Region_S, terrain, or minimap decode cannot be proven from the
  selected winner.
- Evidence would require modifying/copying canonical source, vendoring bytes,
  or touching Unity/runtime.
- A name, ID, hash, label, or loose-script reference is the only evidence for
  selection.
