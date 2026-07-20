# State visual residuals: empty is parity

This note documents the 17 root skill/state pairs where PC data names no usable state aura SPR. Do not vendor, alias, or invent fallback VFX for these state IDs.

## Runtime guard

New EditMode guard: `Assets/Tests/EditMode/Sandbox/StateVisualAbsentParityTests.cs`.

Coverage:

- all 17 root skill/state pairs below;
- all 15 unique residual state IDs: `52,53,54,55,56,57,58,59,60,63,64,65,66,120,122`;
- `PcSkillVisualAutoMapper.GetStateAuraData(stateId)` returns default/empty for each residual;
- `PcSkillVisualAutoMapper.GetVisualConfig(skill)` leaves `hasStateAura=false` and `stateAuraSprPath` empty;
- `SkillEffectVisualService.PlaySkillCast(...)` leaves `fx.isAura=false`, state aura frame fields zero, and no `style3.spr` key appears;
- positive control state `44` still maps to `\spr\skill\丐帮\mag_gb_12_打狗阵.spr`, attaches `hasStateAura`, and creates a Unity aura with 8 frames.

Narrow claim: this proves no **state aura** attaches. Some roots still have PC precast, child missile, or impact presentation through `PreCastSpr` / `ChildSkillId`; this document is not evidence that those skills have no other visual path.

## Canonical source evidence

### Skill rows: 17 residual root/state pairs

`vltktool` command:

```bash
cd /home/zet/Projects/vltktool
python3 extract_table_slice.py \
  --input /var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt \
  --key-column SkillId \
  --ids 15,90,174,175,177,273,277,282,332,356,364,391,392,393,394,716,720 \
  --output /tmp/vltk-state-residuals/skills-slice.txt \
  --manifest /tmp/vltk-state-residuals/skills-slice.manifest.json
```

Result:

- source: `/var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt`
- source bytes: `845831`
- source sha256: `c77892fb33b6e63783c554bd075caa4891d9b9ec8abb70084582a5c24156e40c`
- slice sha256: `a8987e7d5e595eedb6fa9e584789026741b68b48b6c875c418e747b324c04a02`
- selected source lines: `15@16`, `90@91`, `174@175`, `175@176`, `177@178`, `273@274`, `277@278`, `282@283`, `332@333`, `356@357`, `364@365`, `391@392`, `392@393`, `393@394`, `394@395`, `716@718`, `720@722`.

Decoded slice facts:

| SkillId | StateSpecialId | ChildSkillId | MisslesForm | Reason state aura stays empty |
|---:|---:|---:|---:|---|
| 15 | 52 | 76 | 3 | `52` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 90 | 64 | 20 | 6 | `64` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 174 | 66 | 20 | 6 | no row/path in loaded 1-49 table or NpcRes stub; PC visual name clears/empty. |
| 175 | 54 | 20 | 6 | `54` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 177 | 65 | 0 | 7 | no row/path in loaded 1-49 table or NpcRes stub; PC visual name clears/empty. |
| 273 | 53 | 0 | 7 | `53` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 277 | 57 | 114 | 6 | `57` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 282 | 55 | 281 | 7 | `55` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 332 | 56 | 333 | 7 | `56` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 356 | 54 | 20 | 6 | `54` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 364 | 58 | 20 | 6 | `58` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 391 | 59 | 20 | 6 | `59` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 392 | 63 | 290 | 6 | `63` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 393 | 65 | 20 | 6 | no row/path in loaded 1-49 table or NpcRes stub; PC visual name clears/empty. |
| 394 | 60 | 20 | 6 | `60` only appears in NpcRes stub table as `style3.spr`; no package bytes/winner. |
| 716 | 122 | 274 | 7 | no row/path in loaded 1-49 table or NpcRes stub; PC visual name clears/empty. |
| 720 | 120 | 275 | 6 | no row/path in loaded 1-49 table or NpcRes stub; PC visual name clears/empty. |

### Loaded state visual table: real 1-49 only

`vltktool` provenance command:

```bash
cd /home/zet/Projects/vltktool
python3 - <<'PY'
from pathlib import Path
import hashlib
loaded=Path('/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/Settings/状态与光效图形对照表.txt')
stub=Path('/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/Settings/NpcRes/状态与光效图形对照表.txt')
for label,p in [('loaded',loaded),('npcres_stub',stub)]:
    raw=p.read_bytes(); text=raw.decode('gbk')
    rows=[]
    for n,line in enumerate(text.splitlines(),1):
        if n==1: continue
        cells=line.split('\t')
        if not cells or not cells[0].startswith('状态'): continue
        sid=int(cells[0][2:])
        if sid in list(range(1,50))+list(range(52,65))+[65,66,120,122]:
            rows.append({'state':sid,'line':n,'file':cells[1] if len(cells)>1 else ''})
    print(label, 'path', p)
    print(label, 'bytes', len(raw), 'sha256', hashlib.sha256(raw).hexdigest(), 'encoding=gbk', 'row_count', len(text.splitlines())-1)
    print(label, 'states_52_64', [r for r in rows if 52 <= r['state'] <= 64])
    print(label, 'states_65_66_120_122', [r for r in rows if r['state'] in (65,66,120,122)])
PY
```

Result:

- loaded table path: `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/Settings/状态与光效图形对照表.txt`
- loaded bytes: `3513`
- loaded sha256: `4166fdd4c8f28db74c244bf7eb71eab16fb6227840cbc3c97ad62511a5700898`
- loaded encoding: `gbk`
- loaded row count: `49`
- loaded residual rows: none for `52-64`, none for `65,66,120,122`.

