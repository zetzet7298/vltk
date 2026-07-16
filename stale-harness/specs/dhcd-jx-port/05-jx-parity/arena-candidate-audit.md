# Arena Candidate Audit

| Trường | Giá trị |
|---|---|
| Mục đích | Audit candidate arena trước khi chọn map pilot |
| Trạng thái | `in_progress` (enumeration advanced; still fail-closed, no winner) |
| Owner / reviewer | JX map owner / technical reviewer |
| Cập nhật | 2026-07-16 |

> Read-only enumeration. No byte under `/var/www/jx-source` was modified or
> copied; nothing was vendored into this repo. Every value below is either an
> absolute disk fact (path/size/SHA-256/count) or explicitly `unresolved`. No
> candidate is selected, enabled, or called a pilot arena.

## Candidate table

Mỗi dòng phải được ghi vào manifest versioned trước khi chọn winner:

| Candidate | Absolute candidate path(s) | Pack/version + load order | Hash/UID/bytes/SHA-256 | Resolver/decode | Evidence hiện có | Rejection/selection reason |
|---|---|---|---|---|---|---|
| `yanwuchang` (IDs `209/210/211` requested) | Loose-source `.wor` (3 byte-identical copies): `…/bin/client/maps/ÖÐÔ­ÄÏÇø/yanwuchang.wor`, `…/bin/Server/maps/ÖÐÔ­ÄÏÇø/yanwuchang.wor`, `…/SwordOnline/Sources/S3Client/Debug/maps/ÖÐÔ­ÄÏÇø/yanwuchang.wor`. Loose Region_C.dat dir `…/bin/client/maps/ÖÐÔ­ÄÏÇø/yanwuchang/v_{097..102}/`. MapList.ini candidates naming `yanwuchang`: `pak_unpacked/{dmjx03,vlmp,slistcache,slistfree}/settings/maplist.ini` and loose `bin/client/settings/MapList.ini`. No runtime package candidate: `pak_unpacked/maps/` holds 0 `.wor`/`Region_C`/`Region_S`/`maplist.ini`. | `unresolved`: 9 `maplist.ini` packages exist in `pak_unpacked`; **no** version/load-order/manifest declares a winner. ID `209/210/211` map to **conflicting** path bytes across packages (see MapList enumeration). | `.wor`: 92 bytes; SHA-256 `6667643864a30cd87cf8bfa689461ec671c26af11716b25ae7fc3f75205b7fad` (identical at all 3 loose locations). Region_C.dat: 45 files, undecoded (per-file hashes not recorded). Hash_UID/encoding/path bytes: `unresolved`. | Resolver path/UID: `unresolved`. `region_c_decode`: `unresolved` (files exist, not decoded). `region_s_decode`: `unresolved` (no `Region_S` for this candidate anywhere). | `minimap.html:1723` names `yanwuchang` (textual only). Requested IDs `209/210/211` are not stable across MapList packages; dmjx03/vlmp carry non-ASCII path bytes at `209-211` (GBK-decoded dmjx03 reads `中原南区\演武场一`) and put ASCII `yanwuchang` at ID `986`, while slistcache/slistfree/loose put ASCII `yanwuchang` at `209-211`. This conflict is recorded as candidate text, not as an established mapping. | Selection: none. Conflicting MapList ID text, no package winner, Region_C undecoded, and Region_S absent do not prove package winner or collision data. |
| `jingjichang` (ID `975`) | Loose-source `.wor` (3 byte-identical copies): `…/bin/client/maps/ÌØÊâÓÃµØ/jingjichang.wor`, `…/bin/Server/maps/ÌØÊâÓÃµØ/jingjichang.wor`, `…/SwordOnline/Sources/S3Client/Debug/maps/ÌØÊâÓÃµØ/jingjichang.wor`. Loose Region_C.dat dir `…/bin/client/maps/ÌØÊâÓÃµØ/jingjichang/v_{097..102}/`. MapList.ini candidates naming `jingjichang` at ID `975`: `pak_unpacked/{dmjx03,vlmp,slistcache}/settings/maplist.ini` and loose `bin/client/settings/MapList.ini`. No runtime package candidate in `pak_unpacked/maps/`. | `unresolved`: same 9-package set, no version/load-order/manifest winner. ID `975` path bytes are consistent in the ASCII tail (`jingjichang`) across the 4 naming packages, but the non-ASCII zone prefix did not decode cleanly as GBK/UTF-8/Big5 in vlmp/slistcache/loose, so encoding is `unresolved` there; dmjx03 decodes as GBK (`特殊用地\jingjichang`). | `.wor`: 92 bytes; SHA-256 `8895eb1fc78e030db7a0fddfba8c80ecae7c982e7b1a04cea1fbb137b80a99ac` (identical at all 3 loose locations). Region_C.dat: 45 files, undecoded. Hash_UID/encoding/path bytes: `unresolved`. | Resolver path/UID: `unresolved`. `region_c_decode`: `unresolved` (files exist, not decoded). `region_s_decode`: `unresolved` (no `Region_S` for this candidate). | `minimap.html:2059` states Map ID `975` and textual path `特殊用地\\jingjichang`; consistent with the 4 MapList.ini candidate lines but not package-winner proof. `.wor` bounds are `96,97,105,102`. | Selection: none. Map ID/path text and loose `.wor` do not prove package winner or collision data. |
| `shiliantang` (ID `925` requested) | Loose-source `.wor` (3 byte-identical copies): `…/bin/client/maps/ÌØÊâÓÃµØ/shiliantang.wor`, `…/bin/Server/maps/ÌØÊâÓÃµØ/shiliantang.wor`, `…/SwordOnline/Sources/S3Client/Debug/maps/ÌØÊâÓÃµØ/shiliantang.wor`. Loose Region_C.dat dir `…/bin/client/maps/ÌØÊâÓÃµØ/shiliantang/v_{097..102}/`. MapList.ini candidates naming `shiliantang` at ID `925`: `pak_unpacked/{dmjx03,vlmp,slistcache}/settings/maplist.ini` and loose `bin/client/settings/MapList.ini`. No runtime package candidate in `pak_unpacked/maps/`. | `unresolved`: same 9-package set, no version/load-order/manifest winner. ID `925` ASCII tail (`shiliantang`) consistent across naming packages; zone prefix encoding `unresolved` except dmjx03 (GBK `特殊用地\shiliantang`). | `.wor`: 92 bytes; SHA-256 `7c9fae1a8bc4a333cae778c138f919789f4978cbfeb1d1da0d4b0c203645370b` (identical at all 3 loose locations). Region_C.dat: 41 files, undecoded. Hash_UID/encoding/path bytes: `unresolved`. | Resolver path/UID: `unresolved`. `region_c_decode`: `unresolved` (files exist, not decoded). `region_s_decode`: `unresolved` (no `Region_S` for this candidate). | `minimap.html:1291` names `shiliantang` (textual). Dungeon scripts under `…/script/missions/dungeon/dungeons/shiliantang/` reference scripts, not a map identity. MapList.ini candidate text ties ID `925` to the name but is not package-winner proof. | Selection: none. Name, script, MapList text, and loose `.wor` do not prove package winner or collision data. |

