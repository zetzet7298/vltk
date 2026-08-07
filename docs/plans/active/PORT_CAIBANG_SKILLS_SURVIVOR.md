# Plan — Port 4 skill chủ động Cái Bang → Survivor runtime

> Scope: port 4 skill active Cái Bang vào survivor runtime có sẵn. KHÔNG phải
> build từ đầu — 90% infra đã tồn tại (catalog parser, SkillDef, SkillCastRuntime,
> SkillChoiceService, SkillCastSpawner, PcCaiBangLuaLevelService). Plan này chỉ
> thêm 4 tầng thiếu: time-tiered unlock, run-start bootstrap, star-level UI,
> VFX asset audit cho 4 skill.
>
> Không theo harness — đây là artifact reference cho Lead/Peer implement.

## 1. Outcome (user request)

| # | Skill VI (user) | SkillId | Lua key (gaibang.lua) | Note |
|---|---|---|---|---|
| 1 | Kháng Long Hữu Hối | **128** | `kanglong_youhui` | Palm path L50, ranged |
| 2 | Bổng Đả Ác Cẩu | **125** | `bangda_egou` | Staff path L50, chains→359/1074 |
| 3 | Thời Thừa Lục Long | **1073** | `zhanggaibang150` | Palm 150 (user "Thừa" = PC "Thặng") |
| 4 | Bổng Huýnh Lược Địa | **1074** | `gungaibang150` | Staff 150 (PC vi-name "Bổng Hoành Lược Mã"; user "Huýnh Lược Địa" = dịch/lỗi gõ biến thể) |

**Yêu cầu user:**
1. Port 4 skill active trên (KHÔNG port passive/support).
2. Card pick UI phải có **icon SPR đúng skill** (không proxy nếu đã staged).
3. Card xuất hiện **tỉ lệ thuận thời gian run** — run càng dài, skill sau (theo thứ tự 1→4) mới mở.
4. **Mặc định run start**: player chọn 1 trong 2 card đầu (128, 125).
5. **Cấp độ sao** từng skill = cấp độ skill trong jx-source (level/MaxLevel).
6. Logic/behavior tham chiếu dhcd (`LevelRandomSkillCtrl` parity đã có sẵn).
7. **VFX phải giống sandbox** — precast SPR + child missile SPR y hệt Sandbox `SkillEffectVisualService`.

## 2. Hiện trạng ( evidence — KHÔNG rebuild )

Đã verify tồn tại, compile sạch, dùng được:

| Yêu cầu user | Hiện trạng | File |
|---|---|---|
| Parse PcSkills.txt + display + missles → SkillDef | ✅ runtime loader, filter `faction="gaibang"` | `Survivor/Skill/SurvivorSkillCatalogService.cs`, `SurvivorSkillParser.cs` |
| Level tuning per-skill từ PC lua | ✅ full SKILLS dict parser, đủ 4 skillId trên | `Sandbox/PcCaiBangLuaLevelService.cs` (table gaibang.lua) |
| Cast runtime Form 7 (ranged fan) + Form 12 (melee), fan spread parity PC KSkills.cpp | ✅ | `Survivor/Skill/SkillCastRuntime.cs` |
| Scene glue: precast SPR + projectile SPR + melee flash, fail-closed proxy | ✅ | `SkillCastSpawner` cùng file |
| Card modal 3-mode (levelup/box/shop) + weight pool + queue + reroll + pause scope | ✅ parity dhcd r-dhcd-002/003 | `Survivor/UI/SkillChoiceService.cs` |
| Card UI render icon SPR fail-closed | ✅ `MakeChoiceButton` resolve uid → SpriteLoader | `Survivor/UI/OverlayPanel.cs` |
| Cái Bang lua đã staged | ✅ `Assets/StreamingAssets/Reference/gaibang.lua` | (verified grep 4 keys) |

**Kết luận:** không cần viết catalog/parser/runtime/spawner mới. Chỉ cần:
- (A) Pool scope = đúng 4 skillId trên (filter 4 id, không phải cả phái).
- (B) Depend-based unlock (parity dhcd `RandomSkillDependEntry`).
- (C) Run-start bootstrap event = 2 card đầu.
- (D) Star-level display = `roster.GetLevel(id) / def.MaxLevel`.
- (E) VFX audit 4 skill (precast + missile SPR staged).

