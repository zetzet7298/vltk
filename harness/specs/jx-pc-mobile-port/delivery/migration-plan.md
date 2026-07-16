# Kế hoạch chuyển đổi brownfield

## Nguyên tắc

- Bảo toàn code hiện tại trong giai đoạn xây song song; route bằng feature flag ở dev/staging và rollout production có gate trước khi retirement.
- Production chỉ có Go backend và content resolver chuẩn; cấm fallback mock/legacy khi connect hoặc resolve thất bại.
- Không import save/mock/PlayerPrefs hiện tại thành dữ liệu production.
- Mọi migration có expand -> backfill/verify -> switch -> contract; rollback không làm mất committed economy.
- HUD hiện tại là baseline geometry `1280x720` và nằm ngoài migration panel. Chỉ panel/popup stale được xây song song; Safe Area chỉ transform toàn HUD root.
- Gameplay-first và client-priority: P0 combat/skill chạy qua DevHarness trước; mỗi
  wave gameplay phải giao đồng thời Unity presentation/controls để test được. Backend
  chỉ xây seam tối thiểu phục vụ wave hiện tại, không đi trước thành một platform rỗng.

## Contract rollout panel qua feature flag

| Thuộc tính | Quy định |
| --- | --- |
| Key | `ui.panel_v2.<panel_id>`; một key chỉ sở hữu một panel domain, không dùng wildcard đổi toàn UI |
| Payload | `variant`, cohort, rollout basis points, `min_client_version`, `content_version`, owner, reason, expiry, rollback key, flag revision; payload nằm trong bootstrap server-signed |
| Stickiness | Quyết định ổn định theo account/character + release; pin variant từ lúc mở tới khi đóng panel |
| Authority seam | Legacy/v2 gửi cùng command và nhận cùng event/schema; không chia đôi business rule, inventory/economy authority hoặc persistence |
| Pending/reconnect | Không đổi variant khi mutation pending. Sau reconnect phải resolve unknown outcome và snapshot revision trước khi route lần mở sau |
| Fail closed | Flag thiếu/hết hạn/không compatible chỉ dùng variant được release manifest cho phép. Legacy đã retire thì không được fallback production |
| Telemetry/gate | Ghi variant + flag revision + state/error/recovery; rollout `internal -> 1% -> 10% -> 50% -> 100%`, mỗi nấc cần parity, a11y, crash/error và unknown-outcome gate |
| Rollback | Đổi route cho lần mở tiếp theo; không rollback transaction đã commit, không thay content/schema/HUD geometry bằng flag |
| Retirement | Sau 100% ổn định qua một release rollback window: xóa route legacy, flag và asset/code unreachable theo evidence; DevHarness fixture có thể giữ riêng |

## Waves

| Wave | Thay đổi | Retirement gate |
| --- | --- | --- |
| `M0` | Pin source/content/tool, dựng catalogs và contracts | G1/G2 pass |
| `M1` | Gameplay-first P0: Go 18 Hz combat/skill + Unity target/skill presentation và test adapter qua DevHarness | Logic/replay G3 đủ evidence; C# không có authoritative rule |
| `M2` | Go auth/bootstrap/WSS seam tối thiểu + Unity production adapter để đưa đúng gameplay M1 lên realm | Legacy REST chỉ còn DevHarness; không còn combat REST per-tick production |
| `M3` | Client-priority vertical slice: Map53, inventory/economy/progression cùng panel v2 SPR-backed; HUD không đổi | G4 và UX/accessibility/parity panel chính pass |
| `M4` | Hoàn tất mọi panel/popup stale theo signed feature flag, rollout/rollback rồi retirement legacy | 100% v2 ổn định qua rollback window; không fallback legacy production |
| `M5` | P2-P4 domain waves | Catalog coverage và domain gate tương ứng |

## Rollback

- App/content/server release manifest pin độc lập nhưng chỉ activate tổ hợp compatible.
- Breaking schema dùng backward-compatible expand trước; server N/N-1 không đọc column chưa tồn tại.
- Content rollback chuyển manifest pointer về bundle đã ký; không hot-edit bundle.
- Economy failure chạy reconciliation từ balanced ledger/idempotency receipt, không sửa DB thủ công.
- Rollback panel giữ nguyên server/data contract. Panel đang pending tiếp tục variant đã pin tới khi semantic outcome được reconcile; chỉ lần mở sau nhận route rollback.
