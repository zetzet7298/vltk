# JX region format & screen-space projection

All of this is already implemented in the project (`MapRenderer.cs`, `RegionParser.cs`,
`GroundLayerParser.cs`, `BuildinObjParser.cs`). This file documents *why* it works so you
can debug or port the renderer elsewhere.

> Provenance: the formulas below were recovered from JX engine C++
> (`KScenePlaceRegionC.{h,cpp}`, `Represent3/KRepresentShell3.cpp`). Those `.cpp` source
> files are NOT in-scope under `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem` (only `represent3.dll`
> ships there) — but the key constant is now **binary-verified** against that DLL. The
> Z-projection `screenY = sceneY/2 - (sceneZ*887)>>10` appears verbatim at
> `represent3.dll:0x1000d08a` as `imul edi,edi,0x377` (887) followed by `sar edi,0xa` (>>10)
> and `sar eax,1` (sceneY/2), then `sub eax,edi`. Two more sites (`0x1000c55e`, `0x1000d00e`)
> use the same `imul …,0x377; sar …,0xa`. Trust the numbers; to re-derive anything deeper,
> open `represent3.dll` (PE i386, baddr 0x10000000) in radare2.

## Region scene geometry

- A region's scene area is **512 × 1024** (`RWPP_AREGION_WIDTH=512`, `RWPP_AREGION_HEIGHT=1024`).
- Ground cell = `512/16 = 32` wide, `1024/2/16 = 32` tall. Ground sprites are **64×64**.
- Ground tiles use an 8×8 grid: `h` and `v` run `0,2,4,…,14`; tile screen-local pos = `(h*32, v*32)`.
- Region screen origin (the engine halves scene Y): `(col*512, row*512)`.

## The Z-projection formula (THIS IS CRITICAL — do not skip)

The original engine's `KRepresentShell3::CoordinateTransform` (line 2157 of
`KRepresentShell3.cpp`) converts scene coordinates to screen coordinates:

```cpp
void KRepresentShell3::CoordinateTransform(int& nX, int& nY, int nZ)
{
    nX = nX - m_nLeft;
    nY = nY / 2 - m_nTop - ((nZ * 887) >> 10);
}
```

