# US-RUN-COMBAT-AUTHORITY Design

## Domain Model

Decision `0008-game-v1-runtime-authority` fixes two boundaries: `RuntimeSession`
owns realm/character/content/epoch/sequence/tick/queued input/replay state, while
`CombatRuntimeState` owns authoritative entities, vitals, action and status/effect
provenance. Domain code must not depend on FastAPI, SQLAlchemy, generated
Protobuf or Unity DTOs.

## Application Flow

Admission binds realm/character/content release to a session epoch. WSS parses
`ClientEnvelope`, validates sequence/window/target tick, and passes intent-only
commands to the application runtime. Default admission resumes only an exact
`LoadedCheckpoint` matching realm, character, content release, epoch, active
batch and actor. The runtime advances trusted ticks against `CombatRuntimeState`,
invokes the bounded real status processor, commits checkpoint/outcome/outbox only
after completion, then produces ACK, command result and snapshot.

PC controlled semantics are explicit: `process_state() -> bool`; a controlled
tick skips command/status stages, preserves the queue and does not ACK. Exact
same-batch retry retains raw `requested_target_tick` separately from normalized
`due_tick`; legacy checkpoints missing the raw value and fingerprint mismatches
fail closed rather than silently falling back.

## Interface Contract

Normative realtime messages are `/var/www/vltk-mobile/contracts/proto/game/v1/game.proto`.
`CastSkillInput` carries skill/target/coordinates, never life, mana, status,
damage range, relation, distance or server clock. Legacy combat REST endpoints
remain migration/regression surfaces and cannot satisfy runtime acceptance.

## Data Model

Session/checkpoint ownership, entity-state persistence and transaction boundaries
follow decision 0008: deterministic private Protobuf checkpoint bytes/hash and
one atomic checkpoint-command outcome-outbox UoW. Actor/status/fingerprint/raw
target tick are checkpointed deterministically. Resource/HP wire scalars are
`sint64`, coordinates `sint32`, IDs/facing/flags `uint32`, version `uint64`;
domain `life_max/max_hp` remain nonnegative. Cross-module state is accessed
through application ports, not direct foreign-table imports.

## UI / Platform Impact

Unity still needs a `game.v1` adapter and prediction/reconciliation path. External
ticket issuance and fresh-character bootstrap are also successor slices; this
story proves only the backend authority boundary and mounted bounded caller.

## Observability

Record session epoch, client/server sequence, server tick, command id, result,
checkpoint/outbox status, tick lag and resync reason without logging secrets.

## Alternatives Considered

1. Keep client-authored REST combat state: rejected by normative server-runtime
   and legacy-mapping contracts.
2. Add more pure combat helpers without a runtime caller: useful only as bounded
   domain work and insufficient for `runtime-wired`.
3. Read/write PlayerState directly from the combat module: rejected because the
   target contract forbids direct cross-module table ownership violations.
