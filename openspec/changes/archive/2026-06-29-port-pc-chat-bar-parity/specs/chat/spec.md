# Chat Specification

> Domain: **chat**. New domain introduced by change `port-pc-chat-bar-parity`.
> PC source of truth: `jx-source/pak_unpacked/1024/unknown/7e20a7ac.ini`
> (the 聊天条 "chat bar" window). `default_locale: vi`.

## Purpose

A persistent, PC-parity chat surface docked in the HUD bottom-center lane. It renders a
scrolling message history, a toggleable system/combat message strip, a channel selector
with on/off filtering, and a message input row. All structural pieces, channel data, and
visible art are sourced 100% from the PC client (`jx-source`); no fabricated art and
no invented channel names, colors, or rate limits.

The data layer already exists and is PC-authentic (`VLTK.Sandbox.ChatService` channel
colors and `VLTK.UI.ChatRoomPanelService.PcChannels`). This spec governs the HUD chat bar
that renders that data using the PC 聊天条 layout and SPR art.

## Open design question (for the design phase, not decided here)

The repo currently has TWO chat UIs: (1) a code-built uGUI `ChatPanel` (`ChatSystem.cs`)
that already carries full history + channel logic but uses no PC SPR art, and (2) the UI
Toolkit HUD `ChatBar` that this change brings to PC parity. The spec requires the HUD chat
bar to be **the** single chat surface. Whether the uGUI `ChatPanel` is fully retired,
hidden behind a debug flag, or kept as a fallback is a **design decision** — flagged here,
not resolved by this spec. The spec only mandates that there is one user-facing chat bar
matching PC.

## Requirements

### Requirement: PC chat bar structure

The HUD bottom-center chat surface SHALL render the four PC 聊天条 regions in the PC
arrangement: a message history region (`ChatRoom_List`), a system message strip
(`SysRoom_List`), a channel selector with on/off toggle (`ChannelBtn` /
`CheckOnImage`/`CheckOffImage`), and a message input row. The history region SHALL sit
above the system strip, which SHALL sit above the input row, matching the PC stacking in
`7e20a7ac.ini` (`[ChatRoom]` → `[SysRoom]` → input).

#### Scenario: All four regions present

- GIVEN the Sandbox scene is playing and the HUD has loaded
- WHEN the chat bar renders
- THEN the message history region, the system message strip, the channel selector, and the
  input row are all visible in the bottom-center lane
- AND they are stacked history-on-top, system strip, then input row

### Requirement: Message history display

The history region SHALL display filtered chat messages with the newest message at the
bottom (PC `TextBottom=1`). The default message text color SHALL be the PC
`MsgColor=255,249,148`, and each message SHALL additionally honor the per-channel PC
`TextColor` of its channel (see "Channel data fidelity"). The history SHALL retain at most
the PC `MaxMsgCount=120` messages and SHALL show the PC `MsgLineCount` number of visible
lines. Messages MUST be sourced from the existing `ChatService` history (no separate store).

#### Scenario: Messages render newest-at-bottom in PC color

- GIVEN the `ChatService` history contains messages on several channels
- WHEN the chat bar refreshes the history region
- THEN messages appear oldest-first with the newest at the bottom
- AND the default text color is the PC message color `255,249,148`
- AND a message whose channel defines a different PC `TextColor` renders in that channel
  color

#### Scenario: History bounded by PC max count

- GIVEN more than 120 messages have been posted to the `ChatService`
- WHEN the history region renders
- THEN at most the PC `MaxMsgCount` messages are retained/displayed (older messages dropped)

### Requirement: System message strip

The system message strip SHALL display system and combat-log messages (`ChatChannel.System`,
including `PostSystemMessage` / `PostCombatLog` output) separately from the player channel
history, using the PC `SysRoom_List` region (PC `MsgColor=255,249,148`). The strip SHALL be
collapsible via the PC system-window toggle button (`SysRoom_Open`, `提示信息窗－开关.spr`).

#### Scenario: System messages separated and toggleable

- GIVEN a system message and a player message have both been posted
- WHEN the chat bar renders
- THEN the system message appears in the system strip and the player message appears in the
  history region
- WHEN the system-window toggle is tapped
- THEN the system strip collapses or expands

### Requirement: Channel selector and filtering

The channel selector SHALL provide the six PC chat tabs in order: "Tất cả" (all),
"Mật" (private), "Phòng" (room), "Bang hội" (guild), "Môn phái" (faction), "Khác" (other) —
matching `[ChatTab] ChatTabNum=6` and `ChatTabLabel_0..5`. Selecting a tab SHALL filter the
history region to that tab's channels. The selector SHALL expose the PC channel on/off
toggle (`CheckOnImage`=`频道开与关a.spr` on, `CheckOffImage`=`频道开与关b.spr` off) and the
PC channel identity icons (self/friend/stranger `聊天频道图示` SPRs).

#### Scenario: Tabs filter the history

- GIVEN the history contains messages on team, faction, and world channels
- WHEN the "Môn phái" (faction) tab is selected
- THEN the history region shows only faction-channel messages (and system messages)
- AND selecting "Tất cả" (all) shows messages from every channel

#### Scenario: Channel toggle swaps PC frames

- GIVEN the channel on/off toggle is in the "off" state showing `频道开与关b`
- WHEN the toggle is tapped to "on"
- THEN the toggle icon swaps to the `频道开与关a` (on) frame

### Requirement: Channel data fidelity

