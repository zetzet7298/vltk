# US-RUN-WSS-VERTICAL Overview

## Status

implemented

## Lane

high-risk

## Current Behavior

FastAPI mounts `/v1/game.v1` as an app-scoped, fail-closed binary WebSocket
route. An injected opaque ticket authority, combat port and PostgreSQL durability
adapter execute one bounded `move` intent through authoritative ordering/tick,
commit checkpoint/idempotency/outbox before ACK, and emit a full resources
snapshot. Higher epochs and shutdown drain share a per-character command fence.

## Target Behavior

A mounted FastAPI WebSocket route admits canonical `game.v1` binary envelopes,
orders exactly one trusted move intent, replays durable duplicates before a
second mutation, commits state/outcome/outbox before success, and emits
authoritative `ServerEnvelope` result/resources snapshot.

## Affected Users

- Realtime players, Unity adapter and runtime operators.

## Affected Product Docs

- `docs/decisions/0008-game-v1-runtime-authority.md`
- `/var/www/vltk-mobile/contracts/proto/game/v1/game.proto`
- `/var/www/vltk-mobile/domains/server-runtime/README.md`

## Non-Goals

- Full PC combat parity, entity/checksum projection and Unity rollout/cutover.
- Public ticket issuing, cross-process/HA fencing and unpinned restart-resume
  epoch migration.
