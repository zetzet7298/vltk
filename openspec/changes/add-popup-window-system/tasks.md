# Tasks — Popup Window System + Character Info (HUD-003)

> Implementation tasks for `add-popup-window-system`. Spec=`spec.md`, Design=`design.md`.
> Design space 1280×720. Vietnamese UI + art only. Strict TDD: RED→GREEN→REFACTOR per phase.

## Review Workload Forecast
- **Commit 1 (infrastructure):** `PopupWindow`/`PopupManager`/`IPopupContent` + UXML/USS shell + close-art decode + tests ≈ **~300 lines**.
- **Commit 2 (Character Info):** `CharacterInfoContent`/paperdoll + UXML/USS + BtnStatus wiring + tests ≈ **~300 lines**.
- Total ≈ **~600 lines across 2 commits**. Each commit < 400-line budget. → **Split into 2 commits** (decision recorded; applies the review-workload guard).

---

## Phase A — PC art decode (Vietnamese SPRs)  [prep, no test]
- [ ] A1. Decode `关闭_vn.spr` (`962ab518`) → `btn_close_vn{,_h,_p}.png` (3 frames) into `Assets/UI/Popup/Art/`.
- [ ] A2. Decode `玲珑盒内框.spr` (`a210b99e`) → `popup_frame_inner.png` into `Assets/UI/Popup/Art/`.
- [ ] A3. Import settings: filter-mode Point (pixel art), no mipmaps, crunched-compress off.
- [ ] A4. Vision-check `btn_close_vn` renders "Đóng" (confirm VI before shipping).

## Phase B — PopupManager + contract  [TDD: RED first]
- [ ] B1. Write `PopupManagerTests.cs` (RED): Show adds backdrop+window; IsOpen true; second Show closes prior (single-focus); Close removes both.
- [ ] B2. Implement `IPopupContent.cs` (contract: TitleVi, Build, OnShow, OnClose).
- [ ] B3. Implement `PopupManager.cs` to GREEN the tests (static Instance, Show/Close, backdrop, single-focus).
- [ ] B4. REFACTOR: extract backdrop toggling into a private helper.

## Phase C — PopupWindow shell + UXML/USS
- [ ] C1. Author `PopupWindow.uxml` (PopupBackdrop handled by manager; shell = PopupFrameChrome[Title, CloseBtn, Body-slot]).
- [ ] C2. Author `PopupWindow.uss`: 玲珑盒 frame bg (natural aspect), title bar, close button uses `btn_close_vn` (3-state via USS :hover/:active), centered, within 1280×720.
- [ ] C3. Implement `PopupWindow.cs` + `PopupWindowBinder.cs`: build shell from UXML, mount `IPopupContent.Build(body)`, wire close-btn + backdrop-click → `PopupManager.Close()`.
- [ ] C4. Write `PopupShellCloseTests.cs`: close button + backdrop tap invoke onClose callback.
- [ ] C5. Add `<VisualElement name="PopupOverlay" class="hud-popup-overlay hidden"/>` to `GameHud.uxml`; add `.hud-popup-overlay` USS (full-screen, pickingMode=Position when open).

## Phase D — Commit 1: infrastructure
- [ ] D1. Recompile; run `run_tests(mode=EditMode, category_names=["Popup"])` → all green.
- [ ] D2. Commit: `popup(SDD): reusable PopupWindow/PopupManager shell + Đóng VI close art`.

## Phase E — Character Info content  [TDD]
- [ ] E1. Write `CharacterInfoContentTests.cs` (RED): seed `PlayerEquipmentService`(Weapon+Armor) + stub item resolver → Trang bị shows resolved icons for Weapon/Armor, empty frame for Helmet unequipped; seed `PlayerStateResponse`(strength=35) → Thuộc tính shows Sức Mạnh=35; tab switch toggles visible body.
- [ ] E2. Implement `CharacterInfoPaperdoll.cs`: slot layout per reference; bind Weapon/Body/Head/Mount real; Ring/Necklace/Belt/Boots = mapping framework; Mask/Amulet/Charm/Trinket = display-only empty. Inject `IItemResolver` for EditMode testability.
- [ ] E3. Implement `CharacterInfoContent.cs` (Build 3-tab body + header + action buttons; OnShow re-read equipment/stats; OnClose drop refs). Đánh giá = "sắp ra mắt" placeholder.
- [ ] E4. Author `CharacterInfo.uxml`/`.uss`: 3 tabs, header (name/PK/Trùng sinh/watermark), paperdoll grid, Khóa/Đính/Tháo buttons.
- [ ] E5. GREEN: tests pass.

## Phase F — Wire BtnStatus + verify
- [ ] F1. `GameHudController.OnStatusClick` → `PopupManager.Instance.Show(new CharacterInfoContent(equipment, mappings, GetPlayerState))`. Resolve equipment/mappings from `SandboxManager`; statsProvider reads latest `PlayerStateResponse` (or null→placeholder rows).
- [ ] F2. Init `PopupManager.Instance` on `GameHudController` Awake/Init against `PopupOverlay`.
- [ ] F3. Compile + play + screenshot Character Info open (Trang bị default, then each tab). Vision-check matches reference layout; close (Đóng) + backdrop-click close both work.
- [ ] F4. `run_tests(mode=EditMode, category_names=["Popup"])` → green; also run HUD category to confirm no regression.

## Phase G — Commit 2: Character Info + ship
- [ ] G1. Update `pc-evidence/hud/README.md` §popup provenance (SPR hashes, VI verification).
- [ ] G2. Commit: `popup(SDD): Character Info window (3 tabs, paperdoll, VI art) wired to BtnStatus`.
- [ ] G3. Push origin/dev.

## Follow-up (NOT this change)
- [ ] Inventory window (`BtnItems`) reusing the base.
- [ ] Treasure window (`BtnTreasure`).
- [ ] Mask/Amulet/Charm/Trinket data binding.
- [ ] Equip/unequip/socket gameplay (Khóa/Đính/Tháo real logic).
- [ ] Migrate SkillPicker/Team/Faction onto PopupManager base.