Dropping the viewport offset (irrelevant for Unity's world space):

```
screenX = sceneX
screenY = sceneY / 2 - sceneZ * (887 / 1024)
```

The `(nZ * 887) >> 10` is `sceneZ * 887 / 1024 ≈ sceneZ * 0.866`.

### What the Z coordinate means

In the JX engine, Z is the **height above the ground plane**. For the isometric projection,
height shifts the sprite **upward** on screen (negative screen-Y in Unity's north-up coords).

Typical Z values from 巴陵县 data:
- Ground-level objects: Z ≈ 0
- Building wall bases: Z ≈ 0-13
- 牌坊 gate pillars (Z4 column): Z = 159-173
- 牌坊 gate crossbeams (top): Z = 441-628 (!!!)
- Large trees: Z = 100-200

### What happens if you ignore Z

A gate crossbeam at scene `(51426, 102198)` with Z=503:
- **With Z**: screenY = 102198/2 - 503*0.866 = 51099 - 436 = **50663** ✓ (beam above pillars)
- **Without Z**: screenY = 102198/2 = **51099** ✗ (beam at ground level, 436 pixels too low!)

The beam is supposed to be at the top of the gate archway. Without Z, it renders at the
same height as the pillar bases — creating a massive dark gap where the background shows
through the archway.

## Combined Region_C.dat layout

```
uint32 sectionCount                      # usually 6
{ uint32 offset, uint32 length } * sectionCount    # offsets are relative to end-of-header
... section payloads ...
```

Section indices (`SCENE_FILE_INDEX`):
`0=OBSTACLE, 1=TRAP, 2=NPC, 3=OBJ, 4=GROUND, 5=BUILTIN`.

- **OBSTACLE** (idx 0): `int32[16*32] = 2048 bytes`; non-zero = blocked.
- **GROUND** (idx 4): `KGroundFileHead{ uint32 numTiles, numObjects, objectDataOffset }`
  then `numTiles` × `{ u16 h, u16 v, u16 frame, u16 nameLen, char[nameLen] szImage(GBK) }`;
  then at `objectDataOffset`, `numObjects` × packed(2) `KSPRCoverGroundObj` (146 bytes):
  `int32 posX, int32 posY, char szImage[128], u16 w, u16 h, u16 frame, u8 relateRegion, u8 order, i16 layer`.
- **BUILTIN** (idx 5): `{ u32 total, u16 numTree, numLine, numPoint, numAbove, maxAboveOrder, numLights }`
  then `total` × `KBuildinObj` (~228 bytes). Fields in order:

### KBuildinObj full layout (228 bytes)

```
Offset  Size  Field
0       4     props (u32) — bit 0-1: plane type (0=H, 2=V), other bits unknown
4       12    ImgPos1 (x:i32, y:i32, z:i32) — quad top-left in scene coords
16      12    ImgPos2 (x:i32, y:i32, z:i32) — quad bottom-left
28      12    ImgPos3 (x:i32, y:i32, z:i32) — quad bottom-right
40      12    ImgPos4 (x:i32, y:i32, z:i32) — quad top-right
52      2     imgWidth (i16)
54      2     imgHeight (i16)
56      128   szImage (null-terminated GBK string)
184     4     flipTime (u32)
188     2     frame (u16)
190     2     imgNumFrames (u16)
192     2     aniSpeed (u16)
194     2     order (u16)
196     12    oPos1 (x:i32, y:i32, z:i32)
208     12    oPos2 (x:i32, y:i32, z:i32)
220     4     angleXY (float)
224     4     nodicalY (float)
```

The four ImgPos points define a 3D quad in scene space that the sprite is mapped onto.
For standard upright objects (houses, trees, gates), the quad is:
- ImgPos1 = top-left at full Z height
- ImgPos3 = bottom-right at Z=0
- The X/Y span of the quad should approximately match the sprite pixel dimensions

The `props` field's `IsPlaneTypeH = (props & 0x03) == 0` and `IsPlaneTypeV = (props & 0x03) == 0x02`.
Objects with props `0x00000117` (common for buildings/gates) are neither H nor V — they are
standard upright sprites that use all 4 ImgPos corners.

## Screen → Unity world

The renderer works in **1 px = 1 world unit**, north-up (`worldY = -screenY`):

### Ground tiles
Pivot **top-left (0,1)**, ppu=1.
```
worldX = regionScreenX + h * 32
worldY = -(regionScreenY + v * 32)
sortingOrder = -1000
```

### Cover objects (KSPRCoverGroundObj)
Absolute scene coords, NO Z coordinate (flat ground decals).
Pivot **bottom-center (0.5, 0)**.
```
worldX = posX
worldY = -(posY * 0.5)
sortingOrder = 0
```

### Builtin objects (KBuildinObj)
Full isometric projection WITH Z. Pivot **top-left (0, 1)**.
```
screenX = imgX1
screenY = imgY1 * 0.5 - imgZ1 * (887/1024)
worldX = screenX
worldY = -screenY
sortingOrder = 1000 + fileOrderCounter++
```

The top-left pivot is critical: ImgPos1 defines the quad's top-left corner. The sprite
extends rightward and downward from this point. Using bottom-center pivot (0.5, 0) shifts
every builtin by half its width and its full height, causing severe misalignment.

### Sanity checks

A 牌坊 gate crossbeam at scene `(51426, 102198, 503)`:
```
screenX = 51426
screenY = 102198/2 - 503*(887/1024) = 51099 - 435.7 = 50663.3
world = (51426, -50663)
```

Ground tile at row=100, v=2:
```
regionScreenY = 100*512 = 51200
worldY = -(51200 + 2*32) = -51264
```

These are in the same coordinate space — the gate renders above the ground tiles as expected.

## The original engine's spatial tree (KIpoTree)

The original engine does NOT use simple Y-sorting for draw order. It uses a **spatial
binary tree** (`KIpoTree`) implemented in `jxwin-kinnox/.../Scene/KIpoTree.cpp` and
`KIpotBranch.cpp`:

1. A branch has a split line (head point → end point) and two children (UP=0, DOWN=1).
2. Objects are inserted into the tree based on their position relative to split lines
   (`AddLeafLine` for line objects, `AddLeafPoint` for point objects).
3. `PaintObjectLayer` traverses the tree in-order: child[0] (far/UP) → root object →
   child[1] (near/DOWN). This naturally produces correct isometric back-to-front order.
4. Rendering happens in three passes: `IPOT_RL_COVER_GROUND` → `IPOT_RL_OBJECT` →
   `IPOT_RL_INFRONTOF_ALL`.

The Unity port preserves this ordering by using a monotonically-increasing `sortingOrder`
counter that matches the file storage order (which already reflects the tree's in-order
traversal). See `references/sorting.md` for the full sorting model.

## SPR decode

`SprDecoder` (project) reads: `SPRHEAD(32)` = `char[4] "SPR\0", u16 Width, Height, CenterX,
CenterY, Frames, Colors, Directions, Interval, u16 reserved[6]`. Then palette `Colors*3`,
then `SPROFFS[Frames]{ u32 Offset, u32 Length }`, then frame data. Each frame =
`SPRFRAME{ u16 Width, Height, i16 OffsetX, OffsetY, byte rle[] }`. RLE is
`(pixelNum, alpha)` runs; `alpha==0` skips `pixelNum` transparent pixels, else `pixelNum`
palette-indexed pixels follow. `filterMode=Point`. The extractor rebuilds per-frame-compressed
pak SPRs into this exact flat layout before staging (see scripts/jx_map_port.py).

## Filenames / coordinates

Loose region files (server tree) and the pak both name cells `v_{Y:03d}/{X:03d}_Region_C.dat`
where the `v_` folder number is **Y (row)** and the file number is **X (col)**. In Unity we
store `{col}_{row}_Region_C.dat` and `MapRenderer` parses `parts[0]=col, parts[1]=row`.
`m_LeftTopCornerScenePos = (X*512, Y*1024)`, so X is horizontal, Y vertical.
