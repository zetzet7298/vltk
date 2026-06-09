# US-100 Map 907 Runtime Smoke

## Status

planned

## Lane

normal

## Product Contract

Complete the Map 907 (Vượt ải Nhiếp Thí Trần) runtime test by adding player to the scene and verifying in-editor movement feel, bounds clamping, minimap synchronization, and click-to-move capabilities to mark the runtime as `✅` in `PORT_STATUS.md`.

## Relevant Product Docs

- `docs/PORT_STATUS.md`

## Acceptance Criteria

- Player can spawn and move smoothly in Map 907 inside the Unity Editor (Play Mode).
- Movement is correctly clamped to map bounds derived from PC data.
- Minimap updates correctly based on player position.
- Click-to-move functions properly.
- `PORT_STATUS.md` is updated to reflect runtime completion for Map 907.

## Design Notes

- Commands: Add player to scene, run Play Mode, perform click-to-move tests.
- UI surfaces: Minimap UI, Player visual.
- Domain rules: PC Map limits and bounds for Map 907.

## Validation

When updating durable proof status, use numeric booleans:
`scripts/bin/harness-cli story update --id US-100 --unit 1 --integration 1 --e2e 0 --platform 0`.

| Layer | Expected proof |
| --- | --- |
| Unit | Map907 bounds and minimap calculations |
| Integration | Scene playback |
| E2E | In-editor play test |
| Platform | |
| Release | |

## Harness Delta

None.

## Evidence

To be added.