## 3. Gaps cần build

### Gap A — Pool scoped 4 skill (filter id, không filter cả phái)

`SurvivorSkillCatalogService.Defs(catalog, Player, "gaibang")` hiện trả về toàn bộ
skill Cái Bang (passive + active + NPC variant). Cần whitelist 4 id:

```csharp
// Survivor/Skill/CaiBangActiveSkillSet.cs (NEW, ~15 dòng)
public static readonly int[] ActiveSkillIds = { 128, 125, 1073, 1074 };
public static bool IsActive(int id) => Array.IndexOf(ActiveSkillIds, id) >= 0;
```

`SurvivorGameDirector` build pool: load catalog → Defs(Player,"gaibang") →
filter `CaiBangActiveSkillSet.IsActive` → add vào `SkillChoicePool` với weight
theo tier (xem Gap B).

### Gap B — Depend-based unlock (parity dhcd `RandomSkillDependEntry`)

User chốt Q2 = bắt chước dhcd. dhcd KHÔNG dùng time-seconds — dùng skill-to-
skill depend chain (`RandomSkillLibraryConfig.DependSkills`). Run càng dài ↔
player/roster level càng cao ↔ depend đáp ứng ↔ skill sau mở.

```csharp
// Survivor/Skill/SurvivorSkillDependEntry.cs (NEW, parity RandomSkillDependEntry)
public readonly struct SurvivorSkillDependEntry {
    public readonly int Id;      // prereq skillId
    public readonly int Lv;      // prereq level cần có
    public readonly bool Remove; // true = bỏ khỏi pool khi điều kiện thỏa
    public SurvivorSkillDependEntry(int id, int lv, bool remove) { ... }
}

// Survivor/Skill/SurvivorSkillLibraryConfig.cs (NEW, parity RandomSkillLibraryConfig)
public sealed class SurvivorSkillLibraryConfig {
    public SkillDef Def;
    public List<SurvivorSkillDependEntry> DependSkills; // null/empty = luôn sẵn
    public bool IsDependMet(SkillCastRuntime roster) { ... }
}
```

`SkillChoicePool` thay `List<SkillDef> Entries` →
`List<SurvivorSkillLibraryConfig>` (wrapper). `Draw` cand-filter:
skip nếu `!IsDependMet(roster)` (lookup `roster.GetLevel(prereq.Id) >=
prereq.Lv`, respecting `Remove` byte).

Config (1 SO `CaiBangSkillLibrary_SO` hoặc inline trong director):

| skillId | DependSkills | Ý nghĩa |
|---|---|---|
| 128 | [] | luôn sẵn (tier 1) |
| 125 | [] | luôn sẵn (tier 1) |
| 1073 | [{128, 5, false}] | mở khi Kháng Long ≥ Lv5 |
| 1074 | [{125, 5, false}] | mở khi Bổng Đả Ác Cẩu ≥ Lv5 |

(Lv threshold 5 = own-tuning, SO field, adjust không code change — ponytail.)

**Hook:** `SurvivorGameDirector` build pool từ catalog filter whitelist + gán
DependSkills từ SO. `SkillChoiceService.DrawCards` cand-filter đọc depend —
KHÔNG cần Tick time, không cần biến elapsed.

### Gap C — Run-start bootstrap (BẮT PICK 1-of-2 card đầu)

User chốt Q3 = bắt pick. `SkillChoiceService.Request(LevelUp)` draw 3 card
ngẫu nhiên — không đúng. Cần path riêng ép modal 2 card (128, 125), KHÔNG
skip, KHÔNG reroll. Pause timescale=0 tới click.

Design: thêm method tách rời không đụng parity draw path (dhcd
`FirstLevelRandomSkillWeight` = weight boost; own-extension: hard modal).

```csharp
// SkillChoiceService (EDIT, +1 method)
public bool TriggerBootstrap(ulong roleId, int[] skillIds) {
    var d = GetOrCreate(roleId);
    if (IsWaiting(d)) return false;
    var cards = new SkillChoiceCard[skillIds.Length];
    for (int i=0;i<skillIds.Length;i++) {
        var def = _pool.FindById(skillIds[i]);  // +1 helper trên pool
        if (def == null) return false;          // fail-closed
        cards[i] = new SkillChoiceCard(def, 0);
    }
    d.Current = new SkillChoiceEvent {
        RoleId = roleId, Mode = SkillChoiceMode.LevelUp,
        Cards = cards, RerollsLeft = 0,   // bootstrap: KHÔNG reroll
    };
    d.BeginWaitingLearnTime = _now + WaitingLearnWindow;
    Pause.Acquire(SurvivorPause.CardChoiceScope);  // timescale=0 tới pick
    return true;
}
```

