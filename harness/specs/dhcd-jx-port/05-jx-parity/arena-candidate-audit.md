# Arena Candidate Audit

| Trường | Giá trị |
|---|---|
| Mục đích | Audit candidate arena trước khi chọn map pilot |
| Trạng thái | `not_started` |
| Owner / reviewer | JX map owner / technical reviewer |
| Cập nhật | 2026-07-15 |

## Candidate table

Mỗi dòng phải được ghi vào manifest versioned trước khi chọn winner:

| Candidate | Absolute candidate path(s) | Pack/version + load order | Hash/UID/bytes/SHA-256 | Resolver/decode | Evidence hiện có | Rejection/selection reason |
|---|---|---|---|---|---|---|
| `yanwuchang` | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | Tên textual trong `/var/www/jx-source/html/minimap.html` | `[CẦN XÁC NHẬN]` |
| `jingjichang` | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | Tên textual trong cùng HTML | `[CẦN XÁC NHẬN]` |
| `shiliantang` | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | `[CẦN XÁC NHẬN]` | dungeon script path dưới `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/missions/dungeon/shiliantang/` | `[CẦN XÁC NHẬN]` |

Textual name/script không chứng minh map ID, collision, bounds, spawn hay winner.

## Audit procedure

1. Enumerate all logical/hashed candidates using resolver and current PAK order; record every absolute path, including rejected candidates.
2. Decode `Region_C`, `Region_S`, terrain/minimap and record hash/provenance.
3. Import to isolated Unity scene; compare world coordinates, walkable mask, camera crop.
4. Spawn one verified player/NPC, test collision/height/portal and capture golden.
5. Select only one candidate for P0; keep rejected candidate path/provenance and reason in this manifest or a linked artifact.

## Acceptance

- Audit record complete for every candidate.
- Selected arena has deterministic map conversion version and collision golden.
- No candidate is promoted from name-only evidence.
