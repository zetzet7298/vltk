# 46 — Y-sort refine: monster/player PC visual chung depth ordering

**What to build:** `PcNpcVisual` (Sandbox) hardcode `_renderer.sortingOrder = MapRenderer.PlayerSortingOrder - 10`
(MalePlayerVisual = `PlayerSortingOrder` + offset) → monster LUÔN render dưới player bất kể Y.
Side-view XY plane (camera +Z): Y cao = xa camera = phải render TRƯỚC (dưới); Y thấp = gần = trên.
Player đứng sau monster (Y cao hơn) → player vẽ đè monster = sai depth order.

**Blocked by:** None — baseline `eae0ede21` (45) + `65dc4e864` (null-guard). Y-sort defer
comment (JxPlayerVisual.cs:11-12 "monster P1.5 vẫn Proxy → Y-sort refine = P2 khi monster cũng
PC visual") — giờ monster ĐÃ PC visual (ticket 35 JxNpcVisual) → condition met.

**Status:** done — council dual review PASS (46) / followups applied (47)

## Root cause

- `MalePlayerVisual.cs:445,483`: `runtime.renderer.sortingOrder = baseOrder + SortingOffset(...)` với
  `baseOrder` từ đâu? Tra: player visual lấy base từ MapRenderer.PlayerSortingOrder (32200) — cố định,
  KHÔNG theo Y.
- `PcNpcVisual.cs:175,267`: `MapRenderer.PlayerSortingOrder - 10` — cố định dưới player, KHÔNG theo Y.
- → 2 actor cùng sorting band cố định, thứ tự depth KHÔNG phản ánh Y (world z-depth của side-view).

## Fix (chọn nhỏ nhất đúng lifecycle — đọc code thật trước)

Mục tiêu: sortingOrder động theo world Y mỗi frame (hoặc khi SyncPosition), player + monster chung
1 công thức.

1. **Survivor actor layer (Survivor namespace) — KHÔNG sửa Sandbox nếu tránh được** (Sandbox = read-only
   shared lib, JX bridge = adapter pattern):
   - IActorVisual thêm `SyncDepth(float worldY)` (hoặc gộp vào SyncPosition — check interface hiện có).
   - `JxPlayerVisual`/`JxNpcVisual` bridge forward: `baseOrder = PlayerSortingOrder + offsetY(worldY)`
     (offsetY = round(-worldY * k), k = px/unit scale — đọc code xem MalePlayerVisual baseOrder lấy đâu,
     tái dùng hằng số cho nhất quán).
   - ProxyActorVisual cũng nhận SyncDepth (proxy dùng chung ordering — fail-closed phải đúng thứ tự).
   - SurvivorMonster/SurvivorPlayer Update gọi SyncDepth(position.y) sau SyncPosition.
   - Boss/elite/monster/proxy cùng công thức — Y quyết định, tier không override thứ tự.

2. **Giới hạn:** sortingOrder int16 (-32768..32767) — player 32200 + offset phải trong band; arena
   ground/cover dưới. Nếu k quá lớn → clamp. MapRenderer đã có comment "sortingOrder is a 16-bit field".

3. **Shadow:** PcNpcVisual shadow `PlayerSortingOrder - 20` — shadow phải luôn dưới actor tương ứng,
   kể cả khi actor Y cao. Kiểm tra shadow có theo renderer chung không (shadow = renderer - 10 → nếu
   renderer động, shadow cũng động nếu cùng base).

## Acceptance

- [ ] EditMode: test Y-sort — actor Y cao → sortingOrder thấp hơn actor Y thấp (player + monster
      cùng công thức, proxy cũng vậy)
- [ ] EditMode: monster đứng TRƯỚC player (Y thấp hơn) → sortingOrder monster > player (monster đè
      player); monster đứng sau → < player
- [ ] EditMode: shadow luôn dưới actor chủ (mọi Y), int16 không overflow ở Y cực trị (clamp test)
- [ ] EditMode survivor suite xanh (271 + test mới)
- [ ] KHÔNG regression: visual JX vẫn render (stand/walk), proxy fail-closed vẫn đúng thứ tự

## Verified

**Commit:** `8edaa0d22` (branch dev)

**Files changed (15):**
- `Assets/Scripts/Survivor/Actor/ActorDepth.cs` (+.meta) — NEW: công thức chung `BaseOrder(worldY)` = `PlayerSortingOrder - worldY*40` (ppu bridge), clamp int16 (-32768..32767)
- `Assets/Scripts/Survivor/Actor/IActorVisual.cs` — thêm `SyncDepth(float worldY)`
- `Assets/Scripts/Survivor/Actor/JxPlayerVisual.cs` — MaleBridge.SyncDepth: set `sortingBaseOverride = ActorDepth.BaseOrder(worldY)` (MalePlayerVisual tự re-sort qua ApplyFrame mỗi Tick, playAutomatically=true)
- `Assets/Scripts/Survivor/Actor/JxNpcVisual.cs` — NpcBridge.SyncDepth: set override + `ApplySortingBase()` ngay
- `Assets/Scripts/Survivor/Actor/ProxyActorVisual.cs` — SyncDepth fail-closed (chưa có renderer → no-op, không crash)
- `Assets/Scripts/Survivor/Actor/SurvivorPlayer.cs` — Init + Update gọi SyncDepth(p.y) sau SyncPosition
- `Assets/Scripts/Survivor/Actor/SurvivorMonster.cs` — Init + Update (move branch) gọi SyncDepth(p.y)
- `Assets/Scripts/Sandbox/MalePlayerVisual.cs` — hook `sortingBaseOverride` (sentinel `int.MinValue` = PC default, không đổi behavior); `PlayerBaseSortingOrder()` đọc override — **Sandbox edit bắt buộc**: ApplyFrame ghi sortingOrder hardcode mỗi frame → adapter không thể thắng write per-frame từ ngoài; hook 2 dòng, sentinel giữ nguyên mọi behavior cũ
- `Assets/Scripts/Sandbox/PcNpcVisual.cs` — hook `sortingBaseOverride` + `ActorSortingBase`/`ShadowSortingBase` (shadow = base - 10, clamp int16) + `public ApplySortingBase()`; 4 điểm gán sortingOrder cũ thay bằng base động
- `Assets/Tests/EditMode/Survivor/SurvivorYSortTests.cs` (+.meta) — NEW: 6 test Y-sort
- `Assets/Tests/EditMode/Survivor/SurvivorP1LogicTests.cs`, `SurvivorSkillCastTests.cs`, `SurvivorSupplyTests.cs` — stub `SyncDepth` cho StubVisual (interface mở rộng)

**Test output (thật, EditMode):**
```
SurvivorYSortTests: 6/6 passed (1.1s)
  - ActorDepth_HigherY_LowerOrder        (monotonic, Y=0 → PlayerSortingOrder)
  - ActorDepth_ClampsToInt16Band         (±10000Y → -32768/32767, không overflow)
  - Proxy_SyncDepth_OrdersByY            (runtime: 32000/32200/32400 theo Y, cùng công thức)
  - Proxy_SyncDepth_BeforeStart_FailClosed (no renderer → no-op, không crash)
  - NpcVisual_SyncDepth_OrdersByY_ShadowBelowActor (default 32190/32180 giữ nguyên PC;
    override → base động; shadow = base-10; clamp: override=-32768 → shadow=-32768 không tràn)
  - PlayerAndMonster_SharedFormula_OrderFollowsWorldY (player Y=2 vs monster Y=-1:
    monster > player body; đảo → monster < player)
Full survivor suite (VLTK.Tests.Survivor): 277/277 passed (0 failed, 0 skipped)
  = 271 cũ + 6 mới — tất cả xanh (testcases trong TestResults.xml: 277, 0 fail)
Full assembly EditMode: 4951/4978 passed, 26 failed — 26 fail = pre-existing Backend.* (10) +
  Sandbox.* (16) đúng như note issue 45 (data/config tests: PcSkills parsing, package bytes,
  sect catalog, benchmark — không đụng file diff ticket 46); 0 fail ở Survivor namespace
```

**Ghi chú:**
- Sentinel `int.MinValue` (KHÔNG phải -1): BaseOrder clamp có thể trả đúng -32768 → sentinel phải nằm ngoài band int16 (bug phát hiện trong test clamp, đã fix trước khi merge).
- Y-sort test cho MalePlayerVisual dùng real SPR staging (MalePlayerSprStaging) — xác nhận không regression render stand/walk: Sandbox `MalePlayerVisualTests` 0 fail trong run.
- Arena ±5.8 → offset ±232 bậc, không chạm clamp khi chơi; clamp chỉ chống overflow ở Y cực trị.
- KHÔNG chạm: XpGem/Projectile (ngoài scope acceptance), arena ground (-10 giữ nguyên), tier không override thứ tự (boss/elite dùng chung formula qua SurvivorMonster).
- Sandbox edits ghi rõ lý do ở trên (per-frame hardcode write — adapter pattern bất khả thi nếu không có hook); hook additive, sentinel default = hành vi PC y nguyên.