Unity mirror:

- `Assets/StreamingAssets/Reference/PcAttrib/state_visual_mapping.txt`
- sha256: `4166fdd4c8f28db74c244bf7eb71eab16fb6227840cbc3c97ad62511a5700898`
- `PcSkillVisualAutoMapper.GetStateAuraData` maps only state IDs `6-49`; default otherwise.

### NpcRes stub table: style3 only, not loader truth

Same `vltktool` command above.

Result:

- stub table path: `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/Settings/NpcRes/状态与光效图形对照表.txt`
- stub bytes: `2699`
- stub sha256: `a4210c45fba27ba0e6d1829c2cd0587a341b73c40a6da3d10a3561ed9f0317c0`
- stub encoding: `gbk`
- stub row count: `64`
- states `52-64` lines `53-65` all name `style3.spr`.
- states `65,66,120,122`: no row.

This NpcRes file is a stub table. It does not name a unique PC skill aura path, and `style3.spr` has no package winner/bytes in current source.

## PAK / SPR evidence

`vltktool` command for package UID presence:

```bash
cd /home/zet/Projects/vltktool
python3 - <<'PY'
from pathlib import Path
from jx_hash import hash_resource_path
from resolve_uid import pak_uids
root=Path('/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/data')
paks=list(root.glob('*.pak'))
uid_to_paks={}
for pak in paks:
    uids=pak_uids(pak)
    for uid in uids:
        uid_to_paks.setdefault(uid,[]).append(pak.name)
print('pak_count',len(paks),'uid_count',len(uid_to_paks))
for raw in ['style3.spr', r'\style3.spr', r'\spr\style3.spr', r'\spr\npcres\style3.spr', r'\spr\npcres\style\style3.spr', r'\spr\skill\style3.spr', r'\spr\skill\丐帮\mag_gb_12_打狗阵.spr']:
    print('\nPATH',raw)
    for h in hash_resource_path(raw):
        p=uid_to_paks.get(h.uid, [])
        print(f'  uid={h.uid:08x} encoding={h.encoding} path_bytes_hex={h.byte_hex} in_paks={p[:8]} count={len(p)}')
PY
find /var/www/jx-source/pak_unpacked -iname 'style3.spr' -print | sed -n '1,20p'
```

Result:

- PAKs checked: `46`; unique indexed UIDs: `190186`.
- `style3.spr` candidate hashes checked:
  - `style3.spr` / `\style3.spr`: uid `970d046b`, `in_paks=[]`.
  - `\spr\style3.spr`: uid `c104c289`, `in_paks=[]`.
  - `\spr\npcres\style3.spr`: uid `fffc0ca9`, `in_paks=[]`.
  - `\spr\npcres\style\style3.spr`: uid `a138b3ab`, `in_paks=[]`.
  - `\spr\skill\style3.spr`: uid `0044d249`, `in_paks=[]`.
- unpacked bytes check: no `/var/www/jx-source/pak_unpacked/**/style3.spr` output.
- positive control state `44` path `\spr\skill\丐帮\mag_gb_12_打狗阵.spr`:
  - winning package: `skills.pak`
  - UID: `202667bb`
  - encoding: `gbk`
  - unpacked bytes: `/var/www/jx-source/pak_unpacked/skills/unknown/202667bb.spr`

`vltktool` command for positive SPR frame proof:

```bash
cd /home/zet/Projects/vltktool
python3 - <<'PY'
from pathlib import Path
from extract_item_spr import parse_frames, decode_frame_rgba
import hashlib
p=Path('/var/www/jx-source/pak_unpacked/skills/unknown/202667bb.spr')
data=p.read_bytes(); fc,cc,frames,off=parse_frames(data); palette=data[0x20:0x20+cc*3]
print('file',p)
print('bytes',len(data),'sha256',hashlib.sha256(data).hexdigest(),'frame_count',fc,'color_count',cc)
for frame in frames[:8]:
    blob=data[off+frame.rel_offset:off+frame.rel_offset+frame.size]
    w,h,_=decode_frame_rgba(data, palette, blob)
    print('frame',frame.index,'size',frame.size,'width',w,'height',h)
PY
```

Result:

- `202667bb.spr` bytes: `39103`
- sha256: `42ae6bd6824becba9d73ae25b8246203607e98f83510c33447c1e5903c309705`
- frame count: `8`
- color count: `256`
- frame sizes: `98x73`, `92x72`, `96x74`, `95x70`, `91x71`, `90x73`, `99x74`, `84x68`.

## Proven / disproven / unresolved

Proven:

- 17 listed root skills carry residual `StateSpecialId` values in canonical `skills.txt`.
- loaded state visual table ends at `49`; residual states are not in loaded state visual source.
- residual states `52-64` only occur in `NpcRes/.../状态与光效图形对照表.txt` as `style3.spr` stubs.
- residual states `65,66,120,122` have no row/path in loaded or stub state visual tables.
- common plausible `style3.spr` paths have no PAK winner and no unpacked SPR bytes.
- state `44` still has real package bytes/UID/frame data and remains positive control.

Disproven:

- `style3.spr` is not a safe fallback state aura asset.
- lack of residual mapping is not missing mobile art work; it matches available PC evidence.

Unresolved / caveats:

- Lost external packages could theoretically contain another `style3.spr`, but current canonical jx-source PAK set and unpacked tree do not.
- Some root skills route through child missile/precast visuals. Empty residual state aura must not be worded as "skill has no visual." It only means no state aura is attached.
- No production C# or state mapping data changed here; future real PC package evidence should update mapping with exact path/UID/frames, not a guessed fallback.
