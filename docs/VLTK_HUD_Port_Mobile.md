# 🎮 Tài Liệu Port HUD — Võ Lâm Truyền Kỳ PC → Mobile

> **Mục tiêu**: Tái tạo 100% HUD game PC tại `/var/www/jx-pc` sang phiên bản mobile tại `/var/www/vltk-mobile`, giữ nguyên layout, Việt hoá, và logic hiển thị.

---

## 📁 Cấu Trúc File HUD PC (Nguồn)

```
jx-pc/pak_unpacked/ (unpacked from update01/update03/etc.)
├── ui/                         # UI config root (runtime)
│   ├── setting.ini             # Cài đặt chung, Map colors, shortcut sets
│   ├── MiniSkill.ini           # Buff/Debuff panel (204 buffs đã Việt hoá)
│   ├── ChatPics.ini            # 58 icon emote chat
│   ├── 表情大全.ini (±íÇé´óÈ«.ini)  # 153 emote faces list
│   ├── 五行.ini (ÎåÐÐ.ini)         # 5 hành lựa chọn nhân vật
│   └── ui3/
│       ├── 队伍一览信息.ini      # Team member preview overlay
│       └── TradeInfo.ini       # Trade partner info panel
│
├── Ui800/                      # UI config (độ phân giải 800px)
│   ├── 顶部控制条.ini            # ★ TOP HUD BAR - Thanh HP/MP/Stamina/EXP
│   ├── Setting.ini             # Copy of ui/setting.ini cho Ui800
│   ├── ChatPics.ini            # Chat emoticons
│   ├── 五行.ini                 # Five Elements class selector
│   ├── 树型排名主题.ini          # Ranking tree theme
│   ├── npcbobo.ini             # NPC bobo chat bubble config
│   ├── dxwrapper.ini           # DirectX wrapper config (PC-only)
│   └── Ui3/                   # Thành phần HUD chi tiết
│       ├── icon_bar.ini        # ★ Icon action bar (8 icons)
│       ├── SkillState.ini      # ★ Buff/Debuff list panel (185 buffs)
│       ├── 技能状态列表.ini      # Skill state list (duplicate)
│       ├── UiStallCurrency.ini # Stall currency selector
│       ├── 队伍一览信息.ini      # Team preview (Ui800 version)
│       ├── 交流帮助.ini         # Chat help dialog
│       ├── 界面帮助.ini         # Interface help dialog
│       ├── 操作帮助.ini         # Operation help dialog
│       ├── 装备链接功能.ini      # Equipment link feature
│       ├── 录像回放系统.ini      # Replay system UI
│       ├── 属性技能帮助.ini      # Attribute/Skill help
│       ├── 详细规则.ini         # Detailed rules
│       ├── 详细任务.ini         # Detailed quest UI
│       ├── 详细帮助项目.ini      # Help items
│       ├── 装备帮助.ini         # Equipment help
│       ├── battle/            # Battle selection UI
│       │   ├── battle_select.ini   # Active: Task trace UI
│       │   └── battle_select_old.ini
│       └── luckyturntable/
│           ├── itemspr.ini
│           └── luckyturntable.ini
```

---

## 🖥️ Kiến Trúc HUD PC — Phân Tích Chi Tiết

### Hệ thống render INI

Mỗi thành phần HUD được khai báo trong file `.ini` theo cú pháp:
```ini
[ComponentName]
Left=<px>      ; Tọa độ X từ trái màn hình (hoặc parent container)
Top=<px>       ; Tọa độ Y từ trên
Width=<px>
Height=<px>
Image=\spr\...\file.spr   ; Sprite file (`.spr` format, proprietary)
Moveable=0|1              ; Có thể kéo thả
Trans=0|1                 ; Transparency
ScriptFile=\script\...lua ; Lua script điều khiển logic
ClassType=Player_Life|Player_Mana|... ; Liên kết engine
```

**Màn hình gốc PC**: `800×600px` (mode Ui800) — mọi tọa độ tính trên base này.

---

## 🔢 Thành Phần HUD — Bảng Tọa Độ Gốc PC

