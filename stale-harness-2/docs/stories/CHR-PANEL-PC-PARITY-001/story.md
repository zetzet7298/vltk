# US-CHR Panel Nhân vật theo PC (sheet 318×438, 4 tab)

## Status

in_progress

## Lane

normal

## Product Contract

Thay popup Nhân vật 560×520 tự dựng bằng sheet PC 318×438 hiển thị 1:1 và căn giữa
như panel Kỹ năng. Có đủ 4 tab Thuộc tính (default), Trang bị, Đánh giá, Kinh mạch.
Dữ liệu/runtime hiện có nối thật; chức năng cần backend chưa tồn tại hiện đúng hình
PC nhưng disabled.

## Relevant Product Docs

- `contracts/popup-window-system.md`
- `docs/product/hud-003-popup-window-system.md`
- PC canonical: 2711122c/11da85ea/3f5d0331/4cf41f88/df252c4e + 8936c4d7/51176130

## Acceptance Criteria

- PopupWindow bỏ type-name string check; dùng PopupChromeKind + IPopupChromeHint.
- CharacterInfoContent implement IPopupLayoutHint (318×438), default tab Thuộc tính.
- 4 tab có đủ, frame state đúng PC; không còn placeholder "sắp ra mắt".
- Nút + gọi DistributePotential(1), frame disabled khi hết điểm; không còn "--".
- 16 hit-zone trang bị đúng config PC; chọn nền nam/nữ từ PlayerController.isFemale.
- Bind EquipSlot hiện có; slot chưa model hóa để trống, không tạo art thay thế.
- Tab Đánh giá: nền + controls PC disabled (chưa có backend community).
- Tab Kinh mạch: 8 huyệt đọc từ MeridianService; lệnh mua thời gian/Khí Doanh Đan
  Điền disabled.
- Khi popup mở: refresh theo event + tick HP/MP/EXP; đóng hủy scheduler/listener.
- OnStatusClick null-check giống OnSkillsClick, đóng map preview, mở qua PopupManager.
- Skill popup không đổi hình học/hành vi (regression).
- Asset audit: pin SHA-256 mọi PNG PC; provenance logical path→UID→package→frame;
  không có sprite chữ Trung fallback.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | footprint/chrome/centering; 4 tab + default; toạ độ/control tree; frame states; +/-điểm; 16 slot + gender; không còn generic chrome/--/placeholder |
| Integration | regression SkillContent/PopupManager/GameHudController pass |
| E2E | PlayMode: 4 tab, +/-điểm, đổi giới tính, mở Hành trang, đóng; chụp 1280×720 |
| Platform | full Debug compile, Console sạch, focused EditMode/PlayMode + SandboxBootE2E |

## Harness Delta

Intake #23 recorded. Brownfield UI parity slice.

## Evidence

(sẽ điền sau khi validate)

## Evidence (validated this slice)

- Compile: clean (Unity 6000.4.7f1, Debug).
- EditMode focused: 67/67 pass — CharacterInfoContentTests (14 new), PopupManagerTests, SkillContentTests, GameHudControllerTests, FactionContentTests, InventoryContentTests.
- Category sweep: Popup+Skill = 114/114; HUD+Skill = 81/81.
- Console errors after compile: 0 (only Vulkan memory warnings, pre-existing).
- Regression: SkillContent popup geometry/behavior unchanged; PopupManager lifecycle intact.

## Deferred (next slice)

- PNG vendor + SHA-256 pin for the 4 sub-page backgrounds + gender backgrounds.
- Outer-sheet tab frame states (selected/unselected sprites).
- PlayMode 1280×720 capture vs PC source (needs sheet art first).
- Full Debug compile + scripts/run_unity_test_profile.py incl. SandboxBootE2ETests.

## Visual parity correction — real 2711122c SPR

User visual reference matches the PC combined `TRANG BỊ VÀ THUỘC TÍNH` panel, not the earlier placeholder 4-tab recreation. Replaced the Character Info surface with the exact `2711122c` 428×430 panel and extracted the real PC SPR winners:

- `e3ecbac9` `\Spr\Ui3\状态与装备\装备和属性-男.spr` from `update03` → `panel_male.png`
- `6ce319ab` `\Spr\Ui3\状态与装备\装备和属性-女.spr` from `update03` → `panel_female.png`
- `9e87942b` `\Spr\Ui3\状态与装备\状态加点按钮改.spr` from `update01` → add-point frames

Validation:

- EditMode `CharacterInfoContentTests` + Popup + Skill + HUD: 42/42 passed.
- PlayMode GameView capture: `Assets/Screenshots/character-info-pc-real-spr-screen-chrome-final.png` shows full frame, `Vật phẩm`/`Đóng` footer visible, no generic chrome/header.

Deferred: real item icons inside equipment slots still need item SPR binding; current pass only restores PC panel/chrome/frame exactly.
