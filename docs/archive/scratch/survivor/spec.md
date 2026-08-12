# Survivor Mode — Spec

Tracker: local-markdown (`.scratch/survivor/`)
Status: `ready-for-agent`
Created: 2026-08-02
Handoff từ: [wayfinder map](map.md) (đã close research 01-09 + design 16/25; open design tickets 10-15/17-24 được resolve thành Implementation Decisions dưới đây)

## Problem Statement

Người chơi mobile muốn một mode survivor roguelike offline (portrait, chạm) trong
`vltk-mobile`: đứng trong arena, đánh quái theo đợt, hút XP, lên cấp, chọn skill từ 3 card,
sống sót càng lâu càng tốt. Loop-shape theo Đại Hiệp Chế Đạo (DHCD), visual/content lấy từ
JX (jx-pc) đã port trong Sandbox. Hiện chỉ có P1 skeleton: auto-attack + 1 loại card flat-stat,
chưa có skill library thật, wave đơn điệu, không boss/shop/box/endless, không save/settings/i18n,
không audio/VFX, chưa build mobile. DHCD RandomSkillConfig mã hóa (FastXXTEA, key blocked) →
KHÔNG port được data dhcd; balance số phải tự thiết kế.

## Solution

Mode mới `Survivor` song song Sandbox (KHÔNG sửa code Sandbox, bridge qua adapter đọc-only)
đạt mức ship được: offline single-player, portrait, touch, 60fps, Android + iOS.

- Loop đầy đủ parity-shape dhcd: wave trigger đa kiểu → boss multi-phase → 3-mode skill choice
  (levelup/box/shop) + reroll + per-role queue → supply skill → impact/buff → endless ramp.
- Skill library tự author từ JX `PcSkills.txt` + `missles.txt` (1.216 skill / 441 missile),
  visual fail-closed qua SPR staged (`/SpritesRuntime`).
- Số balance (weight card, drop rate, XP curve, ramp endless) = **own-design**, ghi rationale,
  KHÔNG clone dhcd.
- Save/settings/i18n/pause đủ cho offline ship; audio/VFX bridge qua Sandbox service + own
  pipeline cho BGM/mixer.
- Test seam duy nhất: **EditMode pure-logic** (pattern `SurvivorP1LogicTests`); feel = manual
  play-checklist per phase.

## User Stories