### 1. ★ Thanh Trạng Thái Trên (Top Status Bar)

**File**: `Ui800/顶部控制条.ini`  
**Sprite nền**: `\spr\ui3\screen_top\frame_all.spr`  
**Vị trí container**: `Left=218, Top=0, Width=552, Height=17`

| Thành phần | ClassType | Left | Top | Width | Height | Sprite | Màu text | Tooltip VN |
|---|---|---|---|---|---|---|---|---|
| **Sinh lực (HP)** | `Player_Life` | 182 | 2 | 104 | 14 | `frame_life.spr` | 255,255,255 | `Sinh lực` |
| **Nội lực (MP)** | `Player_Mana` | 277 | 2 | 104 | 14 | `frame_mana.spr` | 255,255,255 | `Nội lực` |
| **Thể lực (STA)** | `Player_Stamina` | 87 | 2 | 104 | 14 | `frame_stamina.spr` | 255,255,255 | `Thể lực` |
| **Kinh nghiệm (EXP)** | `Player_Exp` | 372 | 2 | 104 | 12 | `frame_exp.spr` | 255,255,255 | `Kinh nghiệm` |
| **Cấp độ** | `Player_Level` | 53 | 1 | 20 | 12 | *(text only)* | 55,231,63 | `Đẳng cấp nhân vật` |
| **Thứ hạng** | `Player_WorldSort` | 499 | 2 | 28 | 12 | *(text only)* | 55,231,63 | `Thứ hạng giang hồ` |

**Cấu trúc mỗi bar** (ví dụ Life):
```ini
[Life]           ; Container bar tổng
Left=182, Top=2, Width=104, Height=14

[Life_Image]     ; Sprite thanh fill
Left=0, Top=0, Width=104, Height=9
Image=\spr\ui3\screen_top\frame_life.spr
PartType=0       ; 0=fill left-to-right

[Life_Text]      ; Text hiển thị HP số
Left=-5, Top=12, Width=104, Height=12
Font=12, HAlign=1 (center), Color=255,255,255
```

> **Port Note**: `PartType=0` = thanh fill từ trái sang phải. `PartType=1` = fill ngược.

---

### 2. ★ Icon Action Bar (Thanh Icon Nhanh)

**File**: `Ui800/Ui3/icon_bar.ini`  
**Lua script**: `\script\ui\icon_bar.lua`  
**Vị trí**: `Left=750, Top=195` (góc phải)  
**Kích thước icon**: `IconWidth=25, IconHeight=25, IconInterval=0`  
**Số lượng active**: `IconCount=1` (nhưng có 8 icon định nghĩa)

| Index | Sprite | Tooltip VN | Chức năng |
|---|---|---|---|
| Icon_0 | `\spr\Ui3\luckyturntable\icon.spr` | `Vòng Quay May Mắn` | Lucky Wheel |
| Icon_1 | `\spr\Ui3\TreasureChest\icon.spr` | `Phong Vân Bảo Điện` | Treasure Chest |
| Icon_2 | `\spr\Ui3\TreasureChest\shop.spr` | *(no tip)* | Shop |
| Icon_3 | `\spr\Ui3\pet\icon.spr` | *(no tip)* | Pet system |
| Icon_4 | `\spr\Ui3\loginprize\icon.spr` | *(no tip)* | Login prize |
| Icon_5 | `\spr\Ui3\funcprize\funcprize.spr` | *(no tip)* | Function prize (Twinkle=1) |
| Icon_6 | `\spr\Ui4\主界面\任务指南资源\opentracebtn.spr` | `Mở/Tắt theo dõi nhiệm vụ` | Task tracker toggle |
| Icon_7 | `\spr\Ui3\活动大厅\新服活动按钮.spr` | `Sảnh hoạt động` | Activity Hall (Twinkle=1) |

**Twinkle=1** = icon nhấp nháy (animation).

---

### 3. ★ Buff / Debuff Panel (Skill State)

**Files**:
- `Ui800/Ui3/SkillState.ini` — version Ui800 (**185 buffs**, có `btnClose`)
- `ui/MiniSkill.ini` — version nhỏ (**204 buffs**)
- `Ui800/Ui3/技能状态列表.ini` — version khác (**183 buffs**)

