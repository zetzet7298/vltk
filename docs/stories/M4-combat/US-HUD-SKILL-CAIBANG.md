# US-HUD-SKILL-CAIBANG HUD skill button opens Cái Bang skills

## Status

implemented

## Lane

normal

## Intake

Change request. Flags: Existing behavior, Public/user-visible UI, Weak proof. Scope limited to HUD “Võ” button, Cái Bang skill panel, sandbox level/skill points, and player visual non-regression.

## PC / Current Evidence

- Current Unity HUD:
  - `Assets/UI/HUD/GameHud.uxml`: bottom menu has `BtnSkills` label “Võ”.
  - `Assets/Scripts/UI/GameHudController.cs`: `BtnSkills` only logged `Open Skills` before this story.
  - `Assets/Scripts/UI/PcHudVietnameseTextOverlay.cs`: draws bottom labels `Nhân`, `Túi`, `Võ`, `Đội`, `Bang`, `PK`.
- PC open behavior:
  - `jxwin-kinnox/.../S3Client/Ui/UiShell.cpp:736-740`: `Player_Skills::OnButtonClick()` executes `SCK_SHORTCUT_SKILLS`.
  - `jxwin-kinnox/.../S3Client/Ui/ShortcutKey.cpp:201-205`: skills shortcut toggles `KUiSkills::OpenWindow()`/`CloseWindow()`.
- PC skill sheet layout:
  - `jxwin-kinnox/SourceNew/swrod3/bin/Client/Ui/Ui3/UiSkillsSheet.ini`: skill window at `Left=338`, `Top=110`, `Width=205`, `Height=376`, fight/live buttons, close button.
  - `jxwin-kinnox/.../UiSkillsFightSub.ini`: 5x5 skill slots (`Skill_0..24`) each `36x36`; level text offset/color.
  - `jxwin-kinnox/.../UiSkillsLive.ini`: remain point text field.
- Combat catalog source:
  - `PcCombatCatalogFactory` from US-M44, sourced from PC `Skills.txt` + Cái Bang Lua scripts.
- Player visual invariant:
  - `MalePlayerVisual.cs`, `MalePlayerSpriteCatalog.cs`, `SandboxPlayerController.cs` are current PC-parity player visual path; this story must not alter SPR layering/sorting/cache logic.

## Implementation

- Added sandbox progression state with level 200 + 200 fight skill points grant.
- Wired `SandboxManager` to expose progression and grant Cái Bang skills from existing PC-derived catalog.
- Added UI panel service for Cái Bang skill rows and PC-like +1 upgrade handling.
- Changed `BtnSkills` click from log-only to open/toggle Cái Bang skill panel.
- Added UXML/USS panel matching PC placement intent and skill-slot structure while preserving mobile HUD/joystick rules.
- Added tests for progression, panel snapshot, HUD open behavior, and PC upgrade gates.
- PC upgrade parity: Cái Bang faction skills are seeded as known at level 0; tapping a skill spends 1 fight skill point and raises it by 1 only if `desiredLevel <= skill.maxLevel` and `desiredLevel <= playerLevel - skill.reqLevel + 1`, matching `KPlayer::AddSkillPoint` / `ApplyAddSkillLevel`.

## Acceptance Criteria

- AC1: Tapping “Võ” opens a Cái Bang skill panel.
- AC2: Opening panel grants sandbox player level 200 and 200 skill points.
- AC3: Panel lists all Cái Bang skills (ids 115–130) from PC-derived catalog.
- AC4: Skill upgrade spends one skill point and respects PC max-level/player-level gates.
- AC5: Player visual is not modified/regressed in this scope.
- AC6: Relevant tests/harness trace pass.

## Validation

- Targeted EditMode: `CaiBangSkillPanelTests` + Cái Bang combat + HUD bridge + player visual, 25/25 pass, job `f8a907e6dc8c41978fbf33f1eeea50ed`.
- Full EditMode suite: 433/433 pass, job `a3d9d613ed664bf2b07764ddf588131f`.
- Player visual proof included `MalePlayerVisualTests` 4/4 in targeted run; no `MalePlayerVisual`, `MalePlayerSpriteCatalog`, or player SPR asset files changed.
