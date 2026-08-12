# Design

## Source Authority

1. `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/` cho engine semantics.
2. `/var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt` và exact PAK/config rows cho static metadata.
3. Canonical sect Lua cho level curves, event gates và special behavior.
4. `~/Projects/vltktool` bắt buộc cho PAK/SPR/DAT/hash/encoded config.

## Coverage Model

Mỗi skill row trong matrix giữ:

- faction, skill ID, category, player/NPC scope;
- static-row source và Lua/C++ source;
- Unity definition owner;
- runtime handlers dùng;
- test fixture/cases;
- presentation contract: sex, mount state, concrete weapon resource variant,
  CharAnim/action bank, PC tick/effect timing, VFX lifecycle rows, audio slots,
  persistent buff/debuff/aura visuals and golden identifiers;
- proof state: `missing`, `source_only`, `mapped`, `functional`, `runtime_golden`, `parity_done`;
- blocker và next action.

`ChildSkillId` là dual namespace: PC `BaseSkill != 0` tạo missile trực tiếp,
còn `BaseSkill == 0` dereference child skill qua `g_SkillManager`. Không dùng
heuristic "ID có trong missles table thì là missile". `unity_ref` chỉ là candidate
navigation cho tới khi generator dereference được symbol/assignment thật; candidate
không được tự nâng field lên `verified`.

Presentation inventory joins the canonical membership slice to `skills.txt`,
`missles.txt`/`missles1.txt`, the state-aura mapping table, and shared NpcRes
action-bank tables. Each row records explicit source/Unity fields for cast,
flight, collide, vanish, sound, state persistence, gender/mount/weapon coverage,
and per-field provenance; absent bytes remain `missing` or `source_only` rather
than becoming a fabricated fallback.

## Implementation Strategy

- Sửa shared root trước per-skill patch.
- Exact source slices được load bằng Android-safe packaged resources khi runtime cần sync access.
- Stable identity là skill ID + level; child/event missiles giữ owner/level riêng.
- UI deck/panel chỉ expose skill có definition và runtime behavior hợp lệ.
- Mỗi wave có story con, verifier command và Herdr independent review.
- Presentation proof không được suy từ static catalog. `functional` cần runtime
  lifecycle assertions; `runtime_golden` cần same-frame PC/Unity capture metadata;
  `parity_done` cần threshold, platform và unresolved-byte gates đều pass.

## Wave Order

1. Coverage inventory + generic static-row diff across all sects.
2. Shared active/missile/melee/event runtime gaps.
3. Passive/buff/debuff/aura semantics.
4. Per-faction formula and child-chain parity.
5. Shared player-presentation foundation: gender/mount/weapon pose and PC clock.
6. Per-skill animation/VFX/audio/buff presentation lifecycle closure.
7. Platform/device smoke and PC runtime golden closure.