Textual name/script/MapList line không chứng minh map ID, collision, bounds, spawn hay winner.

## MapList.ini candidate enumeration

Nine `maplist.ini` files exist under `/var/www/jx-source/pak_unpacked/`; **no**
`version`/`load-order`/`manifest`/`paklist`/`filelist` artifact was found at
depth ≤ 2 of `pak_unpacked` to declare an active winner. Recorded sizes/SHA-256:

| Absolute path | Bytes | SHA-256 | Names present | Encoding note |
|---|---|---|---|---|
| `pak_unpacked/dmjx03/settings/maplist.ini` | 206781 | `6808cc54352bcf6247d62384fa371cc0b37276a67b9b08a98705b8dd2aa76959` | `yanwuchang`,`jingjichang`,`shiliantang` | decodes as GBK |
| `pak_unpacked/vlmp/settings/maplist.ini` | 182758 | `40ce8114c83d3a687b977ad280c0a2a2f2e73150d138569df6565d691db62894` | `yanwuchang`,`jingjichang`,`shiliantang` | non-ASCII bytes did not cleanly decode as GBK/UTF-8/Big5; `unresolved` |
| `pak_unpacked/slistcache/settings/maplist.ini` | 206848 | `c730c5bdd94c1ebebefd8b158ce33d8fa02585eecfc2039289c1c130128478f0` | `yanwuchang`,`jingjichang`,`shiliantang` | same — `unresolved` |
| `pak_unpacked/slistfree/settings/maplist.ini` | 165667 | `16f0b7b6592363f77f8672b777d520d48e3689f9ba5f43817899168569d99d62` | `yanwuchang` only | same — `unresolved` |
| `pak_unpacked/update/settings/maplist.ini` | 165526 | — (not hashed; none of the 3 names present) | none | — |
| `pak_unpacked/update01/settings/maplist.ini` | 122167 | — (not hashed; none present) | none | — |
| `pak_unpacked/update03/settings/maplist.ini` | 157746 | — (not hashed; none present) | none | — |
| `pak_unpacked/update04/settings/maplist.ini` | 161952 | — (not hashed; none present) | none | — |
| `pak_unpacked/vng00/settings/maplist.ini` | 3700 | — (not hashed; none present) | none | — |

