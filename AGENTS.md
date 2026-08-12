# Agent Instructions

## PC Source Of Truth

- PC source: `C:/Projects/jx-pc`.

## Canonical PC Rules

- Với PAK, SPR, DAT, Hash_UID hoặc encoded config, bắt buộc dùng `C:/Projects/vltktool`; không tự hash/decode hoặc đoán encoding.
- Với SPR thì nên dùng hash/disk path không nên dùng logical path. logical path Chỉ để hiểu logic,behavior code của Pc
- SPR có text/UI: luôn kiểm tra `bin/client/package.ini` để chọn **winner theo package priority** (ví dụ Vietnamese override `update01.pak` có thể ghi đè `spr.pak`); không dùng fallback tiếng Trung chỉ vì logical path trùng.
- Resolve logical path → UID bằng `vltktool resolve_uid.py`, extract đúng frame winner bằng `vltktool extract_item_spr.py`, rồi `cmp` với PNG Unity và lưu UID/package/frame + SHA-256 vào provenance trước khi dùng.
- Không copy candidate chỉ để làm evidence. Chỉ vendor exact bytes vào repo-local slice khi asset/config đã được chọn và thực sự dùng.
- Không sửa bất kỳ file nào dưới `C:/Projects/jx-pc`.

## SPR Parity PC → Mobile (kinh nghiệm)

- Mobile chỉ hiển thị SPR đã staged hash dạng `SpritesRuntime/{hash}.spr` (root = project `/SpritesRuntime`, 67.499 file — KHÔNG phải `Assets/StreamingAssets/Sprites` chỉ có 1.160 file; xem `SprRuntimeService.DefaultSpritesRoot`); preCast/missile path từ PcSkills.txt / missles.txt là GBK bytes, hash = `SprRuntimeService.ComputePathUidHex` (GB2312, thử signed + unsigned). Không bịa path.
- Fail-closed: không gán sprite chưa staged. Skill Finished-ngay không phải lúc nào cũng bug — nhiều child missile KHÔNG có cột sprite trong missles.txt (ví dụ 20/408/274/1083..1088), PC cũng không visual.
- Melee (MisslesForm=12 + IsMelee=1): visual qua child missile, không cần PreCastSpr; nếu `isMelee` set sai → fail-closed. Form 12 không nằm trong enum `SkillMissileForm`.
- Fan spread (SKILL_MF_Spread) PC: dir_i = castDir + Param1×(i−half), đơn vị 1/64 vòng, spawn offset = Param2 px (KSkills.cpp CastSpread). Không chia 360° quanh caster.
- Faction của skill = LvlSetScript (tianwang.lua, kunlun.lua...), không phải cột CharClass (cặp phái).
- `PcAllFactionLearnedDisplaySkills.txt` = TCVN3; `PcSkills.txt` = GBK — decode bằng `PcText.Tcvn3Table`/GBK, đọc UTF-8 sẽ sinh U+FFFD.
- Verify chuẩn: probe `PlaySkillCast` trong play mode (phase + HasPcPreCastSprite + missileDirections), đối chiếu `PcFanSpreadParity` từ PcSkills.txt Param1/Param2.

## Survivor Mode (DHCD-parity roguelike)
Sandbox đã port được khá nhiều so với PC rồi. khi làm survivor phải tham chiếu sandbox
Mode MỚI song song Sandbox — KHÔNG sửa code Sandbox. Offline prototype trước, backend P3. Portrait bắt buộc.

**Tham chiếu:**
- Plan: `docs/SURVIVOR_PLAN.md` (phases P0–P3, JX→dhcd slot map)
- DHCD distilled loop: `C:/Projects/dhcd/docs/evidence/r-dhcd-*.md` (001 card economy, 002 modal queue, 003 timescale pause, 006 drop-xp)
- DHCD server: KHÔNG reverse (xem `C:/Projects/dhcd/docs/server-reverse-decision.md`)
- DHCD RandomSkillConfig encrypted (FastXXTEA) → build own skill library từ JX PcSkills.txt, không port dhcd data

**Quyết định kỹ thuật:**
- `float` không FP (chưa cần deterministic)
- Camera 2D ortho nhìn +Z, XY plane (JX SPR = side-view)
- Visual seam: `IActorVisual` — proxy màu P1, bridge JX (MalePlayerVisual adapter) P1.5

**P1 skeleton đã build + verified (compile sạch, play test OK):**
- Scene: `Assets/Scenes/Survivor.unity` (Main Camera ortho size 6 + SurvivorDirector)
- asmdef: `Assets/Scripts/Survivor/VLTK.Survivor.Runtime.asmdef` (refs Sandbox, Core, InputSystem)
- `SurvivorGameDirector.cs` — lifecycle parity BattleLevelLogic (Init/Start/GameStart/Update/GameEnd) + match brain (spawn/XP/levelup/gameover)
- `Actor/` — `SurvivorPlayer` (joystick+keyboard, auto-attack, XP/level, i-frame, ApplyCard), `SurvivorMonster` (AI chase+contact dmg+drop), `XpGem` (magnet pickup), `IActorVisual`+`ProxyActorVisual`+`ProxyVisuals` (placeholder sprite màu)
- `Combat/Projectile.cs` — travel + hit
- `Level/WaveSpawner.cs` — ramp interval/batch, perimeter spawn
- `Skill/SkillCard.cs` — 5 flat stat card (Damage/AtkSpeed/MoveSpeed/MultiShot/MaxHp)
- `UI/SurvivorJoystick.cs` (touch left-half + WASD), `UI/OverlayPanel.cs` (portrait uGUI: levelup 3-card + gameover restart, timescale pause parity r-dhcd-003)
- `BattleContext.cs`, `LevelStatus.cs` (stubs)


<!-- HARNESS:BEGIN -->
## Harness

Start with the requested outcome, then use the repository as the system of
record. Read `docs/WORKFLOW.md` and only relevant product, design, plan, code,
and validation material.

- Answers, explanations, reviews, diagnoses, plans, and status reports are
  read-only. Inspect only what is needed and do not mutate repository or Harness
  state.
- For a bounded change, use an ephemeral plan: inspect the affected behavior and
  proof, implement, and validate. No control-plane operation is required.
- Create or update one file under `docs/plans/active/` when work spans sessions,
  needs coordination, has meaningful dependencies, or requires recovery steps.
  Move it to `docs/plans/completed/` only after validation.
- Before editing, identify repository authority for each new externally
  observable policy. If materially different choices remain open, stop before
  edits; configurable defaults are not authority.
- Report reusable agent friction. Change guidance, tools, runbooks, or validation
  for that purpose only when explicitly asked to use `$improve-harness`.
- Also pause when product intent remains ambiguous, recovery is difficult,
  validation is weakened, or authority is insufficient.
- Claim completion only with relevant executable or observable evidence. Report
  the outcome, important changes, validation, and unresolved risks.

SQLite intake, story, trace, scoring, audit, and proposal commands are optional
compatibility features. Use them only when explicitly requested or required by
an external orchestrator.
<!-- HARNESS:END -->
