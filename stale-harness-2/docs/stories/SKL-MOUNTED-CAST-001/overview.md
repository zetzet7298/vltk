# SKL-MOUNTED-CAST-001 — PC player cast presentation

## Contract

- `CharAnimId`: `9 -> Attack`, `10 -> Attack1`, `11 -> Magic`.
- Foot magic banks: empty/hidden `MG02`, short `MG03`, long `MG04`, dual `MG05`.
- Foot physical banks: hidden uses `MG01` for both actions. Knife, staff and
  dual-hammer variants reverse the primary family attack order; the exact weapon
  variant, not only the broad family, selects the bank.
- Mounted banks: slash `HA01`, thrust `HA02`, magic `HM01`. Never `RA01`/`RM01`.
- Mounted layers: rider `BD/HD/HR/LH/RH`, `LW/RW`, horse `HH/HB/HT`. Female rider uses `FM_*`; female horse resolves canonical male `MA_H*` paths.
- Presentation matrix: male/female × foot/mounted × empty/hidden plus every
  canonical melee resource variant `1..30` × physical/magic cast action. The
  exact variant selects both the hand/body action bank and `LW/RW` overlay path.
- Body, left hand and right hand must use the PC driver's direction-inclusive
  absolute SPR index. A missing required layer stays fail-closed; no neighboring
  weapon, gender or armor resource may be substituted.
- Recast cadence uses `TimePerCast` on foot and `TimePerCastOnHorse` exactly while
  mounted; zero is a real mounted override, not a fallback. `WaitTime` remains
  missile-generation timing, and aura casts do not arm `NextCastTime`.

## Source authority

- `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/Utility/Run/Settings/NpcRes/男主角未骑马关联表.txt:1-33`
- Same directory: `女主角未骑马关联表.txt:1-33`, `男主角骑马关联表.txt:1-33`, `女主角骑马关联表.txt:1-33`.
- Layer filename columns: `男主角躯体.txt:1-5`, `女主角躯体.txt:1-5`, `男主角左手武器.txt:1-5`, `男主角右手武器.txt:1-5`, `女主角左手武器.txt:1-5`, `女主角右手武器.txt:1-5`.
- `SwordOnline/Sources/Core/Src/KNpc.h:70-88` defines `cdo_attack`, `cdo_attack1`, `cdo_magic`; `KNpc.cpp:2162-2197` clocks physical/cast actions and resets logical frame; `KNpc.cpp:2292-2300` recovers at total frame and emits at effect percentage.
- `KSkills.cpp:245-260` rejects mounted `HorseLimit=1` before action.
- `KNpcRes.cpp:253-299` keeps shadow scaling separate, chooses the first loaded
  numeric player part as the frame driver, and passes its absolute
  direction-inclusive frame index unchanged to all following layers.
- `KItemChangeRes.cpp:73-101` and `GameDataDef.h:237-250` own the melee/range
  resource-row classification used by equipment changes.

## Package probe

Table SHA-256: male/female foot `c2fd765e2bed7bd58aa07194dde30e4cb44d5f4de20b4125282bbd28bdd0eae6`; male mount `ea7a46f1e0895a7200c451e266d5c400c4dad7ce1d74b24be703c9c301f5dd61`; female mount `e72cbc268e441a8aad4cec609754dc10d83ba1705686f93dd61f7a81e4aabd44`.

Exact UID bytes for the selected foot, mounted rider, horse and weapon banks are
recorded in the three repo-local provenance manifests. The remaining required
cast hole is canonical female `FM_HR_019`; no `AT08` path is requested because
the PC relation table maps hidden Attack1 to `MG01`.

The cast manifest attributes the 60 family-default cells. A second runtime
matrix exercises all 30 melee variants for body/hand motion and exact overlay
paths; non-default weapon overlay bytes are not promoted to proven provenance
until they are resolved and selected through `vltktool`.
