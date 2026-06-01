# Changelog

All notable changes to VLTK Mobile are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Male player port** (US-M21-002): layered 8-part SPR avatar
  (shadow, body, head, hair, left/right hand, left/right empty-hand weapon)
  rendered as individual `SpriteRenderer`s, matching the original JX Online 3
  layered client (`男主角贴图顺序表` draw order).
- 8-way directional run animation driven by joystick / keyboard input
  (E, NE, N, NW, W, SW, S, SE).
- `MobileJoystick` on-screen control plus keyboard fallback, with continuous
  world movement and smooth camera follow.
- Auto-spawn of player, joystick (uGUI canvas), and camera when a map loads;
  player placed at the active map's content-bounds center.
- 64 staged male SPR frames + `male_player_sprites.json` manifest in
  `StreamingAssets`, resolved at runtime via the GB2312 signed-byte path-hash UID.
- EditMode tests `MalePlayerVisualTests` (catalog, direction map, SPR load, move).
- Project legal files: `LICENSE` (proprietary), `NOTICE.md` (IP attribution),
  source copyright headers, and this changelog.

### Fixed
- Player rendered **under** the map in dense town centers — player base
  `sortingOrder` now clamps above the map ceiling (32000) so the avatar always
  draws on top.
- Player turned **invisible after stop → play** (fast enter play mode): the
  static `ClipCache` retained runtime `Sprite`s destroyed on play-stop and
  handed back fake-null sprites on replay. Caches are now cleared via
  `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` and a re-decode guard
  rebuilds any clip whose sprites were destroyed.

### Notes
- `SandboxPlayerController.moveSpeed` is temporarily set to 5x (900) for movement
  testing. Revert to 180 before a production build.

## [0.3.0] - 2026-06-01

### Added
- **JX isometric Z-projection** for builtin objects: `screenY = sceneY/2 - sceneZ*(887/1024)`
  reverse-engineered from `KRepresentShell3::CoordinateTransform`. Tall structures
  (gate beams Z=441-628, trees, multi-story buildings) now render at correct heights.
- **File-order sorting** for builtin objects: monotonically-increasing `sortingOrder`
  counter (1000+) preserves the original KIpoTree spatial-tree draw order. Solves
  multi-piece structure layering (e.g. 牌坊 gate crossbeams behind near pillars).
- **Cover/builtin layer separation**: cover objects (grass, roads) at `sortingOrder=0`,
  builtin objects (houses, trees) at `sortingOrder≥1000`. Eliminates grass-on-rooftop.
- **CustomAxis Y-sort camera**: `transparencySortMode=CustomAxis, axis=(0,1,0)` replaces
  broken `screenY*2` encoding into int16 sortingOrder. Depth now via world-Y, not hash.
- **Top-left pivot for builtin sprites** `(0,1)`: ImgPos1 is the quad top-left anchor;
  using bottom-center pivot caused half-width + full-height offset.
- **Orphan cleanup in MalePlayerVisual**: `RefreshActionParts` now destroys untracked
  child GameObjects when switching actions (Idle↔Move), preventing ghost/duplicate player.
- **Camera zoom-out**: `followOrthoSize` increased 240→480 for wider map view.
- **`jx-map-port` skill upgraded** with Z-projection formula, KBuildinObj layout,
  sorting model, sprite pivot docs, and new `references/sorting.md`.

### Fixed
- **Dark gaps through gate archways**: root cause was ignoring Z coordinate in builtin
  projection. Gate beams (Z=503) rendered 436px too low → background showed through.
- **Grass/road decals on rooftops**: cover and builtin shared same sortingOrder (0 or 1).
  Now cover=0, builtin≥1000 — cover never draws above any structure.
- **Player ghost/duplicate character**: switching actions left orphan child GameObjects
  enabled but untracked. Now orphans are deactivated and destroyed on action switch.
- **int16 sortingOrder overflow**: `screenY*2` values (~100000) overflowed int16
  (-32768..32767). `Mathf.Clamp(±32000)` pinned 3580 objects at identical values.
  Replaced with CustomAxis Y-sort + fixed layer sortingOrders.

### Verified
- Ba Lăng Huyện (巴陵县) map: 618 regions, 32442 ground tiles, 927 cover, 2645 builtin,
  0 missing art, 0 procedural fallbacks, 0 null decor — **100% visual fidelity**.
- 牌坊 gate (12 pieces, b013_v2_*): correct Z-projected heights, file-order layering.
- EditMode tests: **410/410 pass** (was 406, +4 from new tests).

### References
- `KRepresentShell3::CoordinateTransform` (Represent3/KRepresentShell3.cpp:2157)
- `KIpoTree::Paint` → `PaintObjectLayer` (Scene/KIpoTree.cpp, KIpotBranch.cpp)
- `KBuildinObj` struct layout (228 bytes, 4× ImgPos with x/y/z)

---

[Unreleased]: https://github.com/zetzet7298/vltk/tree/feature/male-player-port
