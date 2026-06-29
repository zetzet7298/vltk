# Tasks: port-pc-chat-bar-parity

> Implementation tasks for the PC-parity chat bar. Follows design §12 (rollout sequence)
> and §10 (3-PR split). PC source of truth: `7e20a7ac.ini` (聊天条 chat bar layout).
> Mandatory skills: `jx-pc-port-rule`, `jx-hud-port`.

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~489 (UXML +36, USS +70, controller +280, integration +15, retirement +8, tests +80) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (UXML+USS+art verify, ~106) → PR 2 (controller+integration, ~295) → PR 3 (retirement+tests, ~88) |
| Delivery strategy | auto-chain |
| Chain strategy | stacked-to-main |

```text
Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: High
```

---

## Prerequisite (DONE — not a task)

- [x] **Stage 20 PC SPR PNGs to both art roots** — 15 PC SPRs (hash-resolved from GBK paths
      in `7e20a7ac.ini`) decoded via `~/Projects/vltktool/extract_item_spr.py` and copied to
      BOTH `Assets/UI/HUD/Art/` and `Assets/StreamingAssets/UI/HUD/Art/`. Includes 2 fixes
      (`chat_bar_top`=8fa68495, `chat_bar_bottom`=bdf9af98) + 13 new pieces
      (`chat_bar_middle`, `btn_chat_shadow`, `btn_chat_channel_on/off`, `btn_chat_sys_toggle`
      +`_f1`, `btn_chat_sys_up`+`_f1`/`_f2`, `btn_chat_sys_down`+`_f1`/`_f2`,
      `btn_chat_scroll_thumb_pc`, `chat_icon_self_pc`, `chat_icon_friend_pc`,
      `chat_icon_stranger_pc`, `btn_chat_channel_friend`, `btn_chat_channel_stranger`).
      PC source: `vltksource_new/vl_update_27/pak_unpacked/{spr,dmjx01}/unknown/<hash>.spr`.

---

## PR 1 — Structural shell: UXML + USS + art verification (~106 lines)

> Goal: rebuild the `ChatBar` element tree + styles to match the PC 聊天条 layout.
> No logic yet — just structure + art binding. Verify art loads after structure is in place.

### T1.1 — RED: write failing art-presence test for new chat PNGs

- [x] In `Assets/Tests/EditMode/Sandbox/ChatBarIconLoadingTests.cs`, add a new
      `[TestFixture, Category("Chat")]` class `HudChatBarArtTests` with a test
      `NewChatBarPngs_ExistInBothArtRoots` that asserts all 15 new art names exist in both
      `Assets/UI/HUD/Art/` and `Assets/StreamingAssets/UI/HUD/Art/`. Expected names:
      `chat_bar_top, chat_bar_bottom, chat_bar_middle, btn_chat_shadow,
      btn_chat_channel_on, btn_chat_channel_off, btn_chat_sys_toggle, btn_chat_sys_up,
      btn_chat_sys_down, btn_chat_scroll_thumb_pc, chat_icon_self_pc, chat_icon_friend_pc,
      chat_icon_stranger_pc, btn_chat_channel_friend, btn_chat_channel_stranger`.
      Run: `unityMCP_run_tests(mode="EditMode", category_names=["Chat"])` — should PASS
      (art is pre-staged). This is a guard test ensuring art survives future changes.

### T1.2 — Rebuild ChatBar UXML element tree

- [x] In `Assets/UI/HUD/GameHud.uxml`, replace the current minimal `ChatBar` element
      (lines ~74–82, which only has `ChatInput` + `FaceBtn` + `SendBtnIcon`) with the full
      PC layout from design §2. Structure (top→bottom): `ChannelSelector` row
      (toggle + 6 tabs + identity icon) → `ChatRoomArea` (ScrollView history + scroll track +
      thumb + up/down buttons) → `SysRoomArea` (toggle + system content Label) →
      `ChatInputRow` (existing `ChatInput` + `FaceBtn` + `SendBtnIcon` — preserve exact names).
      Remove the static placeholder `<ui:Label text="!! Hãy sử dụng hồi phục">`.
      PC source: `7e20a7ac.ini` sections `[ChannelBtn] [ChatRoom_List] [SysRoom_List]
      [SysRoom_Open] [SysRoom_Up] [SysRoom_Down]`.

### T1.3 — Add USS style classes for the new chat layout

