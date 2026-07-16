# Skill System

| Trường | Giá trị |
|---|---|
| Mục đích | Giữ JX skill identity/base behavior/visual, đưa auto-cast vào loop DHCD |
| Trạng thái | `provisional` |
| Owner / reviewer | Gameplay owner / JX reviewer |
| Cập nhật | 2026-07-15 |

## Authority

- Skill ID, name mapping, level/base formula, icon, pre-cast, missile/child skill, VFX/WAV: JX source sau resolver.
- Card/upgrade timing, choice/reroll flow: DHCD evidence trong phạm vi recovered; thiếu thì ADR.
- Unity skill runtime phải trace được về PC row/Hash_UID; không tạo “skill tương tự” khi PC row tồn tại.

## Resolver JX-first

1. Resolve `SkillId` từ JX catalog.
2. Load exact config version và referenced resources.
3. Validate target/formation/damage/state/child event.
4. Nếu thiếu resource hoặc conflict, mark unavailable và fail content gate; không silent fallback.
5. Auto-cast chọn skill theo deterministic priority đã cấu hình trong run build; priority là product design nếu JX không có tương đương.

## Run/card boundary

Permanent skill/buff/support level và loadout nằm ngoài run. Card chỉ nâng parameter run-local khi schema ghi rõ cap, stacking và expiry; card không mutate permanent progression trực tiếp. Buff/support permanent chỉ được nâng qua transaction ngoài run, và card projection chỉ eligible khi cap tương ứng còn chỗ.

## Acceptance

- Catalog row -> Unity runtime -> visual golden traceable.
- Damage/status/child skill golden vectors pass trên C# và Go.
- Missing/ambiguous resource không được spawn invisibly.
- Auto-cast cùng state/input tạo cùng skill ID và event sequence.

## Chưa chốt

Exact card cap, curve giảm weight theo copy, cost, reroll price, target tie-break và pause semantics. Invariant weight không tăng theo owned-copy là product contract; exact curve vẫn unresolved. Xem [deck-timeline](deck-timeline.md) và [unresolved-rules](../10-research/unresolved-rules.md).
