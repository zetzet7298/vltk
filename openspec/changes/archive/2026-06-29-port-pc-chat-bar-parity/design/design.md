# Design: port-pc-chat-bar-parity

> PC source of truth: `vltksource_new/vl_update_27/pak_unpacked/1024/unknown/7e20a7ac.ini`
> (聊天条 chat bar window). Skill: `jx-hud-port` + `jx-pc-port-rule`.

## Skill resolution

`paths-injected` — both mandatory skills (`jx-pc-port-rule`, `jx-hud-port`) were injected
by the parent and read before work.

---

## 1. Architecture Overview

```
┌─ GameHudController (existing, ~1416 lines)
│   ├── Start() → InitializeHudChatBar()      ← NEW call
│   ├── BindElements() — existing ChatInput/FaceBtn bindings remain
│   └── LoadArt() — existing SendBtnIcon load remains
│
├─ HudChatBarController (NEW MonoBehaviour)     ← owns all chat bar logic
│   ├── Initialize(root, artFolder)            ← called by GameHudController
│   ├── BindElements() — query UXML subtree
│   ├── LoadChatArt() — PC SPR → VisualElement via LoadIconStatic
│   ├── BindChatService() — SandboxManager.ChatService
│   ├── RefreshHistory() — split system/non-system, render MsgColor
│   ├── OnChannelTabClicked / OnChannelToggle / OnSysToggle
│   ├── OnScrollUp / OnScrollDown
│   └── OnSend / OnInputChanged
│
└─ SandboxManager (existing)
    └── ChatPanel instantiation → GATED OFF (retired, see §6)
```

**Why a separate controller (not inline in GameHudController):** GameHudController is
already 1416 lines. The chat bar needs ~250 lines of dedicated logic (message rendering,
channel filtering, scroll, toggle state). A separate MonoBehaviour follows the established
pattern of `CombatSkillSlotController` (also initialized from GameHudController.Start(),
also queries its own UXML subtree). This keeps GameHudController's diff to ~15 lines.

---

## 2. UXML Element Tree — Rebuilt ChatBar

The current `ChatBar` element is a minimal input row (6 lines). It is replaced with the
PC 聊天条 layout: history region (top), system strip (middle), input row (bottom), framed
by PC SPR pieces. Each element maps to a PC INI section.

### PC INI → UXML mapping

| PC INI section | PC dims (1024-space) | Mobile design-space (1280×720, sx=1.25 sy=0.9375) | UXML element | Art (SPR → staged PNG) |
|---|---|---|---|---|
| `[Main]` root | 0,485 — bottom-anchored | bottom-left anchor, w=640, h=variable | `ChatBar` | — |
| `[SizeBtn]` bottom drag | 15×16 | 19×15 | `ChatBarBottomFrame` | `chat_bar_bottom` (bdf9af98) |
| `[MoveImg]` top resize | 15×17 | 19×16 | `ChatBarTopFrame` | `chat_bar_top` (8fa68495) |
| `[ChatRoom]` container | DummyWnd | flex-grow | `ChatRoomArea` | — |
| `[ChatRoom_List]` history | 280×140 | 350×131 | `ChatRoomList` (ScrollView) | MsgColor text only |
| `[ChatRoom_Scroll]` track | 12×95 | 15×89 | `ChatRoomScrollTrack` | `chat_bar_middle` (3483ec02) |
| `[ChatRoom_Scroll_Btn]` thumb | 13×27 | 16×25 | `ChatRoomScrollThumb` | `btn_chat_scroll_thumb_pc` (23fe2a10) |
| `[ChannelBtn]` + `[Main]` CheckOn/Off | toggle | 20×20 | `ChannelToggle` | `btn_chat_channel_on`/`off` (3b255f40/34fc44d5) |
| chat tabs (ChatTabNum=6) | — | flex-row | `ChatTab0`..`ChatTab5` | text labels (VI) |
| channel identity | — | 16×16 | `ChatChannelIcon` | `chat_icon_self_pc` (50304af7) |
| `[SysRoom_List]` system | 235×42 | 294×39 | `SysRoomList` (Label) | MsgColor text only |
| `[SysRoom_Open]` toggle | 15×14, CheckBox=1 | 19×13 | `SysToggle` | `btn_chat_sys_toggle` (7c6eaab0, 2f) |
| `[SysRoom_Up]` | 15×14, Up=0 Down=1 Over=2 | 19×13 | `SysScrollUp` | `btn_chat_sys_up` (b3e52a98, 3f) |
| `[SysRoom_Down]` | 15×14 | 19×13 | `SysScrollDown` | `btn_chat_sys_down` (af1cbe4c, 3f) |
| `[ShadowBtn]` | 15×17, CheckBox=1 | 19×16 | `ShadowToggle` | `btn_chat_shadow` (bcca4952) |
| input row (mobile-native) | — | flex-row | `ChatInputRow` (existing) | `btn_chat_send` (existing) |

