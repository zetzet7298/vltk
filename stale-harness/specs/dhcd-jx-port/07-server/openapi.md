# OpenAPI Contract

| Trường | Giá trị |
|---|---|
| Mục đích | Khung REST contract cho Go server |
| Trạng thái | `not_started` |
| Owner / reviewer | Server lead / API reviewer |
| Cập nhật | 2026-07-15 |

## Versioning

Base path `/v1`; JSON UTF-8; request ID; `ETag`/resource version; idempotency key cho mutation; error envelope ổn định.

## Endpoint outline

| Method/path | Mục đích | Auth |
|---|---|---|
| `POST /v1/guest/session` | guest session | none/device proof |
| `GET /v1/catalog/{version}` | JX/game data manifest | session |
| `GET /v1/player` | profile/progression | session |
| `POST /v1/characters` | create character | session |
| `POST /v1/inventory/transactions` | equip/consume/split | session + idempotency |
| `POST /v1/runs` | reserve seed/config | session |
| `POST /v1/runs/{id}/checkpoints` | submit checkpoint | session |
| `POST /v1/runs/{id}/complete` | replay/final verify | session |
| `GET /v1/runs/{id}/receipt` | reward receipt/status | session |
| `POST /v1/replays` | diagnostic upload | session, bounded |

## Schema IDs và trace

`RunReservationV1`, `RunCheckpointV1`, `RunCompleteV1`, `RewardReceiptV1`, `InventoryTransactionV1`, `ErrorV1` phải map field tới `runs`, `run_checkpoints`, `reward_proposals`, `inventory_items`, `idempotency_keys` và replay header. Mỗi mutation ghi authorization scope, idempotency semantics, catalog/config version và corresponding contract test ID.

## Error classes

`invalid_request`, `unauthorized`, `forbidden`, `conflict_version`, `duplicate_idempotency`, `stale_run`, `verification_failed`, `quarantined`, `rate_limited`, `internal`.

## Acceptance

Commit `openapi.yaml`, generated Go types, contract tests (`API-*`), auth/error examples và backward compatibility check. Chưa có artifact nên chưa được gọi là API đã tồn tại.
