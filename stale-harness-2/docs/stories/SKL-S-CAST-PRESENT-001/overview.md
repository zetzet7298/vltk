# SKL-S-CAST-PRESENT-001 — Shaolin cast presentation metadata

## Evidence

- Exact row authority: `harness/docs/stories/SKL-S-PROOF-001/PcShaolinSkills.txt`, SHA-256 `b6978c108ac0dc521e143e5f45d192babf2a818c75128983f2f3157b69e3c5c6`.
- Byte-preserving provenance: `harness/docs/stories/SKL-S-PROOF-001/PcShaolinSkills.provenance.json`, SHA-256 `e8920fdd977127651cea6d584a9e0823f21ac438b036ed8ab536fd2ea834a9a1`.
- Encoded source hash verified only through vltktool provenance: `/var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt` = `c77892fb33b6e63783c554bd075caa4891d9b9ec8abb70084582a5c24156e40c`.

## Catalog mapping

All 17 current Shaolin factory rows map exact `CharAnimId`/`WaitTime` from slice:
`3:14/0, 4:14/0, 6:14/0, 8:14/0, 9:14/0, 10:9/0, 11:10/0, 12:14/0, 13:11/5, 14:11/5, 15:11/5, 16:14/0, 17:10/0, 18:11/0, 19:11/5, 20:11/2, 21:14/0`.

Canonical `FMCastSnd` is empty or `0` for all current factory rows; factory clears female cast SFX. Canonical `ManCastSnd` is non-empty only for `10,11,13,14,15,17,18,19,20`; factory wires exactly those paths and clears all others.

Proven source PreCast SPR mappings, signed GB2312 JX UID computed with `vltktool/jx_hash.py`:

| Skill | Exact PreCastSpr | UID | Source presence |
| --- | --- | --- | --- |
| 13 | `\spr\skill\少林\sl_02_清心梵音.spr` | `ccea16f5` | `pak_unpacked/skills/unknown/ccea16f5.spr` |
| 15 | `\spr\skill\少林\sl_05_不动明王咒.spr` | `dd035109` | `pak_unpacked/skills/unknown/dd035109.spr` |
| 18 | `\spr\skill\少林\sl_04_慧眼咒.spr` | `afe532e2` | `pak_unpacked/skills/unknown/afe532e2.spr` |
| 20 | `\spr\skill\少林\sl_06_狮子吼.spr` | `65707acf` | `pak_unpacked/skills/unknown/65707acf.spr` |

Canonical ManCastSnd paths: `10 sound_k001`, `11 sound_k002`, `13 sound_k011`, `14 sound_k003`, `15 不动明王咒`, `17 sound_k004`, `18 慧眼咒`, `19 摩诃无量`, `20 狮子吼`. vltktool signed GB2312 UIDs: `88070215,8a75515e,abee70bb,847aefa7,2b2250c6,86683ae0,7988b3ac,45e9545c,3675c92c`.

## Fail closed

- All factory rows without canonical PreCastSpr clear generic `effectSourceId`.
- Learned-only roots `271,273,318,319,321,709,1055,1056,1057` have no existing Shaolin factory row; no row is created here. `273` references `born01.spr`, whose source asset was not found. `1056` references source-present `sl_150_gunshao_dl.spr` (`e5cd3974`) but remains unregistered.
- Source PreCast SPRs above are not currently staged under mobile `StreamingAssets`; logical catalog references are source-proven only, not packaged visual proof.
- CLIENTACTION weapon/blend map remains incomplete. No action mapping inference.
