# US-RUN-COMBAT-AUTHORITY Validation

## Proof Strategy

Completion requires a real production call path, not only pure helpers: an
intent-only `game.v1` command must enter a server-owned session, be ordered and
executed on a trusted tick against authoritative entity state, then emit a
deterministic result/snapshot. Exact PC and target provenance must remain pinned.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Tick cadence, epoch/sequence/window, entity invariants, deterministic combat ordering. |
| Integration | WSS envelope → application runtime → authoritative state/checkpoint/outbox on disposable infrastructure. |
| E2E | Unity/fake adapter sends intent only and reconciles authoritative snapshot. |
| Platform | Shutdown/drain and reconnect/resume behavior. |
| Performance | Fixed 18 Hz lag/catch-up budget with no skipped ACKed durable command. |
| Logs/Audit | Epoch/seq/tick/command correlation and resync/checkpoint outcomes. |

## Fixtures

- Deterministic clock, RNG/content release and session epoch.
- Character/NPC state owned by a fake application repository before isolated DB proof.
- Duplicate, gap, future tick, resync and reconnect command streams.
- PC ProcessState/CalcDamage golden cases added only after exact dependencies are mapped.

## Commands

```text
cd /var/www/vltk-mobile/backend
python scripts/generate_runtime_checkpoint_v1_proto.py --check
pytest -q tests/unit/modules/runtime tests/unit/modules/combat/test_server_owned_runtime.py
pytest -q tests/integration/modules/runtime/test_game_v1_wss_vertical.py
ruff check app/modules/runtime tests/unit/modules/runtime tests/unit/modules/combat/test_server_owned_runtime.py
black --check app/modules/runtime tests/unit/modules/runtime tests/unit/modules/combat/test_server_owned_runtime.py
python specs/scripts/validate.py --strict
```

The selected WSS integration module uses fake/in-memory ports and does not connect
to PostgreSQL. Database-backed integration/e2e remains prohibited until a fresh
disposable PostgreSQL environment is proven isolated.

## Acceptance Evidence

The architecture/data decision blocker was resolved by explicit user approval of
production option 1 and accepted decision `0008-game-v1-runtime-authority`.

- Canonical PC revision `d4bfc04a3dbb8f964be1ee8cd9b6dec6fc4e1b91`;
  exact `KNpc.cpp` SHA-256
  `f8e274b459850e9c9a90442d9b5dc9a606eaaa200b15691c11c0d9b461fb6cea`;
  stage anchors `496-548`, `609-673`, `702-785`; controlled `ProcessState`
  anchors `787-1195`.
- `CombatRuntimeState` owns actor/vitals/status/version/tick. The concrete
  server-owned adapter executes bounded real status behavior; controlled ticks
  preserve queue and skip command/status completion exactly at the port boundary.
- Exact same-batch retry stores raw requested target tick and deterministic
  fingerprint. Incomplete retry commits nothing; mismatch/legacy checkpoint
  fails closed; completion emits exactly one checkpoint/outcome/outbox before ACK.
- WSS default admission requires exact checkpoint identity/epoch/content/actor,
  replies `resumed=true` plus `initial_tick`, and emits normative
  `PlayerResources` plus `EntityState` snapshots with checked scalar bounds.
- Root focused proof passed `71` checkpoint/combat-authority tests. Fresh root
  proof passed `200` combined runtime/server-owned combat tests; writer proof
  separately passed `183` runtime unit tests and `17` server-owned combat tests.
  Ruff/Black passed. Full fake-port WSS module passed `6` with `1` environment
  skip and no database connection.
- Final reviewer attempt `attempt-afaca9f5c6` reported P0/P1 clear and four
  targeted tests passed. Earlier pre-fix proof output is not used as final evidence.
- Harness `story complete` reran the configured fresh proof after evidence update:
  `37 passed`, then atomically marked the story `implemented`.
- Delivery Herdr run `orch-2e35882107fd2c88` finished verified with all `18`
  attempt boundaries clean; deterministic `RESULT.json` SHA-256 is
  `aeceaa9039efbc0a0351e4a091704b5e0b5729e3d147f25709e0f648a7a0e4fd`.
- Residual gaps: external ticket issuer, fresh-character bootstrap, Unity adapter,
  global world/game loop, full PC `ProcessState`/`CalcDamage`, RNG and engine
  dependencies. `GAP-RUN-001`, `GAP-RUN-002`, `GAP-CBT-001` and `GAP-CBT-002`
  remain open.
