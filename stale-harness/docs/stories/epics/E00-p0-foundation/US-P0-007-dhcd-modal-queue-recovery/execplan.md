# Exec Plan

## Goal

Recover DHCD modal queue, response, and input-lock semantics or preserve a bounded failure boundary.

## Scope

In scope: role-keyed native branches, player-data callee mapping, client modal callers, runtime round trips, evidence and verifier.

Out of scope: Unity UI implementation, global pause defaults, and cross-player design.

## Risk Classification

Risk flags: player-visible contract, multiplayer behavior, cross-platform native proof.

Hard gates: R-DHCD-003 pause story precedes closure; missing player-data bodies keep this story unresolved.

## Work Phases

1. Verify role-keyed controller slices and input provenance.
2. Recover player-data method bodies and response transitions.
3. Map client show/close and input-control callers.
4. Capture an authorized role/gid round trip.
5. Run verifier, dependency, and Harness gates.

## Stop Conditions

Stop if FIFO, lock, pause, or serialization would require labels or malformed generated C# alone.
