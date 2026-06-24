# HUD Migration Plan: jx-cocos (Cocos2d-x) → vltk-mobile (Unity)

> **Nguồn truth**: `/home/zet/Projects/jx-cocos` (client cocos2d-x, file `*VN.cpp`).
> **Sprite truth**: `/home/zet/Projects/jx-cocos/client/Resources/ui_vn/**` (127 PNG gốc — TÁI SỬ DỤNG, KHÔNG tự vẽ).
> **Đích**: `/var/www/vltk-mobile` (Unity, UI Toolkit).
> **Lý do migrate**: HUD hiện tại (port từ `vltkunity`) sai, không dùng nữa.

## Nguyên tắc port

1. **Port 100% từ `*VN.cpp`** — không đoán, không bịa behavior.
2. **Tái sử dụng sprite `ui_vn/**` gốc** — KHÔNG tự vẽ placeholder. (Các file
   `harness/hud_preview/art/*.png` hiện tại là placeholder tự vẽ → phải thay
   bằng sprite thật khi implement.)
3. **Modular**: mỗi panel/bar là một component (adapter) riêng, tái sử dụng được.
4. **Behavior-first**: mọi nút có behavior, mọi panel có open/close, mọi slot
   có logic gắn/hoán đổi/drag-drop/tooltip đúng PC.

## Asset inventory (ui_vn gốc, đã verify tồn tại)

| Thư mục | File chính | Kích thước | Dùng cho |
| `KuiTopControl/` | rolestate.png | 211x71 | nền panel trạng thái trên (HP/MP/EXP/Avatar) |
| | blood.png | 139x14 | thanh HP fill |
| | mana.png | 139x15 | thanh MP fill |
| | stamina.png | 139x15 | thanh thể lực fill |
| | kinhnghiem.png | 139x15 | thanh kinh nghiệm fill |
| | AvatarNam.png / AvatarNu.png | 70x70 | avatar theo giới tính |
| `KgameWorld/` | mainskillmix.png, 64x64.png, immediacybox.png | — | thanh skill + ô item dùng ngay (bottom bar) |
| | life_bg.png, life_normal/blue/pk/deathmatch/killer/tusha.png | — | huy hiệu trạng thái PvP/trạng thái nhân vật |
| | yaoganx.png, mr-1_new.png, mr-2_new.png | — | joystick di chuyển + nút tấn công |
| | battery_small.png, tili.png, useskill_b.png | — | pin / thể lực / nút dùng skill |
| | attack_0..8.png, attackSpr_dir.png | — | hướng tấn công 8 hướng |
| `toolbar/` | 9 cặp nút menu (nhanvat, hanhtrang, vocong, baodanh, banghuu, todoi, banghoi, caidat, kytrancac) + auto/horse/giaodich/giaotiep/hide/ngoi/... | — | top menu (popup panels) + ancillary |
| `KuiChatList/` | khungchat.png + ChatListIcon-001..003 + tab icons (tatca/todoi/banghoi/hethong/thegioi) | — | chat panel tabs |
| `KuiPlayerSelFun/` | series-0..4, avatarmenu, selplayer_*, 0-0..4-1 | — | menu ngữ cảnh chọn player |

## Map element → source C++ → behavior

### Slice A — Top Status Bar (always visible) — `KuiTopControlVN.cpp` (509 L)
- Layout: nền rolestate.png (211x71), avatar 70x70 (Nam/Nu theo gender), level
  label (BMFont số xanh, dưới avatar), rank label (số xanh).
- 3 thanh fill (blood/mana/stamina) anchor (0,0), scaleX = cur/max (0..1).
- Thanh EXP (kinhnghiem.png) scaleX = cur/max, label dạng "%NN".
- Text HP/MP/Stamina: "cur/max" (TTF, outline đen).
- Update API: `upRoleInfo(min, max, kind, str)`:
  - kind 0 = HP (plife_spr scale + plifeLabel)
  - kind 1 = MP (pNeiLi_spr + pNeiLiLabel)
  - kind 2 = Stamina (pTiLi_spr + pTiLiLabel)
  - kind 3 = EXP (pExp_spr scaleX + pExpLabel "%")
  - kind 4 = Level (plevelLabel, 0 → RANK_WORLD_ZERO)
  - kind 5 = Rank (pRankLabel)
  - kind 6 = Name (translate(str))
- **Behavior**: read-only display, bind runtime state (HP/MP/Stamina/EXP/Level/Rank/Name/Gender/Avatar).

### Slice B — Buff/Status icon row (hidden default) — `KuiStateSkillControlVN.cpp`
- Vẽ lưới icon buff đang active (skillBuffData[skillId].buffPath), 10 cột, mỗi
  icon kèm text đếm ngược ("Ns"/"Nm"/"Nh"/"N/A"), màu xanh lá, stroke đen.
- Ẩn khi không có buff nào. startX=13, startY=height-87, offX=26, offY=-36.

### Slice C — Minimap (top-right) — `KuiMinMapVN.cpp` (1422 L)
- 128x128, nền RenderTexture, player dot trung tâm, NPC dot, nameplate.
- Coord text (x:y PC), zoom buttons, toggle worldmap (KuiMaxMapVN overlay).
- Click minimap → mở big map (KuiMaxMapVN) — KHÔNG phải click-to-move.

