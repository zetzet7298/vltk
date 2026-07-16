# Environment And Canonical Inputs

Do not mount, download, or use legacy VMDK trees. The canonical, read-only PC
corpus is already available:

| What | Path |
|---|---|
| Loose source/config/map tree | `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/` |
| MapList.ini | `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/Settings/MapList.ini` |
| Loose maps and `.wor` bounds | `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/maps/` |
| C++ map/renderer format evidence | `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/` |
| Unpacked runtime PAK assets/data | `/var/www/jx-source/pak_unpacked/` |

The extraction scripts accept original PAK input only when it exists beneath
the canonical source root. For asset selection, paths, encodings, and
load-order winners, use `jx-pc-resource-resolver` against `pak_unpacked/`
before invoking an extractor.

## Project (Unity) paths

| What | Path |
|---|---|
| Region output | `/var/www/vltk-mobile/Assets/StreamingAssets/TestData/Regions/Map_{id}_C/` |
| Sprite output (shared) | `/var/www/vltk-mobile/Assets/StreamingAssets/Sprites/` |
| Project mapId mapping | `/var/www/vltk-mobile/Assets/StreamingAssets/MapCatalog.json` |
| Renderer / parsers / decoder | `Assets/Scripts/Sandbox/*.cs`, `Assets/Scripts/Sprites/SprRuntimeService.cs` |

## Dependencies

- `libucl.so` — UCL (NRV2B/D/E) decompression, loaded via ctypes. Install: `libucl1` /
  build from upstream. The pak compression methods map to nrv2b (UCL/BZIP2 flags),
  nrv2d (FRAGMENT), nrv2e (FRAGMENTA).
- Python 3 with `pefile` + `capstone` — **only** needed to re-disassemble a different
  `engine.dll` to re-confirm the hash; the normal pipeline doesn't need them.
  - `iconv` for inspecting GBK config files.
