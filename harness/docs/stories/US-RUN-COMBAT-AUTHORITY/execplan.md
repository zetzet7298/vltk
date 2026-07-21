# US-RUN-COMBAT-AUTHORITY Exec Plan

## Goal

Create the smallest production-wired server runtime seam that owns combat state
and trusted tick, allowing later PC behavior slices to run without client-authored
state.

## Scope

In scope:

- Pin PC loop/ProcessState ordering and target `game.v1` invariants.
- Decide aggregate, session, clock, application-port and checkpoint boundaries.
- Decompose implementation into non-overlapping runtime, combat, persistence and
  transport child stories.
- Implement and prove one intent → trusted tick → state mutation → snapshot path
  before unblocking combat parity stories.

Out of scope:

- Full CalcDamage/ProcessState parity in one change.
- Legacy endpoint expansion or client clock/state authority.
- Database migration without isolated PostgreSQL and an approved mapping.

## Risk Classification

Risk flags:

- Public contract.
- Data ownership/model.
- Existing behavior.
- Weak proof.
- Multi-domain/runtime-client boundary.

Hard gates:

- Data migration/loss if a persisted checkpoint design is introduced.
- Validation may not be weakened to route/import/unit-helper proof.

## Work Phases

1. Discover exact PC loop, target proto and current backend ownership/callers.
2. Record an accepted aggregate/session/clock compatibility decision.
3. Split runnable child stories with exact ownership and proof. — complete
4. Implement the minimal production WSS/tick/aggregate vertical slice. — complete
5. Verify deterministic ordering, authority, snapshot and failure cases. — complete
6. Refresh specs, Harness graph and dependent combat story. — complete

## Stop Conditions

Pause for human confirmation if:

- Normative target and exact PC behavior require incompatible protocol semantics.
- A migration/destructive data change is required before an isolated database is proven.
- Architecture would bypass module ownership or replace the accepted WSS direction.
- Required deterministic verification would need to be weakened.

## Current State

Resolved by the user's production approval and accepted decision
`0008-game-v1-runtime-authority`. Delivery is decomposed into:

1. `US-RUN-RUNTIME-CORE` and `US-RUN-PROTO-CODEC` (independent first wave candidates).
2. `US-RUN-DATA-FOUNDATION` after both foundations.
3. `US-RUN-DURABLE-CHECKPOINT` after identity/data foundation.
4. `US-RUN-WSS-VERTICAL` after runtime core, codec and durable persistence.

The mounted bounded production path passed focused root and independent reviewer
proof in Herdr run `orch-2e35882107fd2c88`. Harness refresh, detailed trace and
story completion are done; no implementation blocker remains.
`US-CBT-SERVER-OWNED-BRANCH` is the next low-blocker successor. Global runtime
and full PC combat parity remain incomplete by design.