### UXML structure (new ChatBar replacement)

```xml
<ui:VisualElement name="ChatBar" class="hud-chat-panel">
    <!-- Channel selector row: on/off toggle + 6 tabs + identity icon -->
    <ui:VisualElement name="ChannelSelector" class="hud-chat-channel-row">
        <ui:VisualElement name="ChannelToggle" class="hud-chat-channel-toggle">
            <ui:VisualElement name="ChannelToggleIcon" class="hud-chat-toggle-icon"/>
        </ui:VisualElement>
        <ui:VisualElement name="ChatTab0" class="hud-chat-tab"><ui:Label text="Tất cả" class="hud-chat-tab-label"/></ui:VisualElement>
        <ui:VisualElement name="ChatTab1" class="hud-chat-tab"><ui:Label text="Mật" class="hud-chat-tab-label"/></ui:VisualElement>
        <ui:VisualElement name="ChatTab2" class="hud-chat-tab"><ui:Label text="Phòng" class="hud-chat-tab-label"/></ui:VisualElement>
        <ui:VisualElement name="ChatTab3" class="hud-chat-tab"><ui:Label text="Bang hội" class="hud-chat-tab-label"/></ui:VisualElement>
        <ui:VisualElement name="ChatTab4" class="hud-chat-tab"><ui:Label text="Môn phái" class="hud-chat-tab-label"/></ui:VisualElement>
        <ui:VisualElement name="ChatTab5" class="hud-chat-tab"><ui:Label text="Khác" class="hud-chat-tab-label"/></ui:VisualElement>
        <ui:VisualElement name="ChatChannelIcon" class="hud-chat-channel-identity"/>
    </ui:VisualElement>

    <!-- ChatRoom: message history + scroll controls -->
    <ui:VisualElement name="ChatRoomArea" class="hud-chat-room-area">
        <ui:ScrollView name="ChatRoomList" class="hud-chat-room-list">
            <ui:Label name="ChatRoomContent" class="hud-chat-room-content" text=""/>
        </ui:ScrollView>
        <ui:VisualElement name="ChatRoomScrollTrack" class="hud-chat-scroll-track">
            <ui:VisualElement name="ChatRoomScrollThumb" class="hud-chat-scroll-thumb"/>
        </ui:VisualElement>
        <ui:VisualElement name="SysScrollUp" class="hud-chat-sys-btn"/>
        <ui:VisualElement name="SysScrollDown" class="hud-chat-sys-btn"/>
    </ui:VisualElement>

    <!-- SysRoom: system/combat message strip (collapsible) -->
    <ui:VisualElement name="SysRoomArea" class="hud-chat-sys-area">
        <ui:VisualElement name="SysToggle" class="hud-chat-sys-toggle">
            <ui:VisualElement name="SysToggleIcon" class="hud-chat-toggle-icon"/>
        </ui:VisualElement>
        <ui:Label name="SysRoomContent" class="hud-chat-sys-content" text=""/>
    </ui:VisualElement>

    <!-- Input row (existing elements, preserved) -->
    <ui:VisualElement name="ChatInputRow" class="hud-chat-input-row">
        <ui:TextField name="ChatInput" value="" class="hud-chat-input"/>
        <ui:Button name="FaceBtn" text="😊" class="hud-face-btn"/>
        <ui:VisualElement name="SendBtnIcon" class="hud-send-icon"/>
    </ui:VisualElement>
</ui:VisualElement>
```

