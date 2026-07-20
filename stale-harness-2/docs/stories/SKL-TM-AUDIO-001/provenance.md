# SKL-TM-AUDIO-001 — missile 352 sound provenance

## Verdict

`01e95b13` resolves exactly to `\sound\skill\飘雪穿云.wav`. It is **flight** audio: missile 352 `SndFile2` / `MS_DoFly`. It is not impact audio.

Missile 352 `SndFile4` / `MS_DoCollision` is empty. No collision WAV exists in its canonical row. Runtime must remain silent/fail-closed for this collision; no substitute, rename, transcode, or synthesized impact byte was added.

## Canonical config proof

`~/Projects/vltktool/extract_table_slice.py` selected row 352 from canonical `/var/www/jx-source/pak_unpacked/slistcache/settings/missles.txt`. Its SHA-256 is byte-identical to pinned `Assets/StreamingAssets/Reference/PcAttrib/missles1.txt` (`e893c7af74d43672f1513b8325e31ba3270ebe425ac668f1b444e81db845e8bc`). `slistcache.pak` is package.ini priority index `2`; available update-table copies either omit 352 or have the same selected Snd fields.

| field | value |
| --- | --- |
| source SHA-256 | `e893c7af74d43672f1513b8325e31ba3270ebe425ac668f1b444e81db845e8bc` |
| row | 353 |
| SndFile2 | `\sound\skill\飘雪穿云.wav` |
| SndFile4 | empty |

Status mapping comes from `PcMissileFullVisualParser`: `AnimFile2/SndFile2 = MS_DoFly`; `AnimFile4/SndFile4 = MS_DoCollision`.

## UID, package, and bytes

`~/Projects/vltktool/jx_hash.py` hashes exact GBK path bytes:

```text
logical_path: \sound\skill\飘雪穿云.wav
encoding: gbk
path_bytes_hex: 5c736f756e645c736b696c6c5cc6aed1a9b4a9d4c62e776176
uid: 01e95b13
```

Canonical client `package.ini` lists `sound.pak` at priority index `30`. Header-index scan with `~/Projects/vltktool/unpak_tool.py::decompress_entry` found exactly one `01e95b13` entry across available listed packages: `sound.pak`, ordinal `4`, offset `8752002`, flag `0x0100b249`. Therefore no same-UID package conflict exists; `sound.pak` is selected winner.

| field | value |
| --- | --- |
| selected PAK | `sound.pak` |
| exact byte count | `69204` |
| SHA-256 | `e0c82072b554cb3f69d82c4fe4b24dc106f9bf0d7cc4dfde96a9491e382fb39a` |
| WAV | RIFF/WAVE, PCM, stereo, 22050 Hz, 16-bit |
| canonical unpack compare | byte-identical to `pak_unpacked/sound/unknown/01e95b13.wav` |

## Active runtime destination

Vendored byte: `Assets/StreamingAssets/sound/skill/飘雪穿云.wav`.

`AudioService.PlaySkillCast` normalizes PC `\sound\skill\...` to `sound/skill/...`; `LoadClipAsync` then calls `ResolveStreamingAssetsUri`, producing this StreamingAssets file. `SandboxManager` wires `SkillEffectVisualService.OnCastSound` to `AudioService.PlaySkillCast`.

`SkillEffectVisualService` emits `flightSoundPath` at missile activation. It emits `impactSoundPath` only at collision. Since 352 has empty `SndFile4`, this asset does not establish collision audio or audible device parity.

## Event-skill mapping boundary

Relationship skill `352` retains child missile `162` for SPR/animation. Its `ByMissle=0` event status audio resolves from direct missile row `352` first when that row has `SndFile2`; therefore its emitted flight path is `\sound\skill\飘雪穿云.wav`. Both status slots come from one selected audio row: direct row `352` has empty `SndFile4`, so mapper must not fall back to child `162` collision audio.

This is config/callback reachability only. It does not prove Android APK decode, `UnityWebRequest` completion, mixer routing, output hardware, or PC process parity.

## Reproduce

```bash
cd ~/Projects/vltktool
python3 extract_table_slice.py --input /var/www/jx-source/pak_unpacked/slistcache/settings/missles.txt --key-column MissleId --ids 352 --output /tmp/tm352.tsv --manifest /tmp/tm352.json
cmp /var/www/jx-source/pak_unpacked/slistcache/settings/missles.txt /var/www/vltk-mobile/Assets/StreamingAssets/Reference/PcAttrib/missles1.txt
python3 resolve_uid.py --pak /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/data/sound.pak --uid 01e95b13 --scan-dir /var/www/jx-source/pak_unpacked/slistcache/settings/missles.txt --max-files 1
cmp /tmp/tm352-01e95b13-winner.wav /var/www/jx-source/pak_unpacked/sound/unknown/01e95b13.wav
sha256sum /var/www/vltk-mobile/Assets/StreamingAssets/sound/skill/飘雪穿云.wav
```
