# Fixture P0

## Phạm vi

Thư mục này chứa input máy đọc, được pin từ source hiện hữu để tái lập lab. Fixture không tự trở thành PC golden và không được dùng để nâng lifecycle parity nếu các oracle còn `BLOCKED`.

| Fixture | Nguồn | Dùng cho | Giới hạn authority |
| --- | --- | --- | --- |
| `training-npcs.p0.json` | `TrainingNpcSpawner.cs`, `SandboxManager.cs` theo path/hash/line range trong file | Spawn/order/position/state baseline của 5 NPC tại Combat Parity Lab | DevHarness `DISCOVERED / UNVERIFIED`; resistance, hitbox, rounding, death/reset và PC visual oracle còn `BLOCKED` |

## Quy tắc cập nhật

1. Hash source đổi thì fixture không tự cập nhật; DRI Gameplay phải review semantic diff.
2. Mọi giá trị mới phải có source line/hash hoặc trusted runtime capture. Không copy default từ code Go/Unity tương lai để lấp trường PC còn thiếu.
3. Position cố định chỉ áp dụng `fixture_mode=MAP_53_FIXED_CENTER` với `usePlayerPosition=false`. No-map fast boot đặt center theo player là behavior đã phát hiện nhưng bị loại khỏi deterministic case.
4. Position được kiểm theo công thức tâm/bán kính/góc và tolerance trong fixture; thứ tự `index` quyết định template/instance ID.
5. Chỉ đổi `verification` sau khi runner tái lập fixture và evidence artifact được hash. Chỉ gỡ `BLOCKED` sau PC oracle và reviewer QA Parity sign-off.
