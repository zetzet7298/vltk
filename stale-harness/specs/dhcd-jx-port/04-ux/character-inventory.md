# Character Và Inventory

| Trường | Giá trị |
|---|---|
| Mục đích | Thiết kế portrait flow cho character/equipment/inventory với server authority |
| Trạng thái | `design` |
| Owner / reviewer | UX owner / server reviewer |
| Cập nhật | 2026-07-15 |

## Flow

`Lobby -> Character select/create -> Faction/build -> Equipment -> Inventory -> Enter run`.

Mọi màn hình có loading, stale-version, empty-state, retry và reconnect. Không cho equip item chưa có provenance hoặc không hợp faction/slot.

## Identity contracts

- Item identity là immutable `item_def_id`; instance stack/equipment có `instance_id`.
- Equip/unequip/split/consume là transaction server-side, idempotent.
- Client optimistic preview phải revert nếu version conflict.
- Starter gear grant một lần theo account/character.

## Visual

Portrait card/slot sử dụng JX item/player visual đã resolve; gender/armor combination thiếu frame/action phải bị ẩn khỏi picker, không render placeholder.

## Acceptance

- Contract test cho stale version, duplicate idempotency, reconnect.
- UI golden cho empty/loading/error/equipped.
- Reward receipt từ run link được tới inventory transaction.
