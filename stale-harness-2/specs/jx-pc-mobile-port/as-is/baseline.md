# Baseline brownfield và gap ưu tiên

## Kết luận

Unity hiện tại chứa nhiều implementation và fixture có giá trị để tái sử dụng, nhưng không được coi là bằng chứng parity mặc định. Panel/popup phải làm mới song song; HUD chỉ freeze geometry. Python FastAPI backend tồn tại trong `backend/`; realtime production và E2E Unity vẫn chưa được chứng minh.

## Ma trận hiện trạng

| Miền | Hiện trạng có bằng chứng | Gap/to-be | Ưu tiên |
| --- | --- | --- | --- |
| Asset resolver | Tìm first-match theo filesystem roots | Enforce package order, locale, content version và hash | P0 |
| Skill | Có catalog/test/audit rời rạc | Census đủ novice + 10 phái, Lua/child/missile và oracle Go/C# | P0 |
| Combat input | Có nhiều policy auto-target cạnh tranh | Một intent pipeline server-authoritative, tap attack/skill auto-acquire + hold-drag | P0 |
| Training | Có training NPC phục vụ test | Chỉ tồn tại trong DevHarness, thống nhất HP/config | P0 |
| Map Ba Lăng | Có dữ liệu và đường render nhưng tồn tại alias 53 -> 79 | Port canonical mapId 53, không alias/remap | P1 |
| Inventory | UI 6x10 xuất hiện nhưng domain cap/mutation drift và stub | 60 slot authoritative, item luôn một slot, mutation Python backend | P1 |
| Panel/popup | Có implementation stale, flat color/font fallback | Viết mới qua feature flag, dùng SPR PC Việt | P1 |
| HUD | Layout mobile hiện tại được chấp nhận | Giữ geometry; sửa binding/state/hitbox/Safe Area | P0/P1 |
| Backend | Python FastAPI có REST account/role/map/movement/gameplay; Unity có typed client | Hoàn tất Python realtime, auth/session production và live E2E; mock chỉ DevHarness | P1 |
| Persistence | Có mock/save slot cục bộ | PostgreSQL transaction/checkpoint/idempotency | P1 |
| Visual golden | Có comparer/fixture Unity | Chưa có PC runtime oracle; cần MinIO golden manifest | P0 blocker |
| Content delivery | Assets/SPR runtime rất lớn, chưa có download pipeline hoàn chỉnh | Core+Ba Lăng <=1.5GB, Addressables versioned | P1 |

## Quy tắc bảo toàn

- Không xóa code brownfield chỉ vì stale. Spec migration yêu cầu implementation mới song song, route bằng feature flag và legacy disabled để đối chiếu.
- Không migrate mock/local save thành dữ liệu production. Chỉ giữ làm fixture DevHarness.
- Không sửa HUD geometry; mọi thay đổi layout phải bị screenshot/rect hash gate chặn.
