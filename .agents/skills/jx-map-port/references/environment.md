# Environment: VMDK mount, paths, dependencies

## The game data lives on a VMDK (read-only)

The original JX/VLTK PC game (art + paks + source) is inside a VMware disk that must be
mounted before extraction. Check whether it's already mounted:

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

## Key paths inside the VMDK

| What | Path |
|---|---|
| Client paks (extract from here) | `/mnt/jxwin/SourceNew/swrod3/bin/Client/data/*.pak` |
| MapList.ini (mapId ↔ name) | `/mnt/jxwin/SourceNew/swrod3/Utility/Run/Settings/MapList.ini` (GBK) |
| Loose map tree (.wor bounds, server regions) | `/mnt/jxwin/SourceNew/swrod3/Utility/Run/maps/<region>/<name>.wor` |
| Loose art fallback roots | `…/Utility/Run` and `…/bin/Client` |
| engine.dll (to re-derive hash if ever needed) | `…/Utility/Run/engine.dll` (export `g_FileName2Id`) |

`bin/Client/data` is the authoritative client; `Utility/Run/maps` is an older server tree —
do not cross-match them (see pitfalls.md).

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
