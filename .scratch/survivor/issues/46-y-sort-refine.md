# 46 — Y-sort refine: monster/player PC visual chung depth ordering

**What to build:** `PcNpcVisual` (Sandbox) hardcode `_renderer.sortingOrder = MapRenderer.PlayerSortingOrder - 10`
(MalePlayerVisual = `PlayerSortingOrder` + offset) → monster LUÔN render dưới player bất kể Y.
Side-view XY plane (camera +Z): Y cao = xa camera = phải render TRƯỚC (dưới); Y thấp = gần = trên.
Player đứng sau monster (Y cao hơn) → player vẽ đè monster = sai depth order.

**Blocked by:** None — baseline `eae0ede21` (45) + `65dc4e864` (null-guard). Y-sort defer
comment (JxPlayerVisual.cs:11-12 "monster P1.5 vẫn Proxy → Y-sort refine = P2 khi monster cũng
PC visual") — giờ monster ĐÃ PC visual (ticket 35 JxNpcVisual) → condition met.

**Status:** ready-for-agent

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

- (trống — chờ implementer)
