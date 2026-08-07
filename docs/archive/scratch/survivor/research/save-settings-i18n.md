# 09 — Research: save/load + settings + localization + pause/resume

Status: `done` (research/save-settings-i18n)
Scope: offline single-player bar. Parity = structure/lifecycle-shape (map Decision 01),
KHÔNG numeric. Pause scope = **own-design** (r-dhcd-003 BOUNDED).

---

## 1. Save model

### 1.1 Hai lớp dữ liệu riêng biệt

| Lớp | Nội dung | Persistence | Ghi chú |
|---|---|---|---|
| **Progress (meta, cross-run)** | unlocked stages, best score/floor, tổng kills, run count, meta-upgrade currency/levels (nếu có) | bền vững, giữ qua restart app | bằng lòng shippable bar (P2 "Meta progression (offline save)" — `docs/SURVIVOR_PLAN.md` L88) |
| **Run-state (mid-run)** | loadout skill đã chọn, level, XP, thời gian run, wave index, vị trí | KHÔNG bắt buộc; nếu có → chỉ "resume last run" 1 slot | roguelike run ngắn (15–30 phút); resume sau app-kill là nice-to-have, không phải bar |

**Khuyến nghị:** progress bắt buộc; run-state **P2+, defer** (ponytail: chưa có meta-upgrade hệ
thống nào được chốt trong map — ticket 09 chỉ hỏi; save run-state chỉ khi "resume run" thành
requirement rõ). Unit dữ liệu:

```
SurvivorProgressData {
  version: int              // schema version, migrate khi thêm field
  bestFloor: int
  bestScore: long
  totalKills: long
  runCount: int
  unlockedStageIds: List<int>   // nếu stage unlock là feature
  metaUpgrades: Dictionary<string,int>  // khi P2 chốt meta-upgrade cụ thể
  settings: {...}            // hoặc tách riêng key (xem §3)
}
```

### 1.2 Structure-parity (PC/dhcd)

- dhcd lưu client data theo **string-key → JSON body**, không phải file game-save:
  `C:/Projects/dhcd/reconstructed-types/GameLogic/A5Game.BaseClientData.cs` — `PlayerPrefs.GetString(key)` / `PlayerPrefs.SetString(key, ...)` + `PlayerPrefs.Save()`; JSON qua custom `DJson.Serialize/Deserialize` (không port được, xem §2). Key = config name (vd `A5Game.CommFlagSaveData.cs` L15 có `Select_Language_Type` → language cũng nằm trong save-data class này).
- Sandbox đã có slot-shape: `Assets/Scripts/Sandbox/PcSaveSlotService.cs` — `SaveSlotData` (metadata: slotId, playerName, level, mapId, playTimeSec, saveTimeUnix, faction, gold, isAutoSave) + `PlayerSnapshot` (thêm learnedSkillIds, inventoryItemIds, serializedState) + auto-save slot (-1) + `SaveSlotListWrapper` serialize `JsonUtility.ToJson`. **In-memory only, chưa có disk write** (không có `persistentDataPath` trong toàn project — verified grep).
- dhcd `ClientSaveData` = `Singleton` + `Dictionary<string, BaseClientData>` đăng ký theo tên → mỗi hệ thống tự có 1 `*SaveData` class (`ActivitySaveData`, `ActorSaveData`, `BanSkillSaveData`, `ClientSaveData`...).

**Khuyến nghị survivor:** pattern dhcd (một wrapper class save-data, string key, JSON body) +
tách progress/settings/run thành 3 key riêng. KHÔNG copy `PcSaveSlotService` (5-slot RPG-shaped,
survivor không cần slot — 1 progress + 1 settings + optional 1 resume). Reuse chỉ ở dạng shape.

---

## 2. Format comparison

