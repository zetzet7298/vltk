# Combat Loop

| Trường | Giá trị |
|---|---|
| Mục đích | Định nghĩa battle lifecycle kiểu DHCD trên nền actor/skill JX |
| Trạng thái | `provisional` |
| Owner / reviewer | Gameplay owner / DHCD reviewer |
| Cập nhật | 2026-07-15 |

## Evidence được phép dùng

`/home/zet/Projects/dhcd/docs/gameplay-evidence-map.md:14-26` ghi lifecycle `GameStart/GameEnd`, wave manager, spawn, monster config, hit, drop, random skill và actor concepts. Đây là declaration/recovered-IL evidence; không chứng minh exact balance, portrait, pause hay original server.

## To-be state machine

```text
Lobby
  -> Loading
  -> RunReady
  -> WaveActive
  -> WaveCleared
  -> ChoiceModal (optional, per-player state; ordering unresolved)
  -> WaveActive
  -> RunSuccess | RunFailed | RunAborted
  -> RewardPending
  -> Verified | Quarantined
```

- Server phát run seed và immutable config snapshot/version; snapshot được giữ ít nhất suốt retention của run/reward. Client mirror dùng cùng input sequence.
- Wave manager sở hữu spawn timing, active count, clear condition và cleanup.
- Actor JX sở hữu stats, animation, skill resolution và collider; wave không được tự sửa stat ngoài contract.
- Drop/XP event được sequence hóa; reward chỉ commit sau Go verify.
- Modal event không tự ý dừng toàn server. Corpus hiện chỉ cho thấy per-player state/waiting-list declarations; ordering/serialization, pause toàn trận, input lock, card count/weight/cost/cap và multiplayer pause là `[CẦN XÁC NHẬN]`.

## Input contract

Input tối thiểu: `run_id`, `sequence`, `client_tick`, player intent (move/target/skill/choice/reroll), client state hash. Reject sequence gap/duplicate sai idempotency; server canonical hóa tick và random seed.

## Acceptance

- State transition/property test không có transition ngoài graph.
- Wave clear/reward replay tái dựng cùng hash trên Unity và Go.
- Modal nối tiếp theo player không làm mất event.
- Mismatch đưa run vào quarantine, không cấp reward. Run đã commit không bị đổi kết quả khi config bị revoke/rollback; chỉ proposal pending mới bị re-verify theo snapshot hoặc expire theo policy.

## Open

Reverse tiếp `NormalLevelLogic`, `LevelRandomSkillCtrl`, `LevelMonsterMgr`, `WaveRefresh`, `LevelCollectItemMgr`; ghi method/IL evidence vào reverse queue trước khi chốt rule thiếu.
