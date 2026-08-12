# Hướng dẫn tích hợp kỹ thuật

## Yêu cầu runtime

Gói này là **map data package**. Để chạy được cần runtime có các phần tương đương VLTK Mobile:

- `MapManager` đọc `MapAliasCatalog.json`, `MapGeometryCatalog.json`, `MapServerRegionCatalog.json`.
- `MapRenderer` đọc `Generated/MapRegions/<geometryKey>/*_Region_C.dat`.
- `RegionParser`, `GroundLayerParser`, `BuildinObjParser` parse Region_C.
- `SprRuntimeService` resolve SPR theo PC resource path/UID trong `Generated/MapSprites`.
- Optional: Region_S/static service đọc `Generated/MapServerRegions/<geometryKey>/*_Region_S.dat`.

Nếu khách chưa có runtime tương thích, cần bán thêm gói integration/runtime license.

## Cài vào Unity project có runtime tương thích

1. Copy `Assets/StreamingAssets` từ gói này vào Unity project.
2. Đảm bảo runtime load StreamingAssets path đúng.
3. Load map theo ID:
   - `396` — Hội trường liên đấu Kiệt xuất (1)
   - `397` — Đấu trường liên đấu Kiệt xuất (1)
4. Nếu dùng server-region/trap/static data, bật loader `MapServerRegionCatalog.json`.
5. Chạy verifier trước khi build:

```bash
python3 scripts/verify_lien_dau_package.py .
```

## Bounds

- Map 396 bounds: `x=44544, y=-51200, width=9216, height=6144`
- Map 397 bounds: `x=38912, y=-62464, width=28672, height=14336`

## Known gap

`\system\spr\RegionTileDefault.spr` không có trong scoped PC paks. Đây là engine fallback known gap. Runtime hiện tại vẫn verify package với gap này được chấp nhận.