1. Là người chơi, tôi muốn chạy game ở chế độ portrait với joystick chạm + WASD, để di chuyển nhân vật trong arena.
2. Là người chơi, tôi muốn nhân vật tự động bắn đạn về quái gần nhất, để tập trung né đòn thay vì bấm bắn.
3. Là người chơi, tôi muốn quái spawn liên tục từ rìa arena theo đợt, để có nhịp chiến đấu không ngừng.
4. Là người chơi, tôi muốn quái đuổi theo và chạm vào gây sát thương cho tôi, để có áp lực né tránh.
5. Là người chơi, tôi muốn quái chết rơi XP gem, để tiến tới levelup.
6. Là người chơi, tôi muốn XP gem tự hút về khi tôi tới gần (magnet), để không phải nhặt từng viên.
7. Là người chơi, tôi muốn lên cấp thì game tạm dừng và hiện 3 card skill, để chọn hướng build.
8. Là người chơi, tôi muốn card skill hiển thị tên + mô tả + icon skill thật (JX), để chọn có thông tin.
9. Là người chơi, tôi muốn skill có nhiều loại khác nhau (đạn thẳng, fan spread, melee child-missile, buff thụ động), để build đa dạng.
10. Là người chơi, tôi muốn skill library lấy từ 10 phái JX (tối thiểu pool player ~452 skill), để nội dung quen thuộc.
11. Là người chơi, tôi muốn chọn skill từ pool có weight, để card hiếm xuất hiện hợp lý.
12. Là người chơi, tôi muốn skill đã chọn được nâng cấp cấp độ khi gặp lại, để thấy build lớn dần.
13. Là người chơi, tôi muốn reroll danh sách card (giá riêng cho shop), để không kẹt build.
14. Là người chơi, tôi muốn hòm (box) mở ra nhiều skill hơn lúc levelup, để có khoảnh khắc buff mạnh.
15. Là người chơi, tôi muốn cửa hàng (shop) giữa run cho mua card cố định giá, để tiêu vàng có chủ đích.
16. Là người chơi, tôi muốn skill hỗ trợ (heal/bomb/magnet/full-clear) có slot + cooldown riêng, để cứu nguy khi nguy hiểm.
17. Là người chơi, tôi muốn đợt boss xuất hiện theo trigger (thời gian / kill% / HP%), để có mục tiêu lớn.
18. Là người chơi, tôi muốn boss đổi phase theo % máu mất (đổi AI + skill pool), để boss không đơn điệu.
19. Là người chơi, tôi muốn boss chết rơi thưởng lớn (nhiều XP/gem + hòm), để đánh boss xứng đáng.
20. Là người chơi, tôi muốn đợt swarm (đông, spawn động theo dynamic cap), để có phút dồn dập.
21. Là người chơi, tôi muốn elite quái (own-design) xuất hiện xen kẽ, để có quái tinh anh giữa đám thường.
22. Là người chơi, tôi muốn sau đợt cố định mode endless tự mở với ramp khó dần, để chơi không hồi kết.
23. Là người chơi, tôi muốn quái tăng máu/sát thương/số lượng/tốc độ theo thời gian, để độ khó leo đều.
24. Là người chơi, tôi muốn hiệu ứng trạng thái (buff ATK, debuff, DOT độc/bỏng, stun/freeze) áp dụng qua model generic, để skill có chiều sâu.
25. Là người chơi, tôi muốn attribution sát thương về đúng skill/buff (kill credit → XP), để điểm kinh nghiệm công bằng.
26. Là người chơi, tôi muốn skill/missile chưa staged SPR vẫn cast được với fallback màu proxy, để không có skill chết im lặng (fail-closed).
27. Là người chơi, tôi muốn nhân vật hiển thị bằng visual JX thật (MalePlayerVisual/PcNpcVisual), để đẹp và đúng thương hiệu.
28. Là người chơi, tôi muốn precast + missile SPR hiển thị khi cast skill (SkillEffectVisualService parity), để skill nhìn rõ ràng.
29. Là người chơi, tôi muốn SFX (hit/cast/pickup/levelup/die) + BGM (menu/battle/boss) + mixer master/bgm/sfx, để có cảm giác game.
30. Là người chơi, tôi muốn HUD hiển thị HP/XP/level/timer + icon skill + cooldown, để theo dõi trạng thái.
31. Là người chơi, tôi muốn màn gameover + restart nhanh, để thử lại run mới không chờ đợi.
32. Là người chơi, tôi muốn main menu + settings (volume/graphics/ngôn ngữ) lưu lại giữa các phiên, để không phải chỉnh lại mỗi lần chơi.
33. Là người chơi, tôi muốn pause khi mở card/settings/ra ngoài app (timescale 0 đúng scope), để không chết oan khi đang bận.
34. Là người chơi, tôi muốn tiến trình (unlock/best run/meta-upgrade) lưu an toàn + chống hỏng, để không mất thành quả.
35. Là người chơi, tôi muốn ngôn ngữ VN/EN chuyển đổi runtime không restart, để chơi bằng ngôn ngữ mình thích.
36. Là người chơi, tôi muốn game chạy 60fps trên mobile trung bình với monster cap + profiling plan, để không giật lag.
37. Là người chơi, tôi muốn build Android + iOS (IL2CPP, portrait lock), để chơi trên điện thoại thật.

## Implementation Decisions

Mọi decision: **structure-parity** (cite declaration dhcd) + **own-design** (số balance, rationale).
`float`, không FP. KHÔNG sửa code Sandbox — bridge qua adapter read-only. Portrait.

### D1. Parity định nghĩa (ticket 01)
- Structure-parity: lifecycle hook, field/schema, command shape, queue structure, state machine —
  trích từ `reconstructed-types/BattleCore/*.cs` + evidence `r-dhcd-*.md`, cite declaration path.
- Own-design: mọi con số + balance curve + feel, ghi rationale. KHÔNG clone số dhcd.

### D2. Skill library (tickets 02, 04)
- Nguồn: JX `PcSkills.txt` (GBK) + `PcAllFactionLearnedDisplaySkills.txt` (TCVN3) + `missles.txt`
  (441 missile); decode bằng Sandbox parser có sẵn (read-only). Output = `SkillDef` ScriptableObject
  riêng của Survivor.
- Schema map (col → SkillDef): `2→Id, 70→Faction, 19→Form, 26→IsMelee, 20→ChildMissileId,
  6→PreCastSprUid(GBK→ComputePathUidHex), 58/60→Fan Param1/2, 52/53→Req/MaxLevel, 71-110→LevelScaling`.