**Removed from old UXML:** the static `<ui:Label text="!! Hãy sử dụng hồi phục" class="hud-chat-warning"/>`
(hardcoded placeholder text, not PC-derived). The warning style class is removed from USS.

**Preserved element names:** `ChatInput`, `FaceBtn`, `SendBtnIcon` keep their exact names so
existing `GameHudController` bindings (`_chatInput = root.Q<TextField>("ChatInput")`, face
picker wiring, `SendBtnIcon` load) still resolve without changes to those code paths.

---

## 3. USS Styling

### Anchor & sizing rules

The chat bar is **bottom-anchored, center-lane**, between the bottom-left joystick
(x ≤ ~155px in 1280-space) and the bottom-right combat cluster (right: 16px, ~320px wide).
Per `jx-hud-port` skill: "Keep bottom-left content at x >= 155 to free the joystick lane."

```css
/* Expanded chat bar: history + system + input */
.hud-chat-panel {
    position: absolute;
    left: 160px;          /* clear of bottom-left joystick */
    bottom: 16px;
    width: 640px;         /* spans to ~x=800, clear of combat cluster (right ~960) */
    min-height: 60px;     /* collapsed: input only */
    flex-direction: column-reverse;  /* input at bottom, history at top (PC TextBottom=1) */
    picking-mode: Ignore;  /* root passthrough; interactive children re-enable Position */
    overflow: visible;
}

/* Collapsed state: channel toggle off → hide history + system, show input only */
.hud-chat-panel.collapsed .hud-chat-room-area,
.hud-chat-panel.collapsed .hud-chat-sys-area {
    display: none;
}
```

### Key style classes (new)

| Class | Purpose | Key properties |
|---|---|---|
| `.hud-chat-channel-row` | tab bar row | `flex-direction: row; height: 22px;` |
| `.hud-chat-tab` | individual channel tab | `flex-grow:1; min-width:50px; picking-mode:Position` |
| `.hud-chat-tab.selected` | active tab highlight | `border-bottom-color: rgb(255,249,148)` |
| `.hud-chat-tab-label` | tab text | `font-size:11px; color:rgb(220,220,220); -unity-text-align:middle-center` |
| `.hud-chat-channel-toggle` | on/off toggle hit area | `width:20px; height:20px; picking-mode:Position` |
| `.hud-chat-toggle-icon` | toggle SPR icon child | `width:100%; height:100%; scale-to-fit` |
| `.hud-chat-room-area` | ChatRoom container | `flex-grow:1; min-height:100px; flex-direction:row` |
| `.hud-chat-room-list` | history ScrollView | `flex-grow:1; overflow:hidden` |
| `.hud-chat-room-content` | history text Label | `font-size:12px; color:rgb(255,249,148); enableRichText=true` (PC MsgColor) |
| `.hud-chat-scroll-track` | PC middle SPR track | `width:15px; background-image:chat_bar_middle` |
| `.hud-chat-scroll-thumb` | PC thumb SPR | `width:16px; height:25px; background-image:btn_chat_scroll_thumb_pc` |
| `.hud-chat-sys-area` | system strip container | `height:42px; flex-direction:row` |
| `.hud-chat-sys-toggle` | sys open/close toggle | `width:19px; height:13px; picking-mode:Position` |
| `.hud-chat-sys-content` | system text Label | `font-size:11px; color:rgb(255,249,148); enableRichText=true` |
| `.hud-chat-sys-btn` | scroll up/down SPR | `width:19px; height:13px; picking-mode:Position` |

**Frame SPR art** (`ChatBarTopFrame`/`BottomFrame`) is loaded at runtime via
`LoadIconStatic` onto VisualElements, not hardcoded in USS `background-image: url(...)`.
This matches the existing pattern where all HUD icons load from StreamingAssets (not
`project://database/` USS URLs), so runtime and editor builds use the same source.

