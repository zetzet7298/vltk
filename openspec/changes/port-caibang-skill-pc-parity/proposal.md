# Proposal: Port Cai Bang Skills to PC Parity

## Summary
Port and repair the Cai Bang/Gaibang skill set so gameplay mechanics, runtime state effects, projectile behavior, visuals, SFX, and test coverage are traceable to the PC VLTK source instead of approximated mobile behavior.

## Problem
The mobile implementation already contains Cai Bang skill handling, but several mechanics and visuals are not PC-faithful. A concrete example is `Phi Long Tại Thiên` (`SkillId=357`): in PC, at level 20 it fires four dragon missiles that continue tracking the target even if the target moves. Mobile behavior and visuals are currently only approximate, and prior investigations found gaps around missile targeting, Kháng Long spread, buff application, default decks, and resource parity.

## Goals
- Establish PC evidence traceability for every Cai Bang skill ported or corrected.
- Align active attack skills, buff skills, support/aura visuals, missile/projectile motion, VFX, and SFX with PC behavior.
- Preserve deterministic, testable Unity runtime behavior while matching PC semantics.
- Use `jx-pc-resource-resolver` for all PC SPR/PAK/resource path lookup; do not guess asset filenames.
- Add/extend focused EditMode tests under the `CaiBang` category; avoid full EditMode suite in the dev loop.

## Non-Goals
- Do not port unrelated factions in this change.
- Do not rewrite the whole combat runtime unless a small slice proves it necessary.
- Do not invent new mobile-only visuals when a PC resource exists.
- Do not run full EditMode suite except as a final pre-push gate or after shared combat changes require it.

## Evidence Sources
- PC docs: `/var/www/vltksource_new/docs/3_ky_nang_va_chieu_thuc.md`
- PC client skill docs: `/var/www/vltksource_new/docs/client_port/03_skills.md`
- PC backend skill docs: `/var/www/vltksource_new/docs/backend_port/03_skills.md`
- PC source/config candidates:
  - `/var/www/vltksource_new/vl_update_27/Client 6.0/file/skill/gaibang.lua`
  - `/var/www/vltksource_new/vl_update_27/Client 6.0/file/skill/gaibang/gaibang-zhangfa.lua`
  - `/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/script/skill/gaibang.lua`
  - `/var/www/vltksource_new/vl_update_27/pak_unpacked/*/script/skill/gaibang.lua`
- Mobile reference data:
  - `Assets/StreamingAssets/Reference/gaibang.lua`
  - `Assets/StreamingAssets/Reference/PcSkill/skills.txt`
  - `Assets/StreamingAssets/Reference/PcAttrib/missles.txt`
- Existing mobile code/tests:
  - `Assets/Scripts/Sandbox/PcCaiBangLuaLevelService.cs`
  - `Assets/Scripts/Sandbox/SkillEffectVisualService.cs`
  - `Assets/Scripts/Sandbox/CombatRuntimeService.cs`
  - `Assets/Scripts/UI/SkillEffectRenderer.cs`
  - `Assets/Scripts/UI/SkillEffectWorldOverlay.cs`
  - `Assets/Tests/EditMode/Sandbox/CaiBang*.cs`
- Prior investigation artifacts:
  - `.agents/explorer_cai_bang_m1_1/handoff.md`
  - `.agents/explorer_cai_bang_m2_1/handoff.md`

## First Slice Strategy
1. Build a PC evidence matrix for all current Cai Bang skills in mobile.
2. Fix and test `Phi Long Tại Thiên` homing/level-20 four-dragon behavior first because the user provided it as the concrete acceptance example.
3. Then fix Cái Bang spread/buff/resource slices in small reviewable PR-sized increments.

## Risks
- PC data uses legacy encodings and hashed PAK resources; incorrect decoding/hash lookup can silently select the wrong asset.
- Some runtime gaps are shared combat architecture (`CombatRuntimeService`, buff state expiration, defender stats), so changes can affect other factions.
- Visual parity may require SPR decode/import work beyond pure C# logic.
