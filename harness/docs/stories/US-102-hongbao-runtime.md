# US-102 Hongbao Runtime Inventory Mutation

## Status

planned

## Lane

normal

## Product Contract

Connect the `PcHongbaoOpenService` and `PcCityHongbaoOpenService` (weighted-open models) to real inventory mutations and UI feedback in `SandboxManager`. Opening a Hongbao or City Hongbao should deduct the item, calculate the reward, add the reward to inventory, and optionally broadcast a message to the player or server.

## Verification

- Unit Tests: `HongbaoRuntimeTests.cs` proving end-to-end open flow with mocked inventory, success, failures (inventory full).
- Verification script proves integration.
- `PORT_STATUS.md` is updated.
