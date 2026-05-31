# JX region format & screen-space projection

All of this is already implemented in the project (`MapRenderer.cs`, `RegionParser.cs`,
`GroundLayerParser.cs`, `BuildinObjParser.cs`). This file documents *why* it works so you
can debug or port the renderer elsewhere. Source of truth:
`jxwin-kinnox/.../Core/Src/Scene/KScenePlaceRegionC.{h,cpp}`.

## Region scene geometry

- A region's scene area is **512 × 1024** (`RWPP_AREGION_WIDTH=512`, `RWPP_AREGION_HEIGHT=1024`).
- Ground cell = `512/16 = 32` wide, `1024/2/16 = 32` tall. Ground sprites are **64×64**.
- Ground tiles use an 8×8 grid: `h` and `v` run `0,2,4,…,14`; tile screen-local pos = `(h*32, v*32)`.
- Region screen origin (the engine halves scene Y): `(col*512, row*512)`.

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
  then `total` × `KBuildinObj` (~228 bytes). `szImage` is at **+56** (Props 4 + ImgPos1-4 = 48 + w/h 4).

## Screen → world

The renderer works in **1 px = 1 world unit**, north-up (`worldY = -screenY`):

- **Ground tile** `(h,v)` in region `(col,row)`: pivot **top-left (0,1)**, ppu=1, placed at
  world `(col*512 + h*32, -(row*512 + v*32))`, `sortingOrder` very low (under everything).
- **Cover object** (GROUND objects): absolute scene coords → screen `(posX, posY/2)`,
  world `(posX, -posY/2)`. Pivot **bottom-center (0.5,0)**. `sortingOrder ≈ round(screenY)*2`.
- **Builtin object**: `ImgPos1` is a scene coord → screen `(imgX1, imgY1/2)`, world
  `(imgX1, -imgY1/2)`. Pivot bottom-center. `sortingOrder = round(screenY)*2 + 1`
  (the +1 keeps trees/structures above flat cover on the same row — painter's order, south draws over north).

Sanity check used during development: a 菩提树 (bodhi tree) at scene `(54500,100045)` in
region `(106,97)` (origin scene `54272,99328`) → screen-local `(228, 358)`, inside 512×512. ✓

## Filenames / coordinates

Loose region files (server tree) and the pak both name cells `v_{Y:03d}/{X:03d}_Region_C.dat`
where the `v_` folder number is **Y (row)** and the file number is **X (col)**. In Unity we
store `{col}_{row}_Region_C.dat` and `MapRenderer` parses `parts[0]=col, parts[1]=row`.
`m_LeftTopCornerScenePos = (X*512, Y*1024)`, so X is horizontal, Y vertical.

## SPR decode

`SprDecoder` (project) reads: `SPRHEAD(32)` = `char[4] "SPR\0", u16 Width, Height, CenterX,
CenterY, Frames, Colors, Directions, Interval, u16 reserved[6]`. Then palette `Colors*3`,
then `SPROFFS[Frames]{ u32 Offset, u32 Length }`, then frame data. Each frame =
`SPRFRAME{ u16 Width, Height, i16 OffsetX, OffsetY, byte rle[] }`. RLE is
`(pixelNum, alpha)` runs; `alpha==0` skips `pixelNum` transparent pixels, else `pixelNum`
palette-indexed pixels follow. `filterMode=Point`. The extractor rebuilds per-frame-compressed
pak SPRs into this exact flat layout before staging (see scripts/jx_map_port.py).