`SurvivorGameDirector.OnGameStart` (sau spawn player):
`_skillChoice.TriggerBootstrap(playerRoleId, new[]{128,125})`.

**Bắt pick enforcement:** `OverlayPanel.ShowSkillChoice` KHÔNG render nút close,
click card mới release. Nếu modal timeout (WaitingLearnWindow) → re-trigger
cùng event (KHÔNG auto-close, KHÔNG auto-learn). Có thể cần +1 flag
`SkillChoiceEvent.IsBootstrap` → `Close()` từ chối nếu true (chỉ pick mới đóng).

### Gap D — Star level = jx skill level

`SkillDef.MaxLevel` đã có (từ PcSkills.txt). `SkillCastRuntime.GetLevel(id)`
đã có. Star = `Mathf.CeilToInt(level * N_STARS / MaxLevel)`, N_STARS=5
(config 1 const `SurvivorSkillTuning.StarLevels = 5`).

UI render: `OverlayPanel.MakeChoiceButton` thêm block star row dưới title:

```
[icon]  Kháng Long Hữu Hối       ★★★☆☆
        Lv 12/20                  (+1 sao khi pick)
```

`SkillChoiceCard` +1 field `int Stars` + `int Level` + `int MaxLevel`.
Compute lúc draw trong `SkillChoiceService.DrawCards` từ roster+def.

### Gap E — VFX audit 4 skill (fail-closed per AGENTS.md)

MỖI skill cần 2 SPR staged trong `/SpritesRuntime/{hash}.spr`:
- `PreCastSprUid` (skill precast animation)
- `ChildMissile.AnimFileUid` (missile fly animation)

**Quy trình (per AGENTS.md SPR parity):**
1. Lấy logical path từ PcSkills.txt (col PreCastPath) + missles.txt (missile
   sprite path) cho 4 skillId. **Dùng vltktool** — không tự decode GBK.
2. Resolve logical→UID: `vltktool resolve_uid.py <gbk-bytes>` (thử signed +
   unsigned GB2312).
3. Extract winner frame: `vltktool extract_item_spr.py --uid <uid>
   --winner-only` (chọn winner theo `bin/client/package.ini` priority).
4. `cmp` PNG extract ↔ chưa có → stage vào `SpritesRuntime/{uid}.spr`.
5. Gán uid vào SkillDef (parser tự làm nếu staged), lưu provenance
   (`<skill>.provenance.json`: uid + package + frame + SHA-256).

**Fail-closed:** SPR chưa staged → SkillDef.PreCastSprUid="" → spawner +
card UI hiện proxy màu xanh (đã wired). KHÔNG bịa path, KHÔNG fallback
tiếng Trung.

**Checklist audit (fill khi implement):**
- [x] 128 kanglong_youhui — precast `b91ab706` (skills.pak, sha16 2da899f1, MATCH) + missile `a31b9f04` (skills.pak entry corrupt/decode-fail, staged 80-frame decodes OK — giữ staging, provenance ghi rõ)
- [x] 125 bangda_egou — precast `3cae8f47` (skills.pak, MATCH) + missile `04e27976` (skills.pak, MATCH)
- [x] 1073 zhanggaibang150 — precast `70d46004` (updatejx09, MATCH) + missile `377228dc` (updatejx09, MATCH)
- [x] 1074 gungaibang150 — precast `3cae8f47` (skills.pak, MATCH — dùng chung 施魔法 với 125) + missile `e46d8c0d` (updatejx09, MATCH)

**Audit kết quả (2025-08-07):** 8/8 uid staged trong `/SpritesRuntime/`, 7/8 khớp bytes winner pak, 1 exception có provenance. Provenance: `docs/plans/active/caibang-skill-spr-provenance/<skill>_<kind>.provenance.json` (8 file, sha256 đủ). Parser probe signed→unsigned (JxPathHash) — tất cả uid signed match, không cần fallback.

