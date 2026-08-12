# Chiến lược kiểm thử

## Test ladder

1. Spec validator và schema fixtures.
2. Source provenance, package winner, catalog coverage/reference closure.
3. Go unit/property/fuzz và Unity EditMode.
4. Deterministic combat/replay và contract golden vectors Go-C#.
5. Unity PlayMode/runtime E2E.
6. Visual/audio golden và human parity review.
7. PostgreSQL transaction/crash/reconciliation/security.
8. Load, soak, reconnect storm, device/thermal.
9. Migration, backup/restore, rollback và release drill.

## Ma trận bắt buộc

| Miền | Positive | Boundary/negative | Failure/recovery |
| --- | --- | --- | --- |
| Auth/session | Register/login/refresh/select | brute force, token/ticket replay, duplicate login | socket drop, revoke, reconnect 15s |
| Movement/channel | joystick/path/transfer | invalid speed/coordinate/epoch | channel drain, process restart |
| Combat/skill | cast/hit/buff/missile | out-of-range, cooldown, cancel, bad target | late/reordered input, reconciliation |
| Inventory/economy | loot/equip/buy/trade | full bag, insufficient funds, stale offer | crash before/after commit, retry receipt |
| Quest/Lua | accept/progress/choice/reward | forged choice, forbidden API | timeout/memory, script quarantine |
| Content | valid bundle/resolver | wrong locale/hash/package order | corrupt/missing pack, rollback |
| UI | screen states, Back, Safe Area | loading/empty/error/offline | reconnect/content download failure |
| Visual/audio | canonical asset/frame/event | missing layer/clip/frame | golden unavailable/corrupt |

## Performance profile

- Realm gate: 1000 persistent WSS connections, scripted mix movement/combat/economy/quest.
- Network model: RTT 100ms, jitter 20ms, packet loss 1%; authoritative response p95 <=200ms.
- AOI: tối đa 64 actor động và 128 entity nhẹ/người chơi.
- Simulation: 18Hz, không drift dài hạn; overload có metric và degrade fail-safe, không bỏ economy commit.
- Client: Android ARM64 4GB cỡ Snapdragon 680/Helio G85 giữ 30 FPS; tier 6GB cỡ Snapdragon 778G hướng tới 60 FPS.
- Checkpoint age tối đa 5 giây; item/economy durable không rollback sau ACK.

## Visual parity

- Skill/effect: compare từng frame alpha/pivot/order/timing trong deterministic viewport; không dùng average SSIM che case lỗi.
- UI: asset-level hash/pixel diff cho SPR; layout mobile được review theo screen contract, không full-screen compare với PC.
- HUD: rect/anchor/z-order regression theo baseline mobile 1280x720 trước Safe Area transform.
- Map/avatar/audio: fixture riêng, source provenance và human review; skill audio timing là một phần parity gate.

## Hợp đồng kiểm thử SQL âm

- `TEST-SQLNEG-001` chạy `contracts/sql/game.v1.negative.sql` sau khi nạp
  `contracts/sql/game.v1.sql` vào database PostgreSQL 16 disposable. Suite rollback
  toàn bộ fixture và fail nếu mutation bị cấm không phát sinh lỗi mong đợi.
- Phạm vi bắt buộc: FK chéo realm; slot túi `60`; Lua khác `5.1`; ledger chéo
  currency; thêm entry sau khi posted; update/delete entry; posted transaction không
  cân bằng; trùng slot active; provenance chéo content release; admission sai owner.
- Đây là contract test ở tầng dữ liệu, không thay thế transaction/integration test của
  Go service, RLS test bằng role runtime không phải owner, crash recovery hoặc restore drill.

```bash
(
  set -e
  DB="jx_spec_negative_${RANDOM}"
  trap 'dropdb --if-exists "$DB"' EXIT
  createdb "$DB"
  psql -X -v ON_ERROR_STOP=1 -d "$DB" \
    -f harness/specs/jx-pc-mobile-port/contracts/sql/game.v1.sql \
    -f harness/specs/jx-pc-mobile-port/contracts/sql/game.v1.negative.sql
)
```
