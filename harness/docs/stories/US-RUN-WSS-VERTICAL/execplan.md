# US-RUN-WSS-VERTICAL Exec Plan

## Goal

Wire and prove one bounded server-owned `game.v1` move command path.

## Scope

In scope:

- Mounted WSS router, app-scoped injection seam, admission, epoch fencing,
  durability preflight/commit, resources snapshot, shutdown/drain and tests.

Out of scope:

- Unity rollout, public ticket issuer, full CalcDamage/ProcessState parity,
  distributed fencing and unpinned restart-resume migration.

## Risk Classification

Risk flags:

- Public contract, session authority, durability and multi-domain behavior.

Hard gates:

- Runtime core, codec and durable persistence prerequisites must pass.

## Work Phases

1. Compose injected ports and app-scoped endpoint seam. — complete
2. Mount route and prove real WebSocket handshake. — complete
3. Execute one intent through authoritative completion and PostgreSQL UoW. — complete
4. Verify replay/conflict/failure, replacement fence, resync and drain. — complete

## Stop Conditions

- Any path accepts client state/clock.
- ACK can be emitted before durable commit.
