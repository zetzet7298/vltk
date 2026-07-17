# Design

## Source Authority

1. `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/` cho engine semantics.
2. `/var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt` và exact PAK/config rows cho static metadata.
3. Canonical sect Lua cho level curves, event gates và special behavior.
4. `~/Projects/vltktool` bắt buộc cho PAK/SPR/DAT/hash/encoded config.

## Coverage Model

Mỗi skill row trong matrix giữ:

- faction, skill ID, category, player/NPC scope;
- static-row source và Lua/C++ source;
- Unity definition owner;
- runtime handlers dùng;
- test fixture/cases;
- proof state: `missing`, `source_only`, `mapped`, `functional`, `runtime_golden`, `parity_done`;
- blocker và next action.

## Implementation Strategy

- Sửa shared root trước per-skill patch.
- Exact source slices được load bằng Android-safe packaged resources khi runtime cần sync access.
- Stable identity là skill ID + level; child/event missiles giữ owner/level riêng.
- UI deck/panel chỉ expose skill có definition và runtime behavior hợp lệ.
- Mỗi wave có story con, verifier command và Herdr independent review.

## Wave Order

1. Coverage inventory + generic static-row diff across all sects.
2. Shared active/missile/melee/event runtime gaps.
3. Passive/buff/debuff/aura semantics.
4. Per-faction formula and child-chain parity.
5. Platform/device smoke and PC runtime golden closure.
