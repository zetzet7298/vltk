# Client Migration Plan

| Trường | Giá trị |
|---|---|
| Mục đích | Chuyển client hiện tại sang portrait/DHCD loop mà vẫn rollback được |
| Trạng thái | `design` |
| Owner / reviewer | Client lead / technical lead |
| Cập nhật | 2026-07-15 |

## Slices

1. Inventory và feature flags, không đổi behavior.
2. Portrait shell + safe-area + input.
3. JX asset resolver/import và player/NPC/map golden.
4. Deterministic battle mirror + wave/card UI.
5. Go API/replay integration.
6. Inventory/progression/reward commit.
7. Remove legacy path sau hai release pilot ổn định.

## Migration gate

Mỗi slice cần compile, unit/PlayMode, visual golden, replay vector, telemetry, data migration và rollback test. Flag có owner/expiry; old/new path chạy shadow compare trước cutover.

## Rollback

Disable flag để quay về last verified slice; không rollback schema destructive. Replay/checkpoint schema giữ backward reader trong retention window 30 ngày.

## Acceptance

- [ ] Mỗi slice có flag, migration test, shadow compare, telemetry và rollback evidence.
- [ ] Save/inventory/replay backward reader pass trong retention window.
- [ ] Old path chỉ retire sau hai chu kỳ pilot không divergence.
