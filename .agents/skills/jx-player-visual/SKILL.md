---
name: jx-player-visual
description: >-
  Build, fix, or extend the on-screen PLAYER AVATAR (and player-like NPC) of the
  VLTK-mobile Unity client using the original JX Online 1 / Võ Lâm Truyền Kỳ layered
  SPR character system. Use this skill WHENEVER the user works on a player/character
  VISUAL — e.g. "port the female player", "add the woman avatar", "đổi giáp/vũ khí/mũ
  cho nhân vật", "thêm cưỡi ngựa", "nhân vật bị vô hình / vẽ dưới map", "animation sai
  hướng", "thêm action chém/đứng/chạy", "render player nữ", "spawn player vào map",
  or mentions MA_*/WO_* SPR parts, 男主角/女主角贴图顺序表, npcres\man / npcres\woman,
  body/head/hair/hand/weapon layers, 8-way direction, or MalePlayerVisual /
  MalePlayerSpriteCatalog / SandboxPlayerController. This skill encodes the hard-won
  layered-part model, the signed/unsigned hash split, the SPR staging pipeline, and
  the two visibility bugs (sorting ceiling + static-cache-on-replay) already solved —
  reuse it instead of re-deriving.
---

# JX Player Visual (layered SPR avatar)

Render a JX/VLTK player character in the Unity mobile sandbox the way the original PC
client does: **many SPR parts stacked as separate `SpriteRenderer`s** (shadow, body,
head, hair, hands, weapons, horse), animated per-action, ordered per-direction by the
PC draw-order table. Male is the reference implementation; the same model covers the
female avatar and any equipment/mount swap.

## Mental model (read first)

A JX character is NOT one sprite. It is a stack of independent part SPRs that share a
common canvas and reference pixel. Each part SPR holds **all 8 directions** of one
action (e.g. body-idle, body-run). To draw a frame you pick the part's sprite for the
current `direction * framesPerDirection + frame`, place it by its per-frame offset
relative to a shared reference pixel, and set its `sortingOrder` from the
**per-direction draw-order table** so parts overlap correctly (hair behind/in-front of
head depending on facing, weapon behind body when facing away, etc).

Get this model right and male/female/equipment all fall out of the same code — only
the part filenames and variant numbers change.

## What already exists (reuse, don't rebuild)

The player visual system is fully working for both male and female characters. Read these before writing anything new:

| File | Role |
|------|------|
| `Assets/Scripts/Sandbox/MalePlayerSpriteCatalog.cs` | Part enum, part->SPR table (idle/move), 8-way `DirectionFromMove`, per-direction `SortingOffset` from the PC draw-order table. Mặc định trang bị nam gồm "áo vải thô" và "mũ bố cân" (variant `019`). |
| `Assets/Scripts/Sandbox/FemalePlayerSpriteCatalog.cs` | Tương đương cho nhân vật nữ. |
| `Assets/Scripts/Sandbox/MalePlayerVisual.cs` / `FemalePlayerVisual.cs` | Renderer: một `SpriteRenderer` cho mỗi bộ phận (part), giải mã SPR + bộ nhớ đệm cache, áp dụng offset điểm tham chiếu, frame/sort. |
| `Assets/Scripts/Sandbox/SandboxPlayerController.cs` | Điều khiển di chuyển qua Joystick + keyboard; cập nhật trạng thái cưỡi ngựa (mount/dismount) và truyền vectơ di chuyển tới visual component. |
| `Assets/Scripts/Sandbox/SandboxManager.cs` | Tự động sinh (spawn) nhân vật + joystick + camera khi tải bản đồ. |
| `Assets/Scripts/Sprites/SprRuntimeService.cs` | `ComputePathUidHex` (biến thể UNSIGNED) — dùng để đặt tên tệp SPR runtime. |
| `Assets/Scripts/Sprites/SprDecoder.cs` | Giải mã SPR sang Texture2D / frame / offset. Chứa logic lật ngược hàng dọc của SPR gốc. |
| `Assets/StreamingAssets/male_player_sprites.json` | Manifest chứa thông tin ánh xạ tên -> uid -> file đã staged cho nam. |
| `Assets/Tests/EditMode/Sandbox/MalePlayerVisualTests.cs` | Unit test kiểm tra Catalog, hướng, tải bộ phận, và di chuyển cho nam/nữ. |
| `scripts/extract_horse_019.py` | Script Python tự động trích xuất các bộ phận thú cưỡi Siêu Quang (variant `019`) từ các tệp PAK gốc. |
| `scripts/stage_all_sandbox_sprites.py` | Script Python tự động staging tất cả sprite cần thiết cho sandbox bao gồm cả ngựa Siêu Quang. |
| `scripts/verify_runner.py` | Trình chạy test tự động: chạy EditMode tests, load scene, vào PlayMode, chạy kiểm tra runtime (A/B render diff, 8-way move) cho cả nam/nữ rồi xuất báo cáo. |

