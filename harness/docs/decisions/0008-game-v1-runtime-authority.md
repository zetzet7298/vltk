# 0008 Game v1 Runtime Authority

Date: 2026-07-20

## Status

Accepted

## Context

Legacy combat REST reconstructs attacker, target, status and tick from client JSON.
Canonical PC server code instead admits messages before a server-owned 18 Hz-gated
loop, queues NPC commands, advances world/NPC state and defers skill cast to the
action frame. The normative `game.v1` contract requires intent-only binary WSS,
session epoch/sequence ordering, trusted server tick, authoritative snapshots,
durable checkpoint/idempotency and transactional outbox. The backend has none of
those production seams and currently relies on `Base.metadata.create_all`.

## Decision

1. Production realtime transport is FastAPI WebSocket carrying exactly one
   length-delimited generated Protobuf `game.v1.ClientEnvelope` or
   `game.v1.ServerEnvelope` per binary message. JSON combat REST remains a legacy
   regression surface and is never combat authority.
2. `RuntimeSession` owns realm ID, character ID, content release ID, session
   epoch, client/server sequence cursors, trusted server tick, pending inputs and
   replay/deduplication state. It does not accept client vitals, status or clock.
3. `CombatRuntimeState` owns character/NPC identity, position/action, vitals,
   series, status/effect provenance, command queue and version. Combat and skill
   modules are invoked through application ports; they do not read runtime-owned
   tables directly.
4. The scheduler targets 18 Hz using an injected monotonic clock and preserves
   the proven PC order: message admission before simulation, queued command,
   state processing, command processing, status/action-frame execution. Client
   `target_tick` is bounded intent, never the authoritative clock.
5. Checkpoints use a private, versioned deterministic Protobuf
   `RuntimeCheckpointV1`, not public `WorldSnapshot` alone. SHA-256 is computed
   over the exact deterministic bytes stored in `runtime_checkpoints.state_blob`;
   SQL columns remain authoritative for epoch, tick, last client sequence and
   schema version.
6. An accepted durable command commits its command outcome/idempotency record,
   new checkpoint and outbox events in one PostgreSQL transaction. Success ACK is
   emitted only after commit; duplicates replay the stored outcome without a
   second mutation.
7. Python Protobuf bindings are generated reproducibly from the canonical
   `/var/www/vltk-mobile/contracts/proto/game/v1/game.proto`, committed to the
   backend and guarded by a drift check. Hand-written codecs are forbidden.
8. Production persistence uses Alembic revisions derived from the normative SQL,
   including realm/character UUID mapping and RLS session context. Startup
   `create_all` is not migration authority. Upgrade, rollback and RLS proof run
   only against a proven disposable PostgreSQL database.

## Alternatives Considered

1. Keep client-authored JSON combat: rejected because it expands client authority.
2. Use an in-memory registry as production persistence: rejected because resume,
   dedupe and acknowledged outcomes would be lost on restart.
3. Store only public `WorldSnapshot`: rejected because it omits internal command,
   status/effect provenance and replay state needed for deterministic resume.
4. Hand-write Protobuf framing: rejected because generated bindings are the
   normative compatibility boundary.
5. Point runtime foreign keys at legacy integer `roles.id`: rejected because the
   target contract requires realm-scoped UUID character identity.

## Consequences

Positive:

- Combat state and time become server-owned and later PC slices gain one trusted
  runtime caller.
- Reconnect, replay and outbox semantics have one explicit transaction boundary.
- Wire and checkpoint formats are versioned and reproducible.

Tradeoffs:

- Runtime delivery now depends on generated Protobuf, target identity migration,
  Alembic and isolated PostgreSQL proof.
- Legacy account/role/player schemas cannot be silently treated as target tables;
  a separate migration/mapping child is required.
- Unity realtime adaptation and production rollout remain separate work.

## Follow-Up

- Implement pure runtime session/ordering/tick/state ports first.
- Add reproducible `game.v1` Python code generation and binary framing tests.
- Establish realm/account/character migration foundation.
- Implement atomic checkpoint/idempotency/outbox persistence.
- Wire and prove the production WSS vertical path before unblocking combat parity.
