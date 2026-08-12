# SKL-S-PROOF-001 — Thiếu Lâm canonical learned-membership and static catalog proof

## Scope

Prove canonical PC learned-membership, observed Unity display membership, and exact
static rows for Thiếu Lâm. Không infer UI order. Không claim runtime, visual, audio,
platform hay PC golden parity.

## Canonical Evidence

- Progression: `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/global/skills_table.lua`, block `shaolin`, lines 1386–1396, SHA-256 `7e46896c4d5c3fc33cf3b1119ec3e6cf7b1a2c8d7a64ab25d2087331646642b3`.
- Skillbook: `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/script/item/skillbook.lua`, faction index 0, line 2, SHA-256 `4e5361a6d2756f3596fcc86155dd579b8bf15f69c73651d7f9e8c40f3337d0d9`.
- Static rows: `/var/www/jx-pc/pak_unpacked/slistcache/settings/skills.txt`, SHA-256 `c77892fb33b6e63783c554bd075caa4891d9b9ec8abb70084582a5c24156e40c`, verified only through vltktool provenance.

## Result

- Learned roots (20): `4,6,8,10,11,14,15,16,19,20,21,271,273,318,319,321,709,1055,1056,1057`.
- Observed Unity display roots (17): `3,4,6,8,9,10,11,12,13,14,15,16,17,18,19,20,21`.
- Partition: 11 shared; 9 PC-learned-only (`271,273,318,319,321,709,1055,1056,1057`); 6 Unity-only unresolved (`3,9,12,13,17,18`); union 26; gap 15.
- Exact 26-row slice: `PcShaolinSkills.txt`, SHA-256 `b6978c108ac0dc521e143e5f45d192babf2a818c75128983f2f3157b69e3c5c6`; byte-preserving provenance SHA-256 `e8920fdd977127651cea6d584a9e0823f21ac438b036ed8ab536fd2ea834a9a1`.
- Direct relationship targets from learned roots: `22,61,66,76,77,86,135,136,186,202,216,272,317,318,319,1083,1085`. Only `318,319` overlap learned roots. No Unity-only root is a learned-root relationship target; `13,17,18` have outbound relationships but remain unresolved display rows.
