# Change: port-pc-chat-bar-parity

## Why

The mobile chat UI does not match the PC client. The PC chat bar (layout defined in
`pak_unpacked/.../7e20a7ac.ini`, the 聊天条 "chat bar" window) is a structured panel:

- `ChatRoom_List` — scrolling message history (280×140, PC text color `255,249,148`)
- `SysRoom_List` — system/combat message strip (235×42)
- `ChannelBtn` + channel on/off toggle — per-channel filtering with SPR identity icons
- Frame pieces — `聊天条底部改` / `顶部改` / `中部改` (bottom/top/middle) + `阴影按钮` shadow toggle
- Scroll controls — `提示信息窗` open/close toggle + up/down scroll buttons

The mobile HUD currently only renders a minimal input row (`GameHud.uxml` `ChatBar`:
`ChatInput` TextField + `FaceBtn` + `SendBtnIcon`). A second, separate `ChatPanel`
(uGUI, code-built in `ChatSystem.cs`) carries the real history + channel logic but is
not the HUD's chat bar and does not use PC SPR art.

Goal: bring the HUD chat bar to PC parity — full message history, system strip,
channel selector, and PC SPR frame pieces — sourced 100% from `jx-pc`.

## What Changes

- **Art (PC SPR, hash-resolved + decoded)**: stage 20 PNGs from 15 PC SPRs into both
  `Assets/UI/HUD/Art/` and `Assets/StreamingAssets/UI/HUD/Art/` (runtime loads via
  StreamingAssets). Fix 2 mismatched frames (`chat_bar_top`, `chat_bar_bottom`); add 13
  new pieces (middle frame, shadow toggle, channel on/off, sys window toggle/up/down,
  scroll thumb, channel identity icons self/friend/stranger + channel menu buttons).
  DONE during exploration (pre-staged).
- **HUD structure (UI Toolkit, `GameHud.uxml`)**: rebuild the `ChatBar` element into the
  PC layout — `ChatRoom_List` history (yellow PC text), `SysRoom_List` system strip,
  channel selector row with on/off toggle + identity icons, scroll up/down, shadow
  toggle, all framed by the PC SPR top/middle/bottom pieces.
- **Logic (`GameHudController.cs` + new `HudChatBarController.cs`)**: bind the HUD chat
  bar to the existing `SandboxManager.ChatService` (history + channels already
  implemented). Render filtered messages with PC `MsgColor`, wire channel toggle, sys
  window open/close, scroll, and input/send. Consolidate so the HUD chat bar is the
  single chat surface (deprecate the separate uGUI `ChatPanel` toggle path or keep it
  gated behind a debug flag — decided in design).
- **Tests**: EditMode tests for art presence + chat bar icon loading
  (extend `ChatBarIconLoadingTests.cs`); category tag for the new chat fixtures.

## PC Source (evidence)

- Layout INI: `jx-pc/pak_unpacked/1024/unknown/7e20a7ac.ini`
  (聊天条 chat bar) — sections `[Main] [SysRoom] [SizeBtn] [MoveImg] [ShadowBtn]
  [ChannelBtn] [ChatRoom] [ChatRoom_List] [ChatRoom_Scroll] [ChatRoom_Scroll_Btn]
  [SplitBtn] [MSNRoom] [SysRoom_Open] [SysRoom_List] [SysRoom_Up] [SysRoom_Down]`.
- Channel data INI: `7e20a7ac.ini [MSNRoom]` + `c9c8a750.ini [Channels]` (already
  ported into `ChatRoomPanelService.PcChannels`).
- SPR hash resolution (GBK path → JX Pack Hash UID → file on disk), all 15 found:
  `聊天条底部改`=bdf9af98, `顶部改`=8fa68495, `中部改`=3483ec02, `阴影按钮`=bcca4952,
  `频道开与关a`=3b255f40 / `b`=34fc44d5, `提示信息窗开关`=7c6eaab0, `上`=b3e52a98,
  `下`=af1cbe4c, `通用拖动条`=23fe2a10, `频道图示自己说`=50304af7, `好友`=2c66b90e,
  `密人`=69fbc7e6, `好友频道选择`=7addeacc, `密人频道选择`=3be3a09f.

## Non-goals

- Backend chat transport / network (still local `ChatService`).
- ChatPics emoji picker (face button) — separate slice.
- Pop-up chat room member list (`87248bea.ini`) — separate window.
