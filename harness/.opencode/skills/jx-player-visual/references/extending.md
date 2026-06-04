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

## Mount (cưỡi ngựa)

Mount adds horse parts (ids 12/13/14) and the `RD` (ride) action set. The draw-order
rows already include 12/13/14, and Dir9..Dir16 in the source tables cover mounted poses
if you later need more than 8 facings. Load the `RD` SPRs for both rider parts and horse
parts, switch the action to a `Ride` state, and reuse the same offset/sort path.

## Before shipping a new avatar

- Stage SPRs with `scripts/stage_player_spr.py` into a per-avatar manifest
  (`woman_player_sprites.json`) and `refresh_unity`.
- Run `scripts/verify_player.cs` (point the GameObject name at the new avatar) — all
  four checks must pass, especially CHECK 1 (parts loaded) and CHECK 3 (A/B visible).
- Add/extend EditMode tests mirroring `MalePlayerVisualTests`.
- Update `CHANGELOG.md` and the harness story.