Loose-source `MapList.ini` candidates (read-only):

| Absolute path | Bytes | SHA-256 | Names present |
|---|---|---|---|
| `…/00.src-tinh-kiem/bin/client/settings/MapList.ini` | 193794 | `e0b18ae3430f2d7babb968067b431be8d7b59a3c9f528187497b5b0e3d7aa250` | `yanwuchang`(209-211),`jingjichang`(975),`shiliantang`(925) |
| `…/00.src-tinh-kiem/bin/Server/settings/MapList.ini` | 193794 | `e0b18ae3430f2d7babb968067b431be8d7b59a3c9f528187497b5b0e3d7aa250` | identical bytes to client copy |
| `…/00.src-tinh-kiem/bin/Server/Server/settings/MapList.ini` | 193794 | `e0b18ae3430f2d7babb968067b431be8d7b59a3c9f528187497b5b0e3d7aa250` | identical bytes to client copy |
| `…/00.src-tinh-kiem/SwordOnline/Sources/S3Client/Debug/settings/MapList.ini` | 193794 | `e0b18ae3430f2d7babb968067b431be8d7b59a3c9f528187497b5b0e3d7aa250` | identical bytes to client copy |
| `…/00.src-tinh-kiem/Utility/Run/Settings/MapList.ini` | 7893 | `ce113344ee264dc9f3f1bab8b275523fa47b1495ee47f74137b2092af39e5612` | **none** of `209/210/211/925/975` |

ID-to-path-byte conflict (recorded as raw candidate text, **not** an asserted
mapping):

- IDs `209/210/211` → non-ASCII GBK bytes (`中原南区\演武场一` when dmjx03 is
  GBK-decoded) in `dmjx03` and `vlmp`; the ASCII literal `yanwuchang` appears at
  ID `986` (`dmjx03`/`vlmp`) / `987` differs.
- IDs `209/210/211` → ASCII literal `yanwuchang` in `slistcache`, `slistfree`,
  and all four loose `bin/.../settings/MapList.ini` copies.
- IDs `925`/`975` → ASCII tails `shiliantang`/`jingjichang` consistently across
  `dmjx03`,`vlmp`,`slistcache`, and loose copies (zone prefix encoding
  `unresolved` except dmjx03 GBK).

Because the requested `209/210/211` bytes disagree across packages and no
package winner is declared, the `yanwuchang` ID identity itself is
`unresolved`; this is load-order proof missing, not just decode missing.

## Loose-source geometry enumeration

- `.wor`: all three candidates exist as 92-byte files at three loose locations
  (`bin/client/maps`, `bin/Server/maps`, `SwordOnline/Sources/S3Client/Debug/maps`);
  bytes are identical per candidate across the three locations (SHA-256 in
  candidate table). `shiliantang.wor` **does** exist — earlier "`unresolved`: no
  map `.wor`" was corrected.
- `Region_C.dat` (file pattern `NNN_Region_C.dat`, undecoded): `yanwuchang` 45,
  `jingjichang` 45, `shiliantang` 41, under
  `…/bin/client/maps/<zone>/<name>/v_{097..102}/`. Per-file SHA-256 not recorded.