To add a NEW avatar (female, alt class) the cleanest path is to **generalize the male
classes by gender/variant** rather than copy-paste. See `references/extending.md`.

## Source data (jxwin-kinnox PC client)

Character definitions live in `SourceNew/swrod3/Utility/Run/Settings/NpcRes/` and art in
`.../Run/spr/npcres/`. The master row table is `人物类型.txt` (tab-separated):

- `男主角` (male hero) -> art `spr\npcres\man`, draw-order `男主角贴图顺序表.txt`
- `女主角` (female hero) -> art `spr\npcres\woman`, draw-order `女主角贴图顺序表.txt`

The male and female draw-order tables are **identical for Dir1..Dir8**, so the existing
`SortingOffset` works unchanged for the female avatar.

### SPR filename grammar

`MA_<PART>_<VARIANT>_<ACTION><NN>.spr` (male; female uses its own prefix in `woman/`).

| PART | meaning | part id (draw-order) |
|------|---------|----------------------|
| YY | shadow (影) | -1 |
| HD | head (头) | 0 |
| HR | hair (发) | 1 |
| HT/HB/HH | hat/headgear variants | shoulder/headwear region |
| BD | body/armor (躯体) | 5 |
| LH / RH | left / right hand | 6 / 7 |
| LW / RW | left / right weapon | 8 / 9 |
| (horse) | mount front/mid/rear | 12 / 13 / 14 |

`VARIANT` is the equipment id: body/head `019`, empty-hand weapon `000`, shadow `999`.
Swapping armor/weapon = swap the variant number for that part only.

### Action codes (`<ACTION>` in the filename)

| code | action | frames (male 019) |
|------|--------|-------------------|
| ST | stand / idle | 120 = 15/dir x 8 |
| RN | run / move | 88 = 11/dir x 8 |
| RD | ride (mount) | varies |
| ZZ | special/emote | varies |

Always derive `framesPerDirection = totalFrames / directions` at load time — do not
hard-code 11 or 15; different parts/actions differ.

## The two hashes (do not mix them up)

There are TWO different path-hash functions in this project. Mixing them = 0 matches
or wrong files. Both lowercase ASCII `A-Z` and run the same `value` recurrence; they
differ ONLY in how each path byte is treated:

1. **Pak lookup hash (`g_FileName2Id`, SIGNED byte).** Used to find an entry inside
   `maps.pak` / `spr.pak`. High bytes (Chinese GBK, >=0x80) are treated as signed
   (`b - 256`). This is the `jx-map-port` skill's hash. Use it ONLY to read from paks.
2. **Runtime file-naming hash (`ComputePathUid`, UNSIGNED byte).** Used by
   `SprRuntimeService.ComputePathUidHex` to name staged files `{uid}.spr`. ASCII-only
   player paths (`spr\npcres\man\MA_BD_019_ST01.spr`) contain no high bytes, so signed
   vs unsigned is irrelevant here — but the staging script MUST use the SAME unsigned
   function as the C# runtime so the names line up.

Verified: `spr\npcres\man\MA_BD_019_ST01.spr` -> unsigned uid `45488ea8`, which matches
the manifest and the file the runtime loads. `scripts/uid.py` is the reference impl.

## Staging pipeline (get art into the build)

The runtime reads `Assets/StreamingAssets/Sprites/{uid}.spr`. To stage a part set:

1. Collect the source `.spr` files for the avatar (from `npcres/man` or `npcres/woman`,
   or extracted from `spr.pak` via the `jx-map-port` pak reader if not on disk).
2. For each, compute the UNSIGNED uid (`scripts/uid.py` or `ComputePathUid`).
3. Copy to `Assets/StreamingAssets/Sprites/{uid}.spr`.
4. Append `{name, sourcePath, uid, unityPath, bytes}` to the manifest json.
5. `refresh_unity` so Unity imports the new files.

`scripts/stage_player_spr.py` does steps 2-4 for a folder. Keep `sourcePath` in the
exact backslash form the catalog uses (`spr\npcres\man\MA_BD_019_ST01.spr`) — that
string is what the runtime re-hashes, so any mismatch silently breaks the lookup.

## Critical rendering and visibility bugs solved (don't relive these)

Các lỗi hiển thị và dựng hình dưới đây đã được khắc phục hoàn toàn trong hệ thống. Nếu gặp lại các lỗi tương tự ở các nhân vật mới, hãy đối chiếu ngay với các giải pháp này:

