# US-RUN-PROTO-CODEC Exec Plan

## Goal

Create deterministic generated Python bindings and binary framing proof.

## Scope

In scope:

- Runtime/dev dependencies, generation script, committed generated files and tests.

Out of scope:

- Public proto edits, WSS route, session state and persistence.

## Risk Classification

Risk flags:

- Public contract.
- External dependency/toolchain.

Hard gates:

- Generated output must match canonical proto without manual edits.

## Work Phases

1. Pin toolchain.
2. Generate.
3. Add framing boundary.
4. Prove round-trip and malformed frames.
5. Verify drift check.

## Stop Conditions

- Canonical proto requires modification.
- Reproducible generation cannot be proven.
