# Apply Progress: port-pc-chat-bar-parity

## PR 1 — Structural shell: UXML + USS + art verification (COMPLETE)

### Completed tasks

| Task | Status | Persisted checkbox |
|------|--------|--------------------|
| T1.1 RED: art-presence guard test | ✅ DONE | `[x]` |
| T1.2 UXML: rebuild ChatBar element tree | ✅ DONE | `[x]` |
| T1.3 USS: replace hud-chat-* styles | ✅ DONE | `[x]` |
| T1.4 Verify: compile + test + element resolution | ✅ DONE | `[x]` |

### Files changed (3 scoped files only)

1. **`Assets/Tests/EditMode/Sandbox/ChatBarIconLoadingTests.cs`** (+55 lines, -0)
   - New `[TestFixture, Category("Chat")]` class `HudChatBarArtTests`
   - Test `NewChatBarPngs_ExistInBothArtRoots`: asserts all 15 PC chat SPR PNGs exist
     in BOTH `Assets/UI/HUD/Art/` and `Assets/StreamingAssets/UI/HUD/Art/`.
   - Each art name annotated with PC SPR hash + Chinese source name for traceability.

2. **`Assets/UI/HUD/GameHud.uxml`** (+44 lines, -3)
   - Replaced minimal ChatBar (ChatInput + FaceBtn + SendBtnIcon + static warning label)
     with full PC 聊天条 layout tree per design §2.
   - Structure: ChannelSelector (toggle + 6 tabs + identity icon) → ChatRoomArea
     (ScrollView history + scroll track + thumb) → SysRoomArea (toggle + up/down + content)
     → ChatInputRow (preserved ChatInput/FaceBtn/SendBtnIcon).
   - Removed static `<ui:Label text="!! Hãy sử dụng hồi phục">` placeholder.
   - 22 new named elements; 3 preserved element names verified via XML parse.

3. **`Assets/UI/HUD/GameHud.uss`** (+129 lines, -27)
   - Replaced old chat styles (panel at left:425/bottom:24/width:430/height:70 +
     warning style) with design §3 class set.
   - `.hud-chat-panel`: position:absolute, left:160px, bottom:16px, width:640px,
     min-height:60px, flex-direction:column-reverse, picking-mode:Ignore.
   - `.hud-chat-room-content`: PC MsgColor rgb(255,249,148).
   - `.hud-chat-tab-label`: PC NameTextColor rgb(220,220,220).
   - `.hud-chat-tab.selected`: border-bottom-color rgb(255,249,148).
   - Collapsed modifier: `.hud-chat-panel.collapsed .hud-chat-room-area/.hud-chat-sys-area`
     → display:none.
   - Frame SPR art loads at runtime via C# (not USS background-image), per design.

### Test commands run

| Command | Result | Details |
|---------|--------|---------|
| `refresh_unity(mode=if_dirty, scope=assets)` | ✅ passed | Editor ready after refresh |
| `read_console` | ✅ 0 errors | Only pre-existing CS1998 warning + MCP WebSocket notice |
| `run_tests(mode=EditMode, category_names=["Chat"])` | ✅ 1/1 passed | `HudChatBarArtTests.NewChatBarPngs_ExistInBothArtRoots` — 0.29s |
| XML well-formedness parse | ✅ valid | 22 new elements + 3 preserved names all resolve |
| USS brace balance | ✅ balanced | 152 open / 152 close |

### UI diff verification (play mode screenshot analysis)

**Screenshot**: `Assets/Screenshots/screenshot-20260629-080335.png` (1568×882)
**PC evidence**: `pc-evidence/hud/chat.png` (1024×654)

**What matches PC:**
- ✅ Chat bar renders at correct position: left=160px (196px render), bottom=16px (20px render)
- ✅ Input row border visible: rgb(70,70,70) detected at design-space bottom=180-208px
- ✅ Input row dark background visible: rgba(5,10,8) region at design-space bottom=183-207px (24px height ≈ 28px row)
- ✅ Width correct: border extends ~784px render = 640px design × 1.225 scale
- ✅ Top status bar also renders (HP red 252,31,47, MP blue 13,116,252, EXP yellow 246,242,190) — confirms UI Toolkit capture works

**What differs from PC (expected for PR 1):**
- ⚠️ History labels empty (ChatRoomContent/SysRoomContent have no text) — PR 2 wires ChatService
- ⚠️ No SPR frame art loaded yet (top/bottom/middle/scroll/toggle icons) — PR 2 loads via LoadIconStatic
- ⚠️ Column-reverse places ChatInputRow at TOP of panel (design-space bottom=180-208px) rather than
  at the very bottom of the screen. This is per design §2 UXML ordering + `column-reverse`. PR 2
  controller can adjust child order if the visual layout needs input-at-screen-bottom.

### PC source evidence (no guessing)

- Layout INI: `vltksource_new/.../1024/unknown/7e20a7ac.ini` (聊天条)
- Sections mapped: `[ChatRoom_List]` (280×140, MsgColor=255,249,148, MaxMsgCount=120),
  `[SysRoom_List]` (235×42), `[ChannelBtn]`+`[Main]` CheckOnImage/CheckOffImage,
  `[ChatRoom_Scroll]`, `[SysRoom_Open]`, `[SysRoom_Up]`, `[SysRoom_Down]`
- All 15 SPR hashes verified present in both art roots (test passes)

### Remaining tasks (PR 2 + PR 3 — NOT started)

PR 2: T2.1–T2.7 (HudChatBarController + GameHudController integration)
PR 3: T3.1–T3.4 (ChatPanel retirement + tests + final screenshot parity)

### Workload / PR boundary

PR 1 = 228 changed lines (55 test + 129 USS + 44 UXML). Within scope. No PR 2/3 files touched.

### Structured status consumed

- `actionContext.mode: repo-local`, `allowedEditRoots: [/var/www/vltk-mobile/harness]`
- `artifactStore: both` (non-authoritative — resolved from openspec/)
- No blocking conditions; proceeded with implementation.