- [x] In `Assets/UI/HUD/GameHud.uss`, replace the existing `hud-chat-*` styles (~15 lines)
      with the expanded class set from design §3. Key classes: `.hud-chat-panel`
      (bottom-anchored, `left:160px; bottom:16px; width:640px; flex-direction:column-reverse;
      picking-mode:Ignore`), `.hud-chat-channel-row`, `.hud-chat-tab` / `.selected`,
      `.hud-chat-room-area`, `.hud-chat-room-content` (PC `MsgColor=255,249,148`),
      `.hud-chat-scroll-track`, `.hud-chat-sys-area`, `.hud-chat-sys-content`,
      `.hud-chat-input-row`, `.collapsed` modifier. Frame SPRs load at runtime via C#
      (not USS `url()`). PC source: `7e20a7ac.ini` `MsgColor`, `MsgLineCount`, dimensions.

### T1.4 — Verify PR 1: compile + art loads in structure

- [x] Refresh Unity (`unityMCP_refresh_unity`), confirm 0 compile errors. Run
      `unityMCP_run_tests(mode="EditMode", category_names=["Chat"])` — T1.1 passes.
      Verify `ChatInput`/`FaceBtn`/`SendBtnIcon` element names still resolve (no broken
      `GameHudController` bindings). Commit: `feat(hud-chat): PC-parity chat bar structure (UXML+USS)`.

---

## PR 2 — Logic: HudChatBarController + GameHudController integration (~295 lines)

> Goal: wire the chat bar to the existing `ChatService` — render history, channel filter,
> system strip, scroll, toggle, input/send. Depends on PR 1 structure.

### T2.1 — RED: write failing test for channel data fidelity

- [ ] In `Assets/Tests/EditMode/Sandbox/ChatBarIconLoadingTests.cs` (the `Chat` category
      class), add `ChannelColors_MatchPC_TextColorValues` asserting
      `ChatService.ChannelColor(ChatChannel.System)` ≈ rgb(255,0,0),
      `ChannelColor(ChatChannel.Team)` ≈ rgb(64,190,255),
      `ChannelColor(ChatChannel.World)` ≈ rgb(146,255,143),
      `ChannelColor(ChatChannel.Guild)` ≈ rgb(255,244,0). Run `category_names=["Chat"]`.
      This should PASS already (data layer is PC-authentic) — it's a regression guard
      that locks the colors before the controller renders them.

### T2.2 — Create HudChatBarController.cs

- [ ] Create `Assets/Scripts/UI/HudChatBarController.cs` per design §4. A
      `[RequireComponent(typeof(UIDocument))]` MonoBehaviour with:
      `Initialize(hudRoot, artFolder)` → `BindElements()` (query the ChatBar subtree from
      PR 1: `ChatRoomList`, `ChatRoomContent`, `SysRoomContent`, `ChatInput`, tabs,
      toggles) → `LoadChatArt(artFolder)` (load 15 PC SPR PNGs onto VisualElements via
      `GameHudController.LoadIconStatic` per design §7 art map) → `BindChatService()`
      (`SandboxManager.Instance.ChatService`, subscribe `OnMessageReceived`) →
      `RegisterInteractions()` (tabs, toggles, scroll, send) → `RefreshHistory()`.
      `Update()` polls at `_refreshInterval=0.5f` for startup-race robustness.
      PC source: `7e20a7ac.ini` `MsgColor=255,249,148`, `MaxMsgCount=120`, `TextBottom=1`.

### T2.3 — Implement RefreshHistory (core render)

- [ ] In `HudChatBarController.cs`, implement `RefreshHistory()` per design §4.4: call
      `_chat.GetFilteredMessages(120)` (PC `MaxMsgCount`), split into system vs non-system,
      render each as rich-text `<color=#hex>` into `ChatRoomContent` / `SysRoomContent`
      Labels (using `msg.color` from `ChatService` — already PC-authentic). Auto-scroll to
      bottom (`_historyScroll.scrollOffset = (0, float.MaxValue)` — PC `TextBottom=1`).
      PC source: `7e20a7ac.ini [ChatRoom_List] MsgColor, [SysRoom_List] MsgColor`.

### T2.4 — Implement channel tabs + on/off toggle + sys toggle + scroll

- [ ] In `HudChatBarController.cs` per design §4.5–§4.7:
      - 6 tabs (`ChatTab0`..`5`): click → `_chat.SetChannel(channel)` + `RefreshHistory()`;
        selected tab gets `.selected` USS class.
      - `ChannelToggle`: on=`btn_chat_channel_on` (filter active), off=`btn_chat_channel_off`
        (adds `.collapsed` to ChatBar, hides history+sys). Swap SPR frame on toggle.
      - `SysToggle` (`btn_chat_sys_toggle`, 2f): toggle `.collapsed` on `SysRoomArea`;
        frame 0=closed, frame 1=open (PC `SysRoom_Open Up=0 Down=1`).
      - `SysScrollUp`/`Down`: cycle system message history.
      PC source: `7e20a7ac.ini [ChannelBtn] CheckOnImage/CheckOffImage, [SysRoom_Open] [SysRoom_Up] [SysRoom_Down]`.

