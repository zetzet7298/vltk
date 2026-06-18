# Project: JX Player Visual Runtime Fixes

## Architecture
- Module/package boundaries: player visual system in `VLTK.Sandbox` namespace.
- Main components:
  - `PlayerEquipmentService.cs`: Manages equipped slots and triggers visual changes.
  - `MalePlayerSpriteCatalog.cs` & `FemalePlayerSpriteCatalog.cs`: Resolves JX sprite paths from PC source.
  - `MalePlayerVisual.cs` & `FemalePlayerVisual.cs`: Layered `SpriteRenderer` system and SPR decoder.
  - `SandboxPlayerController.cs`: Integrates movement, joystick, and mount service.
  - `GMEquipmentTab.cs`: UI tab in GM panel ("G") for equipping weapons/armor/mounts.

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| 1 | M1. Default Visuals | Default male player to wearing "áo vải thô" and "mũ bố cân" | None | PLANNED |
| 2 | M2. Mounting Transitions | Fix invisibility on horse mount and transition logic | M1 | PLANNED |
| 3 | M3. Debug Panel fixes | Fix "G" debug panel equipment and mount updates for both genders | M2 | PLANNED |
| 4 | M4. Verification | Verify via verify_player.cs and EditMode tests | M3 | PLANNED |

## Interface Contracts
- `IPlayerVisual` defines the common API for player visuals.
- `PlayerEquipmentService` fires `OnEquipChanged` to notify player controller and update visuals.