| Format | Dùng khi | Serialization | Verdict |
|---|---|---|---|
| **PlayerPrefs + JSON string** | settings nhỏ + progress đơn giản; **parity dhcd** (`BaseClientData.Save/Load`) | `JsonUtility` (Sandbox precedent: `PcSaveSlotService.cs` L151) hoặc Newtonsoft | ✅ chính cho settings + progress v1. Không atomic-write (corrupt nếu crash giữa lúc set) nhưng chấp nhận được cho offline roguelike. |
| **JSON file** (`Application.persistentDataPath/survivor.json`) | save lớn hơn / muốn atomic write (write-temp-then-rename), backup, debug đọc được | Newtonsoft (`com.unity.nuget.newtonsoft-json` 3.2.2 ĐÃ có trong `Packages/manifest.json`) | ✅ khi run-state resume hoặc meta-upgrade phình lên. |
| **ScriptableObject** | KHÔNG cho save (asset = immutable, không phải persistence runtime) | — | ❌ chỉ dùng làm config/thư viện (map Decision 02 đã chốt self-author SO/config cho skill/wave/drop). |
| **SQLite** | cần query/transaction/nhiều bảng — offline roguelike KHÔNG có nhu cầu này | thêm dep | ❌ overkill. |

- `com.unity.localization` 1.5.12 và `newtonsoft-json` 3.2.2 đều **đã cài** (`Packages/manifest.json`).
- dhcd `DJson` = custom serializer, IL recovery garbage → **KHÔNG port**; JSON schema tự định nghĩa.
- Lazy: v1 = PlayerPrefs + JsonUtility cho cả progress+settings (0 dependency mới, Sandbox-precedent).
  Nâng lên JSON file + Newtonsoft khi resume-run vào P2.

---

## 3. Settings categories + persistence

### 3.1 Audio — REUSE Sandbox (đã có sẵn category volume)

`Assets/Scripts/Sandbox/AudioService.cs`:
- `AudioCategory` enum + `_categoryVolume` dict (BGM 0.6, SFX 0.8, Combat...), `SetCategoryVolume(cat, v)` clamp01, `GetCategoryVolume`.
- Music/SFX def có volume riêng nhân với category volume (L142, L176).
- **Chưa có persistence** (không PlayerPrefs trong AudioService).

Parity dhcd: `AudioMixMgr` (`reconstructed-types/GameLogic/A5Game.AudioMixMgr.cs`) dùng `UnityEngine.Audio.AudioMixer` + `AudioMixerGroup[]` — dhcd quản mixer groups, volume qua mixer. Sandbox đã đơn giản hoá bằng category dict (đủ cho bar).

**Khuyến nghị:** giữ `AudioService` category-volume làm runtime; thêm persistence `audioBgm/sfx/combat` float 0–1 vào settings save (PlayerPrefs key riêng hoặc chung JSON). Mixer (`AudioMixer.SetFloat`) chỉ khi cần master/ducking — defer.

### 3.2 Graphics

- Quality: `QualitySettings.SetQualityLevel` — mobile thường 2-3 tier; persist int index.
- Resolution/fullscreen: **không áp dụng mobile** (Android/iOS fixed); bỏ qua.
- FPS cap / v-sync: nếu cần, `Application.targetFrameRate` persist int. Defer nếu chưa có complaint.

### 3.3 Language

- Persist mã ngôn ngữ (`"vi"` / `"en"`) trong settings save. Parity: dhcd cũng để trong save-data class (`CommFlagSaveData.Select_Language_Type`).
- Apply: reload strings tại runtime, không restart app.

### 3.4 Persistence pattern (khuyến nghị chung)

```
PlayerPrefs keys:
  survivor.settings.v1   -> JSON {"audioBgm":0.6,"audioSfx":0.8,"lang":"vi","quality":1,...}
  survivor.progress.v1   -> JSON SurvivorProgressData
```
`PlayerPrefs.Save()` sau mỗi write (parity `BaseClientData.Save()` calls `PlayerPrefs.Save()`).
Settings áp dụng ngay khi thay đổi (AudioService volume set tức thì), ghi lười (write-on-change).

---

## 4. i18n

### 4.1 Sandbox có gì (structure-parity)

- `Assets/Scripts/Sandbox/PcTextResourceParser.cs` + `TextResourceService.cs`: parse
  `settings/text/textresource.txt` (StreamingAssets `Reference/PcText`) → `PcTextResourceEntry { key, vietnamese, chinese, description }`; lookup `GetVietnamese(key)` / `GetChinese(key)` / `GetOrVietnamese(key, fallback)`.