- **BUG parser Sandbox**: `PcSkillFullParser.LvlSetScriptCol=71` sai — col đúng là 70 (71 =
  LvlSetting1). Survivor KHÔNG gọi đường parse bị lỗi; tự parse faction theo col 70.
- Faction = `LvlSetScript` (tianwang.lua, kunlun.lua...), không phải col CharClass.
- Pool: 10 phái ~452 skill player pool; remainder (special/npc/partner/battles) = boss/npc pool.
- Cast form: MisslesForm 7 (đạn chủ đạo) + 12-melee (IsMelee=1, visual qua child missile —
  KHÔNG nằm enum `SkillMissileForm`); `child<>0` (675 skill) cần resolve child missile.
- Fail-closed: runtime `SprRuntimeService.FindSprDataInRoot(uid)` → null ⇒ proxy màu; child
  missile không có AnimFile (vd 20/408/274/1083-88) ⇒ KHÔNG gán sprite, vẫn cast (PC cũng
  không visual).
- Supply subset (ticket 13): heal=`lifereplenish_v/lifemax_v`; bomb=`special/bomb.lua` + dmg;
  magnet = own (collect mgr); buff = `IsAura`.
- Card pool composition law: weight theo faction + rarity own-design; nâng cấp skill đã có khi
  trùng (stack level theo `ReqLevel/MaxLevel`).
- SPR root thực tế = `/SpritesRuntime` (67.499 file) — đã sửa note AGENTS.md. Hash =
  `SprRuntimeService.ComputePathUidHex` (GB2312, thử signed + unsigned).

### D3. Wave system (tickets 05, 10)
- Wave-type KHÔNG phải enum đơn — 2 lớp: **trigger** = `WaveEventFuncType` 9 giá trị
  (1=time, 2=kill%, 3=HP%, 4=skill-cast, 5/6=kill-all, 7-9=occupy) + **boss flag**
  `MonsterCfg.IsBoss` + **swarm** dynamic fields (`DynamicMonsterTime/LoopNum/MaxNum`,
  `Isloop`). Elite KHÔNG có trong dhcd → own flag `IsElite`.
- Lifecycle parity: `LevelMonsterMgr.StartSpawn → WaveFuncByX.Trigger → LevelWave.CreateCurWave
  → WaveRefresh.Start → batch spawn theo Interval/SingleNum/dynamic → TimeOver/Finish →
  BattleFinsh`.
- Author wave table trực tiếp qua DIY hook `InitByDiyLevelWave` (KHÔNG binary cfg dhcd).
- Own-design: wave timing, batch size, ramp interval/count, elite ratio — config ScriptableObject.

### D4. Boss multi-phase (tickets 06, 11)
- Phase-switch = **damage-window keyed**: `BossChangeBehaviorCmpt.OnHpChg → GetJiangHuBossPhaseConfig(lossHp)`
  → phase table `{BossDamageMin/Max, MonsterAI, Skill[], BootyID}` — KHÔNG timer/cast-count.
- Boss skill pool = subset skill library (npc/boss pool từ D2). Boss spawn qua wave boss-type.
- Own: số phase, HP window, thưởng (BootyID → drop table D6).

### D5. Skill choice 3-mode + queue (tickets 06, 12)
- 3 mode = `RandomSkillParam.Type {1 levelup, 2 box, 3 shop}` + `boxParam.learnNum` (box mở
  nhiều card hơn); `RequestRandomSkill` enqueue-if-waiting / trigger-ngay.
- Per-role queue parity: `Dictionary<ulong, PlayerRandomSkillData>` + `Queue<RandomSkillParam>`
  + `beginWaitingLearnTime` (time-predicate); FIFO semantics own (r-dhcd-002 chỉ prove structure).
- Reroll = 2 cmd riêng: `FrameCmdRerandomSkill` (levelup) + `FrameCmdReSelectRandomSkill{RollCnt}`
  (shop, giá cố định). Command shape = `SurvivorBattleCmd` (SelectRandomSkill/ReRandomSkill/
  SelectBoxSkill/SkillRun-equivalent).
- Pause khi mở card: **own-design** ref-counted `SurvivorPause` per-scope
  (CardChoice/Settings/AppLifecycle/GameOver) → `Time.timeScale ∈ {0,1}` (bounded theo r-dhcd-003,
  KHÔNG claim global/input).

### D6. XP/gold/drop/magnet/level (tickets 05, 14)
- `SurvivorCollectItemMgr`: drop khi die (`OnActorDie`/`TriggerWave`/`TestRate` parity-shape),
  merge gem, magnet pickup (radius/speed own), `LevelExpCalc.AddExp` parity-shape.
