# US-101 Travel and Revive Runtime Behavior

## Status

planned

## Lane

normal

## Product Contract

Connect the data-proven `PcMapTravelActionService` to the actual Unity game scene. When a teleport action is resolved (e.g. Waypoint, Default Revive), the game must seamlessly teleport the player. If the target map is different, it must trigger a map change via `MapManager` and move the `SandboxPlayerController` to the target coordinates.

## Relevant Product Docs

- `docs/PORT_STATUS.md`

## Acceptance Criteria

- `PcMapTravelBehaviorService` exists and handles `PcMapTravelActionResult`.
- Teleportation to the same map updates the player's position immediately.
- Teleportation to a different map triggers `MapManager.LoadMap` followed by position update.
- E2E Play Mode verification passes.
- `PORT_STATUS.md` is updated to reflect runtime completion for Waypoint/wharf/revive/scroll.

## Design Notes

- Commands: Implement `ExecuteTravelAction(PcMapTravelActionResult result, SandboxPlayerController player, SandboxManager manager)`.
- The UI (Waypoint dialog, Revive dialog) can call this service directly.

## Validation

When updating durable proof status, use numeric booleans:
`scripts/bin/harness-cli story update --id US-101 --unit 1 --integration 1 --e2e 0 --platform 0`.

| Layer | Expected proof |
| --- | --- |
| Unit | Teleport coordinate parsing |
| Integration | Scene playback teleport execution |
| E2E | In-editor play test |
| Platform | |
| Release | |

## Harness Delta

None.

## Evidence

To be added.