- `Assets/Scripts/PortData/PcText.cs` (`internal static class PcText`): `ReadLinesTcvn3` — windows-1252 bytes + TCVN3→Unicode table; `ReadLines(path, encoding)` với GBK. **Chỉ phục vụ đọc file PC**, không phải hệ thống UI i18n.
- `TextResourceService` public, `VLTK.Sandbox` asmdef → **Survivor asmdef đã ref Sandbox** (`VLTK.Survivor.Runtime.asmdef` refs `VLTK.Sandbox.Runtime`) → dùng được trực tiếp.

### 4.2 Vấn đề

- Kho textresource chỉ có **VN + zh**, KHÔNG có EN. zh = tiếng Trung, không thể dùng làm EN.
- Key set là key PC (objdata-style), không phủ hết UI survivor mới (card tên, button, popup).
- TCVN3/GBK decode chỉ đúng khi đọc file PC source — không liên quan runtime UI.

### 4.3 Unity Localization package — ĐÃ CÀI (1.5.12)

- `com.unity.localization` 1.5.12 có trong manifest → dùng StringTable/AssetTable, locale asset, runtime `LocalizedString`, editor tooling (bảng dịch, import CSV).
- Chi phí: cài đặt asset locale + bảng; phù hợp khi key set lớn + cần workflow dịch.

### 4.4 Khuyến nghị (lazy-first)

1. **V1: không cần package.** Tự author 2 bundle text:
   - `Assets/StreamingAssets/Survivor/Lang/vi.txt` + `en.txt` (hoặc 1 CSV), format `key<TAB>text`.
   - `SurvivorText` service: load theo `settings.lang`, fallback EN → key. ~30 dòng, 0 dependency, pattern copy `TextResourceService` (registry + lookup + fallback).
   - Đủ cho bar: card name/desc (tự author — map Decision 02: skill lib tự định nghĩa từ JX, KHÔNG copy dhcd data), HUD, popup, gameover.
2. **Nâng cấp Unity Localization** khi: key count > ~100, cần editor workflow dịch, hoặc asset table cho fonts/ảnh theo locale. Sandbox `TextResourceService` giữ nguyên cho nội dung PC-ported (VN/zh từ textresource.txt) — dùng cho skill names nếu muốn tái dùng chuỗi JX; EN tự dịch.

Không dùng `PcText` TCVN3/GBK cho UI runtime — đó là decode file, không phải i18n.

---

## 5. Pause/resume

### 5.1 BOUNDED evidence (r-dhcd-003 — đọc đầy đủ, tóm tắt)

- Proved: `BattleSys.set_IsPause(bool)` = **signed counter** tại `this+0xDC`, bool tại `this+0xD9` = `counter > 0`; tail-call `ReCalcTimeScale`. Normal card UI `OnVisible`→`set_IsPause(true)`, `OnHidden`→`false`. Đúng **1 acquire/release path**.
- `ReCalcTimeScale` sink chọn float trong `{0, 1, 1.5, 2}` bởi `IsPause` bool + byte speed flag. Sink không pin được identity (`Time.set_timeScale` = navigation evidence only).
- **KHÔNG proved**: global sim pause, timer suspension, input lock. 42/45 caller VAs chưa name. Quick UI pause path unresolved.
- Reconstructed `BattleSys.cs` (`reconstructed-types/GameLogic/A5Game.BattleSys.cs` L817 `IsPause`, L1568 `ReCalcTimeScale`) = IL recovery garbage — **dùng làm spec chỉ ở mức native facts trên**, KHÔNG copy code.

### 5.2 Own-design: pause scope (tự quyết, không claim parity)

Trạng thái hiện tại P1 (`Assets/Scripts/Survivor/SurvivorGameDirector.cs` L145-170):
- `OnLevelUp`: `_paused = true; Time.timeScale = 0f;` → show card → callback `Time.timeScale = 1f; _paused = false;`
- `OnPlayerDied`: tương tự + restart scene.
- Toàn bộ sim (monster move, player, projectile, gem, spawner tick) dùng `Time.deltaTime` (verified grep) → `timeScale = 0` đóng băng toàn bộ sim đúng ý. uGUI không phụ thuộc timeScale → card panel vẫn hoạt động. Input: `SurvivorJoystick` đọc trong Update — vẫn sống khi timeScale=0, nhưng panel overlay chặn pointer; WASD vẫn di chuyển player logic? KHÔNG — player move dùng `Time.deltaTime` (L71) nên đứng yên. OK.

