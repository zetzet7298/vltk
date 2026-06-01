# US-HUD-MINIMAP-PC-PREVIEW HUD minimap preview navigation

## Status

implemented

## Lane

normal

## Product Contract

Port the PC minimap behavior into the current mobile HUD placeholder: the top-right minimap displays the real active-map preview coordinate space, the player marker follows live movement, tapping the minimap opens a larger map preview, and tapping inside that preview moves the player to the selected world/map point. Mobile behavior intentionally opens the preview from direct minimap tap instead of requiring the PC magnifier button.

## Relevant Product Docs

- `AGENTS.md`
- `docs/ARCHITECTURE.md`
- `scripts/harness query matrix` (`US-M18-001`, `US-M21-002`, `US-M61-001`, `US-M64-001`)
- PC references:
  - `jxwin-kinnox/SourceNew/swrod3/Utility/Run/Ui/ui3/小地图_小.ini`
  - `jxwin-kinnox/SourceNew/swrod3/SwordOnline/Sources/S3Client/Ui/UiCase/UiMiniMap.cpp`
  - `jxwin-kinnox/SourceNew/swrod3/SwordOnline/Sources/S3Client/Ui/UiCase/UiWorldMap.cpp`

## Acceptance Criteria

- HUD minimap uses active map bounds and live player world position, not fixed placeholder coordinates.
- Player movement changes the displayed scene position and minimap dot position every frame.
- Tapping the minimap frame opens a large map preview overlay.
- Tapping a point in the map preview converts UI point -> map normalized -> world target correctly and commands player movement there.
- Preview can close via close button/background flow and does not break joystick touches.
- Coordinate transforms are covered by EditMode tests.
- Unity compiles and PlayMode screenshot/console verify live HUD behavior.

## Design Notes

- Keep PC minimap dimensions: small map rect 128x128 inside 130x130 frame; buttons at 101/115 and 115/115 from `小地图_小.ini`.
- `UiMiniMap.cpp` forwards left-clicks to game space and PC world map closes on click/key. Mobile changes direct minimap tap to preview-open, then preview click to move target.
- Use `MinimapService` for bidirectional world/minimap conversion so tests prove coordinate parity.
- Use `SandboxPlayerController` target movement instead of teleporting for map-preview click.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | EditMode `VLTK.Tests.Sandbox.MinimapTests` pass, including inverse click mapping. |
| Integration | Unity compile no errors. |
| E2E | PlayMode screenshot after movement/preview interaction shows HUD and no console errors. |
| Platform | Joystick remains uGUI, HUD decorative picking remains ignored. |
| Release | N/A sandbox HUD behavior. |

## Harness Delta

None expected unless proof workflow friction appears.

## Evidence

- Compile/import: Unity refresh + script compile completed, console errors = 0.
- Unit: EditMode job `8bcbf08b613449e0952b03d219d3f043`, 20/20 passed (`MinimapTests`, `HudDataBridgeTests`).
- PlayMode interaction probe:
  - active map bounds: `(48128,-53248,12800,6656)`
  - before `(54528,-49920)`, preview target `(57728,-48256)`, after movement `(55725.77,-49297.09)`, moved `1350.1`, `hasTarget=True`
  - `ScenePos` updated to `6965/6162`
  - minimap/preview backgrounds use runtime-captured map texture (`miniBg=True`, `previewBg=True`)
  - preview visible after minimap/open action (`previewVisible=True`)
- Screenshot: `Assets/Screenshots/hud-minimap-preview-realmap-fit.png` shows large preview opened from HUD minimap, real active map texture, live player dot, minimap dot, and HUD/joystick not overlapped.
- Follow-up fix screenshot: `Assets/Screenshots/hud-minimap-zoom-coordinates.png` shows zoomed small minimap and visible PC-style coordinates on both small minimap and preview.
- Follow-up validation: Unity compile console errors = 0; EditMode job `a4fbb436e08c4ce88b2f270c0a4eb4e2`, 20/20 passed; PlayMode console errors = 0.
- Scene saved: `Assets/Scenes/Sandbox.unity`.
