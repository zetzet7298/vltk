# Acceptance checklist

Khách nghiệm thu theo checklist này.

## Package integrity

- [ ] `TECHNICAL_MANIFEST.json` tồn tại.
- [ ] `MapAliasCatalog.json` chỉ chứa map 396/397.
- [ ] `MapGeometryCatalog.json` chỉ chứa 2 geometry key.
- [ ] `MapServerRegionCatalog.json` chỉ chứa 2 geometry key nếu gói có server-region.
- [ ] Verifier pass.

## Visual map data

- [ ] Map 396 có 176 `*_Region_C.dat`.
- [ ] Map 397 có 826 `*_Region_C.dat`.
- [ ] Sprite refs tổng 76, copied sprites 75, missing chỉ `\system\spr\RegionTileDefault.spr`.

## Server/static data

- [ ] Map 396 có 122 `*_Region_S.dat`.
- [ ] Map 397 có 826 `*_Region_S.dat`.

## Unity runtime demo

- [ ] Load map 396 không crash.
- [ ] Load map 397 không crash.
- [ ] Map hiển thị terrain/object thật, không phải placeholder xám.
- [ ] Camera/player nằm trong map bounds.
- [ ] Console không có lỗi compile/runtime mới.
- [ ] Nếu có minimap: preview/click-to-move dùng đúng bounds active map.

## Hợp đồng

- [ ] Khách xác nhận quyền dùng IP/asset PC nếu dùng thương mại.
- [ ] Scope support và license đã ghi rõ.
