# Exec Plan

## Goal

Provide the missing vltktool evidence primitive required by TangMen oracle work.

## Scope

In scope:

- New `extract_table_slice.py` and focused tests only.
- Byte-preserving selection, deterministic provenance and `--check`.

Out of scope:

- Existing dirty vltktool files, PAK extraction, decoding, Unity or jx-source edits.

## Risk Classification

Risk flags: existing behavior, weak proof, public validation contract,
cross-platform byte fidelity.

## Work Phases

1. Implement new-file-only CLI.
2. Add binary/CRLF/error/check-mode tests.
3. Independent review and focused pytest.
4. Use the tool to pin the TangMen source slice in a later story phase.

## Stop Conditions

- Tool would normalize bytes or guess encoding.
- Existing dirty vltktool files must be edited.
- Source tree mutation or validation weakening is required.
