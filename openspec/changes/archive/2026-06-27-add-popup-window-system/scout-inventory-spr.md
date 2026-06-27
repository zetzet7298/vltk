# Scout Findings: Player Inventory Window Art (Vietnamese SPRs)

This document details the scouting and research results for porting the PC player inventory (背包/Hành trang) window art assets to the mobile client as a standalone popup window.

---

## 1. Exist vs Not Found Art Assets

### Standalone Inventory Windows (F4)
These backgrounds are purely graphical wood/stone frames containing the grid layout. They do not contain printed text labels (text is drawn programmatically), hence no `_vn` variants exist.
*   **EXIST:** `\spr\ui3\道具\道具面板.spr` $\rightarrow$ **`da1f1d62.spr`** (214x454, 1 frame) — Standard standalone F4 inventory window background.
*   **EXIST:** `\spr\ui3\道具\道具面板2.spr` $\rightarrow$ **`3725e580.spr`** (214x430, 1 frame) — Standalone F4 inventory window background (variant 2).
*   **EXIST:** `\spr\ui3\道具\道具面板3.spr` $\rightarrow$ **`312b30c9.spr`** (214x454, 1 frame) — Standalone F4 inventory window background (variant 3).
*   **NOT FOUND:** `\spr\ui3\道具\道具面板_vn.spr` (No Vietnamese suffix variant on disk).

### Unified Character Info & Inventory Windows (F3 Vietnamese Version)
In the Vietnamese client (`updatejx08.pak`), the character sheet and inventory are unified into a single window.
*   **EXIST:** `\Spr\UI3\状态与装备\角色信息底图_vn.spr` $\rightarrow$ **`bc31847f.spr`** (318x438, 1 frame) — The unified container window frame in Vietnamese.
*   **EXIST:** `\Spr\UI3\状态与装备\属性页面_vn.spr` $\rightarrow$ **`26236e24.spr`** (314x356, 1 frame) — Attributes tab overlay page in Vietnamese.
*   **EXIST:** `\Spr\UI3\主界面\关闭_vn.spr` $\rightarrow$ **`962ab518.spr`** (164x28, 3 frames) — Close button with Vietnamese text "Đóng".

### Miscellaneous Inventory/Storage backgrounds
*   **EXIST:** `\spr\Ui3\储物箱\储物箱更新.spr` $\rightarrow$ **`7eb2646a.spr`** (214x454, 1 frame) — Storage chest background.
*   **EXIST:** `\spr\Ui3\储物箱\储藏箱.spr` $\rightarrow$ **`6781f204.spr`** (214x430, 1 frame) — Storage chest background.
*   **EXIST:** `\spr\Ui3\增加储物箱\储物箱更新.spr` $\rightarrow$ **`4aa4b430.spr`** (214x454, 1 frame) — Expanded storage background.
*   **EXIST:** `\spr\Ui3\增加储物箱\储藏箱增加.spr` $\rightarrow$ **`53dcd834.spr`** (214x400, 1 frame) — Expanded storage background.
*   **EXIST:** `\spr\ui3\物品栏\backpack\界面.spr` $\rightarrow$ **`ba02a164.spr`** (47x76, 21 frames) — Small tab/button icon, NOT a window background.

---

## 2. Player Inventory INI File
*   **Verdict:** There is **NO** player-only inventory window `.ini` file in the unpacked PC client. The player inventory bag layout is hardcoded directly in the engine executable.
*   Companion/Pet bag uses `94a9b42e.ini`.
*   Stash/Storage uses `b49267df.ini` (references `\Spr\Ui3\增加储物箱\储藏箱增加.spr`).
*   Bottom toolbar button bar uses `dc11ac12.ini` (defines the bag icon trigger button `[Items]` using `\Spr\Ui3\主界面\按钮条按钮\物品.spr`).

---

## 3. Decoded Vietnamese Sprites Confirmation
Decoded via `extract_item_spr.py` and visually verified:
*   `bc31847f.spr` (`角色信息底图_vn.spr`): Decodes to `bc31847f_frame_0.png` (318x438). It features the Vietnamese window title header "Nhân Vật" and the frame lines matching the reference screenshot.
*   `26236e24.spr` (`属性页面_vn.spr`): Decodes to `26236e24_frame_0.png` (314x356). It contains the attribute name labels in Vietnamese (Sức mạnh, Thân pháp, Sinh khí, Nội công, etc.) matching the left-side panel overlay of the status sheet.
*   `962ab518.spr` (`关闭_vn.spr`): Decodes to `962ab518_frame_0/1/2.png` (164x28). It displays the button label "Đóng" (Đóng).

---

## 4. Inventory Slots & Filter Tabs

### Inventory Slots
*   There is **no** dedicated slot background sprite defined in the INI files. The grid is drawn programmatically by the client engine using cell coordinates.

### Filter Tabs
*   **EXIST:** `\spr\Ui3\道具\道具－装备.spr` $\rightarrow$ **`8bc8706b.spr`** (72x28, 3 frames) — Used for "Trang bị" filter tab.
*   **NOT FOUND:** Separate tab sprites for "Tất cả", "Dược phẩm", "Nhiệm vụ", or "Khác" do not exist on disk.
*   **Reason:** The PC client inventory did not feature filtering (only pages 1, 2, 3). The category filtering is a custom mobile HUD feature.

---

## 5. Grid Geometry (`[ItemBox]`)
Mapped from `b49267df.ini` and `94a9b42e.ini`:
*   **HUnits (Columns):** 6
*   **VUnits (Rows):** 10
*   **UnitBorder (Border/Spacing):** 2 px
*   **Box Width:** 170 px
*   **Box Height:** 280 px
*   **Derived Single Cell Size:** **26x26 px**
    *   *Calculation:* $6 \text{ columns} \times 26\text{px} + 7 \text{ borders} \times 2\text{px} = 156 + 14 = 170\text{px}$ width.

---

## 6. Recommendation
Since the custom mobile UI requires filter tabs (Tất cả / Trang bị / Thuốc / ...) which do not exist natively as complete individual Vietnamese SPR files, we recommend:
1.  **Reconstruct from Reference:** Implement the standalone inventory window UI dynamically using Unity UI components, rather than using raw PC background sprites.
2.  **Slicing & Styling:** Use the container border style from the Vietnamese background `bc31847f.spr` (318x438) or character info sprite to create a modular, scalable panel frame.
3.  **Dynamic Grid Layout:** Build the 6x10 grid dynamically in Unity using `GridLayoutGroup` with cell size `26` and spacing `2`, placing item icons inside.
4.  **Close Button:** Reuse the Vietnamese close button sprite `962ab518.spr` ("Đóng") for window closure.