**Layout**:
```
Left=120(Ui800)/170(ui)/92(Ui3), Top=48, Width=240, Height=72
```

**Kích thước icon buff**: `24×24px`  
**Text đếm giờ (txtBuffTime)**: `Width=30, Height=12, Font=12, Color=0,255,0`  
**Debuff**: cùng size, `Color=255,140,0` (ui/MiniSkill) hoặc `55,231,63`

**Buff List đã Việt hoá** (trích từ SkillState.ini — 185 buffs):

| ID | Tên VN | Sprite | Mô tả | Debuff? |
|---|---|---|---|---|
| 33 | Tĩnh Tâm Quyết | `静心诀.spr` | Tăng chính xác | Buff |
| 42 | Kim Chung Trào | `金钟罩.spr` | Tăng phòng ngự | Buff |
| 15 | Bất Động Minh Vương | `不动明王.spr` | Tăng phòng ngự | Buff |
| 202 | La Hán Trận | `罗汉阵.spr` | Khai thông phần đánh | Buff |
| 273 | Như Lai Thiên Diệp | `如来千叶.spr` | Tăng kỹ năng | Buff |
| 718 | Bế Nguyệt Phất Trần | `闭月拂尘.spr` | Tăng năng lực toàn diện | Buff |
| 100 | Hộ Thể Hàn Băng | `护体寒冰.spr` | Khai thông phần đánh | Buff |
| 109 | Tuyết Ảnh | `雪影.spr` | Tăng tốc độ chạy/xuất chiêu | Buff |
| 713 | Ngự Tuyết Ẩn | `御雪隐.spr` | Ẩn thân trong trạng thái | Buff |
| 211 | Thất Tinh Trận | `七星阵.spr` | Tăng chính xác lực số thương | Buff |
| 157 | Tọa Vong Vô Ngã | `坐忘无我.spr` | Triệt tiêu số thương | Buff |
| 738 | Xuất Ổ Bất Nhiễm | `出淤不染.spr` | Thanh trừ hiệu quả phản diện | Buff |
| 171 | Thanh Phong Phù | `清风符.spr` | Tăng tốc độ chạy | Buff |
| 73 | Vạn Cổ Thực Tâm | `万蛊蚀心.spr` | Tăng thời gian trúng độc | **Debuff** |
| 150 | Thiên Ma Giải Thể | `天魔解体.spr` | Tăng năng lực chiếu hậu | Buff |
| 277 | Hoạt Bất Lưu Thủ | `滑不溜手.spr` | Tăng năng lực hành động | Buff |
| 130 | Túy Điệp Cuồng Vũ | `醉蝶狂舞.spr` | Tăng lực chiếu hậu | Buff |
| 440 | Tiên Thảo Lộ | `xiancaolu_sp.spr` | Tăng kinh nghiệm khi đánh | Buff |
| 987 | Trường Hiểu Ngũ Lý Hoà (tiêu) | `obj-potion05.spr` | Mỗi nửa giờ hồi phục sinh/nội 15% | Buff |
| 0 | Hành quân đơn | `questkey/003.spr` | Hồi phục sinh mệnh liên tục | Buff |

> **Note**: Sprite path gốc `\spr\Ui\状态图标\<tên_CN>.spr` — cần convert sang asset path mobile.

---

### 4. ★ Team Preview (Thông Tin Đội)

**File** (`ui/ui3/队伍一览信息.ini` — runtime active):
```
Left=5, Top=45, Width=170, Height=50
DummyWnd=1, Trans=1, Moveable=1
```

**File** (`Ui800/Ui3/队伍一览信息.ini` — Ui800 version):
```
Left=10, Top=36, Width=135, Height=30
```

| Thành phần | Sprite | Vị trí | Chức năng |
|---|---|---|---|
| **TxtName** / **TxtName (Ui800)** | `frame_member2.spr` | Left=0,Top=0 | Tên thành viên |
| **LeaderFlag** | `d队旗.spr` | Left=17,Top=-10 | Cờ đội trưởng |
| **FactionImg** | `icon_zd_new.spr` | Left=2,Top=2,26×26 | Icon môn phái |
| **ImgBlood (HP bar)** | `life.spr` | Left=27,Top=19,73×5 | Thanh HP thành viên |
| **ImgMana (MP bar)** | `mana.spr` | Left=27,Top=25,73×5 | Thanh MP thành viên |

