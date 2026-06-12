# Gói bàn giao 2 map Liên đấu

Gói này là bản staging kỹ thuật để trao đổi/bàn giao cho khách về 2 map đã port và chạy được trong Unity runtime của dự án VLTK Mobile.

## Map trong gói

| Map ID | Tên Việt | Tên PC | Geometry key | Visual Region_C | Server Region_S | Sprite refs |
|---:|---|---|---|---:|---:|---:|
| 396 | Hội trường liên đấu Kiệt xuất (1) | 武林大会会场 | `g_7e0478bbbbc310c5` | 176 | 122 | 68 |
| 397 | Đấu trường liên đấu Kiệt xuất (1) | 联赛比赛用地 | `g_15f3c8b336d024d4` | 826 | 826 | 10 |

## Nội dung

```text
Assets/StreamingAssets/
  MapAliasCatalog.json
  MapGeometryCatalog.json
  MapServerRegionCatalog.json
  Generated/MapRegions/<geometryKey>/        # Region_C visual geometry
  Generated/MapServerRegions/<geometryKey>/  # Region_S server/static data
  Generated/MapSprites/                      # SPR refs tối thiểu cho 2 map
TECHNICAL_MANIFEST.json
sprite_copy_report.json
scripts/verify_lien_dau_package.py
docs/
```

## Lưu ý pháp lý ngắn

Gói có dữ liệu/art port từ PC JX/VLTK. Không nên bán/nhận là IP riêng nếu chưa có quyền phân phối asset gốc. Cách an toàn: bán dịch vụ port/runtime/tooling; khách phải xác nhận quyền dùng IP gốc nếu dùng dữ liệu này trong game thương mại.

Xem `docs/LEGAL_NOTICE.md` và `docs/DEAL_BRIEF.md`.

## Verify nhanh

```bash
python3 scripts/verify_lien_dau_package.py .
```

Kỳ vọng: `PASS`, chỉ có known gap `\system\spr\RegionTileDefault.spr` nếu được báo.