Channel metadata SHALL match the PC `[Channels]` + per-channel sections of `7e20a7ac.ini`:
the 15 channels (`CH_NEARBY`..`CH_CUSTOM`), each channel's `TextColor`,
`SendMsgInterval`, and `SendMsgNum`, the default channel `CH_SYSTEM`, and the default
send name "Nhắc nhở". The implementation SHALL reuse the existing PC-authentic data
(`ChatRoomPanelService.PcChannels` and `ChatService.ChannelColor`); it SHALL NOT introduce
channel names, colors, or limits that differ from PC.

#### Scenario: Channel colors match PC

- GIVEN the channel data
- WHEN each channel's text color is inspected
- THEN the colors match the PC `TextColor` values (e.g. `CH_TEAM`=64,190,255;
  `CH_WORLD`=146,255,143; `CH_SYSTEM`=255,0,0; `CH_TONG`=255,244,0)

#### Scenario: Default channel is the PC system channel

- GIVEN the chat bar initializes with no user selection
- THEN the active default channel is `CH_SYSTEM` and the send label is "Nhắc nhở"

### Requirement: PC SPR art and provenance

Every visible chat-bar frame piece, toggle, scroll control, and channel icon SHALL be a
genuine decoded PC SPR from `jx-source`, resolved by JX Pack Hash (GBK path → hash →
file on disk). The pieces include: chat bar bottom/top/middle frames
(`聊天条底部改`/`顶部改`/`中部改`), shadow toggle (`聊天条阴影按钮`), system-window toggle
(`提示信息窗－开关`), scroll up/down (`提示信息窗－上`/`下`), scroll thumb (`通用拖动条`),
and channel identity icons (self/friend/stranger `聊天频道图示`). The change SHALL NOT
introduce fabricated or placeholder art. Decoded PNGs SHALL be staged to BOTH
`Assets/UI/HUD/Art/` and `Assets/StreamingAssets/UI/HUD/Art/` (runtime loads via the
StreamingAssets path; staging only the editor copy is a silent load failure).

#### Scenario: Each visible piece traces to a PC SPR

- GIVEN the staged chat-bar art set
- WHEN each visible chat-bar element's art is inspected
- THEN it traces to a PC SPR resolved by hash (e.g. middle frame = `3483ec02` =
  `聊天条中部改.spr`; sys toggle = `7c6eaab0` = `提示信息窗－开关.spr`)

#### Scenario: Art present in both art roots

- GIVEN the chat-bar PNG set
- WHEN the runtime StreamingAssets art root is checked
- THEN every chat-bar PNG resolves and loads at runtime (no missing-texture fallback)

### Requirement: History scrolling

The history region SHALL be scrollable for content exceeding the visible line count, using
the PC scroll controls: a scroll track (`聊天条中部改`), a drag thumb (`通用拖动条`), and
up/down scroll buttons (`提示信息窗－上`/`下`, each with PC `Up`/`Down`/`Over` frames).

#### Scenario: Scrolling reveals older messages

- GIVEN the history holds more messages than the visible line count
- WHEN the scroll-up control is activated
- THEN older messages scroll into view
- AND the scroll thumb reflects the current scroll position

### Requirement: Message input and send

The input row SHALL provide a text input field and a send button. Sending SHALL post the
entered text to the active channel through the existing `ChatService.SendPlayerMessage`,
then clear the input. The send button SHALL use the PC send-button SPR. Empty/whitespace
input SHALL be rejected without posting (matching `ChatService` empty-message handling).

#### Scenario: Send posts to the active channel

- GIVEN the active channel is "Đội" (team) and the input contains "xin chào"
- WHEN the send button is activated
- THEN "xin chào" is posted to the team channel via `ChatService`
- AND the input field is cleared

#### Scenario: Empty input is rejected

- GIVEN the input field contains only whitespace
- WHEN send is activated
- THEN no message is posted and the input is not cleared of meaningful content

### Requirement: Vietnamese localization

All user-facing chat text SHALL be in Vietnamese (`default_locale: vi`): chat tab labels,
channel names, the default send name "Nhắc nhở", the input placeholder, and any
user-facing status text. PC channel SPR art that carries text SHALL be the Vietnamese
variant where one exists; where only a Chinese variant exists, the change SHALL stop and
flag it for a user decision rather than shipping Chinese text.

#### Scenario: Tabs and names are Vietnamese

- GIVEN the chat bar renders
- THEN the six tab labels read "Tất cả", "Mật", "Phòng", "Bang hội", "Môn phái", "Khác"
- AND the default send name reads "Nhắc nhở"

### Requirement: Single user-facing chat surface

There SHALL be exactly one user-facing chat bar matching the PC 聊天条 layout (the HUD chat
bar). The HUD chat bar SHALL bind to the existing `SandboxManager.ChatService`. The fate of
the pre-existing code-built uGUI `ChatPanel` (retire, debug-gate, or fallback) is a design
decision (see Open design question); regardless of that decision, the user MUST see only
the PC-parity chat bar as the chat surface.

#### Scenario: One chat surface visible to the user

- GIVEN the HUD and sandbox are loaded
- WHEN the player views the chat UI
- THEN exactly one PC-parity chat bar is visible and it is bound to the shared
  `ChatService` history

### Requirement: Chat regression tests stay green

The chat EditMode tests SHALL cover chat-bar art presence (both art roots), chat-bar icon
loading, and channel/tab data fidelity, and SHALL remain green. New chat test fixtures
SHALL use a dedicated category tag.

#### Scenario: Chat tests pass

- GIVEN the chat EditMode tests
- WHEN the chat test category runs
- THEN all chat tests pass with zero failures
