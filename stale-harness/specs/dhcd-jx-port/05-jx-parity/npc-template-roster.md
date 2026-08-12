# NPC Template Roster

| Trường | Giá trị |
|---|---|
| Mục đích | Tạo roster NPC có source row và visual/AI provenance |
| Trạng thái | `not_started` |
| Owner / reviewer | JX enemy owner / gameplay reviewer |
| Cập nhật | 2026-07-15 |

## Required row

| Field | Nội dung |
|---|---|
| `npc_template_id` | ID JX, immutable |
| Source row | Absolute `NpcS.txt`/`npcs.txt` path + line/record key |
| Map placement | Region_S/map ID/cell nếu có |
| Stats/AI | Level, HP, attack, speed, task, flags |
| Skills | JX skill IDs và resolved assets |
| Visual | SPR logical/hashed path, action/direction/frame, shadow |
| Drop | Reward table link, không copy số chưa audit |
| Provenance | pack/version, UID, encoding, byte count, SHA-256 |
| Tests | spawn/AI/collision/death/drop golden |

## Migration

Import roster vào data versioned; generated Unity mapping chỉ là artifact, không chỉnh tay để lấp thiếu. Existing sandbox rows được đánh dấu `provisional` cho tới khi import row + tests pass.

## Acceptance

- [ ] Mỗi pilot template có source row, map placement, stat/AI, skill và visual provenance.
- [ ] Spawn/AI/collision/death/drop tests pass từ cùng data version.
- [ ] Fallback/default row không xuất hiện trong pilot export.
