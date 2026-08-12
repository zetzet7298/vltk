# Chính sách nguồn canonical

## Thứ tự nguồn

1. Runtime manifest tiếng Việt active tại `bin/client/package.ini` và winner theo đúng `KPakList`.
2. Client C++/Lua/config/PAK tương ứng cho behavior quan sát được, visual, UI và audio.
3. Server C++/Lua/config cho validation, persistence, economy và công thức authoritative khi evidence đầy đủ.
4. Tài liệu PC tại `/var/www/jx-pc/01_tinh_kiem_source/tai-lieu-game` làm nguồn hỗ trợ.
5. Unity hiện tại chỉ chứng minh as-is; `~/Projects/vltk` chỉ tham khảo UX.

Khi client và server mâu thuẫn, không tự chọn bằng tên file, mtime hoặc cảm tính. Ghi cả hai claim vào contradiction ledger; visual/interaction ưu tiên active client, authority ẩn ưu tiên server chỉ khi protocol/version khớp.

## Quy tắc package

- Client mở `package.ini`, đọc key số tăng dần, append PAK mở thành công và lookup first-match.
- Package không nằm trong manifest active là `inactive`, kể cả tên có suffix mới hơn.
- Logical path, UID, encoding, raw bytes, decoded bytes và checksum phải do `vltktool` cung cấp.
- Cấm alphabetical filesystem fallback, tự hash lại, decode đoán hoặc gộp asset nhiều phiên bản.
- Mỗi artifact được pin: source revision, package index, logical path bytes, UID, locale, byte count, SHA-256, vltktool revision/command và winner reason.

## Phân biệt census và provenance canonical

Hash trực tiếp do `harness/scripts/generate-jx-spec-catalog.py` tính chỉ là
`discovery census hash`: nó kiểm kê path/file bytes nhìn thấy trên filesystem,
phát hiện drift và giúp tái sinh catalog ổn định. Hash này không chứng minh
logical path bên trong PAK, package winner, UID, locale, encoding, raw bytes sau
lookup hoặc decoded bytes; vì vậy không được dùng để mở G1 hay promote artifact
thành `SOURCE_PROVEN`.

Canonical PAK provenance tiếp tục là `BLOCKED` cho đến khi đồng thời có:

1. Source snapshot và `vltktool` đều ở revision đã pin, clean, không có thay đổi
   chưa commit.
2. Resolver chạy theo active `package.ini` và đúng thứ tự `KPakList`, không dùng
   filesystem fallback.
3. Output lưu đủ package index, logical path bytes, UID, locale, encoding, raw và
   decoded SHA-256, command cùng revision của tool.
4. Reconciler đóng mọi winner/locale contradiction và reviewer duyệt artifact
   cùng revision.

Thiếu bất kỳ điều kiện nào thì census vẫn hữu ích cho discovery/coverage nhưng
G1 phải fail-closed. Không được đổi nhãn hash census thành hash canonical để né
blocker.

## Mức bằng chứng

| Mức | Ý nghĩa | Trạng thái tối đa |
| --- | --- | --- |
| `E0` | Chỉ thấy file/symbol | `DISCOVERED` |
| `E1` | Source/config được trace, provenance pin | `SOURCE_PROVEN` |
| `E2` | Contract và behavior được đặc tả, contradiction đóng | `SPECIFIED` |
| `E3` | Automated/runtime test cùng revision | `FUNCTIONAL` hoặc `AUTOMATED_VERIFIED` |
| `E4` | PC golden + mobile capture + reviewer | `PARITY_DONE` |

Live PC runtime hiện chưa chạy được do thiếu trusted binary/config/server stack. Static evidence được dùng để tiếp tục đặc tả và implementation, nhưng visual/audio/behavior cần runtime oracle không được lên `PARITY_DONE`.