**Faction icon list** (12 môn phái + expansions):
```
icon_zd_new.spr  icon_zd_sl.spr   icon_zd_wu.spr   icon_zd_em.spr
icon_zd_kl.spr   icon_zd_tm.spr   icon_zd_cy.spr   icon_zd_gb.spr
icon_zd_tw.spr   icon_zd_wd.spr   icon_zd_tr.spr   icon_zd_hsp.spr
icon_zd_wht.spr  icon_zd_xy.spr
```

---

### 5. ★ Stall Currency Selector (Chợ Trời)

**File**: `Ui800/Ui3/UiStallCurrency.ini`  
**Vị trí**: `Left=460, Top=50, Width=122, Height=82`  
**Sprite nền**: `\Spr\Ui3\Stall\StallCurrencyMain.spr`

| Button | Sprite | Vị trí | Chức năng |
|---|---|---|---|
| MoneyBtn | `StallMoney.spr` | Left=1,Top=27,60×54 | Bạch ngân (tiền đồng) |
| CoinBtn | `StallCoin.spr` | Left=61,Top=27,60×54 | Xu (premium currency) |

---

### 6. ★ Chat System

**File**: `ui/ChatPics.ini` / `Ui800/ChatPics.ini`  
**Số lượng emote**: 58  
**Sprite path**: `\spr\Ui3\表情\01.spr` → `58.spr`

**Emote faces** (`表情大全.ini` — 153 faces, đã Việt hoá):

| Face | Tip VN | Text code |
|---|---|---|
| Face1 | Mặt cười | `:)` |
| Face2 | Cười lớn | `:D` |
| Face3 | Giả mạnh | `:o` |
| Face4 | Không xấu | `:(` |
| Face5 | Khóc la | `:L` |
| Face6 | Bất lực | `:B` |
| Face7 | Thịnh nộ | `:@` |
| Face8 | Mặt ngu | `:0` |
| ... | ... | ... |

---

### 7. ★ Minimap — Bản Đồ Thu Nhỏ

Cấu hình màu sắc dot từ `ui/setting.ini` section `[Map]`:

| Đối tượng | Màu RGB | Giá trị hex |
|---|---|---|
| Nhân vật bản thân | `255,255,0` | #FFFF00 (Vàng) |
| Đồng đội | `0,255,0` | #00FF00 (Xanh lá) |
| Người chơi khác | `255,72,0` | #FF4800 (Cam) |
| Người chơi thù địch | `255,0,0` | #FF0000 (Đỏ) |
| NPC chiến đấu | `165,48,255` | #A530FF (Tím) |
| NPC thông thường | `165,48,255` | #A530FF (Tím) |
| Thú cưỡi bản thân | `180,230,0` | #B4E600 (Vàng xanh) |
| Thú cưỡi người khác | `255,128,0` | #FF8000 (Cam vàng) |
| NPC bản thân | `155,255,155` | #9BFF9B (Xanh nhạt) |
| NPC khác | `255,155,155` | #FF9B9B (Hồng) |

---

### 8. ★ Trade Info Panel

**File**: `ui/ui3/TradeInfo.ini`

```
Left=15, Top=170, Width=182, Height=98
Image=\spr\Ui3\nhat_ky\frame_all.spr
Font=26, DataTop=45
```

| Label | Nội dung VN |
|---|---|
| Header_txt | *(empty)* |
| Name_txt | `     + Tên:` |
| Level_txt | `    + Cấp:` |
| Faction_txt | `    + Phái:` |
| Guild_txt | `    + Bang:` |

---

## 🔧 Kiến Trúc Lua Scripts HUD

### Flow khởi tạo UI Client

