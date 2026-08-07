# Survivor Mode — Wayfinder Map

Label: `wayfinder:map`
Effort: `survivor`
Tracker: local-markdown (`.scratch/survivor/`)
Created: 2026-08-02

## Destination

Shippable mobile survivor roguelike mode (DHCD / Đại Hiệp Chế Đạo FULL-parity) trong
Unity `C:/Projects/vltk-mobile`. Offline single-player = sản phẩm hoàn chỉnh ship được
(Android+iOS, portrait, 60fps). Multiplayer/server = P3, **effort riêng** (decision ticket 03,
KHÔNG nằm trong offline bar). Map xong khi MỌI mục completeness bar có ticket quyết định +
route clear → handoff sang `to-spec` (session riêng).

## Notes

- Domain: Unity C# 2D-ortho (+Z, side-view JX SPR). `float` không FP. Mode MỚI song song
  Sandbox, KHÔNG sửa Sandbox (bridge via adapter `IActorVisual`). Visual seam: proxy P1 →
  JX bridge P1.5.
- Skills bắt buộc consult: `C:/Users/zet/wiki/pi-wiki-local/skills/wayfinder/SKILL.md`,
  repo `docs/agents/issue-tracker.md`, `docs/agents/triage-labels.md`, `docs/agents/domain.md`.
- Ground truth (read-only): `AGENTS.md` §Survivor Mode, `docs/SURVIVOR_PLAN.md`,
  `C:/Projects/dhcd/docs/evidence/r-dhcd-*.md`, `C:/Projects/dhcd/docs/gameplay-evidence-map.md`,
  `C:/Projects/dhcd/docs/server-reverse-decision.md`, `C:/Projects/dhcd/reconstructed-types/BattleCore/`.
- DHCD evidence = **declaration + partial IL recovery, KHÔNG authoritative behavior**.
  Numeric balance (card weights r-dhcd-001, drop/XP r-dhcd-006) **BLOCKED/encrypted FastXXTEA,
  key blocked** → KHÔNG port dhcd data, build own library từ JX `PcSkills.txt`. KHÔNG reverse
  dhcd server. PC source `C:/Projects/jx-source` read-only; SPR/PAK/hash → `C:/Projects/vltktool`,
  không bịa path. Fail-closed SPR.
- **SPR root thực tế = project `/SpritesRuntime` (67.499 file)**, KHÔNG phải
  `Assets/StreamingAssets/Sprites` (research 08 phát hiện — sửa note AGENTS khi to-spec).
- Standing preference: tuân thủ ask-matt (wayfinder→to-spec→to-tickets→implement). Ra QUYẾT ĐỊNH
  không ra deliverable trong map này.

## Decisions so far

<!-- 1 dòng/ticket đã close: gist + link. Detail nằm trong ticket. -->

- [Parity definition](issues/01-parity-definition.md) — parity = structural/lifecycle/loop-shape (declaration+evidence) + OWN balance tuning; KHÔNG numeric parity (dhcd data blocked).
- [Data config authoring strategy](issues/02-data-config-authoring-strategy.md) — tự author ScriptableObject/text config (skill lib từ JX PcSkills.txt, wave/drop/xp curve own design); dhcd configs chỉ làm schema reference.
- [Backend P3 scope](issues/03-backend-p3-scope.md) — multiplayer/server = effort P3 riêng, KHÔNG trong offline bar; KHÔNG reverse dhcd server.
- [Wave system parity](issues/05-research-wave-system-parity.md) — wave-type = trigger enum `WaveEventFuncType`(9) + boss flag `MonsterCfg.IsBoss` + swarm dynamic fields; **elite KHÔNG có → own**; lifecycle LevelMonsterMgr→WaveFunc→LevelWave→WaveRefresh→BattleFinsh; DIY hook `InitByDiyLevelWave`.
- [Boss/shop/box/endless parity](issues/06-research-boss-shop-box-endless-parity.md) — 3-mode=`RandomSkillParam.Type`(1/2/3) + per-role Queue + 2 reroll cmd riêng; **boss phase = damage-window keyed** (`BossChangeBehaviorCmpt`+`JiangHuBossPhaseConfig`, KHÔNG timer); endless chỉ wave-loop skeleton.
- [Impact/buff parity](issues/07-research-impactmgr-buff-parity.md) — generic model: impact 4-bucket(Ab/Rel/Mul/Effect) + BuffStateID 20-state control + generic BuffDot(poison/burn) + ActorSM stun; **KHÔNG enum status tên riêng → giữ generic + flavor ở config**.
- [Visual/VFX/audio bridge](issues/08-research-visual-vfx-audio-bridge.md) — Sandbox đã đủ surface (SkillEffectVisualService fail-closed sẵn + AudioService); adapter qua IActorVisual KHÔNG sửa Sandbox; **SPR root = /SpritesRuntime (sửa note)**; BGM/mixer chưa staged → own pipeline.
- [Skill library mapping](issues/04-research-skill-library-mapping.md) — 1.216 skill (10 phai ~452 player pool + npc/partner boss pool) + 441 missile; schema PcSkills->SkillDef chot; **BUG parser LvlSetScriptCol 71->70**; fail-closed runtime; supply=heal/bomb(buff), magnet=own.
- [Save/settings/i18n/pause](issues/09-research-save-load-settings-i18n.md) — save=progress+settings riêng (PlayerPrefs+JsonUtility v1); i18n=SurvivorText VN/EN bundle; pause=**own-design** ref-counted `SurvivorPause` per-scope (parity-shape r-dhcd-003 bounded); OnApplicationPause own.
- [P1 completion bar](issues/25-p1-completion-bar.md) — P1 bar = 6 mục (auto-attack+card-pick=skill progression, KHÔNG active skill); skeleton 0 gap functional; acceptance = EditMode self-check (`SurvivorP1LogicTests`) + manual play-checklist; close-condition 3 gates (test green + checklist tick + console sạch).
- [Visual bridge P1.5](issues/16-visual-bridge-design.md) — player-only `JxPlayerVisual` wrap MalePlayerVisual qua IActorVisual; variant BD_019 + ppu=40 (char 1.9 unit); sentinel probe filename `MA_BD_019_ST01.spr` miss→Proxy; Y-sort defer (MalePlayerVisual own sortingOrder); VFX/audio/monster = P2. Verified 8 parts render, spawning intact.