**Vấn đề hiện tại:** hard-set `timeScale = 0/1` — không ref-count, không phân loại nguồn pause; nếu sau này 2 UI pause cùng lúc (card + settings) → release sớm phá pause. Chưa có `OnApplicationPause` (grep toàn project = 0 hit).

**Thiết kế khuyến nghị (own-design, mượn shape dhcd counter):**

```
PauseScope (enum): CardChoice, Settings, AppLifecycle, GameOver
SurvivorPause : MonoBehaviour (hoặc static trong director)
  - Dictionary<PauseScope,int> _counters  // ref-count per scope
  - Acquire(scope)  -> counter++, Recalc()
  - Release(scope)  -> counter--, Recalc()
  - Recalc() -> Time.timeScale = anyActive ? 0 : 1   // sim toàn bộ dt-based -> freeze đúng
```

- Tất cả nguồn pause đi qua counter → 2 UI chồng nhau không phá nhau (card mở + user mở settings → 2 acquire, đóng 1 còn 1 → vẫn pause). Đây là own-scope vì r-dhcd-003 chỉ prove 1 path; counter-shape là mượn cấu trúc native (signed counter) — nói rõ trong code comment `// own-design: ref-counted pause, parity-shape r-dhcd-003 (bounded), scope = sim freeze`.
- Giữ `timeScale ∈ {0,1}` (speed x1.5/x2 là own-feature sau, không cần giờ).
- UI animation cần unscaled (card fly-in): dùng `Time.unscaledDeltaTime` trong tween — ghi chú tại chỗ.

### 5.3 OnApplicationPause (app-lifecycle) — own-design

- Hiện project không có `OnApplicationPause` nào (verified grep). Android/iOS: app về background → Unity tạm dừng game loop nhưng `timeScale` KHÔNG reset; khi resume, nếu run đang chạy mà người chơi chết lúc background → mất mát oan.
- **Khuyến nghị:** `SurvivorGameDirector.OnApplicationPause(paused)`:
  - `paused==true` → `Acquire(AppLifecycle)` (freeze sim, không phải app-kill), nếu đang có card mở → giữ nguyên (counter đã > 0).
  - `paused==false` → `Release(AppLifecycle)`.
  - Nếu cần chống chết oan: tính `Time.unscaledTime` chênh lệch → cảnh báo/cho tiếp tục (defer, chưa cần).
- Đây hoàn toàn own-design — dhcd không có evidence nào về app-lifecycle pause.

---

## Tóm tắt quyết định đề xuất

| Hạng mục | Quyết định | Parity source |
|---|---|---|
| Save model | progress + settings riêng (JSON, string key); run-state defer P2 | dhcd `BaseClientData` PlayerPrefs+JSON; Sandbox `PcSaveSlotService` shape |
| Format | PlayerPrefs + JsonUtility v1; JSON file + Newtonsoft khi resume-run | Sandbox precedent (JsonUtility); manifest đã có Newtonsoft |
| Settings | audio = reuse `AudioService` category volume + persistence; quality int; lang mã `vi`/`en` | Sandbox `AudioService.cs`; dhcd `CommFlagSaveData.Select_Language_Type` |
| i18n | v1 tự author `SurvivorText` VN/EN bundle (StreamingAssets), pattern copy `TextResourceService`; Unity Localization 1.5.12 đã cài → nâng cấp khi key set lớn; `PcText` TCVN3/GBK chỉ decode file PC | Sandbox `TextResourceService` |
| Pause | ref-counted `SurvivorPause` per-scope (CardChoice/Settings/AppLifecycle/GameOver) → `Time.timeScale ∈ {0,1}`; **own-design, không claim parity ngoài counter-shape** | r-dhcd-003 (bounded) |
| App-lifecycle | `OnApplicationPause` → Acquire/Release(AppLifecycle) | own-design (dhcd không có) |