```
game.exe → loads ui/setting.ini
         → [Theme] 0_Path=ui3          ; Chọn theme UI3
         → [ShortcutSet] 0_File=\Ui\autoexec.lua  ; Entry point chính
         
autoexec.lua (trong package .pak)
  ├── Loads icon_bar.lua  (từ icon_bar.ini ScriptFile)
  ├── Loads script/ui/ranking.lua
  ├── Loads script/missions/arena/ui.lua
  ├── Loads script/miniskill/ui.lua   ; Buff icon mapping
  └── Loads simcity/* modules
```

### Script quan trọng cho HUD

| Script | Chức năng |
|---|---|
| `\script\ui\icon_bar.lua` | Điều khiển icon action bar (click handlers) |
| `\script\ui\ranking.lua` | Ghi log xếp hạng EXP: `expranking_string()` |
| `\script\missions\arena\ui.lua` | Arena UI: `open_credits_shop()`, `signup_arean()` |
| `\script\miniskill\ui.lua` | Buff icon mapping table (67+ buffs): `take_buff_info()` |
| `\script\tasktrace\ui.lua` | Task tracker (gắn vào battle_select ScriptFile) |

### Buff Icon Mapping (miniskill/ui.lua)

```lua
-- Format: [skill_id] = {frame_index, is_buff}
icon = {
  [15]={181, 1},   -- Thiên Lâm (TL) - buff
  [202]={182, 1},  -- La Hán Trận (TL)
  [273]={183, 1},  -- Như Lai Thiên Diệp (TL)
  [20]={209, 0},   -- (TL) - debuff
  [33]={184, 1},   -- Tĩnh Tâm Quyết (TV)
  [42]={185, 1},   -- Kim Chung Trào (TV)
  -- ... 67+ entries
}

function take_buff_info(tab)
  -- Builds display string: <pic=N> + timer text
end
```

---

## 📱 Hướng Dẫn Port HUD Sang Mobile

### Bước 1: Scale Tọa Độ PC → Mobile

**Cơ sở PC**: `800×600px` (Ui800 mode)  
**Mobile target**: Responsive — recommend base `375×667` (iPhone SE) hoặc `390×844` (iPhone 14)

**Công thức scale**:
```
mobile_x = pc_left / 800 * screen_width
mobile_y = pc_top / 600 * screen_height
mobile_w = pc_width / 800 * screen_width
mobile_h = pc_height / 600 * screen_height
```

**Bảng scale nhanh** (target 390×844):

| Component | PC (px) | Mobile (px) approx |
|---|---|---|
| Top Bar container | 218,0 → 552×17 | 106,0 → 269×24 |
| HP Bar | +182,2 → 104×14 | +91,3 → 51×20 |
| MP Bar | +277,2 → 104×14 | +135,3 → 51×20 |
| STA Bar | +87,2 → 104×14 | +42,3 → 51×20 |
| EXP Bar | +372,2 → 104×12 | +181,3 → 51×17 |
| Level text | +53,1 → 20×12 | +26,1 → 10×17 |
| World Rank | +499,2 → 28×12 | +243,3 → 14×17 |
| Icon Bar | 750,195 → 25×25ea | Dock phải: 12×12 mỗi icon |
| Buff Panel | 120,48 → 240×72 | 58,57 → 120×86 |
| Team Preview | 5,45 → 170×50 | 2,54 → 83×60 |

> **Khuyến nghị Mobile**: Chuyển Top Bar từ horizontal top-center → luôn top với padding safe area. Dùng `SafeAreaInsets` cho iOS notch.

---

### Bước 2: Thay Thế Sprite `.spr` Bằng Assets Mobile

