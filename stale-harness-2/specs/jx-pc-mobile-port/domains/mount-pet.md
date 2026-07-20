# Domain: Thú cưỡi và pet/đồng hành

## Định danh và phạm vi

- Domain ID: `DOM-MP`; DRI: Gameplay Companion; reviewer: Backend/QA; mount P2, pet P3.
- Sở hữu mount equip/ride modifier và pet summon/follow/skill/persistence; item ownership phối hợp `DOM-IIEL`.

## Bằng chứng as-is

- `EVID-0030`: `KNpcTemplate.h:61-66` có armor/helm/weapon/horse type và ride-horse ở client template.
- `EVID-0031`: `KPlayerPartner.cpp/.h` và `KPartnerSkill.cpp/.h` tồn tại seam partner/skill; source tĩnh chưa đủ xác định pet lifecycle.
- `EVID-0032`: `test_horse_detail.py` trong corpus là artifact khảo sát, không phải runtime PC golden.

## Invariant và contract

- Mount instance thuộc đúng một character; ride chỉ khi item/equipment/map/combat rule cho phép. Modifier tốc độ do server tính.
- Pet instance thuộc đúng một character; tối đa active count BLOCKED `[CẦN XÁC NHẬN]`; owner Gameplay; gỡ block khi source rule + runtime golden được reviewer duyệt; follow/teleport/despawn không vượt authority world.
- Pet cast đi qua combat validator với owner attribution và deterministic RNG stream được pin.
- Wire `CompanionCommand` map chính xác `EQUIP_MOUNT`, `RIDE`, `DISMOUNT`, `SUMMON_PET`, `DISMISS_PET`, `SET_PET_MODE`, có `expected_revision`; server trả `CompanionEvent` và tự tính modifier tốc độ.

## Coverage và nghiệm thu

- P2 catalog toàn bộ horse/mount content, sprites và modifier; P3 catalog partner/pet/skill/progression; P4 interaction PvP/endgame.
- `TEST-MP-001`: mount speed reconcile, map/combat restriction, reconnect while riding.
- `TEST-MP-002`: pet summon/follow/death/dismiss/persistence và owner reward attribution.
- Exact horse/pet catalogs, limits và PC behavior hiện `BLOCKED` đến resolver pin package/version/locale/hash.
