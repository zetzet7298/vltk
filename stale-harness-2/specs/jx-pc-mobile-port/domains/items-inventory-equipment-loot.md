# Domain: Item, túi, trang bị và loot

## Định danh và phạm vi

- Domain ID: `DOM-IIEL`; DRI: Backend Economy; reviewer: Gameplay/QA; sâu P1.
- Sở hữu item instance/content ref, 60 slot, equipment, loot ownership, use/drop/destroy, shop và persistence; direct trade phối hợp `DOM-STGPE`.

## Bằng chứng as-is

- `EVID-0026`: `KInventory.cpp:18-225` init/place/pick/find room trên grid; mục tiêu mobile cố ý đổi thành 60 ô, một item một ô.
- `EVID-0027`: `KItemList.cpp:207-227,918-1082,1271-1739` add, can-equip, equip/unequip/use; `:1850-2020` money/exchange.
- `EVID-0028`: `KPlayerDBFuns.cpp:858-930` lưu genre/detail/level/series, position, version/random seed, durability, stack, expire, lock/trade/drop flags và owner.
- `EVID-0029`: `KBuySell.cpp:222-430` check/can-buy/buy/sell. Source tĩnh không chứng minh transaction/crash behavior.

## Invariant mục tiêu

- Túi authoritative đúng 60 slot; mỗi item instance chiếm đúng một slot bất kể kích thước legacy; UI tuyệt đối không drag/drop.
- Item ở đúng một location: ground, inventory slot, equipment slot, trade escrow hoặc destroyed; transition atomic.
- Mutation use/equip/drop/sell/trade bị khóa khi combat state cấm; exact lock window BLOCKED `[CẦN XÁC NHẬN]`; owner Gameplay; gỡ block khi PC runtime golden + reviewer duyệt.
- Business ACK chỉ sau PostgreSQL commit; mọi economy command có idempotency key + expected aggregate revision.
- Loot có owner/party policy và expiry; exact party distribution BLOCKED `[CẦN XÁC NHẬN]`; owner Gameplay/Economy; gỡ block khi source rule + golden + reviewer duyệt.

## Contract và nghiệm thu

- Commands inventory revisioned: `Pickup`, `Use`, `Equip/Unequip`, `Move`, `Swap`, `Merge`, `Sort`, `Split`, `Drop`, `Destroy`; `Buy/Sell` đi qua economy command. Use không còn đi qua tick input deprecated. Mỗi command có `InventoryEvent` final; auto-loot dùng `LOOT_GRANTED` với grant ID để dedupe.
- `TEST-IIEL-001`: 60/60, túi đầy, duplicate pickup, reconnect; không mất/nhân item.
- `TEST-IIEL-002`: equip prerequisite/stat recompute, combat lock, retry/out-of-order revision.
- `TEST-IIEL-003`: crash tại từng điểm trước/sau commit của buy/sell/loot; tiền và item bảo toàn.
- Content catalog phải phủ 100% item/equipment/loot table đã phát hiện với source hash/provenance; resolver package/version/locale/hash hiện chưa pin nên catalog parity `BLOCKED`.
