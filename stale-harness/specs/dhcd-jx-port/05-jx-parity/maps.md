# JX Maps

| Trường | Giá trị |
|---|---|
| Mục đích | Port map JX exact geometry/collision cho arena dọc |
| Trạng thái | `blocked` |
| Owner / reviewer | JX map owner / JX reviewer |
| Cập nhật | 2026-07-15 |

## Rule

Map runtime truth là candidate thuộc patch/version và active client load-order winner đã chứng minh trong `/var/www/jx-source/pak_unpacked/`; không chọn chỉ vì file mới nhất. Legacy source chỉ là behavior/config evidence, mtime chỉ tie-breaker sau khi version/load-order tương đương. Không sửa canonical source.

## Required evidence

- map ID và logical path;
- `Region_C` collision/height/bounds;
- `Region_S` spawn/NPC placement nếu dùng;
- terrain/scene/tileset/portal;
- minimap source và coordinate transform;
- pack/version/load-order winner, Hash_UID, encoding, byte count, SHA-256;
- Unity conversion artifact và visual/collision golden.

## Candidates

`yanwuchang`, `jingjichang`, `shiliantang` hiện chỉ là tên candidate từ textual/script evidence. Chưa có map ID, Region_C winner, collision geometry hoặc Unity golden nên chưa candidate nào là pilot arena.

## Conversion contract

- Giữ tile/world coordinate và height quantization theo source.
- Collision mask là authoritative cho movement/spawn; không dùng visual mesh thay thế.
- Camera portrait crop theo bounds, không scale làm thay đổi collision.
- Map conversion version đi vào replay header.

## Exit blocker

Tạo [arena-candidate-audit](arena-candidate-audit.md), chọn một winner và chạy map runtime test. Trước đó `DOC-JX-01` vẫn `blocked`.

## Acceptance

- [ ] Mọi candidate có absolute path/provenance và rejection record.
- [ ] Winner có Region_C/Region_S/map conversion, collision, camera và minimap golden.
- [ ] Unity runtime test xác nhận walkable mask/height/spawn trên portrait.
