# Environment: PC source, optional VMDK, paths, dependencies

## Primary source: extracted PC client tree

Use the audited PC source tree first. It already contains the client paks, settings,
scripts, and loose SPR fallback used during the map 389 / Vượt ải port:

```bash
ls '/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/data/maps.pak'
ls '/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/maplist.ini'
```

The bundled `scripts/jx_map_port.py` defaults to this tree. Only pass `--data-dir` when
you intentionally target a different extracted client or a mounted VMDK revision.

## Optional source: VMDK mount (read-only)

Mount the VMware disk only when the extracted tree lacks a raw pak/revision you need.
Check whether it's already mounted:

```bash
ls /mnt/jxwin/SourceNew/swrod3/bin/Client/data/maps.pak   # exists => mounted
mount | grep -i jxwin
```

If NOT mounted, this touches `/dev` and needs `sudo` — **confirm with the user first**:

```bash
sudo modprobe nbd max_part=16
sudo qemu-nbd --read-only --connect=/dev/nbd0 \
  "/var/www/vltk-mobile/jxwin-kinnox/Build JXWin Thien Dieu/Build JXWIN PGaming.vmdk"
sudo mount -o ro /dev/nbd0p1 /mnt/jxwin
```

## Key paths

| What | Path |
|---|---|
| Client paks (primary) | `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/data/*.pak` |
| MapList.ini (primary) | `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/maplist.ini` (GBK) |
| Loose art fallback roots (primary) | `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/{spr,settings,ui,...}` |
| Server/client scripts (primary provenance) | `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/{Client 6.0,Server 6.0}/.../script` |
| Client paks (optional VMDK) | `/mnt/jxwin/SourceNew/swrod3/bin/Client/data/*.pak` |
| MapList.ini / `.wor` (optional VMDK) | `/mnt/jxwin/SourceNew/swrod3/Utility/Run/{Settings/MapList.ini,maps/...}` |
| engine.dll (to re-derive hash if ever needed) | `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/engine.dll` |

`Client 6.0/data` / `bin/Client/data` is the authoritative client pak set. Treat loose
server map trees as provenance/bounds hints only; do not cross-match region payloads across
revisions (see pitfalls.md).

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
- `iconv` for reading GBK config files at the shell.
- `unrar` / `7z` if a new map revision must be pulled from a downloaded client archive
  (RAR5 from these archives extracts with `unrar`, not 7z; use `unrar e -n"<gbk path>"`
  for individual GBK-named files).

## Disk hygiene

Full client archives are 3–7 GB. Extract only the `data/*.pak` you need and delete the
archive + extracted paks afterward; keep an eye on `df -h /var/www`. You normally don't
need to download anything — the mounted VMDK already has every map's data.