---

## 4. HudChatBarController.cs — Responsibilities

### 4.1 Lifecycle

```csharp
[RequireComponent(typeof(UIDocument))]
public sealed class HudChatBarController : MonoBehaviour
{
    private ChatService _chat;
    private VisualElement _chatBar;
    private Label _historyContent, _sysContent;
    private ScrollView _historyScroll;
    private TextField _chatInput;
    // ... toggle/tab fields

    private bool _channelFilterOn = true;   // CheckOnImage default
    private bool _sysExpanded = true;        // SysRoom_Open default (Down=1 = on)
    private ChatChannel _activeTab = ChatChannel.All;
    private float _refreshInterval = 0.5f;   // poll ChatService history
    private float _lastRefresh;
```

### 4.2 Initialize (called from GameHudController.Start)

```csharp
public void Initialize(VisualElement hudRoot, string artFolder)
{
    _chatBar = hudRoot.Q("ChatBar");
    if (_chatBar == null) return;

    BindElements();
    LoadChatArt(artFolder);
    BindChatService();
    RegisterInteractions();
    RefreshHistory();   // initial render
}
```

### 4.3 BindChatService

Obtains `SandboxManager.Instance.ChatService`. Subscribes to `OnMessageReceived` for
live refresh (event-driven, not just polling). Falls back to polling at `_refreshInterval`
in `Update()` for robustness (sandbox startup race).

```csharp
private void BindChatService()
{
    var sandbox = SandboxManager.Instance;
    _chat = sandbox != null ? sandbox.ChatService : null;
    if (_chat != null)
        _chat.OnMessageReceived += _ => RefreshHistory();
}
```

### 4.4 RefreshHistory — the core render

Splits messages into system vs non-system (PC has two separate regions). Renders each as
rich-text into a single Label (matches existing `ChatPanel.Refresh()` pattern, but with
UI Toolkit `enableRichText`).

```csharp
private void RefreshHistory()
{
    if (_chat == null) return;

    var messages = _chat.GetFilteredMessages(120);  // PC MaxMsgCount

    // Split: PC separates system messages into SysRoom_List
    var chatMsgs = new StringBuilder();
    var sysMsgs  = new StringBuilder();

    foreach (var msg in messages)
    {
        string hex = ColorUtility.ToHtmlStringRGBA(msg.color);
        string line = string.IsNullOrEmpty(msg.senderName)
            ? $"<color=#{hex}>{msg.text}</color>"
            : $"<color=#{hex}>{msg.senderName}: {msg.text}</color>";

        if (msg.channel == ChatChannel.System)
            sysMsgs.AppendLine(line);
        else
            chatMsgs.AppendLine(line);
    }

    if (_historyContent != null) _historyContent.text = chatMsgs.ToString();
    if (_sysContent != null)     _sysContent.text     = sysMsgs.ToString();

    // Auto-scroll to bottom (PC TextBottom=1)
    if (_historyScroll != null)
        _historyScroll.scrollOffset = new Vector2(0, float.MaxValue);
}
```

**PC MsgColor fidelity:** the default message color is `255,249,148` (PC `MsgColor`).
Individual messages already carry their per-channel `ChatService.ChannelColor()` (which
maps to PC `TextColor` values). The controller does not invent colors — it renders the
`msg.color` that `ChatService` already assigned PC-authentically.

### 4.5 Channel tab interactions

Six tabs map to the existing `ChatChannel` enum (same as the retired uGUI ChatPanel):

| Tab index | Label (VI) | ChatChannel | PC channel group |
|---|---|---|---|
| 0 | Tất cả | `All` | All channels |
| 1 | Mật | `Private` | CH_CHATROOM / whisper |
| 2 | Phòng | `Room` | CH_CHATROOM |
| 3 | Bang hội | `Guild` | CH_TONG |
| 4 | Môn phái | `Faction` | CH_FACTION |
| 5 | Khác | `Other` | CH_CUSTOM / misc |