### T2.5 — Implement input + send

- [ ] In `HudChatBarController.cs` per design §4.8: rebind `SendBtnIcon` click → `OnSend()`
      reads `ChatInput.text`, trims, rejects empty (PC behavior), posts via
      `_chat.SendPlayerMessage(_chat.ActiveChannel, "Người chơi", text)`, clears input.
      `FaceBtn` stays wired in `GameHudController` (no conflict — picker writes, send reads).
      PC source: `7e20a7ac.ini` input + `ChatService` empty-message handling.

### T2.6 — Integrate into GameHudController

- [ ] In `Assets/Scripts/UI/GameHudController.cs`, add `InitializeHudChatBar()` per design
      §5.1 (~15 lines): get/add `HudChatBarController`, call `Initialize(root, artFolder)`.
      Call it from `Start()` after `InitializeCombatSkillSlots()`, before
      `EnsurePcParityOverlayActive()`. Existing `ChatInput`/`FaceBtn`/`SendBtnIcon` bindings
      + `SendBtnIcon` art load in `LoadArt()` stay unchanged. PC source: N/A (Unity integration).

### T2.7 — Verify PR 2: compile + targeted tests

- [ ] Refresh Unity, confirm 0 compile errors. Run
      `unityMCP_run_tests(mode="EditMode", category_names=["Chat"])`. Enter play mode,
      post test messages via sandbox, confirm history renders in PC color, channel tab
      filters work, system strip separates, send posts + clears. Commit:
      `feat(hud-chat): HudChatBarController binds ChatService (history, channels, send)`.

---

## PR 3 — ChatPanel retirement + full test suite (~88 lines)

> Goal: retire the duplicate uGUI ChatPanel, add remaining tests, final verification.

### T3.1 — RED: write failing test for message split

- [ ] In the `Chat` category test class, add `MessageSplit_SeparatesSystemFromPlayer`:
      create a `ChatService`, `PostSystemMessage("sys")` + `SendPlayerMessage(...)`, then
      assert the split logic (system messages vs player messages are separable by
      `msg.channel == ChatChannel.System`). This may need a small static helper in
      `HudChatBarController` (e.g. `SplitMessages`) to be testable without a MonoBehaviour
      instance — extract the split into a testable pure method.

### T3.2 — Gate ChatPanel + ChatBtn instantiation behind compile flag

- [ ] In `Assets/Scripts/Sandbox/SandboxManager.cs`, wrap the `ChatPanel` creation block
      (~line 2337) and the `ChatBtn` sidebar button (~line 2482) in
      `#if VLTK_LEGACY_CHAT_PANEL ... #endif` per design §6. Add deprecation comment.
      The `ChatPanel` property stays (nullable, null at runtime). The `ChatPanel` class in
      `ChatSystem.cs` gets a `// DEPRECATED` header comment (no code deletion).
      Verify compile with flag undefined (default). PC source: N/A (single-surface mandate).

### T3.3 — Add remaining Chat tests + finalize category

- [ ] In `Assets/Tests/EditMode/Sandbox/ChatBarIconLoadingTests.cs`, complete the `Chat`
      category class from design §9.1: (1) new art decodes to non-zero opaque pixels,
      (2) channel colors match PC (T2.1), (3) 6 tabs map to expected `ChatChannel` enum,
      (4) message split (T3.1), (5) art present in both roots (T1.1). All use
      `[TestFixture, Category("Chat")]`. Run `category_names=["Chat"]`.

### T3.4 — Verify PR 3: full category + play-mode screenshot parity

- [ ] Run `unityMCP_run_tests(mode="EditMode", category_names=["Chat"])` — all pass.
      Enter play mode, capture Game View screenshot, compare against
      `/var/www/vltk-mobile/pc-evidence/hud/chat.png` — confirm: history region (yellow PC
      text), system strip, channel selector, scroll controls, input row all match PC.
      Confirm exactly ONE chat surface visible (no dual uGUI panel). Confirm joystick lane
      (bottom-left, x<155) remains touchable. Commit:
      `feat(hud-chat): retire legacy ChatPanel + parity tests`.

---

## Acceptance / done criteria (per spec)

- All four PC 聊天条 regions visible + stacked (history → system → input).
- History newest-at-bottom, PC `MsgColor=255,249,148`, bounded by `MaxMsgCount=120`.
- System strip collapsible; channel selector filters; channel toggle swaps PC SPR frames.
- Every visible art piece traces to a PC SPR by hash; present in both art roots.
- Exactly one user-facing chat surface (legacy ChatPanel gated off).
- `Chat` EditMode category green; `!Slow` still green.
