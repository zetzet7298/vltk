# Exec Plan

## Goal

Recover exact DHCD pause/timeScale and card-timer semantics, or preserve a reproducible fail-closed boundary.

## Scope

In scope:

- Hash-locked native, metadata, ISIL, declaration, and authorized runtime analysis.
- Caller/callee mapping for card UI and `BattleSys` pause surfaces.
- Evidence card, verifier, and unresolved ledger updates.

Out of scope:

- Unity implementation, gameplay defaults, and `/var/www/jx-pc`.

## Risk Classification

Risk flags: cross-platform, public gameplay contract, weak reverse proof.

Hard gates:

- Native/metadata hashes must match.
- Malformed generated C# cannot establish behavior.
- Unresolved global/timer/input scope keeps the story `in_progress`.

## Work Phases

1. Verify the current card queue evidence and producer hashes.
2. Recover metadata-to-pointer mapping for `BattleSys` and card UI methods.
3. Trace show/select/close and pause setter callers.
4. Corroborate with an authorized runtime trace.
5. Update evidence and run the verifier plus Harness gates.

## Stop Conditions

Pause if native mapping, runtime scope, or validation requirements are ambiguous; do not weaken the negative boundary.
