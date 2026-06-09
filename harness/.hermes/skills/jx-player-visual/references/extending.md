# Extending to female / equipment / mount

The male avatar (`MalePlayerSpriteCatalog` + `MalePlayerVisual`) is the reference. The
layered model is gender-agnostic, so adding a new avatar is mostly data, not new logic.
Prefer **generalizing** over copy-pasting an entire second class hierarchy.

## What actually differs between avatars

| Dimension | Male | Female | Notes |
|-----------|------|--------|-------|
| art folder | `spr\npcres\man` | `spr\npcres\woman` | from `人物类型.txt` |
| filename prefix | `MA_` | female prefix (per `woman/`) | confirm by listing the folder |
| draw-order | `男主角贴图顺序表` | `女主角贴图顺序表` | Dir1..Dir8 identical -> reuse `SortingOffset` |
| part list / offsets | per SPR | per SPR | decoded at runtime, no code change |

Everything else — per-part SpriteRenderer, SPR decode, ref-pixel offset, frame stepping,
8-way direction map, both visibility-bug fixes — is shared and should not be duplicated.

## Recommended refactor

1. Introduce a small `PlayerAppearance` description that the catalog consumes:
   - `sourceRoot` (e.g. `spr\npcres\woman`)
   - `prefix` (e.g. `MA` / female prefix)
   - per-part `variant` numbers (body/head `019`, hair, hands, weapon `000`, shadow `999`)
   - which actions to load (ST, RN, ...)
2. Make `GetParts(action)` build the part->SPR rows from that description instead of the
   hard-coded male arrays. The male set becomes one preset of `PlayerAppearance`.
3. `SortingOffset` / `DirectionFromMove` stay as-is (shared tables).
4. `MalePlayerVisual` becomes `PlayerVisual` taking an appearance; keep a thin
   `MalePlayerVisual` alias if existing scenes/tests reference it, to avoid churn.

This keeps a single renderer and a single tested code path for every avatar.

## Equipment / weapon swap

Swapping armor or weapon is only a **variant number change** for that part (e.g. body
`019` -> another id, empty weapon `000` -> a real weapon id). Stage the new part SPRs,
point the appearance's variant at them, done. The draw-order and everything else are
unchanged. Watch for weapon parts that introduce special multi-weapon poses — those use
`动作贴图顺序表.INI` overrides; only wire that if the pose actually needs it.

## Mount (cưỡi ngựa) — IMPLEMENTED, see SKILL.md "Mounted system"

The mount is already built. It is a **layered horse+rider inside the player visual** (NOT
a separate horse GameObject). Key facts (full detail in SKILL.md):

- Two mounted actions: `Ride` (suffix `RD01`, idle) and `RideMove` (suffix `HR01`,
  gallop). `SetAction` remaps `Move→RideMove`, else `→Ride` when mounted.
- `BuildMountedParts` emits 8 parts: horse `HH`/`HB`/`HT` (variant `016`, parts 12/13/14)
  + rider `BD`/`HD`/`HR`/`LH`/`RH` (variant `050`). No Shadow/Weapon when mounted.
- **HH/HT SPR headers report `directions=1` (wrong)** — must pass `expectedDirections=8`
  per part or the horse spins while idle (Bug 3). The hint also goes in the clip cache key.
- Toggle via `SandboxPlayerController.ToggleMount()` + `MountToggleButton` HUD button.
  Mount has a 0.5s transition (`PlayerMountService`); `IsMounted` is true only in the
  `Mounted` state.
- The legacy single-frame `HorseVisual` GameObject is kept DISABLED on purpose.

To add a NEW horse variant: change `MountHorseVariant` (or add an alt) and stage the
`MA_(HH/HB/HT)_<variant>_RD01.spr` + `_HR01.spr` files. To add a mounted outfit: change
`MountArmorVariant`. Dir9..Dir16 in the source tables exist if you ever need >8 mounted
facings, but the current 8-dir set matches on-foot.

## Before shipping a new avatar

- Stage SPRs with `scripts/stage_player_spr.py` into a per-avatar manifest
  (`woman_player_sprites.json`) and `refresh_unity`.
- Run `scripts/verify_player.cs` (point the GameObject name at the new avatar) — all
  six checks must pass, especially CHECK 1 (parts loaded) and CHECK 3 (A/B visible).
- Add/extend EditMode tests mirroring `MalePlayerVisualTests`.
- Update `CHANGELOG.md` and the harness story.