| Loại | PC `.spr` path | Mobile asset | Format |
|---|---|---|---|
| HP bar fill | `\spr\ui3\screen_top\frame_life.spr` | `assets/hud/bar_hp.png` | 9-patch PNG |
| MP bar fill | `\spr\ui3\screen_top\frame_mana.spr` | `assets/hud/bar_mp.png` | 9-patch PNG |
| STA bar fill | `\spr\ui3\screen_top\frame_stamina.spr` | `assets/hud/bar_sta.png` | 9-patch PNG |
| EXP bar fill | `\spr\ui3\screen_top\frame_exp.spr` | `assets/hud/bar_exp.png` | 9-patch PNG |
| Top bar bg | `\spr\ui3\screen_top\frame_all.spr` | `assets/hud/topbar_bg.png` | 9-patch PNG |
| Buff icon | `\spr\Ui\状态图标\*.spr` | `assets/buffs/<id>.png` | 24×24 PNG |
| Emote icon | `\spr\Ui3\表情\*.spr` | `assets/emotes/<n>.png` | 32×32 PNG |
| Faction icon | `\spr\Ui4\主界面\组队预览\icon_zd_*.spr` | `assets/factions/<name>.png` | 26×26 PNG |
| Team HP | `\spr\Ui4\主界面\组队预览\life.spr` | `assets/hud/team_hp.png` | 9-patch PNG |
| Team MP | `\spr\Ui4\主界面\组队预览\mana.spr` | `assets/hud/team_mp.png` | 9-patch PNG |

---

### Bước 3: Xử Lý Encoding GBK → UTF-8

> [!CAUTION]
> Tất cả file `.ini` gốc được encode **GBK** (Chinese). Khi đọc trong Python/Node phải decode đúng, không đổi encoding file gốc.

**Python đọc GBK an toàn**:
```python
with open('file.ini', 'rb') as f:
    raw = f.read()
text = raw.decode('gbk', errors='replace')
```

**Danh sách text Việt hoá đã có sẵn** trong file gốc (đã decode):

| File | Nội dung VN |
|---|---|
| `顶部控制条.ini` | `Sinh lực`, `Nội lực`, `Thể lực`, `Kinh nghiệm`, `Đẳng cấp nhân vật`, `Thứ hạng giang hồ` |
| `icon_bar.ini` | `Vòng Quay May Mắn`, `Phong Vân Bảo Điện`, `Mở/Tắt theo dõi nhiệm vụ`, `Sảnh hoạt động` |
| `SkillState.ini` | 185 buff names VN + descriptions |
| `MiniSkill.ini` | 204 buff names VN + descriptions |
| `表情大全.ini` | 153 emote tooltip VN |
| `五行.ini` | Mô tả 5 hành: Kim, Mộc, Thủy, Hỏa, Thổ (full VN) |
| `TradeInfo.ini` | `Tên:`, `Cấp:`, `Phái:`, `Bang:` |
| `操作帮助.ini` | Hướng dẫn thao tác game (VN đầy đủ) |
| `setting.ini` | 44+ chuỗi system message VN |
| `树型排名主题.ini` | 27 tiêu đề bảng xếp hạng VN |

---

### Bước 4: Implement ClassType → Data Binding Mobile

| ClassType (PC engine) | Mobile equivalent | API call |
|---|---|---|
| `Player_Life` | `player.hp / player.maxHp` | `GET /player/stats` |
| `Player_Mana` | `player.mp / player.maxMp` | `GET /player/stats` |
| `Player_Stamina` | `player.stamina / player.maxStamina` | `GET /player/stats` |
| `Player_Exp` | `player.exp / player.nextLevelExp` | `GET /player/stats` |
| `Player_Level` | `player.level` | `GET /player/stats` |
| `Player_WorldSort` | `player.worldRank` | `GET /ranking/world` |

---

### Bước 5: Implement PartType (Bar Fill Direction)

```
PartType=0  → Fill LEFT to RIGHT (HP, EXP)
PartType=1  → Fill LEFT to RIGHT nhưng có inverted display (Mana, Stamina)
```

**Mobile implementation** (React Native / CSS):
```css
/* HP bar - PartType=0 */
.bar-fill {
  width: calc(var(--current) / var(--max) * 100%);
  background: linear-gradient(90deg, #e33, #f55);
  transform-origin: left;
}
```

---

### Bước 6: Implement Buff System

**Struct Buff cần implement**:
```typescript
interface BuffEntry {
  id: number;           // Buff ID (từ BuffList trong SkillState.ini)
  name: string;         // Tên VN
  icon: string;         // Asset path (đã convert từ .spr)
  desc: string;         // Mô tả VN
  isDebuff: boolean;    // Buff_N_IsDebuff=1
  level?: number;       // Buff_N_Level (optional)
  remainTime?: number;  // Runtime: thời gian còn lại (giây)
}
```