## Handoff

- 2026-08-02: to-spec xong — [spec.md](spec.md) published `ready-for-agent`. Resolve tickets 10-15/17-24 thành Implementation Decisions (evidence research + own-design defaults); số balance + ramp endless + elite để playtest chốt. Next: to-tickets → implement. AGENTS.md SPR root note đã sửa (`/SpritesRuntime`).
- 2026-08-02: to-tickets xong — tickets 26-42 published `ready-for-agent` (dependency order: 26→27→29/31/33/34/37/40/41/42; frontier = 26, 28, 30, 32, 35, 36, 38, 39).
- 2026-08-04: ticket 43 close — runtime wiring P2 (council FAIL fix) verified + council re-review PASS (pi session). Implementer verify trước đó (265/265) + re-run hiện tại 277/277. Frontier giờ: 29, 31, 33, 34, 37 (42 blocked bởi 34/37; 40/41 thực chất verified 233/233 — header chưa cập nhật, cần close docs).
- 2026-08-04: ticket 29 close — player pool = Cái Bang (`gaibang` LvlSetScript filter, 33 skill, khớp PcCaiBangSkills.txt). 3-mode + queue + reroll đã class-level verified trước; pi session verify final 277/277 + PlayMode (3 card gaibang thật, cast missiles=1). Frontier: 31, 33, 34, 37 (42 blocked 34/37).
- 2026-08-04: ticket 48 mở — 8-direction animation sai (player không gọi SetDirection; monster cần verify mapping). Handoff LEAD, baseline HEAD.
- 2026-08-04: ticket 48 close — 8-way facing fix (commit 63ddd4ac0): player gọi SetDirection qua UpdateFacing (idle-hold + cache), monster cache facing, null-guard Instance. EditMode Survivor 283/283 + PlayMode 2/2 (8 hướng qua bridge + monster chase + screenshots). Full-suite failures Backend/Sandbox proven pre-existing. Frontier: 31, 33, 34, 37.

## Not yet specified

<!-- in-scope fog chưa sharp đủ ticket; graduate khi frontier tới -->

- **Difficulty feel / playtest target**: "parity feel" giữa dhcd loop-shape và own-tuning cân
  bao nhiêu playtest pass — chưa biết đến khi skill library + drop/XP curve có số. Thuộc
  ticket 14 sau research.
- **Endless ramp curve family**: linear/exponential/stair-step chưa chốt — dhcd endless chỉ có
  wave-loop skeleton (`IsReposeWave`+`WaveRefresh` dynamic caps + `GetEndlessWaveCount()`), ramp
  = own. Ticket 23 sẽ chốt khi 06 research đã xong (xong rồi → 23 unblocked).

## Out of scope

<!-- work ruled beyond destination; closed, never graduates -->

- (chưa có) — nếu ticket nào lộ ra nằm ngoài offline bar sẽ close + ghi 1 dòng đây.

