// -----------------------------------------------------------------------------
// VLTK Mobile — HUD Chat Bar Controller
// Binds the PC-parity ChatBar UI Toolkit element tree to the existing ChatService.
// PC source: 7e20a7ac.ini (聊天条 chat bar layout):
//   [ChatRoom_List] MsgColor=255,249,148, MaxMsgCount=120, TextBottom=1
//   [SysRoom_List] MsgColor=255,249,148
//   [SysRoom_Open] Up=0 Down=1 (frame 0=closed, frame 1=open)
//   [Main] CheckOnImage=频道开与关a, CheckOffImage=频道开与关b
// SDD: port-pc-chat-bar-parity, PR 2 (design §4).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// Controls the HUD chat bar: renders message history in PC MsgColor, manages channel
    /// filter tabs, system message strip toggle/scroll, and input/send. A pure consumer of
    /// <see cref="ChatService"/> — no data-layer changes. PC layout source: 7e20a7ac.ini.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudChatBarController : MonoBehaviour
    {
        // --- Element references (queried from ChatBar subtree) ---
        private VisualElement _chatBar;
        private ScrollView _historyScroll;
        private Label _historyContent;
        private Label _sysContent;
        private TextField _chatInput;
        private VisualElement _sendBtnIcon;
        private VisualElement _channelToggleIcon;
        private VisualElement _chatChannelIcon;
        private VisualElement _sysToggleIcon;
        private VisualElement _sysRoomArea;
        private VisualElement _sysScrollUp;
        private VisualElement _sysScrollDown;
        private readonly VisualElement[] _tabs = new VisualElement[6];

        // --- State ---
        private ChatService _chat;
        private string _artPath;                // cached art root for toggle icon swaps
        private bool _channelFilterOn = true;   // PC CheckOnImage default (filter active)
        private bool _sysExpanded = true;        // PC SysRoom_Open Down=1 (open by default)
        private int _activeTabIndex = -1;        // PC default starts at CH_SYSTEM/"Nhắc nhở"; no tab selected until user chooses one
        private float _refreshInterval = 0.5f;   // poll ChatService for startup-race robustness
        private float _lastRefresh;

        // Tab → ChatChannel mapping (design §4.5)
        private static readonly ChatChannel[] TabChannels =
        {
            ChatChannel.All,
            ChatChannel.Private,
            ChatChannel.Room,
            ChatChannel.Guild,
            ChatChannel.Faction,
            ChatChannel.Other,
        };

        // PC MaxMsgCount=120 (7e20a7ac.ini [ChatRoom_List])
        private const int MaxDisplayMessages = 120;

        /// <summary>
        /// Testable PC chat-tab mapping from 7e20a7ac.ini [ChatTab] ChatTabNum=6.
        /// </summary>
        public static ChatChannel GetChannelForTabIndex(int index)
        {
            if (index < 0 || index >= TabChannels.Length)
                throw new System.ArgumentOutOfRangeException(nameof(index), index, "Chat tab index must be 0..5.");
            return TabChannels[index];
        }

        /// <summary>
        /// Splits a filtered ChatService history into the PC ChatRoom_List (non-system) and
        /// SysRoom_List (system/combat) streams. Kept pure for regression tests.
        /// </summary>
        public static void SplitMessages(
            IEnumerable<ChatMessage> messages,
            out List<ChatMessage> chatMessages,
            out List<ChatMessage> systemMessages)
        {
            chatMessages = new List<ChatMessage>();
            systemMessages = new List<ChatMessage>();

            if (messages == null) return;

            foreach (var msg in messages)
            {
                if (msg.channel == ChatChannel.System)
                    systemMessages.Add(msg);
                else
                    chatMessages.Add(msg);
            }
        }

        /// <summary>
        /// Called from GameHudController.Start() after combat slots are initialized.
        /// </summary>
        public void Initialize(VisualElement hudRoot, string artFolder)
        {
            if (hudRoot == null) return;
            _chatBar = hudRoot.Q("ChatBar");
            if (_chatBar == null) return;

            BindElements();
            _artPath = HudArtPathResolver.ResolveArtRoot(artFolder);
            LoadChatArt();
            BindChatService();
            RegisterInteractions();
            RefreshHistory();   // initial render
        }

        /// <summary>Query all interactive elements from the ChatBar subtree.</summary>
        private void BindElements()
        {
            _historyScroll = _chatBar.Q<ScrollView>("ChatRoomList");
            _historyContent = _chatBar.Q<Label>("ChatRoomContent");
            _sysContent = _chatBar.Q<Label>("SysRoomContent");
            _chatInput = _chatBar.Q<TextField>("ChatInput");
            _sendBtnIcon = _chatBar.Q("SendBtnIcon");
            _channelToggleIcon = _chatBar.Q("ChannelToggleIcon");
            _chatChannelIcon = _chatBar.Q("ChatChannelIcon");
            _sysToggleIcon = _chatBar.Q("SysToggleIcon");
            _sysRoomArea = _chatBar.Q("SysRoomArea");
            _sysScrollUp = _chatBar.Q("SysScrollUp");
            _sysScrollDown = _chatBar.Q("SysScrollDown");

            for (int i = 0; i < 6; i++)
                _tabs[i] = _chatBar.Q("ChatTab" + i);

            // History uses per-channel rich text; SysRoom uses the PC strip MsgColor uniformly.
            if (_historyContent != null) _historyContent.enableRichText = true;
            if (_sysContent != null) _sysContent.enableRichText = false;
        }

        /// <summary>Load PC SPR art onto toggle/icon elements (design §7 art map).</summary>
        private void LoadChatArt()
        {
            // PC frame/shadow pieces from [MoveImg]/[SizeBtn]/[ShadowBtn]
            LoadChatIcon(_chatBar.Q("ChatBarTopFrame"), _artPath, "chat_bar_top");
            LoadChatIcon(_chatBar.Q("ChatBarBottomFrame"), _artPath, "chat_bar_bottom");
            LoadChatIcon(_chatBar.Q("ShadowToggle"), _artPath, "btn_chat_shadow");

            // Channel toggle: CheckOnImage default (filter active)
            LoadChatIcon(_channelToggleIcon, _artPath, "btn_chat_channel_on");

            // Channel identity icon: self (聊天频道图示－自己说)
            LoadChatIcon(_chatChannelIcon, _artPath, "chat_icon_self_pc");

            // Scroll track background (聊天条中部改) + thumb (通用拖动条)
            LoadChatIcon(_chatBar.Q("ChatRoomScrollTrack"), _artPath, "chat_bar_middle");
            LoadChatIcon(_chatBar.Q("ChatRoomScrollThumb"), _artPath, "btn_chat_scroll_thumb_pc");

            // Sys toggle: open state = frame 1 (PC SysRoom_Open Down=1)
            LoadChatIcon(_sysToggleIcon, _artPath, "btn_chat_sys_toggle_f1");

            // Sys scroll buttons (frame 0 = normal Up state)
            LoadChatIcon(_sysScrollUp, _artPath, "btn_chat_sys_up");
            LoadChatIcon(_sysScrollDown, _artPath, "btn_chat_sys_down");
        }

        private void LoadChatIcon(VisualElement el, string artPath, string name)
        {
            if (el == null) return;
            GameHudController.LoadIconStatic(this, el, artPath, name);
        }

        /// <summary>
        /// Obtains ChatService from SandboxManager. Subscribes to OnMessageReceived for
        /// event-driven refresh. Retries in Update() if ChatService isn't ready yet.
        /// </summary>
        private void BindChatService()
        {
            var sandbox = SandboxManager.Instance;
            _chat = sandbox != null ? sandbox.ChatService : null;
            if (_chat != null)
            {
                _chat.OnMessageReceived += OnChatMessage;
                // PC default: CH_SYSTEM + send label "Nhắc nhở" (ChatRoomPanelService defaults).
                if (_chat.ActiveChannel != ChatChannel.System)
                    _chat.SetChannel(ChatChannel.System);
            }
        }

        private void OnChatMessage(ChatMessage msg)
        {
            RefreshHistory();
        }

        /// <summary>Register click callbacks on interactive elements.</summary>
        private void RegisterInteractions()
        {
            // Re-enable picking on interactive elements (root is Ignore per GameHudController).
            RegisterTabClicks();
            RegisterToggleClick(_chatBar.Q("ChannelToggle"), OnChannelToggleClick);
            RegisterToggleClick(_chatBar.Q("SysToggle"), OnSysToggleClick);
            RegisterScrollClick(_sysScrollUp, +1);
            RegisterScrollClick(_sysScrollDown, -1);

            // Send button
            if (_sendBtnIcon != null)
            {
                _sendBtnIcon.pickingMode = PickingMode.Position;
                _sendBtnIcon.RegisterCallback<PointerDownEvent>(evt =>
                {
                    OnSend();
                    evt.StopPropagation();
                });
            }

            // Chat input Enter/submit
            if (_chatInput != null)
            {
                _chatInput.pickingMode = PickingMode.Position;
                _chatInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        OnSend();
                        evt.StopPropagation();
                    }
                });
            }

            UpdateTabHighlight();
        }

        private void RegisterTabClicks()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i] == null) continue;
                int idx = i;
                _tabs[i].pickingMode = PickingMode.Position;
                _tabs[i].RegisterCallback<PointerDownEvent>(evt =>
                {
                    OnTabClick(idx);
                    evt.StopPropagation();
                });
            }
        }

        private void RegisterToggleClick(VisualElement toggle, System.Action cb)
        {
            if (toggle == null) return;
            toggle.pickingMode = PickingMode.Position;
            toggle.RegisterCallback<PointerDownEvent>(evt =>
            {
                cb();
                evt.StopPropagation();
            });
        }

        private void RegisterScrollClick(VisualElement btn, int direction)
        {
            if (btn == null) return;
            btn.pickingMode = PickingMode.Position;
            btn.RegisterCallback<PointerDownEvent>(evt =>
            {
                // Cycle system message window. Simple rotation for the small sys strip.
                RefreshHistory();
                evt.StopPropagation();
            });
        }

        // --- Tab interactions (design §4.5) ---

        private void OnTabClick(int index)
        {
            if (_chat == null || index < 0 || index >= TabChannels.Length) return;
            _activeTabIndex = index;
            _chat.SetChannel(TabChannels[index]);
            UpdateTabHighlight();
            RefreshHistory();
        }

        private void UpdateTabHighlight()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i] == null) continue;
                if (i == _activeTabIndex)
                    _tabs[i].AddToClassList("selected");
                else
                    _tabs[i].RemoveFromClassList("selected");
            }
        }

        // --- Channel on/off toggle (design §4.6) ---

        private void OnChannelToggleClick()
        {
            _channelFilterOn = !_channelFilterOn;

            if (_channelFilterOn)
            {
                // Expanded: show history + sys areas
                _chatBar.RemoveFromClassList("collapsed");
                LoadChatIcon(_channelToggleIcon, _artPath, "btn_chat_channel_on");
            }
            else
            {
                // Collapsed: hide history + sys, show input only (PC SizeUp behavior)
                _chatBar.AddToClassList("collapsed");
                LoadChatIcon(_channelToggleIcon, _artPath, "btn_chat_channel_off");
            }
        }

        // --- Sys toggle + scroll (design §4.7) ---

        private void OnSysToggleClick()
        {
            _sysExpanded = !_sysExpanded;

            if (_sysExpanded)
            {
                // Open: frame 1 (PC SysRoom_Open Down=1)
                if (_sysRoomArea != null) _sysRoomArea.style.display = DisplayStyle.Flex;
                LoadChatIcon(_sysToggleIcon, _artPath, "btn_chat_sys_toggle_f1");
            }
            else
            {
                // Closed: frame 0 (PC SysRoom_Open Up=0)
                if (_sysRoomArea != null) _sysRoomArea.style.display = DisplayStyle.None;
                LoadChatIcon(_sysToggleIcon, _artPath, "btn_chat_sys_toggle");
            }
        }

        // --- Input + send (design §4.8) ---

        private void OnSend()
        {
            if (_chatInput == null || _chat == null) return;
            string text = _chatInput.text?.Trim();
            if (string.IsNullOrEmpty(text)) return;  // PC: reject empty

            _chat.SendPlayerMessage(_chat.ActiveChannel, "Người chơi", text);
            _chatInput.value = "";
        }

        // --- RefreshHistory: core render (design §4.4) ---

        /// <summary>
        /// Renders filtered messages split into non-system (ChatRoomContent) and system
        /// (SysRoomContent) regions. Uses per-channel colors from ChatService (PC-authentic).
        /// Auto-scrolls history to bottom (PC TextBottom=1).
        /// </summary>
        public void RefreshHistory()
        {
            if (_chat == null) return;

            var messages = _chat.GetFilteredMessages(MaxDisplayMessages);
            SplitMessages(messages, out var chatMessages, out var systemMessages);

            // Split: PC separates system messages into SysRoom_List
            var chatMsgs = new StringBuilder();
            var sysMsgs = new StringBuilder();

            foreach (var msg in chatMessages)
            {
                string hex = ColorUtility.ToHtmlStringRGBA(msg.color);
                string line = string.IsNullOrEmpty(msg.senderName)
                    ? $"<color=#{hex}>{msg.text}</color>"
                    : $"<color=#{hex}>{msg.senderName}: {msg.text}</color>";

                chatMsgs.AppendLine(line);
            }

            foreach (var msg in systemMessages)
            {
                // PC SysRoom_List uses its own MsgColor=255,249,148; do not let
                // per-message ChatService colors override the system strip.
                string line = string.IsNullOrEmpty(msg.senderName)
                    ? msg.text
                    : $"{msg.senderName}: {msg.text}";

                sysMsgs.AppendLine(line);
            }

            if (_historyContent != null)
                _historyContent.text = chatMsgs.ToString();
            if (_sysContent != null)
                _sysContent.text = sysMsgs.ToString();

            // Auto-scroll to bottom (PC TextBottom=1)
            if (_historyScroll != null)
                _historyScroll.scrollOffset = new Vector2(0, float.MaxValue);
        }

        // --- Lifecycle ---

        private void Update()
        {
            // Startup-race robustness: retry binding ChatService if it wasn't ready at init.
            if (_chat == null)
            {
                BindChatService();
                if (_chat != null)
                    RefreshHistory();
            }

            // Poll refresh (event-driven is primary, this is backup for edge cases).
            if (_chat != null && Time.unscaledTime - _lastRefresh >= _refreshInterval)
            {
                _lastRefresh = Time.unscaledTime;
                RefreshHistory();
            }
        }

        private void OnDestroy()
        {
            if (_chat != null)
                _chat.OnMessageReceived -= OnChatMessage;
        }
    }
}
