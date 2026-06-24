# Story: Full HUD Port from vltkunity

## Overview
User requested to port/clone the entire HUD (buttons, UI, UX, panels, and panel interactions) from the `/var/www/vltk-mobile/vltkunity` game.
Requirement: Re-use sprites, no custom drawing.

## Risks & Considerations
- **Scope:** Massive UI replacement affecting all gameplay screens.
- **Source of Truth Conflict:** User requested porting from another Unity project (`vltkunity`) rather than PC source. The `jx-pc-port-rule` dictates using the PC source as the single source of truth. Needs careful alignment to ensure sprites and logic match PC while fulfilling user's request.
- **Complexity:** Will require breaking down into smaller tasks (e.g., Minimap, Hotbar, Character Panel, Inventory).

## Affected Components
- Unity UI Scripts and Prefabs
- Sprite mapping and atlases

## Tasks (Proposed)
1. Review `vltkunity` HUD structure.
2. Cross-reference sprites and assets with PC Source of Truth (`/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/Ui`).
3. Port individual panels iteratively (Hotbar, Minimap, Menus).
