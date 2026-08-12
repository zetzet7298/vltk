# SKL-EM-PROOF-001 — Nga My static learned-membership proof

## Canonical evidence

- Progression: `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua`, lines 1431–1438, SHA-256 `7e46896c4d5c3fc33cf3b1119ec3e6cf7b1a2c8d7a64ab25d2087331646642b3`.
- Skillbooks: `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua`, line 6, SHA-256 `4e5361a6d2756f3596fcc86155dd579b8bf15f69c73651d7f9e8c40f3337d0d9`.
- Static rows: `/var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt`, source SHA-256 `c77892fb33b6e63783c554bd075caa4891d9b9ec8abb70084582a5c24156e40c`, verified only by vltktool provenance.

## Exact result

- Learned (21): `77,79,80,82,85,86,88,89,91,92,93,252,282,328,332,380,385,712,1061,1062,1114`.
- Observed Unity display (15): `77,79,80,81,82,83,84,85,86,87,88,89,91,92,93`.
- Shared (11): `77,79,80,82,85,86,88,89,91,92,93`.
- PC-only (10): `252,282,328,332,380,385,712,1061,1062,1114`.
- Unity-only unresolved (4): `81,83,84,87`.
- Union: 25. Symmetric gap: 14. `ui_order: null`.
- Skill `90` remains Côn Luân-only learned evidence. Never promoted into Nga My.

## Relationship closure

Learned roots point to 23 support-only targets:
`2,3,4,5,68,101,142,186,191,206,207,208,243,281,323,324,329,331,333,375,718,1089,1115`.

No target is an EMei learned root. No Unity-only unresolved ID is a learned relationship target.

## Pinned artifacts

- `PcEMeiSkills.txt`: `8dabca7226f8dd0ff6c0731a60c87ea842399ba865e43b4800ca1e0d47e117b9`
- `PcEMeiSkills.provenance.json`: `7ea972df58be964ffe7833be5f1ee28ff39349d0b060c636559c0aacaa93d359`
- `PcEMeiRelationshipTargets.txt`: `86d535432340b1d9223a0e9f7c6ccc3de3c8ebe60556932136b8181ce2d9ee8e`
- `PcEMeiRelationshipTargets.provenance.json`: `37e9d89a99689b6c5cf137cae361ff34d890432fb1a64441c64a83059010bdfe`
- `membership-classification.json`: `cafa206bbe716699e996dc15e5e892163c71e67ad3df34d08f955b9a19b89d62`
- `static-catalog-proof.json`: `002618cbfb3c79c0e7e57bc7669de37653dc437ff912ffc5349bf4676db8873d`

No runtime, platform, UI-order, factory, visual, audio, or formula parity claim.