Selecting a tab calls `_chat.SetChannel(channel)` then `RefreshHistory()`. The selected
tab gets the `.selected` USS class (PC-style underline highlight).

### 4.6 Channel on/off toggle

When toggled **on** (`btn_chat_channel_on`, `频道开与关a`): channel filter is active —
history shows only the selected tab's messages (+ system).

When toggled **off** (`btn_chat_channel_off`, `频道开与关b`): the `.collapsed` class is
added to `ChatBar`, hiding the history and system regions. Only the input row remains
visible. Tapping again expands. This maps to the PC `SizeUp=1` + `CheckOn/Off` behavior
(minimize/expand the chat panel).

### 4.7 Sys toggle + scroll

`SysToggle` (`btn_chat_sys_toggle`, 2 frames): toggles `.collapsed` on `SysRoomArea`
(hide/show the system strip). PC `SysRoom_Open` is `CheckBox=1, Up=0, Down=1` → frame 0 =
closed, frame 1 = open. The controller swaps the SPR frame on toggle.

`SysScrollUp` / `SysScrollDown` (`btn_chat_sys_up`/`down`, 3 frames each: Up/Down/Over):
scroll the system content Label. Since the system strip is small (39px), scrolling is
minimal; these buttons cycle through system message history.

### 4.8 Input + send

The send button (`SendBtnIcon`, already loaded by `GameHudController.LoadArt()`) is
re-bound by `HudChatBarController` to post the active channel:

```csharp
private void OnSend()
{
    if (_chatInput == null || _chat == null) return;
    string text = _chatInput.text?.Trim();
    if (string.IsNullOrEmpty(text)) return;  // PC: reject empty

    _chat.SendPlayerMessage(_chat.ActiveChannel, "Người chơi", text);
    _chatInput.value = "";
}
```

**FaceBtn** wiring stays in `GameHudController` (existing emote picker appends to
`ChatInput`). No conflict — both controllers reference the same `ChatInput` TextField
element; the face picker writes, the send button reads.

---

## 5. Integration with GameHudController

### 5.1 New method in GameHudController

```csharp
private void InitializeHudChatBar()
{
    var chatBar = GetComponent<HudChatBarController>();
    if (chatBar == null)
        chatBar = gameObject.AddComponent<HudChatBarController>();

    var doc = GetComponent<UIDocument>();
    var root = doc != null ? doc.rootVisualElement.Q("GameHud") : null;
    chatBar.Initialize(root, artFolder);
}
```

Called from `Start()` after `InitializeCombatSkillSlots()` and before
`EnsurePcParityOverlayActive()`. This mirrors the `CombatSkillSlotController` pattern.

### 5.2 What stays in GameHudController (unchanged)

- `_chatInput` field binding (still queried in `BindElements` — harmless, used by face picker)
- `SendBtnIcon` art load in `LoadArt()` (loads `btn_chat_send` SPR)
- `FaceBtn` click → `OpenFacePicker()` / emote insertion

### 5.3 pickingMode coordination

`GameHudController.BindElements()` sets all root children to `PickingMode.Ignore`.
`HudChatBarController.RegisterInteractions()` re-enables `PickingMode.Position` on the
interactive chat elements (tabs, toggles, scroll buttons, input, send). This is the same
pattern as `RegisterClick()` — no conflict because the chat controller queries its own
subtree after the blanket-ignore pass.

---

## 6. ChatPanel Retirement Plan (open question resolved)

### Decision: Retire the instantiation, keep the class

The code-built uGUI `ChatPanel` (in `ChatSystem.cs`) is **not deleted** but is **no longer
instantiated**. The class remains in the source tree, marked with a deprecation comment.

### Rationale

1. **Single surface mandate:** the spec requires exactly one user-facing chat bar. The
   HUD chat bar (UI Toolkit + PC SPR) is the parity target. Two panels bound to the same
   `ChatService` cause dual message rendering — confusing and not PC-parity.
2. **No PC SPR art:** the uGUI ChatPanel uses code-built `Image`/`Text` with solid colors.
   It does not use any PC SPR art and never will — it predates the PC-art porting effort.
