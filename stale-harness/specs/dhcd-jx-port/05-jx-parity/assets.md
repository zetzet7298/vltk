# SPR, VFX, WAV Và Asset Policy

| Trường | Giá trị |
|---|---|
| Mục đích | Tận dụng tối đa asset có sẵn của JX mà không bịa art/effect |
| Trạng thái | `blocked` |
| Owner / reviewer | Asset owner / legal reviewer |
| Cập nhật | 2026-07-15 |

## Reuse-first

Ưu tiên theo thứ tự: exact selected JX asset -> exact JX variant đã audit -> tắt content/đánh dấu unavailable. Không generate art mới, không đổi màu/ghép effect “cho đẹp” nếu PC source còn câu trả lời.

## Provenance bắt buộc

Mỗi SPR/VFX/WAV/map/UI entry phải có absolute path, logical path, mọi candidate path hợp lệ, pack/version/load-order winner, Hash_UID, encoding, normalized `path_bytes_hex`, byte count, SHA-256, resolver command/output, `_labels.json:name_vi` cross-check khi có, decode validation, Unity import settings, usage references, legal state và golden.

File tồn tại không đủ; SPR phải kiểm action/direction/frame, VFX phải có link từ skill/missile/event, WAV phải có link từ config/runtime. Xem [spr-vfx-wav-manifest](spr-vfx-wav-manifest.md).

## Vendor policy

- `/var/www/jx-source` read-only.
- Chỉ copy exact bytes vào repo-local selected source khi đã chọn và dùng.
- Không commit secret hoặc toàn bộ PAK.
- Manifest ghi source path/hash để có thể tái kiểm.

## Release blocker

Legal clearance JX/DHCD chưa xác minh; asset chưa cleared chỉ được dùng trong internal evidence build theo approval, không public distribution. `DOC-JX-05` vẫn `blocked`.

## Acceptance

- [ ] SPR/VFX/WAV manifest complete, resolver/decode/golden pass.
- [ ] Mỗi selected asset có candidate list, Vietnamese label cross-check và SHA-256.
- [ ] Legal state/approval/expiry được ghi trước internal build; public gate fail khi chưa cleared.
