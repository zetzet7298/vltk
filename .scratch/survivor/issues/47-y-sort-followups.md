# 47 — Y-sort followups (dual review 46 findings, 3 mục nhỏ)

**What to build:** Dual review ticket 46 (46a `f458d4fc` + 46b `105ef9eb`, cả 2 PASS) — 3 mục nhỏ
không block, làm cho đúng + chặt hơn. Baseline `8edaa0d22`.

**Blocked by:** None. **Status:** ready-for-review

## Mục 1 — Comment sentinel sai (46b)

`MalePlayerVisual.cs:483` + `SurvivorYSortTests.cs` header: comment ghi "-1 = mặc định PC" nhưng
sentinel thật là `int.MinValue`. Nguy cơ: maintainer tương lai set override=-1 tưởng default →
thật ra active override (base=-1 → clamp band, renderer dưới mọi thứ).
Fix: sửa comment "int.MinValue = mặc định PC" (2 chỗ).

## Mục 2 — SyncDepth nhánh else (46b)

`SurvivorMonster.Update`: SyncDepth chỉ chạy nhánh move (dist>0.001); nhánh else (dist≤0.001,
monster đứng yên) không gọi → monster đứng yên ngay trên player giữ default 32190 vĩnh viễn
(tie Y → thứ tự chấp nhận, nhưng dọn cho đúng). Fix: gọi `_visual?.SyncDepth(transform.position.y)`
cả nhánh else (1 dòng) — hoặc refactor nhỏ gọn: gọi SyncDepth 1 lần sau block move (đọc code
chọn cách sạch nhất, đảm bảo KHÔNG double-call trong cùng frame).

## Mục 3 — Test assertion chặt hơn (46a)

`SurvivorYSortTests` test 6: `Assert.Greater(0, body-(base+40))` chỉ pin offset < 40 (thực tế
14-22 theo direction) — không phát hiện regression nếu offset đổi trong 0..39.
Fix: pin exact `body == base + MalePlayerSpriteCatalog.SortingOffset(Body, direction)` — 1 dòng,
tăng độ chặt (đọc code xác nhận tên hàm/kiểu trước).

## Defer (KHÔNG làm — ghi note vào issue 46 Verified nếu muốn)

- Player part clamp sau offset (base=32767 + offset 22 → 32789 > int16 max): chỉ reachable Y < -13.7,
  arena thực tế ±5.8 → ±232 bậc — theoretical, defer.
- Player shadow tie monster body tại cùng worldY: float tie hiếm, vô hại — defer.
- SyncDepth trước Start transient: monster AI move ngay frame đầu — chấp nhận.

## Acceptance

- [ ] Comment sentinel đúng (grep "-1 = mặc định" không còn trong code mới)
- [ ] Monster idle (dist≤0.001) vẫn SyncDepth — test hoặc code-inspect verify không double-call
- [ ] Test assert pin exact offset — survivor suite xanh (277 + sửa test, không đổi count)
- [ ] KHÔNG regression: suite 277 xanh

## Verified

**Commit:** `158506fc1` (branch dev)

**Files changed (3):**
- `Assets/Scripts/Sandbox/MalePlayerVisual.cs` — comment sentinel: `-1 = mặc định PC` → `int.MinValue = mặc định PC` (MỤC 1, comment-only)
- `Assets/Scripts/Survivor/Actor/SurvivorMonster.cs` — nhánh else (dist≤0.001) gọi `_visual?.SyncDepth(transform.position.y)` (MỤC 2); không double-call: 2 nhánh if/else loại trừ nhau, move branch giữ `p.y`, else branch dùng `transform.position.y` (position không đổi khi đứng yên)
- `Assets/Tests/EditMode/Survivor/SurvivorYSortTests.cs` — comment header sentinel (MỤC 1) + test 6 pin exact: `Assert.AreEqual(ActorDepth.BaseOrder(2f) + MalePlayerSpriteCatalog.SortingOffset(PlayerSpritePartKind.Body, player.direction), playerBodyBehind)` thay `Assert.Greater(0, ...+40)` (MỤC 3; `direction` là public property trên MalePlayerVisual, đọc live tại assert)

**Test output (thật, EditMode qua Unity MCP):**
```
SurvivorYSortTests (group VLTK.Tests.Survivor.SurvivorYSortTests): 6/6 passed (0 failed, 0 skipped)
  - PlayerAndMonster_SharedFormula_OrderFollowsWorldY  PASS (pin exact offset — SPR staging thật)
  - ActorDepth_HigherY_LowerOrder / ClampsToInt16Band / Proxy_* / NpcVisual_*  PASS
Full survivor suite (group VLTK.Tests.Survivor): 277/277 passed (0 failed, 0 skipped), 4.6s
  — count không đổi (277 = 271 cũ + 6 YSort; không thêm/bớt test)
```

**Ghi chú:**
- Grep `-1 = mặc định` trong Assets/Scripts + Assets/Tests: 0 match (acceptance MỤC 1 ✓)
- MỤC 2 verify bằng code-inspect (acceptance cho phép): else branch gọi SyncDepth mỗi frame khi đứng yên, idempotent (cùng Y → cùng order); move branch chỉ chạy khi dist>0.001 → không bao giờ double-call cùng frame
- Defer mục (issue ghi): KHÔNG làm, giữ nguyên như ticket chỉ định