- Drop table ScriptableObject (PoolID/ItemID/OutputType/Param1/Param2/`BronID` — schema ý tưởng
  từ `CollectItemPoolConfig`, giá trị own).
- Level curve own: P1 `5+(L-1)*3` giữ làm default, P2 thay bằng curve config own (ghi rationale).

### D7. Impact/buff (tickets 07, 15)
- Generic model, KHÔNG enum status tên riêng:
  - Stat buff: `ActorAttrImpactMgr` **4 bucket** (Absolute/Relative/Multiply/Effect, `ActorAttrAddType`).
  - Control: `BuffStateID` bitmap 20 state (stun/no-move/no-skill/sleep/confusion/invisible...);
    freeze/slow/silence = attr-flavor ở config.
  - DOT (poison/burn): generic `BuffDot` (loop-timer tick, `TickWhenAdd`, `RemoveAfterDot`,
    heal variant); `DamageInfo.sourceType=SourceBuffer`.
  - Stun → `ActorSM` qua `ActorStateEvent.Enter/Finish_Stun`.
  - Stack/replace/refresh: `BuffAttrConfig.FindAttr(stack)` + `BufferItem.ReplaceAdd` shape.
- Attribution: `SkillImpactSource{skillId, buffId}` + caster ref → `SumSkillDamage` (kill credit → XP).
- Own: số DOT tick/duration, stacking cap, element flavor ở config.

### D8. Visual bridge (tickets 08, 16)
- Seam `IActorVisual` (đã prove P1.5: `JxPlayerVisual` wrap `MalePlayerVisual`, variant BD_019,
  ppu=40 char ~1.9 unit; sentinel probe `MA_BD_019_ST01.spr` miss → Proxy). P1.5 đã verify 8
  part render + spawning intact.
- Monster visual (P2): `PcNpcVisual` + `NpcTemplateRegistry` qua adapter; VFX/audio monster đi
  cùng skill library (04/13).
- Y-sort defer (MalePlayerVisual tự lo sortingOrder).
- Fail-closed: chưa staged → proxy màu; không bịa path (D2).

### D9. VFX (ticket 19)
- Parity `SkillEffectVisualService` (data-driven `missles1.txt`, fail-closed sẵn): precast SPR +
  missile SPR qua adapter; `PcSkillVisualAutoMapper`/`PcMissileFullVisualParser` read-only.
- `MissileSpawner` gắn host Sandbox → adapter KHÔNG dùng (spawn đạn = `Combat/Projectile` own).
- Hit flash, death effect, levelup burst: own lightweight (particle/scale flash) — VFX sprite
  only khi staged.

### D10. Audio (ticket 18)
- SFX: `AudioService.PlaySkillCast(pcPath)` + `SoundEffectService` — skill SFX staged
  (`StreamingAssets/sound/skill/` 28 wav); hit/pickup/levelup/die = own mapping.
- BGM: ogg JX chưa staged + mixer chưa có → **own pipeline**: `SurvivorAudioMgr` + AudioMixer
  (master/bgm/sfx), menu/battle/boss tracks, volume theo settings D13.
- MusicService/SoundListService/MapMusicService = reference read-only.

### D11. UI (ticket 17)
- Giữ uGUI portrait (pattern hiện có `OverlayPanel`/`SurvivorJoystick` — KHÔNG chuyển UI Toolkit).
- HUD: HP/XP/level/timer + skill icon + cooldown. Modal: levelup 3-card (timescale pause D5),
  shop, box-open, gameover+restart, settings, main menu.
- Screen-flow đơn giản: MainMenu → Survivor → (pause) → Settings; gameover → restart reload
  scene `Survivor`. Overlay parity r-dhcd-003 (OnVisible/OnHidden acquire/release pause scope).

### D12. Save (ticket 20)
- Progress + settings RIÊNG; v1 = PlayerPrefs + JsonUtility (shape `BaseClientData` + Sandbox
  `PcSaveSlotService` reference). Mid-run save **defer** (JSON file + Newtonsoft khi cần).
- Slot/versioning/migration + corrupt-recovery (fail → reset + giữ backup).

### D13. Settings/pause (ticket 21)
- Audio: category volume (master/bgm/sfx) → `AudioService` + own mixer; graphics: quality int;
  language: mã `vi`/`en`. Persist + apply-runtime.
- Pause: `SurvivorPause` ref-count per-scope (D5); app-lifecycle `OnApplicationPause` →
  Acquire/Release(AppLifecycle) — own (dhcd không evidence).

