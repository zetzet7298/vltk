# Add exact-byte gameplay table slicing to vltktool

## Current Behavior

`vltktool` can export editable configs from PAKs, but has no bounded command that
selects reviewed rows from an already-unpacked encoded table while preserving
exact bytes and emitting deterministic provenance. This blocks
`SKL-TM-PROOF-001` from pinning a compliant `skills.txt` slice.

## Target Behavior

Add a stdlib-only CLI that selects rows by an ASCII key column, writes the header
and selected source lines byte-for-byte in source order, emits source/slice
SHA-256 plus line provenance, and supports a non-writing `--check` mode.

## Affected Users

- Agents and reviewers producing canonical PC config evidence.

## Affected Product Docs

- `docs/stories/SKL-TM-PROOF-001/`
- `/home/zet/Projects/vltktool/README.md`

## Non-Goals

- No encoding detection, text normalization, PAK re-unpack, or Unity changes.