### Slice D — Bottom bar: skill bar + immedicy item box + joystick
- `KgameWorldVN.cpp` (7134 L): assembles mainskillmix.png (skill slots),
  immediacybox.png (item dùng ngay), yaoganx.png (joystick), mr-1/mr-2 (tấn công),
  battery, tili.
- Skill slots: gắn skill theo phái, tap để dùng, long-press drag để sắp xếp,
  cooldown overlay.
- Immedicy box: item dùng ngay (F-key PC), tap dùng, drag-drop từ Hành Trang.
- Joystick 8 hướng + nút tấn công tự động target.

### Slice E — 9 Menu buttons + popup panels (`toolbar/`)
| Nút | Source | Panel |
| 0 Nhân Vật | KuiRoleStateVN.cpp (973 L) | Character (thuộc tính, trang bị) |
| 1 Hành Trang | KuiItemVN.cpp (65K) | Inventory grid + tooltip (KuiItemdescVN 118K) |
| 2 Võ Công | KuiSkillVN.cpp + KuiSkilldescVN | Skill panel + tooltip |
| 3 Bảo Danh | KuiTaskInfoVN.cpp | Quest/task |
| 4 Bằng Hữu | KuiFriendListVN.cpp | Friend list |
| 5 Tổ Đội | KuiTeamVN.cpp | Team/party |
| 6 Bang Hội | KuiTongInfoVN.cpp (+11 file KuiTong*) | Guild |
| 7 Cài Đặt | (chưa xác định — inline?) | Settings |
| 8 Kỳ Trân Các | KuiShopVN.cpp | Shop |
- Mỗi panel: open/close (toggle), có nút đóng, drag để di chuyển, modal.
- Inventory: grid slot, drag-drop item, right-click/KuiItemdescVN tooltip, equip/swap.
- Trade: KuiPlayerTradeVN (63K), Give: KuiGiveVN (28K).

### Slice F — Chat panel (MainChatSprite in KgameWorldVN.cpp) + KuiChatList
- 5 tab (Tất cả/Tổ Đội/Bang Hội/Hệ Thống/Thế Giới), input, scroll list.

## Phasing (mỗi slice = 1 story, normal lane)

| Phase | Story | Scope | Risk flags | Status |
| P1 | A: Top Status Bar | HP/MP/Stamina/EXP/Avatar/Level/Rank/Name, bind runtime, real sprites | existing-behavior, public-contracts | ✅ Done (JxTopStatusBarState/Adapter, 12 tests) |
| P2 | B: Buff icon row | active buff grid + countdown | existing-behavior | ✅ Done (JxBuffRowState/Adapter, 18 tests) |
| P3 | C: Minimap | 128x128 + dots + coord + bigmap toggle | existing-behavior, cross-platform | ✅ Done (JxMinimapState/Adapter, 17 tests) |
| P4 | D: Bottom bar | skill slots + immedicy box + joystick + attack btn | existing-behavior, public-contracts | D1 ✅ skill slots (19 tests); D2 ✅ immedicy box (15 tests); D3 joystick/attack todo |
| P5 | E0: 9 Menu toolbar | 9-button toolbar frame, panel toggle via command bus, selected highlight | existing-behavior | ✅ Done (JxToolbarConfig/State/Adapter, 9 tests) |
| P5 | E1: Role/equipment panel | 15 equip slots, slot layout, equip/unequip state | existing-behavior, public-contracts | ✅ E1 equipment state/layout (10 tests) |
| P5 | E2: Inventory panel | inventory grid + tooltip, open/close, drag-drop | existing-behavior, public-contracts | ✅ E2a grid+item+swap (22) + adapter (12); ✅ E2b tooltip durability/action state (19) |
| P6 | E4-E8: Task/Friend/Team/Guild/Shop | social + shop panels, open/close | existing-behavior | todo |
| P7 | F: Chat panel | 5-tab chat + input + scroll | existing-behavior | todo |
| P8 | Settings + misc popups | Settings (recon needed), give/trade | existing-behavior | todo |

## Open questions / flags (KHÔNG giả định)
- **Settings (Cài Đặt, index 7)**: INDEX nói "chưa xác định" — cần recon thêm
  trong KgameWorldVN.cpp để tìm handler nút Cài Đặt.
- **Bottom bar / skill slot gắn skill**: logic gắn skill theo phái + shortcut
  key cần đối chiếu KgameWorldVN.cpp (7134 L) + KuiSkillVN.cpp.
- **Sprite import**: 127 PNG ui_vn phải copy vào
  `Assets/.../HudJxCocos/` (Sprite texture, không import làm sprite-atlas sai).
- **Font**: `_language_FontPath` + `UI_GAME_FONT_NUMBER` (BMFont số) — cần font
  tương đương trên Unity (đã có PcHudVietnameseTextOverlay, verify reuse).
- **HUD hiện tại sai**: các `*VltkUnityAdapter.cs` (TopBar/MiniMap/Money/Avatar/
  DeviceStatus/Skill/Bag/Equipment/Chat + VltkPanelAdapter) sẽ bị thay thế —
  giữ tạm để không break build, xóa dần khi adapter jx-cocos thay thế đủ.

## Validation per slice
- EditMode test (Category `HudJxCocos` mới) cho mỗi adapter: bind state đúng,
  bar fraction = cur/max clamped 0..1, open/close toggle, slot count đúng PC.
- Sprite asset reference test: adapter load đúng sprite path ui_vn.
- Không chạy full suite trong dev loop (xem AGENTS.md test rule).