### Bug 1: Nhân vật bị vẽ DƯỚI bản đồ (Drawn UNDER the map)
- **Triệu chứng**: Nhân vật bị các lớp cây cỏ hoặc mặt đất che khuất hoàn toàn ở các khu vực đông đúc.
- **Nguyên nhân**: Lớp bản đồ (`MapRenderer.cs`) giới hạn `sortingOrder` tối đa là **32000**. Thứ tự vẽ theo toạ độ Y thông thường của nhân vật có thể nhỏ hơn mức này.
- **Giải pháp**: Buộc lớp sắp xếp cơ sở của nhân vật phải nằm trên ngưỡng 32000:
  ```csharp
  int screenOrder = Mathf.RoundToInt(-transform.position.y) * 2 + 2;
  return Mathf.Clamp(Mathf.Max(screenOrder, 32200), 32200, 32700);
  ```
  `SortingOffset` của từng bộ phận (0..14) sẽ được cộng thêm vào giá trị này, giúp toàn bộ nhân vật nằm trong khoảng `[32200..32714]`, luôn hiển thị phía trên bản đồ.

### Bug 2: Nhân vật biến mất khi bật lại PlayMode mà không Recompile (Domain Reload disabled)
- **Triệu chứng**: Chạy game lần đầu hiển thị tốt, tắt PlayMode rồi bật lại thì nhân vật bị tàng hình, không có log nạp SPR.
- **Nguyên nhân**: Clip cache được khai báo là `static` để tối ưu hiệu năng. Khi dừng PlayMode, các đối tượng `Sprite`/`Texture2D` của Unity bị huỷ, nhưng từ điển static vẫn tồn tại (khi Domain Reload bị tắt để tăng tốc độ Editor). Lần chạy tiếp theo sẽ bị trúng cache hit chỉ tới các sprite đã bị hủy (`sprite == null`).
- **Giải pháp**: Đăng ký xoá cache tĩnh khi nạp lại hệ thống và kiểm tra tính toàn vẹn của sprite:
  ```csharp
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void ResetStaticCaches() { ClipCache.Clear(); MissingLogCache.Clear(); }
  ```
  Kết hợp với hàm `IsClipAlive` kiểm tra sprite trước khi trả về từ cache.

