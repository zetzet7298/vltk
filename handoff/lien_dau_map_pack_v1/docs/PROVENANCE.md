# Provenance

## PC source-of-truth

- PC root: `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem`
- Canonical unpack root: `/var/www/vltksource_new/pak_unpacked`
- PC maplist evidence:
  - `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/maplist.ini`
  - map `396` => `特殊用地\武林大会专用\武林大会会场`
  - map `397` => `特殊用地\联赛比赛用地`

## Unity source evidence

- `Assets/Scripts/Sandbox/MapPortManifest.cs`
  - `DauTruongLienDauId = 397`
  - entry `397` => `Đấu trường liên đấu Kiệt xuất (1)`
- `Assets/StreamingAssets/MapAliasCatalog.json`
  - map `396` => `Hội trường liên đấu Kiệt xuất (1)`, geometry `g_7e0478bbbbc310c5`
  - map `397` => `Đấu trường liên đấu Kiệt xuất (1)`, geometry `g_15f3c8b336d024d4`
- `Assets/StreamingAssets/MapGeometryCatalog.json`
  - geometry `g_7e0478bbbbc310c5`: 176 Region_C, 68 image names
  - geometry `g_15f3c8b336d024d4`: 826 Region_C, 10 image names
- `Assets/StreamingAssets/MapServerRegionCatalog.json`
  - geometry `g_7e0478bbbbc310c5`: 122 Region_S, trapCount 30, npcCount 0
  - geometry `g_15f3c8b336d024d4`: 826 Region_S, trapCount 0, npcCount 0

## Package generation

Package copied only:

- 2 alias entries: `396`, `397`
- 2 geometry entries: `g_7e0478bbbbc310c5`, `g_15f3c8b336d024d4`
- 2 server-region entries
- Region_C folders for both keys
- Region_S folders for both keys
- SPR files resolvable from both `image_names.json` files

Known missing PC fallback:

- `\system\spr\RegionTileDefault.spr`
