# Exec Plan

## Goal

Correct the highest-impact Cái Bang and Đường Môn runtime mismatches observed in
fresh Play, establish independent event-level proof, then feed the reusable runtime
seams into the remaining-faction parity epic.

## Scope

In scope:

- Canonical PC C++/Lua/config audit for player-facing Cái Bang and Đường Môn cases.
- Runtime cast, damage/state, projectile/trap, and start/fly/collide/vanish seams.
- Focused fail-before/pass-after Unity tests and fresh Play verification.
- Harness evidence and residual blockers.

Out of scope:

- Unrelated gameplay domains.
- Broad asset vending without a selected runtime consumer.
- `PARITY_DONE` claims without PC golden plus mobile capture.

## Risk Classification

Risk flags:

- Public contracts.
- Cross-platform.
- Existing behavior.
- Weak proof.
- Multi-domain gameplay/runtime/UI behavior.

Hard gates:

- PAK/SPR/DAT/hash/encoding operations use `~/Projects/vltktool` only.
- `/var/www/jx-source` remains read-only.
- No validation weakening or Unity-derived expected oracle.

## Work Phases

1. [completed] Independent Cái Bang, Đường Môn, and proof-coverage audits.
2. [completed] Rank concrete mismatches by player impact and canonical confidence.
3. [completed] Add focused parser/catalog and level-data parity tests for the selected cases.
4. [completed] Implement Cái Bang routing/passive materialization, Đường Môn level-data, and the first canonical FlyEvent 302→301 runtime slice.
5. [completed] Compile, run focused EditMode proof, and perform fresh Play smoke.
6. [in progress] Independent review, Harness trace, and next-faction handoff.

## Stop Conditions

Pause for human confirmation if:

- PC client/server behavior conflicts and source authority cannot resolve it.
- The desired behavior requires a choice not present in PC evidence.
- A required asset winner or encoded config cannot be resolved with `vltktool`.
- Validation must be weakened or runtime architecture direction changes materially.