3. **Minimal blast radius:** gating the instantiation (2 lines) is far safer than deleting
   a 250-line class that other code references (`SandboxManager.ChatPanel` property).
4. **Reversibility:** if the HUD chat bar has issues at runtime, re-enabling the uGUI panel
   is a one-line uncomment — no merge conflicts.

### Implementation (in SandboxManager.cs)

The `ChatPanel` creation block (~line 2337) is gated behind a compile flag:

```csharp
#if VLTK_LEGACY_CHAT_PANEL
    // Chat Panel — RETIRED by port-pc-chat-bar-parity.
    // The HUD chat bar (HudChatBarController) is now the single PC-parity chat surface.
    // Re-enable this define to restore the legacy uGUI ChatPanel for debugging.
    if (ChatPanel == null && ChatService != null)
    {
        var cpGo = new GameObject("ChatPanel");
        cpGo.transform.SetParent(panelCanvas, false);
        ChatPanel = cpGo.AddComponent<ChatPanel>();
        ChatPanel.Initialize(ChatService);
    }
#endif
```

The `ChatBtn` sidebar toggle button (~line 2482) is also gated:

```csharp
#if VLTK_LEGACY_CHAT_PANEL
    EnsurePanelButton(canvasTransform, "ChatBtn", "Chat",
        new Vector2(-32f, 380f), new Color(0.2f, 0.4f, 0.3f, 0.85f),
        () => ChatPanel?.Toggle());
#endif
```

No other code changes needed — `SandboxManager.ChatPanel` property remains (nullable, stays
null at runtime). The `ChatPanel` class in `ChatSystem.cs` gets a `// DEPRECATED` header
comment but is otherwise untouched.

---

## 7. Art Loading

All chat bar art uses `GameHudController.LoadIconStatic(this, element, artPath, name)` —
the existing public static method that loads PNG from StreamingAssets via coroutine.

### Art load map (in HudChatBarController.LoadChatArt)

| Element | Art name | PC SPR hash |
|---|---|---|
| `ChatBarTopFrame` | `chat_bar_top` | 8fa68495 (顶部改) |
| `ChatBarBottomFrame` | `chat_bar_bottom` | bdf9af98 (底部改) |
| `ChatRoomScrollTrack` | `chat_bar_middle` | 3483ec02 (中部改) |
| `ChatRoomScrollThumb` | `btn_chat_scroll_thumb_pc` | 23fe2a10 (通用拖动条) |
| `ChannelToggleIcon` | `btn_chat_channel_on` / `_off` | 3b255f40 / 34fc44d5 |
| `ChatChannelIcon` | `chat_icon_self_pc` | 50304af7 (自己说) |
| `SysToggleIcon` | `btn_chat_sys_toggle` (+`_f1`) | 7c6eaab0 (提示信息窗开关) |
| `SysScrollUp` | `btn_chat_sys_up` (+`_f1`/`_f2`) | b3e52a98 (上) |
| `SysScrollDown` | `btn_chat_sys_down` (+`_f1`/`_f2`) | af1cbe4c (下) |
| `ShadowToggle` | `btn_chat_shadow` | bcca4952 (阴影按钮) |

Toggle buttons swap their icon art name on state change (same pattern as
`RefreshActionToggles` in `GameHudController`).

---

## 8. Data Flow

```
ChatService (VLTK.Sandbox)
  ├── History: List<ChatMessage> (max 200, PC MaxMsgCount governs display = 120)
  ├── ActiveChannel: ChatChannel
  ├── GetFilteredMessages(120) → List<ChatMessage>
  ├── ChannelColor(channel) → Color (PC TextColor values)
  ├── ChannelNameVi(channel) → string (Vietnamese)
  └── SendPlayerMessage(channel, sender, text)
        │
        ▼
HudChatBarController.RefreshHistory()
  ├── Split: system vs non-system
  ├── Render non-system → ChatRoomContent Label (rich text, per-channel color)
  ├── Render system     → SysRoomContent Label (rich text)
  └── Auto-scroll to bottom (PC TextBottom=1)
```

