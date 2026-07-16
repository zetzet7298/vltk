# Roster Và Progression

| Trường | Giá trị |
|---|---|
| Mục đích | Chốt roster pilot và đường tiến triển mà không bịa catalog |
| Trạng thái | `provisional` |
| Owner / reviewer | Content owner / JX reviewer |
| Cập nhật | 2026-07-15 |

## Roster pilot

| Phái | Pilot requirement | Evidence gate |
|---|---|---|
| Đường Môn | Một build verified, starter equipment set, action/direction đủ | Skill/item/player SPR manifest + golden |
| Cái Bang | Một build verified, starter equipment set, action/direction đủ | Như trên |
| Võ Đang | Một build verified, starter equipment set, action/direction đủ | Như trên |

Nam/nữ chỉ expose khi coverage SPR, skeleton/layer, action và equipment đã được decode và golden. Không thêm phái hoặc giới chỉ vì có tên trong config.

## Catalog audit

Trước khi mở build:

1. Enumerate toàn bộ skill tree và ID trong JX source.
2. Gắn từng skill vào icon, pre-cast, missile/child skill và visual.
3. Enumerate starter item/equipment, slot, stat, requirement và visual.
4. Đánh dấu `verified`, `provisional`, `missing`, `conflict`.
5. Chọn build pilot chỉ từ dòng `verified`; mọi fallback cần ADR.

## Progression contract (to-be)

- `Account` sở hữu `Character`.
- `Character` có faction, level, XP, skill points, inventory và equipment.
- Starter gear được cấp một lần, idempotent theo account/character.
- Skill ngoài run có level/cap và được server verify; run card không sửa trực tiếp permanent state.
- Kết thúc run tạo reward proposal; Go verifier xác minh trước khi commit.
- Inventory/equipment transaction có version và idempotency key.

## Chưa được quyết định

- Exact level curve, skill point curve, card cap, reroll cost và duplicate handling.
- Tốc độ mở khóa phái/giới.
- Có reset build hay không.

Các mục trên nằm trong [unresolved-rules](../10-research/unresolved-rules.md), không được hard-code từ suy đoán.

## P2 mount/pet gate

Mount/pet không thuộc P0/P1 và không được suy ra từ player/NPC asset gần giống. Identity, stat/skill/equipment link và SPR/VFX/WAV phải đi từ JX source qua resolver manifest; lifecycle, unlock và interaction mang claim DHCD phải chạy `R-DHCD-011` trước. Nếu reverse vẫn inconclusive, deviation chỉ được ship bằng product ADR đã approve sau reverse evidence.

### P2 acceptance

- Actor/catalog rows có absolute source, version/hash, resolver/decode và legal state.
- Ride/pet state, equipment/skill interaction và replay transition có golden C#/Go + visual test.
- Missing action/direction/layer fail closed; không synthesize mount/pet hoặc effect.

## Acceptance

- [ ] Catalog export ghi đủ ID/source row/asset provenance cho cả ba phái.
- [ ] Mỗi phái có một build verified và starter gear golden trước khi expose.
- [ ] Starter grant, skill progression và inventory transaction pass idempotency/replay test.
