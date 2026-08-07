# 09 — Research: save/load + settings + localization architecture

Type: `research`
Status: ``resolved``
Blocked by: 01

## Question

Save/load progress + settings (audio/graphics/lang) + localization VN/EN + pause/resume. Cần:

1. Save model: progress (unlocked stages? best score? permanent meta-upgrades?) vs run-state
   mid-save (có save giữa run không). Unit dữ liệu.
2. Format options so sánh: JSON file / ScriptableObject / PlayerPrefs / SQLite — khi nào dùng cái
   nào, serialization lib.
3. Settings: categories (audio mixer volume, graphics quality/resolution, language), persistence.
4. i18n: Sandbox có bảng localization nào (`PcText`, TCVN3/GBK decode) dùng được không vs Unity
  Localization package; cách tổ chức key/bundle VN/EN.
5. Pause/resume: battle timescale pause khi card mở (r-dhcd-003 BOUNDED — chỉ 1 pause-counter
   acquire/release path proved, KHÔNG prove global sim pause/input lock → own-design cho pause
   scope) + `OnApplicationPause` app-lifecycle.

## Output

Ghi `research/save-settings-i18n.md`. Đọc `Assets/Scripts/Sandbox/` (grep localization/save/
audio/settings), `BattleSys.cs` (set_IsPause/ReCalcTimeScale), evidence r-dhcd-003. Rõ phần
own-design (pause scope) vs structure-parity.

## Answer

- **Save model**: progress + settings RIÊNG (offline single-player); run-state mid-save defer P2. Shape: dhcd `BaseClientData` (PlayerPrefs+JSON) + Sandbox `PcSaveSlotService`.
- **Format**: PlayerPrefs + JsonUtility v1 (progress/settings); JSON file + Newtonsoft khi resume-run (manifest đã có Newtonsoft).
- **Settings**: audio = reuse `AudioService` category volume + persistence; quality int; lang mã `vi`/`en`.
- **i18n**: v1 tự author `SurvivorText` VN/EN bundle (StreamingAssets, pattern copy `TextResourceService`); Unity Localization 1.5.12 đã cài → nâng cấp khi key set lớn; `PcText` TCVN3/GBK chỉ decode file PC.
- **Pause**: **own-design** ref-counted `SurvivorPause` per-scope (CardChoice/Settings/AppLifecycle/GameOver) → `Time.timeScale ∈ {0,1}`; parity-shape mượn counter r-dhcd-003 (bounded, KHÔNG claim global/input).
- **App-lifecycle**: `OnApplicationPause` → Acquire/Release(AppLifecycle) — own (dhcd không có evidence).
Full: research/save-settings-i18n.md
