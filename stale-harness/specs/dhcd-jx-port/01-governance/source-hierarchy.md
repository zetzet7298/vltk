# Source Hierarchy Và Quy Tắc Provenance

| Trường | Giá trị |
|---|---|
| Mục đích | Chặn việc lấy nhầm source hoặc biến suy đoán thành parity |
| Trạng thái | `verified` cho policy; từng mapping vẫn có thể `provisional` |
| Owner / reviewer | Technical lead / JX reviewer |
| Cập nhật | 2026-07-15 |

## Thứ tự authority theo loại claim

| Claim | Authority | Không được dùng thay thế |
|---|---|---|
| JX identity, ID, map, NPC, item, skill base | `/var/www/jx-source/pak_unpacked/` và source audit liên quan | screenshot, generated dump, mapping cũ |
| PAK hash, encoding, SPR/VFX/WAV | `~/Projects/vltktool` + resolver evidence | tự hash/đoán tên file |
| DHCD loop/wave/card/drop clue | `/home/zet/Projects/dhcd/reconstructed-types` và docs | “cảm giác” từ game khác |
| Reverse gap | `/var/www/reverse-skill` rồi cập nhật `/home/zet/Projects/dhcd` | bịa code để lấp lỗ hổng |
| Current Unity behavior | `/var/www/vltk-mobile` | coi implementation hiện tại là đúng parity |
| New backend | `vltk-mobile/server` contract mới | DHCD server binary/schema |

## Quy tắc chọn JX candidate

1. Enumerate **toàn bộ** candidate hợp lệ trong PAK/unpacked tree và ghi absolute path của từng candidate.
2. Xác định patch/version và active client load-order winner từ package order; không chọn chỉ vì file mới hơn. mtime chỉ là tie-breaker khi version/load-order đã tương đương.
3. Resolve logical path bằng `~/Projects/vltktool`; khi `_labels.json` có label, bắt buộc cross-check `name_vi` để không chọn nhầm bản tiếng Trung.
4. Ghi resolver encoding và `path_bytes_hex`/normalized bytes; decode SPR và kiểm tra action, direction, frame; file tồn tại chưa đủ.
5. Ghi original path, pack/version, load-order winner, Hash_UID, encoding, byte count, SHA-256, resolver/decode command và kết quả cross-check vào manifest.
6. Chỉ vendor exact bytes khi asset đã được chọn và thực sự dùng; không copy để làm “evidence”.

## Quy tắc DHCD

Corpus reverse là evidence cấp declaration/IL-recovery, không phải source C# gốc. Mỗi claim phải ghi loại bằng chứng và giới hạn. Các vùng malformed IL hoặc thiếu method phải đưa vào reverse queue; không tự điền.

## Quy tắc thay đổi

Mọi deviation so với JX hoặc DHCD chỉ được đề xuất sau evidence discovery tương ứng: JX phải enumerate/source-audit đầy đủ; DHCD behavior gap phải chạy reverse task bằng `/var/www/reverse-skill` và cập nhật corpus trước. Nếu evidence vẫn không kết luận được mà sản phẩm cần ship rule mới, ADR đã approve phải ghi owner, lý do, ảnh hưởng save/replay, migration plan và test. ADR không được thay thế bước reverse/source audit; nếu PC source trả lời được thì không dùng product design để thay thế.

## Liên kết

- [evidence-register](evidence-register.md)
- [adr-policy](adr-policy.md)
- [assets](../05-jx-parity/assets.md)

## Acceptance

- [x] JX/DHCD/Unity/backend claim types đều có authority và forbidden substitute.
- [x] Candidate selection giữ full enumeration/load-order/encoding/label/decode evidence.
- [x] Không có selected bytes được vendor chỉ để làm evidence.

## Verification record

Kiểm tra tĩnh ngày `2026-07-15` trong audit orchestrator: ma trận authority bao phủ đủ bốn claim groups; quy trình candidate nêu đủ enumeration, load-order, encoding, `name_vi` và decode; policy cấm vendor bytes chỉ để làm evidence. Đây là kiểm tra policy/document, không thay thế approval của owner/reviewer và không phải bằng chứng asset đã được chọn hoặc runtime đã chạy.