- `Region_S`: **0** files for any of the three candidates in `bin/client/maps`;
  **0** in `pak_unpacked`. (`Region_S` files exist elsewhere in the loose source
  tree for other maps, but none for these three candidates and none in the
  runtime package tree.)
- Runtime package `pak_unpacked/maps/` contains only `spr`/`settings`/`spr800`/
  `unknown`/`vltksource_new_unpaked` — **0** `.wor`, `Region_C`, `Region_S`, or
  `maplist.ini`.

## Decoded loose geometry and canonical lookup rule (still no winner)

The loose-source `Region_C` field above is now decoded as a candidate census,
not selected runtime geometry. All files parsed with the canonical map-port
section reader into the expected six sections (`OBSTACLE`, `TRAP`, `NPC`, `OBJ`,
`GROUND`, `BUILTIN`):

| Candidate | Loose copies / unique bytes | Decode result | Representative canonical loose file |
|---|---:|---|---|
| `yanwuchang` | 135 / 45 | 135 success, 0 failure; 3,363,564 total bytes | `…/bin/client/maps/ÖÐÔ­ÄÏÇø/yanwuchang/v_097/099_Region_C.dat`, 2,108 bytes, SHA-256 `e8a269b915e31d3a35e9fec3b6313bee3eb7bca30d64c0efd459761afbca7477` |
| `jingjichang` | 135 / 45 | 135 success, 0 failure; 821,208 total bytes | `…/bin/client/maps/ÌØÊâÓÃµØ/jingjichang/v_097/100_Region_C.dat`, 880 bytes, SHA-256 `143b610c275e77894bf0cbf6664b949f5238a7250cac6f4bfd2d3e03d1123912` |
| `shiliantang` | 123 / 41 | 123 success, 0 failure; 1,060,191 total bytes | `…/bin/client/maps/ÌØÊâÓÃµØ/shiliantang/v_102/102_Region_C.dat`, 3,934 bytes, SHA-256 `c103cb79acb0d4eeb6c8f1da10f654147f4a73570271722f21fc3f49a4fa8b64` |

Each loose candidate is duplicated byte-for-byte across `bin/client/maps`,
`bin/Server/maps`, and `SwordOnline/Sources/S3Client/Debug/maps`; this proves
only source duplication, not an active client package winner.

The canonical PC client algorithm is now known: `S3Client.cpp` loads
`bin/client/package.ini`; `KPakList.cpp` reads `[Package]` keys in ascending
numeric order, appends successfully opened PAKs in that order, and lookup stops
at the first matching package. Thus first matching package in `package.ini`
order wins. The canonical client `package.ini` is SHA-256
`5df520aa74b2eee925eac58ecf17939d50a6a51ad463568dcc4e755457039784` and
lists `maps.pak` at index 25.

This rule cannot choose a winner from the current unpacked corpus: its nine
conflicting `maplist.ini` candidates are not bound to the package set named by
that client `package.ini`. Therefore `winner`, logical path bytes/UID, active
Region_C, and all Unity staging remain **unresolved**.

## Read-only discovery record

Run on `2026-07-15` (original) and re-checked `2026-07-16` by `US-P0-002`;
canonical source was not modified or copied.