### D14. i18n (ticket 22)
- v1: tự author `SurvivorText` VN/EN bundle (StreamingAssets, pattern copy
  `TextResourceService`), runtime switch không restart, fallback `vi`. Key namespace
  `survivor.<screen>.<key>`.
- Unity Localization package (đã cài 1.5.12) = upgrade path khi key set lớn; `PcText`
  TCVN3/GBK chỉ decode file PC.

### D15. Endless (ticket 23)
- Parity skeleton: `IsReposeWave` + `WaveRefresh` dynamic caps + `GetEndlessWaveCount()`.
- Ramp curve family own: **linear v1** (scale = f(waveIndex): monster hp/atk/count/speed +
  boss frequency), exponential/stair-step = upgrade path sau playtest (fog "Difficulty feel").

### D16. Mobile ship (ticket 24)
- Portrait lock, touch (joystick + tap), 60fps budget: monster cap (config), draw call/batch
  tracking, profiling plan (`FrameTimingManager`/Profiler). Build Android + iOS IL2CPP, verify
  camera ortho size 6. `/SpritesRuntime` cần đóng gói khi build (SprRuntimeService note: copy
  vào StreamingAssets nếu APK cần).

### D17. Config authoring (ticket 02)
- SkillDef/drop table/wave table/level curve = ScriptableObject hoặc text config (StreamingAssets)
  tự author; mỗi parser + config để lại 1 EditMode self-check.

## Testing Decisions

- **Seam duy nhất: EditMode pure-logic** — pattern `SurvivorP1LogicTests` (stub `IActorVisual`
  inline, không scene, không PlayMode cho logic). Logic phải sống ở class thuần
  (init-able không cần MonoBehaviour scene), giữ `SurvivorGameDirector` mỏng.
- Test tốt = external behavior: output curve/queue semantics/phase transition/round-trip,
  KHÔNG assert implementation detail.
- Modules test:
  - Skill parse: col map (đặc biệt faction col 70 ≠ 71 bug), fail-closed list (child không AnimFile).
  - Wave: trigger evaluation (9 type), batch spawn math, ramp.
  - `SurvivorRandomSkillCtrl`: queue FSM (enqueue/wait/dequeue), 3-mode, reroll 2 cmd, learnNum box.
  - Boss phase: damage-window → phase switch đúng window.
  - Impact: 4-bucket math, DOT tick, stun state, attribution → XP credit.
  - Collect: drop table rate (statistical), magnet, level curve.
  - `SurvivorPause`: ref-count acquire/release (timescale ∈ {0,1}).
  - Save: round-trip, version migration, corrupt-recovery.
  - i18n: key fallback + hot-switch.
- Feel (wave timing, difficulty, boss fairness) = manual play-checklist per phase (pattern
  `p1-acceptance.md`), KHÔNG tự động hóa.
- Prior art: `Assets/Tests/EditMode/Survivor/SurvivorP1LogicTests.cs` (8 tests, gate 1 P1),
  `p1-acceptance.md` checklist (gate 2-3).

## Out of Scope

- **P3 backend** (multiplayer/cloud save) — effort riêng (ticket 03), KHÔNG trong offline bar.
- Reverse dhcd server / port dhcd data (RandomSkillConfig FastXXTEA) — cấm theo
  `server-reverse-decision.md` + AGENTS.
- Sửa code Sandbox — bridge đọc-only qua adapter (`IActorVisual`, service).
- Numeric parity dhcd (card weight/drop/XP) — balance = own-design.
- Mid-run save v1 (defer tới khi loop ổn).
- Deterministic networking FP — `float` đủ.
- Non-portrait (landscape), PC-only tính năng JX ngoài mode.

## Further Notes

- AGENTS.md note SPR root đã sửa: `/SpritesRuntime` (67.499) chứ không phải
  `Assets/StreamingAssets/Sprites` (1.160).
- Bug parser `LvlSetScriptCol` phải xử lý ngay khi build skill parser (D2) — nếu dùng lại
  Sandbox parser, gọi đúng col 70.
- P1.5 (visual bridge player) đã implement + verify — spec này kế thừa, monster visual = P2.
- BGM/mixer chưa staged → own pipeline (D10), không chờ JX audio.
- Tickets 10-15/17-24 vẫn `ready-for-human` dưới dạng grilling — spec resolve bằng evidence
  research; nếu playtest phát hiện cần đổi, mở follow-up ticket.
- Y-sort, elite design, endless ramp = điểm mở cần playtest xác nhận (fog cũ).
