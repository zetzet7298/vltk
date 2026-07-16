# Extending to female / equipment / mount

The male avatar (`MalePlayerSpriteCatalog` + `MalePlayerVisual`) is the reference. The
layered model is gender-agnostic, so adding a new avatar is mostly data, not new logic.
Prefer **generalizing** over copy-pasting an entire second class hierarchy.

## What actually differs between avatars

| Dimension | Male | Female | Notes |
|-----------|------|--------|-------|
| Thư mục nghệ thuật (art folder) | `spr\npcres\man` | `spr\npcres\woman` | Lấy từ `人物类型.txt` |
| Tiền tố tệp tin (filename prefix) | `MA_` | `WO_` (ví dụ: `WO_BD_019_ST01.spr`) | Xác nhận bằng cách kiểm tra thư mục `woman/` |
| Bảng thứ tự vẽ (draw-order) | `男主角贴图顺序表` | `女主角贴图顺序表` | Dir1..Dir8 giống hệt nhau -> Dùng chung `SortingOffset` |
| Danh sách phần / offsets | Theo từng file SPR | Theo từng file SPR | Giải mã động lúc runtime, không cần đổi code |

Tất cả các cơ chế khác — SpriteRenderer trên từng bộ phận, giải mã SPR, offset điểm tham chiếu (ref-pixel), chuyển tiếp frame (frame stepping), ánh xạ hướng di chuyển di động 8 hướng, các lỗi hiển thị (vẽ dưới map, Domain Reload cache) — đều được chia sẻ chung hoặc dùng chung thuật toán.

## Trạng thái đã triển khai (Implemented Status)

Hệ thống đã hỗ trợ cả hai giới tính và được chia thành các lớp tương ứng kế thừa cùng một logic cốt lõi:
1. **Catalog**:
   - Nhân vật Nam: [MalePlayerSpriteCatalog.cs](../../../../../Assets/Scripts/Sandbox/MalePlayerSpriteCatalog.cs)
   - Nhân vật Nữ: [FemalePlayerSpriteCatalog.cs](../../../../../Assets/Scripts/Sandbox/FemalePlayerSpriteCatalog.cs)
   - Cả hai Catalog đều dùng chung bảng chỉ số thứ tự vẽ (draw-order offset) cho 8 hướng.
2. **Visual Renderer**:
   - Lớp [MalePlayerVisual.cs](../../../../../Assets/Scripts/Sandbox/MalePlayerVisual.cs) hiển thị cho nam, hỗ trợ cưỡi ngựa và trang bị mặc định là "áo vải thô" (`BD` variant `019`) và "mũ bố cân" (`HD`/`HR`/`HT` variant `019`).
   - Lớp [FemalePlayerVisual.cs](../../../../../Assets/Scripts/Sandbox/FemalePlayerVisual.cs) hiển thị cho nữ, hỗ trợ tải các bộ phận với tiền tố `WO_` từ thư mục `woman`.
3. **Điều khiển và Trạng thái**:
   - `SandboxPlayerController.cs` quản lý di chuyển đầu vào và chuyển đổi trạng thái giữa Cưỡi ngựa (RD - Ride) và Đi bộ (RN - Run) / Đứng yên (ST - Stand).

## Equipment / weapon swap

Swapping armor or weapon is only a **variant number change** for that part (e.g. body
`019` -> another id, empty weapon `000` -> a real weapon id). Stage the new part SPRs,
point the appearance's variant at them, done. The draw-order and everything else are
unchanged. Watch for weapon parts that introduce special multi-weapon poses — those use
`动作贴图顺序表.INI` overrides; only wire that if the pose actually needs it.

## Mount (cưỡi ngựa)

Trạng thái cưỡi ngựa yêu cầu thêm 3 bộ phận ngựa (IDs 12/13/14 tương ứng với đầu ngựa `HH`, thân ngựa `HB`, đuôi ngựa `HT`) kết hợp với tập hành động `RD` (ride) của người cưỡi.
- **Quan trọng**: Tránh sử dụng các biến thể thú cưỡi không có đầy đủ tài nguyên trong PAK (như variant `016`). Luôn kiểm tra sự tồn tại của các bộ phận HH, HB, HT trước khi sử dụng.
- **Biến thể mặc định**: Sử dụng ngựa **Siêu Quang (variant 019)** vì có đầy đủ tài nguyên phân bổ trong `updatejx06.pak` (HH, HT) và `1.pak` (HB). Khi cưỡi ngựa, hệ thống sẽ tải 5 bộ phận người cưỡi trong tư thế `RD` cộng với 3 bộ phận ngựa (HH, HB, HT), tổng cộng là 8/12 bộ phận hoạt động.

## Before shipping a new avatar

- Stage SPRs with `/var/www/vltk-mobile/scripts/stage_all_sandbox_sprites.py` into
  the appropriate manifest and let Unity import them.
- Run `scripts/verify_player.cs` through the live `execute_code` tool (point the
  GameObject name at the new avatar) — all
  four checks must pass, especially CHECK 1 (parts loaded) and CHECK 3 (A/B visible).
- Add/extend EditMode tests mirroring `MalePlayerVisualTests`.
- Update `CHANGELOG.md` and the harness story.
