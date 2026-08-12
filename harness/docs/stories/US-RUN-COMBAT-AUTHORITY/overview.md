# US-RUN-COMBAT-AUTHORITY Overview

## Status

implemented

## Lane

high-risk

## Current Behavior

Legacy FastAPI combat routes still accept request-scoped combat state and remain
non-normative. The new bounded production path is `game.v1` WSS → intent-only
admission → fixed trusted tick → `CombatRuntimeState` owned actor/status → atomic
checkpoint/outcome/outbox → ACK and authoritative snapshot. Exact checkpoint
resume, controlled `ProcessState` early-return and same-batch retry are proven.

This removes the authority prerequisite for successor combat slices without
expanding client authority. It does not make full `KNpc::CalcDamage` or
`KNpc::ProcessState` parity complete.

## Target Behavior

Delivered for one bounded actor vertical: the server owns combat identity,
vitals, action/status provenance, version and trusted tick. `game.v1` WSS accepts
intent only, orders by session epoch/client sequence, resumes an exact matching
checkpoint, runs combat on fixed ticks, commits one durable completion and emits
`PlayerResources` plus `EntityState`. Legacy `/combat/damage/calc` and
`/combat/status/tick` are not production authority.

Residual scope is explicit: external ticket issuer, fresh-character bootstrap,
Unity WSS prediction/reconciliation, global world/NPC loop and full PC combat
branches remain successor work.

## Affected Users

- Players whose Unity client predicts combat and reconciles server snapshots.
- Backend gameplay/runtime operators and developers.

## Affected Product Docs

- `/var/www/vltk-mobile/domains/server-runtime/README.md`
- `/var/www/vltk-mobile/contracts/proto/game/v1/game.proto`
- `/var/www/vltk-mobile/contracts/legacy-mapping.md`
- `/var/www/vltk-mobile/backend/specs/domains/p0-runtime-protocol.md`
- `/var/www/vltk-mobile/backend/specs/domains/p1-skill-combat.md`

## Non-Goals

- Claiming full PC combat parity in this prerequisite.
- Making legacy Unity DTOs normative.
- Guessing checkpoint, RNG, engine or time semantics not present in source.
- Running shared PostgreSQL integration/e2e tests.