**Display rules**:
- Buff: hiển thị ô `24×24`, border xanh `Color=55,231,63`
- Debuff: border cam/đỏ `Color=255,140,0`
- Timer text: dưới icon, `Font=12`, màu xanh hoặc cam
- Max slot hiển thị: 10 columns × 3 rows = 30 slots (Width=240/24=10)

---

### Bước 7: Icon Bar → Mobile Dock

**PC**: Fixed position `Left=750, Top=195` (góc phải)  
**Mobile**: Dock nổi cuộn được, hoặc tab bar dưới màn hình

```typescript
interface IconBarItem {
  index: number;
  sprite: string;       // Asset path
  tip: string;          // Tooltip VN
  twinkle: boolean;     // Animation nhấp nháy
  hide?: boolean;       // Ẩn/hiện
  onPress: () => void;  // Handler
}

// 8 icons từ icon_bar.ini:
const iconBarItems: IconBarItem[] = [
  { index: 0, tip: 'Vòng Quay May Mắn', twinkle: false, ... },
  { index: 1, tip: 'Phong Vân Bảo Điện', twinkle: false, ... },
  { index: 5, twinkle: true, ... },  // funcprize - nhấp nháy
  { index: 7, tip: 'Sảnh hoạt động', twinkle: true, ... },
];
```

---

### Bước 8: Team Preview Widget

**Layout mobile** (dựa trên `ui/ui3/队伍一览信息.ini`):

```
┌─────────────────────────────────┐
│ 🏳️ [FactionIcon 26×26] [Name]  │  ← Height: 50px
│           ████░░░░  ←HP bar    │
│           ████░░░░  ←MP bar    │
└─────────────────────────────────┘
Width=170, Height=50 per member
```

**Màu text tên**: `RGB(165, 255, 203)` = xanh mint

---

## 🗂️ Checklist Port HUD — Mobile

### Phase 1: Assets
- [ ] Extract tất cả `.spr` files sang PNG (dùng SPR extractor tool)
- [ ] Rename theo convention: `buff_{id}.png`, `emote_{n}.png`, `faction_{name}.png`
- [ ] Tạo 9-patch PNG cho các bar fills
- [ ] Scale icon sizes: buff 24px→48px (2x), emote 32px→64px (2x)

### Phase 2: Data Layer
- [ ] Export buff list từ `SkillState.ini` + `MiniSkill.ini` → JSON
- [ ] Export emote list từ `表情大全.ini` → JSON (153 entries)
- [ ] Export faction icons mapping
- [ ] Export ranking titles từ `树型排名主题.ini` (27 titles)
- [ ] Map minimap colors từ `setting.ini [Map]` section

### Phase 3: UI Components
- [ ] `TopStatusBar` — HP/MP/STA/EXP bars + Level + Rank text
- [ ] `BuffPanel` — Buff/Debuff icon grid (10×3) với timer
- [ ] `IconActionBar` — 8-icon dock với twinkle animation
- [ ] `TeamPreview` — Per-member widget (HP/MP mini bar + faction icon)
- [ ] `MiniMap` — Dot overlay với đúng màu sắc
- [ ] `ChatWindow` — Scrollable với emote picker (58 icons)
- [ ] `TradeInfoPanel` — Partner info panel
- [ ] `StallCurrencySelector` — 2-button Money/Coin

### Phase 4: Logic
- [ ] Implement `ClassType` data binding (Player_Life → `hp/maxHp`)
- [ ] Implement `PartType` fill direction
- [ ] Implement buff timer countdown
- [ ] Implement icon `Twinkle` animation
- [ ] Implement `Moveable` drag functionality (cho PC-like feel)
- [ ] Implement minimap dot rendering

### Phase 5: Việt Hoá Verify
- [ ] Verify tất cả tooltip đã Việt hoá (xem bảng Section 3)
- [ ] Verify buff names từ SkillState.ini (185 tên VN)
- [ ] Verify system messages từ `setting.ini` [InfoString] (44 strings)
- [ ] Verify 5 hành descriptions từ `五行.ini`

