# Replay Và Security

| Trường | Giá trị |
|---|---|
| Mục đích | Chống reward forgery và tái dựng run |
| Trạng thái | `not_started` |
| Owner / reviewer | Server lead / security reviewer |
| Cập nhật | 2026-07-15 |

## Replay format

Header: `schema_version`, `run_id`, `player_id`, `seed`, immutable `config_snapshot_id` + `config_hash`, `catalog_version`, `map_conversion_version`, `simulation_version`, `tick_rate`, `start_time`. Body: ordered inputs/events/checkpoints; trailer: final state hash, reward proposal, client diagnostics, signature/transport metadata.

## Verification

1. Authenticate owner and validate run reservation plus immutable config snapshot.
2. Validate sequence, tick window, payload bounds and config versions.
3. Re-run Go canonical simulation/checkpoint hashes.
4. Compare final state/reward proposal.
5. Commit receipt or quarantine with reason and support trace; committed receipt is immutable across config revoke/rollback.

Client state hash is a signal, không phải authority. Replay upload phải rate-limit, encrypt at rest và redact device-sensitive data.

## Threats

Speed/input injection, duplicate completion, altered catalog, replay truncation, oversized payload, account takeover, secret leakage. Mỗi threat có named negative test và telemetry trước pilot; xem [acceptance-gates](../09-quality/acceptance-gates.md).

## Acceptance

- [ ] Forged/altered/truncated/oversized replay và sequence/tick violation bị reject/quarantine.
- [ ] Duplicate completion không nhân đôi receipt; config revoke không đổi receipt đã commit.
- [ ] Authorization/key rotation/account takeover attempt có test và audit signal.
