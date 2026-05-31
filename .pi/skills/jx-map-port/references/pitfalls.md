# Dead-ends already explored — do NOT repeat these

Porting balang took many sessions mostly because of one wrong assumption about the hash.
Everything below was tried and confirmed; the table saves you from re-walking it.

## The root cause of almost all "it doesn't work"

**Wrong:** unsigned-byte path hash (what the project's C# `ComputePathUid` and the old
vltktool used). For ASCII-only paths (`\spr\npcres\...`, `\settings\maplist.ini`) it
*happens* to match because all bytes are <128, so people "validate" the hash and trust it.
But every terrain/building path contains GBK Chinese (bytes ≥0x80) and silently misses.

**Right:** `g_FileName2Id` treats bytes as **signed char** (movsx). Use the version in
`scripts/jx_map_port.py::g_filename2id`. Validate it on a Chinese path, e.g.
`\maps\两湖区\巴陵县\v_106\106_Region_C.dat` should be present in `maps.pak`. If a known
Chinese path resolves, the hash is correct.

To re-derive the hash from a different `engine.dll`: disassemble the export
`?g_FileName2Id@@YAKPAD@Z` (RVA ~0x10799 in the 2021 build). Look for `movsx` (signed) vs
`movzx` (unsigned) on the path byte, the `% 0x8000000B`, the `imul …, -0x11` (=0xFFFFFFEF),
and the final `xor 0x12345678`. capstone/pefile do this in ~30 lines.

## Things that look promising but are WRONG / wasteful

| Approach | Why it fails / wastes time |
|---|---|
| Obstacle-hash matching loose `_Region_S` ↔ `maps.pak` | The loose `Utility/Run/maps` *server* tree is a **different map revision** than `bin/Client/data/maps.pak`. Only ~78–98 of 580 balang cells match. Don't bridge versions this way. |
| Brute-forcing path templates with the unsigned hash | 0 matches even with 630 known (X,Y,UID) Chengdu pairs. The hash, not the template, was wrong. |
| Assuming "terrain art isn't shipped in the client" | False. It's in the paks; it just wouldn't resolve under the wrong hash. With the signed hash, all 198 balang art names resolve. |
| Object-coordinate filtering to isolate a map from maps.pak | Scene coords are reused across maps, so you catch 35k yellow-palette regions from many maps. You can't isolate one map by content/coords. Use the path hash. |
| Relying on a sibling project's pre-converted regions (e.g. an h5 export) | It can be a different/incomplete revision (missing town-center buildings, only 59 vs 198 art names). Extract from the pak yourself. |
| Trusting the in-repo `SprRuntimeService.ComputePathUid` for pak lookups | It's unsigned — correct ONLY as the extractor↔runtime filename scheme, never for reading paks. |
| Downloading whole 3–7 GB clients to "find the art" | Clients are pak-only; the art was always in the local VMDK pak. Crack the hash first; download only if a genuinely different map revision is needed. |

## SPR gotchas

- **Per-frame compressed SPRs** (`桃树`, `竹子`, large trees, `大碧波_*` water blends):
  stored as `head+palette + frame_info[Frames]{compress_size:i32, size:i32} + blobs`.
  Copying the raw bytes yields garbage frame dims (you'll see Unity
  `Texture has out of range height (got 25601 max 16384)`). The script's
  `spr_rebuild_perframe` decompresses each frame and rewrites a flat SPR. If a NEW shape
  appears, detect it by validating frame0's width/height ≤ 16384 and rebuild.
- **Whole-UCL SPRs**: entry flag's high byte is the UCL method; decompress the whole entry.
- **Loose-art 0xCD-filled files**: some loose `游戏资源` SPRs are MSVC-uninitialized
  placeholders (start `CD CD …`). Ignore them; the pak copy is the real one. The script
  prefers pak over loose for exactly this reason.

## Rendering gotchas

- `manage_camera screenshot` only captures real content **while in play mode**. A shot
  after `manage_editor stop` shows just the skybox gradient. Always screenshot during play.
- The Sandbox scene is empty at edit time; `SandboxManager` builds everything at runtime
  under `DontDestroyOnLoad`, so `find_gameobjects` in edit mode returns nothing — that's normal.
- The town-focus heuristic weights named structures (房屋/house/桥/墙/牌坊/井) far above
  trees so the camera frames the settlement, not a forest patch. If a new map's center is
  off, check that its buildings carry those substrings, or adjust the weight list.
- Unity's MCP bridge can drop/restart during long region loads (618 regions × many sprites).
  If `read_console`/screenshot returns "session not ready", wait ~10s and retry; re-enter
  play mode cleanly if the watchdog restarted.

## Verification quicklist

1. `extracted regions: N` with N ≈ cells in the `.wor` rect (618 for balang's 33×20 minus gaps).
2. `staged art: M/M failed=0`.
3. Unity console: `Rendered N regions; SPR stats: 0 resolved, 0 missing` (the "0 resolved"
   counter is cosmetic — it counts the unused `ResolveSprite` cache, not `ResolveTexture`).
4. Play-mode screenshot shows real terrain + buildings + trees, not flat colors.
5. `run_tests EditMode` stays 406/406.