**Visual verify (sandbox play mode, Screenshot `Assets/Screenshots/cb_skill*.png`):** 128 precast = flame wave orange (b91ab706 ✓); 125 precast = glow mờ quanh ngựa (3cae8f47 ✓); 1073 precast = golden aura ground + particles (70d46004 ✓); 1074 precast = 3cae8f47 chung với 125 ✓. Missile keys resolve đúng qua PcMissileFullVisualParser (128→a31b9f04, 125→04e27976 mag_gb_04, 1073→377228dc, 1074→e46d8c0d — verified runtime `pcMissileSpriteKey`).

VFX parity verify song song: mở Sandbox scene, cast 4 skill, chụp screenshot.
Cast trong Survivor scene, chụp screenshot. `zai_vision_ui_diff_check` so 2
ảnh — phải gần giống (scale/position có thể khác do arena).

## 4. Phase triển khai

### Phase 1 — Pool scoped 4 skill + depend unlock (parity dhcd)
- Tạo `CaiBangActiveSkillSet.cs` (whitelist 4 id).
- Tạo `SurvivorSkillDependEntry.cs` + `SurvivorSkillLibraryConfig.cs`
  (parity dhcd structs, pure logic).
- `SkillChoicePool` thay `List<SkillDef>` → `List<SurvivorSkillLibraryConfig>`;
  `Draw` cand-filter skip khi `!IsDependMet(roster)`.
- `SurvivorGameDirector` wire: pool build từ catalog filter whitelist + gán
  DependSkills (128/125=[], 1073=[{128,5,false}], 1074=[{125,5,false}]).

**Validate:** EditMode self-check depend logic; PlayMode probe pool.Draw khi
roster 128 lv5 → 1073 vào cand; roster 125 lv5 → 1074 vào cand; chưa đạt
→ 2 card 128/125.

### Phase 2 — Run-start bootstrap
- `SkillChoiceService.TriggerBootstrap(roleId, int[])` (+test).
- `SkillChoicePool.FindById(int)` helper.
- `SurvivorGameDirector.OnGameStart` → TriggerBootstrap(playerRoleId, {128,125}).

**Validate:** PlayMode — player spawn → modal 2 card (128/125) hiện ngay,
pick 1 → modal đóng, game chạy. KHÔNG có card 1073/1074 ở t=0.

### Phase 3 — Star level UI
- `SurvivorSkillTuning.cs` ScriptableObject (StarLevels=5 default).
- `SkillChoiceCard` +Stars/Level/MaxLevel fields.
- `OverlayPanel.MakeChoiceButton` +star row (text ★/☆ hoặc 5 icon sprite
  nhỏ — ponytail: text đủ, star icon sprite add khi art ready).
- `SkillChoiceService.DrawCards` compute stars từ roster+def.

**Validate:** PlayMode — card Lv0 hiển thị 0★; pick 1 → card lại hiện 1★;
pick tới MaxLevel → 5★ + card biến mất khỏi pool (MaxLevel filter đã có).

### Phase 4 — VFX audit + stage
- Chạy vltktool resolve+extract cho 4 skill (8 SPR: 4 precast + 4 missile).
- Stage vào `SpritesRuntime/`, verify `SurvivorSkillCatalogService.LoadStagedUids`
  nhặt đủ.
- Ghi provenance 4 file JSON.
- Visual diff Sandbox vs Survivor (screenshot cmp).

**Validate:** PlayMode — cast mỗi skill, precast + missile SPR hiện (không
proxy). Screenshot cmp vs Sandbox pass.

## 5. Open questions — ĐÃ CHỐT (owner)

1. ✅ **Skill #4** = 1074 `gungaibang150`. User confirm.
2. ✅ **Tier timing**: bắt chước dhcd — `RandomSkillDependEntry`
   (skill-to-skill depend chain), KHÔNG time-seconds. Run dài ↔ level cao ↔
   depend thỏa ↔ skill sau mở.
3. ✅ **Bootstrap**: BẮT PICK — không skip, không reroll, timescale=0 tới click.
4. ✅ **Pick 1/2 → skill kia vẫn vào pool**: weight bình thường, pick lại được.
5. ✅ **Star**: 5★, parity dhcd `RandomSkillLibraryConfig.Level`.
   `star = ceil(GetLevel(id)*5/MaxLevel)`, UI ★/☆.