No data-layer changes. The controller is a pure consumer of `ChatService` APIs that
already exist and are PC-authentic.

---

## 9. Tests

### 9.1 Extend ChatBarIconLoadingTests.cs

Add a new `[TestFixture, Category("Chat")]` test class (or extend the existing `Hud`
category) covering:

1. **New chat art presence** — verify all 15 new PC SPR PNGs exist in both
   `Assets/UI/HUD/Art/` and `Assets/StreamingAssets/UI/HUD/Art/`.
2. **New chat art decodes** — verify each PNG loads via `LoadImage` with non-zero opaque
   pixels (same pattern as existing `AllChatIconPngs_HaveOpaquePixels`).
3. **Channel data fidelity** — verify `ChatService.ChannelColor()` returns the PC
   `TextColor` values for key channels (System=255,0,0; Team=64,190,255; World=146,255,143).
4. **Tab label count** — verify the 6 PC tabs map to the expected `ChatChannel` enum values.
5. **Message split** — verify that a mix of system + player messages splits correctly
   (system → SysRoom, player → ChatRoom).

Category tag: `Chat` (new). Run with `unityMCP___run_tests(mode="EditMode",
category_names=["Chat"])`.

### 9.2 No full-suite requirement during dev

Per AGENTS.md: only run the `Chat` category during development. Full suite only before
`git push`.

---

## 10. Changed-Lines Forecast

| File | Change type | Est. lines (net) |
|---|---|---|
| `Assets/UI/HUD/GameHud.uxml` | Replace ChatBar (6→42 lines) | +36 |
| `Assets/UI/HUD/GameHud.uss` | Replace chat styles (15→85 lines) | +70 |
| `Assets/Scripts/UI/HudChatBarController.cs` | NEW | +280 |
| `Assets/Scripts/UI/GameHudController.cs` | Add InitializeHudChatBar() | +15 |
| `Assets/Scripts/Sandbox/SandboxManager.cs` | Gate ChatPanel + ChatBtn | +8 |
| `Assets/Tests/EditMode/Sandbox/ChatBarIconLoadingTests.cs` | Extend | +80 |
| Art PNGs + .meta (20 files × 2 roots) | Pre-staged (already done) | +0 code |
| **Total code** | | **~489** |

**Exceeds 400-line review budget** → chained PR recommended (per SDD auto-forecast):
- **PR 1:** UXML + USS + art (structural shell, ~106 lines)
- **PR 2:** HudChatBarController + GameHudController integration (logic, ~295 lines)
- **PR 3:** ChatPanel retirement + tests (~88 lines)

---

## 11. Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| UI Toolkit ScrollView scroll-to-bottom unreliable | History doesn't auto-scroll to newest | Poll `scrollOffset` in Update; fallback: manually set Label content grow direction |
| Rich text color tags render differently in UI Toolkit vs uGUI | Channel colors look wrong | Verify in play mode screenshot; UI Toolkit supports `<color=#RRGGBBAA>` rich text |
| ChatService null at HUD init (startup race) | Chat bar shows empty | Poll in Update() with `_refreshInterval`; BindChatService retries until ChatService available |
| Face picker + send button both reference ChatInput | No conflict — picker writes, send reads | Confirmed: different operations on same TextField |
| ChatPanel retirement breaks SandboxManager.ChatPanel references | Compile error | Property stays nullable; only instantiation gated — no signature changes |

---

## 12. Rollout Sequence

1. **Art** (DONE): 20 PNGs staged to both art roots.
2. **UXML + USS**: Rebuild ChatBar structure + styles.
3. **HudChatBarController**: New controller with all chat logic.
4. **GameHudController integration**: Add InitializeHudChatBar() call.
5. **ChatPanel retirement**: Gate instantiation in SandboxManager.
6. **Tests**: Extend ChatBarIconLoadingTests.
7. **Verify**: Play mode screenshot → compare against `pc-evidence/hud/chat.png`.
8. **Commit**: Conventional commit `feat(hud): PC-parity chat bar (history, channels, system strip, PC SPR)`.