### Bug 3: Sprite bị lộn ngược và các bộ phận rời rạc (Vertical Inversion & Disjointed Parts)
- **Triệu chứng**: Nhân vật bị hiển thị lật ngược đầu xuống đất. Khi di chuyển hoặc cưỡi ngựa, các bộ phận (đầu, thân, tay, chân) bị dịch chuyển lệch vị trí, rời rạc không thành hình người.
- **Nguyên nhân**: Định dạng SPR của VLTK PC lưu các dòng pixel theo chiều từ trên xuống dưới (top-down), trong khi không gian toạ độ cấu trúc Sprite của Unity lại bắt đầu từ góc dưới bên trái (bottom-left, bottom-up). Việc copy trực tiếp làm lộn ngược ảnh và sai lệch toạ độ điểm neo (pivot) của từng bộ phận.
- **Giải pháp**: Sửa logic đọc và copy pixel trong [SprDecoder.cs](file:///var/www/vltk-mobile/Assets/Scripts/Sprites/SprDecoder.cs) để đảo chiều dòng dọc:
  ```csharp
  int rowBase = (frame.height - 1 - row) * frame.width;
  ```
  Điều này đảo ngược chính xác các pixel theo chiều dọc và căn lề đúng cho tất cả các bộ phận chồng lớp lên nhau.

### Bug 4: Thú cưỡi bị vô hình khi cưỡi ngựa (Invisible Horse Mount)
- **Triệu chứng**: Khi kích hoạt trạng thái cưỡi ngựa (Mount), nhân vật nam/nữ chuyển sang tư thế ngồi cưỡi nhưng con ngựa hoàn toàn vô hình (chỉ có người lơ lửng).
- **Nguyên nhân**: Biến thể thú cưỡi mặc định ban đầu (ngựa vàng `016`) không có tài nguyên SPR thực tế nào trong bất kỳ tệp tin `.pak` nào của client. Hệ thống tìm kiếm tài nguyên thất bại nên không thể vẽ.
- **Giải pháp**:
  - Chuyển thú cưỡi mặc định sang ngựa **Siêu Quang (variant 019)**.
  - Trích xuất đầy đủ 3 bộ phận của thú cưỡi variant `019` từ PAK gốc: đầu ngựa `HH` (nằm trong `updatejx06.pak`), thân ngựa `HB` (nằm trong `1.pak`), đuôi ngựa `HT` (nằm trong `updatejx06.pak`).
  - Sử dụng script `extract_horse_019.py` để tự động hóa việc tìm kiếm và trích xuất này, lưu vào `Assets/StreamingAssets/Sprites/` và đặt tên tệp theo mã băm unsigned hex của đường dẫn.

## Workflow

For a new avatar / equipment swap / new action:

1. **Identify the part set** from `人物类型.txt` + the variant numbers wanted. Note the
   art folder (`man` vs `woman`) and the action codes needed (ST, RN, ...).
2. **Stage the SPRs** (`scripts/stage_player_spr.py`) and refresh Unity.
3. **Wire the catalog** — add the part->SPR rows for each action. Reuse `SortingOffset`
   (draw-order is shared). For female, see `references/extending.md`.
4. **Compile + check console** for the per-part `Loaded ... N frames, 8 dirs` logs.
5. **Verify in Play mode** with the runtime checks below.
6. **Tests**: extend `MalePlayerVisualTests` (or add a parallel test) for the new set.
7. Update `CHANGELOG.md` and the harness story.

## Verification (how to actually prove it works)

Để đảm bảo các sửa đổi hiển thị nhân vật hoạt động chính xác 100% (không bị lộn ngược, không bị vô hình, đúng thứ tự sắp xếp lớp), chúng ta bắt buộc phải kiểm thử tự động.

### Trình chạy kiểm thử tự động (`scripts/verify_runner.py`)
Luôn ưu tiên chạy script này đầu tiên:
```bash
python3 scripts/verify_runner.py
```
Script sẽ tự động thực hiện:
1. Chạy tất cả các unit test EditMode (`VLTK.Tests.Sandbox.MalePlayerVisualTests`, `FemalePlayerVisualTests`).
2. Nạp cảnh `Sandbox.unity`, chuyển sang PlayMode.
3. Chạy mã kiểm tra runtime cho nhân vật nam và nữ.
4. Tắt PlayMode và báo cáo kết quả.

### Chi tiết các bước kiểm tra runtime (trong `scripts/verify_player.cs`):
- **Kiểm tra số lượng bộ phận nạp (Parts loaded)**: Tìm đối tượng người chơi, lấy tất cả `SpriteRenderer` và so khớp số lượng sprite thực tế nạp vào bộ phận (ví dụ: nam cưỡi ngựa Siêu Quang phải load đủ **8/12 bộ phận** - 5 người + 3 ngựa; nữ bình thường phải load **5/5 bộ phận**).
- **So sánh lớp vẽ với bản đồ (Above the map)**: Đảm bảo sortingOrder nhỏ nhất của nhân vật phải lớn hơn mức tối đa của MapRenderer (`32000`).
- **Khả năng hiển thị thực tế (A/B Render Diff)**: Vẽ cảnh ra một `RenderTexture` hai lần — một lần bật hiển thị nhân vật và một lần tắt. Tính toán số pixel khác biệt giữa hai ảnh chụp. Nếu số pixel khác biệt lớn hơn `50 px` (thông thường > 1500 px) thì nhân vật thực sự hiển thị trên màn hình (VISIBLE OK). Nếu bằng 0 thì nhân vật bị ẩn hoặc bị che khuất.
- **Di chuyển 8 hướng (8-way move)**: Giả lập Joystick truyền vào 8 hướng di chuyển khác nhau, kiểm tra xem hướng hiển thị của nhân vật có ánh xạ đúng góc độ (ví dụ: S0, SW1, W2, NW3, N4, NE5, E6, SE7) và hoạt động di chuyển/đứng yên có hoạt động đúng hay không.

## Unity MCP quirks (this project)

- Instance `vltk-mobile@...`, Unity 6000.4.7f1, LinuxEditor, scene `Assets/Scenes/Sandbox.unity`.
- Action tools can deregister during `playmode_transition`. The resource
  `mcpforunity://editor/state` always reads — wait for `ready_for_tools==true` before
  calling action tools. `is_changing` may sit true persistently; if `ready_for_tools`
  is true, `execute_code` still works.
- After editing a script: `stop` play -> `refresh_unity(compile=request, force, scripts)`
  -> poll state -> `read_console` -> `play`. Editing while playing does NOT recompile.
- Fallback runtime evidence: `~/.config/unity3d/Editor.log` (grep `[MalePlayer]`/`[Sandbox]`).

## Pointers

- `references/extending.md` — hướng dẫn tổng quát hoá lớp Male thành Female / Trang bị / Thú cưỡi.
- `references/draw-order.md` — bảng mã vẽ của từng bộ phận theo 8 hướng.
- `scripts/uid.py` — bộ mã băm unsigned hex dùng cho runtime (khớp với C#).
- `scripts/stage_player_spr.py` / `scripts/stage_all_sandbox_sprites.py` — staging tài nguyên SPR của nhân vật và ngựa vào thư mục StreamingAssets.
- `scripts/extract_horse_019.py` — script trích xuất ngựa Siêu Quang từ PAK.
- `scripts/verify_runner.py` — trình chạy tự động toàn bộ test EditMode & PlayMode.
- `scripts/verify_player.cs` — mã nguồn C# thực hiện 4 bước kiểm tra runtime của nhân vật.