## 5b. dhcd parity reference (evidence đã verify)

Cơ chế dhcd (BattleCore, diffable-cs đã recover):
- `RandomSkillConfig`: `LevelUpRandomWeight`, `FirstLevelRandomSkillWeight`
  (bootstrap weight dhcd), `CanRepeatSelect`, `IsDependHandbook`.
- `RandomSkillLibraryConfig`: `Level` (skill lv hiện tại), `IsMaxLevel` byte,
  `DependSkills: RandomSkillDependEntry[]`, `FuncType`, `ClasifyType`,
  `IsPetUse`, `SkillID`, `RewardID`, `BuffID`, `IsSuperWeapon`, `EffectID`.
- `RandomSkillDependEntry`: `ID` (prereq skillId), `Lv` (prereq lv),
  `IsRemove` byte.
- `LevelRandomSkillCtrl.RandomLibraryListToParam(roleId, listChachs, libList,
  param, randomCnt, GetWeight, OnlyChooseOnce, SuperlibList)` — cand-filter
  theo DependSkills trước draw.

Port:
- `SurvivorSkillLibraryConfig` (NEW, mirror struct): Def, DependSkills[].
- `SkillChoicePool.Draw` cand-filter skip khi depend không thỏa.
- Bootstrap = dhcd `FirstLevelRandomSkillWeight` lifted thành HARD modal
  (own-extension; tách `TriggerBootstrap` path riêng, không sửa parity draw).

## 6. Non-goal (KHÔNG làm trong scope này)

- Port passive/support Cái Bang (Hóa Hiểm Vi Di, Hoạt Bất Lưu Thủ, Túy Điệp
  Cuồng Vũ, Tiêu Dao Công) — user chỉ list 4 active.
- Port phái khác (Thiếu Lâm, Võ Đang, …).
- Backend P3 (cloud save/multiplayer) — P3 mới đụng.
- Boss/shop/box mode UI — scope này chỉ levelup + bootstrap.
- Sandbox code sửa — KHÔNG đụng file Sandbox, chỉ đọc.

## 7. Risk

| Risk | Mitigation |
|---|---|
| SPR chưa staged → VFX proxy, không giống sandbox | Fail-closed per AGENTS.md; Phase 4 audit bắt buộc trước claim done |
| 4th skill id sai → port nhầm skill | Đã chốt Q1 = 1074 gungaibang150 |
| Tier timing cân bằng kém → card hiếm quá/đầy quá | Config trong SO, tuning sau playtest (note header) |
| dhcd parity bị break bởi depend/bstrap | Depend + bootstrap tách module riêng, không sửa parity draw path có sẵn (TriggerBootstrap path độc lập) |
| Unit-space double-scale (Phase 5 VFX homing): Survivor world units (÷40) lẫn với PC px service (ppu=1) → missile 40× ngắn | FIXED 1064e8391 → REVISE: SurvivorSkillFx.Cast ×PxPerUnit (40) trước PlaySkillCast, NormalizeToWorldUnits ÷40 sau; SkillCastSpawner homing lambda + targetPos trả WORLD units (bỏ k=1/40); targetPos=monster world pos vì Cái Bang MoveKind=1 (Line) → service arrival/impact dùng targetPos cố định (ResolveMissileTarget chỉ homing MoveKind=5). Probe: missile tới monster (3,2), impact rendPositions=[(3,2)] |
| Star display IconSprite chưa có art | Text ★/☆ fallback; art add sau không break logic |

## 8. Validation tổng (claim done)

- [ ] EditMode: `SkillTierUnlock.demo()` pass + pool draw test.
- [ ] PlayMode: bootstrap 2-card hiện đúng 128/125, pick 1 → game chạy.
- [ ] PlayMode: roster 128 đạt Lv5 → card 1073 xuất hiện; roster 125 đạt Lv5 → card 1074 xuất hiện.
- [ ] PlayMode: cast 4 skill, precast+missile SPR đúng (cmp screenshot Sandbox).
- [ ] PlayMode: card star hiển thị đúng level/MaxLevel × 5.
- [ ] Provenance 4 skill JSON ghi uid+SHA-256.
- [ ] Compile sạch, không warning mới.