| Evidence | Command/tool | Result and limitation |
|---|---|---|
| Loose map lookup (default root) | `python3 .agents/skills/jx-map-port/scripts/list_maps.py --id 209` (also `210`,`211`,`925`,`975`) | No output. The helper reads `Utility/Run/Settings/MapList.ini` (7893 bytes), which contains **none** of the target IDs; it therefore cannot establish identity for any requested ID. The target IDs are present in `bin/client/settings/MapList.ini` (193794 bytes) and the pak `maplist.ini` files instead. |
| MapList.ini enumeration | `find /var/www/jx-source/pak_unpacked -iname maplist.ini` + `sha256sum`/`stat -c%s` | 9 runtime `maplist.ini` packages; sizes/SHA-256 recorded above. Only `dmjx03`,`vlmp`,`slistcache`,`slistfree` carry the candidate names; no version/load-order/manifest declares a winner. |
| Runtime map settings (original) | `python3 /home/zet/Projects/vltktool/parse_map_settings.py --maps-pak /var/www/jx-source/pak_unpacked/maps_pak --output …` (2026-07-15) | vltktool commit `fea4c244a4945ba9423f82a4bfd3492e55b4329a`, toolVersion `1.2.0`; emitted a 304-byte empty catalog, SHA-256 `c511239e9a3040c346cfa539c90a9870c487d54ae1ec34cf0b1342317173d3c8`. **Correction:** the path `pak_unpacked/maps_pak` does **not** exist (the unpacked maps package is `pak_unpacked/maps/`); the empty catalog therefore reflects a missing input path, not an empty maps package. |
| Runtime map settings (recheck) | same command (2026-07-16) | 304 bytes again, SHA-256 `493a57e13db814a95d94f8b57301ffe23184af3ff32bb47af9ddc2b29efee2f0`. SHA differs from the original because the emitted JSON embeds a `generatedAt` timestamp; the reproducible facts are "304 bytes, zero map settings, toolVersion 1.2.0". No Region_C/Region_S/`maplist.ini` evidence follows from this. |
| Loose geometry enumeration | `find …/bin/client/maps -ipath '*<name>*' -name '*_Region_C.dat'` and `-iname '*.wor'` | `.wor` + `Region_C.dat` counts recorded above; `Region_S` = 0 for all three candidates. |
| Resolver capability | `python3 /home/zet/Projects/vltktool/resolve_uid.py --help` | Tool requires a concrete PAK, UID, and narrow source path. No PAK winner or non-guessed logical resource path was recovered, so no UID/hash operation was run. |
| Label cross-check | Scoped `_labels.json` discovery under `/var/www/jx-source/pak_unpacked` | No label file recovered in the inspected runtime tree; `name_vi_cross_check` is `unresolved` for all candidates. |

## Decode status

All candidate `Region_C`, `Region_S`, terrain, and minimap decode fields are
`unresolved`. Region_C.dat **files exist** in loose source for all three
candidates (45/45/41) but are **not decoded**; no collision/height/bounds golden
exists. `Region_S` is **absent** for all three candidates in both loose
client-maps and `pak_unpacked`, so `region_s_decode` is `unresolved` for lack of
a source. The only minimap facts above are textual HTML metadata; no minimap
asset bytes or coordinate transform were decoded. No candidate is selected,
enabled, or called a pilot arena.

## Why no candidate advances (and exact input still required)

No candidate advances because **all** of the following remain open:

1. **Load-order winner missing.** 9 `maplist.ini` packages exist with no
   version/load-order/manifest; the requested `209/210/211` path bytes conflict
   across packages, so even the `yanwuchang` map-ID identity is unresolved.
2. **Region_C undecoded.** Region_C.dat files exist but no collision/height/
   bounds decode or golden has been produced for any candidate.
3. **Region_S absent.** No `Region_S` source for any candidate → no spawn/NPC
   placement evidence.
4. **Resolver/UID/encoding unresolved.** No PAK winner or non-guessed logical
   path means no Hash_UID/encoding/path-bytes operation was run.

Exact input still required to unblock (none yet satisfied):

- A declared active package/load-order (or an authoritative manifest) that
  names the winning `maplist.ini`/maps package among the 9 candidates.
- A vltktool resolver run against the winning PAK producing Hash_UID,
  encoding, and normalized path bytes for each selected map path.
- Decoded Region_C (collision/height/bounds) **and** a recovered/decoded
  Region_S (spawn/NPC) for the selected candidate, with per-file SHA-256.
- A reviewer sign-off record (`reviewer`/`reviewed_at`) before any
  `verified`/selected status.

Until all four are satisfied, US-P0-002 stays fail-closed at `in_progress`.

## Audit procedure

1. Enumerate all logical/hashed candidates using resolver and current PAK order; record every absolute path, including rejected candidates.
2. Decode `Region_C`, `Region_S`, terrain/minimap and record hash/provenance.
3. Import to isolated Unity scene; compare world coordinates, walkable mask, camera crop.
4. Spawn one verified player/NPC, test collision/height/portal and capture golden.
5. Select only one candidate for P0; keep rejected candidate path/provenance and reason in this manifest or a linked artifact.

## Acceptance

- Audit record complete for every candidate.
- Selected arena has deterministic map conversion version and collision golden.
- No candidate is promoted from name-only evidence.
