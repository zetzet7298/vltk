# JX Items Và Equipment

| Trường | Giá trị |
|---|---|
| Mục đích | Reuse item/equipment identity và visual JX cho starter gear/inventory |
| Trạng thái | `provisional` |
| Owner / reviewer | Item owner / JX reviewer |
| Cập nhật | 2026-07-15 |

## Manifest fields

`item_def_id`, original logical path, item type/slot, stackability, stats, requirement, faction/gender restriction, equip visual layers, icon/ground SPR, sound/effect, source row, pack/version/load-order, Hash_UID, encoding, byte count, SHA-256, resolver/decode evidence.

## Mapping rule

- Không dùng mapping tên gần giống nếu thiếu ID/source row.
- `MapEnemyDatabase`/item mapping hiện hữu là provisional và không được làm authority.
- Starter gear chỉ chọn item có icon + equip visual/action đủ; thiếu layer thì không expose.
- Item instance/economy contract nằm ở [data-model](../07-server/data-model.md), còn bytes/visual ở đây.

## Acceptance

- Catalog import không tạo duplicate identity.
- Equip/unequip visual golden đúng layer/direction/action.
- Item reward từ replay trỏ tới `item_def_id` đã verified.
