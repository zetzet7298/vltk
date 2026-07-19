# Tang Men lifecycle SPR provenance

Scope: four binary SPR winners only. No mapper, catalog, runtime, or SFX behavior changed.

## Active package winner

Canonical client `package.ini` lists `updatejx09.pak` at index 8 before
`updatejx08.pak` at index 9. `updatejx09` is selected as active winner.
All four same-path entries exist in both packages and are byte-identical, so
priority does not change any vendored byte.

| UID | PC logical path | updatejx09 PAK record | bytes | SHA-256 | SPR header |
| --- | --- | --- | ---: | --- | --- |
| `da0d555d` | `\spr\skill\1502\tm\tm_150_daotang_bz.spr` | offset `67031111`, size `170132`, flag `0x20013a27` | 170132 | `1bf54340a9eb390e728a5203122775e46f39ba77ed18ee638f448eefdf460b7d` | 200x160, 20 frames, 1 dir, interval 40 |
| `56ac3571` | `\spr\skill\150\tm\tm_150_sanhuatiannv_c_a.spr` | offset `24895484`, size `31219`, flag `0x200053f0` | 31219 | `1d60bfa9a4717dad6c2dbb9b243333bd63ce4b04cae27c14c2c5ad2d744e24a0` | 80x80, 16 frames, 16 dirs, interval 1 |
| `d1f0327d` | `\spr\skill\1502\tm\tm_150_daotang_zd.spr` | offset `65290813`, size `65664`, flag `0x20007499` | 65664 | `7f9298e68e80b0ff210361c03f6b3c1e1dd4e2507f8287b5133017d10b121988` | 60x80, 16 frames, 16 dirs, interval 1 |
| `53144a68` | `\spr\skill\150\tm\tm_150_sanhuatiannv_c_b.spr` | offset `23902889`, size `115005`, flag `0x20014f47` | 115005 | `ca53c1c08935e20101b3e0d40e34ca9071514f53b07ad168a4180b28c75680ef` | 80x80, 10 frames, 1 dir, interval 20 |

UIDs are `vltktool/jx_hash.py` signed-byte JX hashes of exact ASCII PC paths
under GBK. `vltktool/unpak_tool.py::decompress_entry` extracted each PAK index
entry in memory; bytes matched its canonical unpacked path and updatejx08.
`vltktool/extract_item_spr.py` decoded every frame: 20/20, 16/16, 16/16, and
10/10 respectively. No candidate file was copied as evidence.

## Lifecycle binding

Canonical `Assets/StreamingAssets/Reference/PcSkill/skills.txt`:

```text
1069 child 331, collide 1097; 1097 child 359
1070 child 332, fly 1098;     1098 child 360
1110 child 374, vanish 1113;  1113 child 161
```

Canonical `PcAttrib/missles1.txt` flight rows:

```text
331 -> tm_150_daotang_zd.spr  (16,16,1)
359 -> tm_150_daotang_bz.spr  (19,1,1)
332 -> tm_150_sanhuatiannv_c_b.spr (10,1,1)
360 -> tm_150_sanhuatiannv_c_a.spr (16,16,1)
```

## Deliberate blocker

SFX UID `fb83e17b` remains absent. No substitute was staged and no SFX mapping
was changed. Runtime stays silent/fail-closed for it.

These bytes prove resource identity and decode, not PC frame similarity,
PlayMode capture, device output, or visual parity.