---

## 📋 Danh Sách Sprites Cần Extract

### Top Status Bar sprites
```
\spr\ui3\screen_top\frame_all.spr
\spr\ui3\screen_top\frame_life.spr
\spr\ui3\screen_top\frame_mana.spr
\spr\ui3\screen_top\frame_stamina.spr
\spr\ui3\screen_top\frame_exp.spr
```

### Team Preview sprites
```
\spr\Ui4\主界面\组队预览\frame_member2.spr
\spr\Ui4\主界面\组队预览\d队旗.spr
\spr\Ui4\主界面\组队预览\icon_zd_new.spr    (+ 13 faction variants)
\spr\Ui4\主界面\组队预览\life.spr
\spr\Ui4\主界面\组队预览\mana.spr
\spr\Ui4\主界面\组队预览\team_show.spr
```

### Buff Icon sprites (185 files)
```
\spr\Ui\状态图标\*.spr  (toàn bộ folder)
```

### Icon Bar sprites
```
\spr\Ui3\luckyturntable\icon.spr
\spr\Ui3\TreasureChest\icon.spr
\spr\Ui3\TreasureChest\shop.spr
\spr\Ui3\pet\icon.spr
\spr\Ui3\loginprize\icon.spr
\spr\Ui3\funcprize\funcprize.spr
\spr\Ui4\主界面\任务指南资源\opentracebtn.spr
\spr\Ui3\活动大厅\新服活动按钮.spr
```

### Emote sprites
```
\spr\Ui3\表情\01.spr → 58.spr
```

### Stall sprites
```
\spr\Ui3\Stall\StallCurrencyMain.spr
\spr\Ui3\Stall\StallMoney.spr
\spr\Ui3\Stall\StallCoin.spr
```

### Trade Info sprites
```
\spr\Ui3\nhat_ky\frame_all.spr
```

### NPC Bobo sprites
```
\spr\Ui3\Npc\series0-4.spr
\spr\UI3\Npc\ActionIcon_RideHorse.spr
\spr\UI3\Npc\ActionIcon_Sit.spr
\spr\UI3\Npc\fill_flash_1-4.spr
```

---

## ⚠️ Lưu Ý Quan Trọng Khi Port

> [!WARNING]
> **GBK Encoding**: Không bao giờ mở và save file `.ini` gốc bằng editor UTF-8. Luôn đọc bằng `decode('gbk')` trong script.

> [!IMPORTANT]
> **Sprite Format**: File `.spr` là định dạng độc quyền của JX Engine, không phải standard. Cần công cụ SPR extractor riêng để convert sang PNG/WebP.

> [!NOTE]
> **Hai version UI**: PC có 2 bộ config: `ui/` (runtime) và `Ui800/` (800px mode). Mobile nên ưu tiên dùng config từ `ui/` vì đây là version active nhất (có cả `TradeInfo.ini` mới nhất).

> [!TIP]
> **Buff deduplication**: `SkillState.ini` có 185 buffs, `MiniSkill.ini` có 204 buffs — nên merge cả hai và deduplicate theo `ID` để có list đầy đủ nhất.

> [!CAUTION]
> **Tọa độ relative vs absolute**: Trong INI, `Left/Top` của child components (như `Life_Image`, `Life_Text`) là **relative** với parent container (`Life`). Khi port sang mobile phải tính offset đúng.

---

## 🔗 File References

| File | Path |
|---|---|
| Top Status Bar | 顶部控制条.ini |
| Icon Bar | icon_bar.ini |
| Skill State (Buff) | SkillState.ini |
| MiniSkill Buff | MiniSkill.ini |
| Setting | setting.ini |
| Team Preview | 队伍一览信息.ini (ui) |
| Trade Info | TradeInfo.ini |
| Buff Icon Script | miniskill/ui.lua |
| Ranking Script | ui/ranking.lua |

---

*Generated từ deep scan knowledge graph + file analysis — `/var/www/jx-pc`*  
*Target project: `/var/www/vltk-mobile`*
