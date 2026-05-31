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

---

[Unreleased]: https://github.com/zetzet7298/vltk/tree/feature/male-player-port
