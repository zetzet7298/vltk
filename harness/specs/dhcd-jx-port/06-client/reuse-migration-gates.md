# Reuse Và Migration Gates

| Trường | Giá trị |
|---|---|
| Mục đích | Ngăn thay thế module hiện hữu không có bằng chứng/rollback |
| Trạng thái | `not_started` |
| Owner / reviewer | Client lead / technical lead |
| Cập nhật | 2026-07-15 |

## Gate

1. Inventory current module và caller/dependency bằng `srcwalk`.
2. Gắn source authority cho behavior/visual/data.
3. Với `CityDefenceService`, chỉ cho phép parser/import seam; cấm reuse `DateTimeOffset.UtcNow`, runtime reward grant hoặc ownership state trong canonical path.
4. Với `MapEnemyDatabase`, chỉ cho phép generated audited roster lookup; cấm fallback/default/curated mapping trong pilot.
5. Viết adapter/interface nếu target contract khác.
6. Shadow-run old/new và so sánh state/event/visual golden.
7. Chạy migration/save/replay/backward compatibility.
8. Feature flag + telemetry + rollback trigger.
9. Chỉ retire old path sau hai chu kỳ pilot không có divergence.

## Không đạt gate nếu

- chỉ có screenshot hoặc file name làm evidence;
- chưa có test deterministic;
- migration làm mất inventory/progression;
- asset legal/provenance chưa rõ;
- module mới phụ thuộc default/synthetic mapping chưa đánh dấu.

## Acceptance

- [ ] Tất cả chín gate có evidence và reviewer.
- [ ] Shadow compare không divergence trên state/event/visual golden.
- [ ] Rollback giữ nguyên save/inventory/replay compatibility.
