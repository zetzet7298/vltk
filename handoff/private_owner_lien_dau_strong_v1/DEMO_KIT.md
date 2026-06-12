# Demo kit — 2 map Liên đấu encrypted handoff

## Mục tiêu demo

Chứng minh 3 ý:

1. 2 map đã chạy được trong Unity runtime.
2. Gói bàn giao cho khách đã được đóng gói/mã hóa, không lộ raw kỹ thuật.
3. Khách chỉ chạy được khi mua runtime/key/integration từ mình.

## Demo 1 — Live screen-share, khuyến nghị

Dùng khi khách đang hỏi mua nhưng chưa thanh toán.

### Chuẩn bị

- Unity project local: `/var/www/vltk-mobile`
- Scene: `Assets/Scenes/Sandbox.unity`
- Public encrypted package:
  - `/var/www/vltk-mobile/handoff/customer_release_lien_dau_strong_v1.zip`
- Private key giữ kín:
  - `/var/www/vltk-mobile/handoff/private_owner_lien_dau_strong_v1/OWNER_KEYS.json`

### Flow nói chuyện

1. Mở folder khách:
   - chỉ có `LienDauMaps.vltkmap`, manifest public, checksum.
   - nói: “Đây là bản bàn giao encrypted, không có Region/Sprite/source rời.”
2. Mở Unity bản nội bộ.
3. Load map `397` — Đấu trường liên đấu.
4. Di chuyển/camera quanh map, show visual thật.
5. Load map `396` — Hội trường liên đấu.
6. Show checksum package.
7. Chốt: “Sau khi thanh toán/hợp đồng, bên mình cấp runtime loader hoặc tích hợp trực tiếp vào project bạn.”

## Demo 2 — Video demo

Dùng khi khách cần gửi sếp/team xem.

### Shot list

1. 5 giây: folder encrypted package, không có raw data.
2. 10 giây: Unity load map 397.
3. 15 giây: đi quanh map 397.
4. 10 giây: switch map 396.
5. 15 giây: đi quanh map 396.
6. 5 giây: checksum + acceptance summary.

### Câu caption

- “Encrypted handoff package — no raw map/source exposed.”
- “Map 397: Đấu trường liên đấu — Unity runtime.”
- “Map 396: Hội trường liên đấu — Unity runtime.”
- “Runtime/license/key delivered after contract.”

## Demo 3 — Hands-on customer trial

Chỉ dùng nếu khách muốn tự chạy.

Không gửi raw key. Làm bản trial riêng:

- build Unity demo executable/APK
- nhúng `LienDauMaps.vltkmap`
- loader/key nằm trong binary
- giới hạn trial:
  - chỉ load 2 map
  - watermark
  - hết hạn theo ngày
  - không export asset
  - không có source/runtime DLL rời nếu chưa mua

## Không được làm khi chưa chốt hợp đồng

- Không gửi `OWNER_KEYS.json`.
- Không gửi `lien_dau_map_pack_v1/` raw staging.
- Không gửi `LienDauMaps.payload.zip`.
- Không gửi script decrypt.
- Không gửi source runtime renderer/parser nếu chưa license.

## Chốt đơn đề xuất

- Bước 1: demo live/video.
- Bước 2: khách xác nhận scope: data-only / integration / full runtime.
- Bước 3: nhận cọc.
- Bước 4: nếu cần hands-on, build trial binary có watermark.
- Bước 5: thanh toán đủ, bàn giao runtime/key/license theo hợp đồng.
