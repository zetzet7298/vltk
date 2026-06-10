// -----------------------------------------------------------------------------
// VLTK Mobile — Chat System
// In-game chat with channels, Vietnamese UI, message history.
// PC source: chat UI layout from Ui3 INI files.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Chat channel types from PC.</summary>
    public enum ChatChannel
    {
        All = 0,        // Hiển thị tất cả
        World = 1,      // Thế giới (toàn server)
        Map = 2,        // Khu vực / bản đồ hiện tại
        Team = 3,       // Đội / group
        Faction = 4,    // Môn phái
        Private = 5,    // Mật / whisper
        System = 6,     // Thông báo hệ thống
        Room = 7,       // Phòng chat PC
        Guild = 8,      // Bang hội
        Other = 9,      // Khác
    }

    /// <summary>A single chat message.</summary>
    [Serializable]
    public class ChatMessage
    {
        public ChatChannel channel;
        public string senderName;
        public string text;
        public long timestamp;
        public Color color;
    }

    /// <summary>
    /// Chat service — manages channels, message history, and message dispatch.
    /// Pure C#, no MonoBehaviour. UI panel (ChatPanel) renders messages.
    /// </summary>
    public class ChatService
    {
        private readonly List<ChatMessage> _history = new();
        private readonly int _maxHistory = 200;
        private ChatChannel _activeChannel = ChatChannel.All;

        public event Action<ChatMessage> OnMessageReceived;
        public event Action<ChatChannel> OnChannelChanged;
        public IReadOnlyList<ChatMessage> History => _history;
        public ChatChannel ActiveChannel => _activeChannel;

        /// <summary>Send a player message to a channel.</summary>
        public void SendPlayerMessage(ChatChannel channel, string senderName, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var msg = new ChatMessage
            {
                channel = channel,
                senderName = senderName,
                text = text,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                color = ChannelColor(channel),
            };

            _history.Add(msg);
            TrimHistory();
            OnMessageReceived?.Invoke(msg);
        }

        /// <summary>Post a system message.</summary>
        public void PostSystemMessage(string text)
        {
            var msg = new ChatMessage
            {
                channel = ChatChannel.System,
                senderName = "[Hệ Thống]",
                text = text,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                color = new Color(1f, 0.85f, 0.3f),
            };
            _history.Add(msg);
            TrimHistory();
            OnMessageReceived?.Invoke(msg);
        }

        /// <summary>Post a combat log message.</summary>
        public void PostCombatLog(string text)
        {
            var msg = new ChatMessage
            {
                channel = ChatChannel.System,
                senderName = "",
                text = text,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                color = new Color(0.9f, 0.5f, 0.3f),
            };
            _history.Add(msg);
            TrimHistory();
            OnMessageReceived?.Invoke(msg);
        }

        public void SetChannel(ChatChannel channel)
        {
            _activeChannel = channel;
            OnChannelChanged?.Invoke(channel);
        }

        /// <summary>Get messages for the active channel (or all if All).</summary>
        public List<ChatMessage> GetFilteredMessages(int maxCount = 50)
        {
            var result = new List<ChatMessage>();
            for (int i = _history.Count - 1; i >= 0 && result.Count < maxCount; i--)
            {
                var msg = _history[i];
                if (_activeChannel == ChatChannel.All || msg.channel == _activeChannel || msg.channel == ChatChannel.System)
                    result.Insert(0, msg);
            }
            return result;
        }

        public static string ChannelNameVi(ChatChannel channel) => channel switch
        {
            ChatChannel.All => "Tất Cả",
            ChatChannel.World => "Thế Giới",
            ChatChannel.Map => "Khu Vực",
            ChatChannel.Team => "Đội",
            ChatChannel.Faction => "Môn Phái",
            ChatChannel.Private => "Mật",
            ChatChannel.System => "Hệ Thống",
            ChatChannel.Room => "Phòng",
            ChatChannel.Guild => "Bang Hội",
            ChatChannel.Other => "Khác",
            _ => "???",
        };

        /// <summary>PC-authentic chat channel text colors from uiconfig.ini SetChannelTextColor.</summary>
        public static Color ChannelColor(ChatChannel channel) => channel switch
        {
            // PC: CH_NEARBY "255,255,255"
            ChatChannel.All => new Color(1f, 1f, 1f),
            // PC: CH_WORLD "146,255,143"
            ChatChannel.World => new Color(0.573f, 1f, 0.561f),
            // PC: CH_TEAM "64,190,255"
            ChatChannel.Team => new Color(0.251f, 0.745f, 1f),
            // PC: CH_FACTION "225,210,165"
            ChatChannel.Faction => new Color(0.882f, 0.824f, 0.647f),
            // PC: CH_CITY "169,255,224"
            ChatChannel.Room => new Color(0.663f, 1f, 0.878f),
            // PC: CH_TONG "255,244,0"
            ChatChannel.Guild => new Color(1f, 0.957f, 0f),
            // PC: CH_CHATROOM "255,255,255" (private/whisper)
            ChatChannel.Private => new Color(1f, 1f, 1f),
            // PC: CH_SYSTEM "255,0,0"
            ChatChannel.System => new Color(1f, 0f, 0f),
            // Map channel uses CH_JABBER "193,193,193"
            ChatChannel.Map => new Color(0.757f, 0.757f, 0.757f),
            // Other
            ChatChannel.Other => new Color(0.757f, 0.757f, 0.757f),
            _ => Color.white,
        };

        private void TrimHistory()
        {
            while (_history.Count > _maxHistory)
                _history.RemoveAt(0);
        }
    }

    /// <summary>
    /// Chat UI panel — rendered at bottom of screen.
    /// Shows message history, channel tabs, and input field.
    /// </summary>
    public class ChatPanel : MonoBehaviour
    {
        private ChatService _service;
        private GameObject _panelRoot;
        private Text _messagesText;
        private InputField _inputField;
        private Transform _tabRoot;
        private Font _font;
        private bool _isOpen = false;
        private float _lastRefresh;

        public bool IsOpen => _isOpen;

        public void Initialize(ChatService service)
        {
            _service = service;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 14);
            BuildUI();
            _service.OnMessageReceived += msg => Refresh();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            if (_panelRoot != null)
                _panelRoot.SetActive(_isOpen);
        }

        private void Update()
        {
            if (!_isOpen || _service == null) return;
            // Auto-refresh every 2 seconds
            if (Time.time - _lastRefresh > 2f)
            {
                Refresh();
                _lastRefresh = Time.time;
            }
        }

        public void Refresh()
        {
            if (_messagesText == null || _service == null) return;
            var messages = _service.GetFilteredMessages(30);
            string display = "";
            foreach (var msg in messages)
            {
                if (!string.IsNullOrEmpty(msg.senderName))
                    display += $"<color=#{ColorUtility.ToHtmlStringRGBA(msg.color)}>{msg.senderName}: {msg.text}</color>\n";
                else
                    display += $"<color=#{ColorUtility.ToHtmlStringRGBA(msg.color)}>{msg.text}</color>\n";
            }
            _messagesText.text = display;
        }

        private void SendInput()
        {
            if (_inputField == null || _service == null) return;
            string text = _inputField.text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            _service.SendPlayerMessage(_service.ActiveChannel, "Người chơi", text);
            _inputField.text = "";
        }

        private void BuildUI()
        {
            try
            {
                BuildUIInternal();
                if (_panelRoot != null)
                    _panelRoot.SetActive(_isOpen);
            }
            catch (Exception ex)
            {
                SubsystemLog.Warn("ChatPanel", $"BuildUI failed: {ex.Message}");
            }
        }

        private void BuildUIInternal()
        {
            // Host RectTransform phải full-stretch, nếu không _panelRoot (neo 0–0.45×0–0.35)
            // sẽ resolve theo host 0-size ở tâm canvas → mọi panel sụp về giữa màn (ngay trên player).
            var hostRt = GetComponent<RectTransform>();
            if (hostRt == null)
                hostRt = gameObject.AddComponent<RectTransform>();
            hostRt.anchorMin = Vector2.zero;
            hostRt.anchorMax = Vector2.one;
            hostRt.offsetMin = Vector2.zero;
            hostRt.offsetMax = Vector2.zero;

            _panelRoot = new GameObject("ChatPanel");
            _panelRoot.transform.SetParent(transform, false);

            var mainRt = _panelRoot.AddComponent<RectTransform>();
            mainRt.anchorMin = new Vector2(0f, 0f);
            mainRt.anchorMax = new Vector2(0.45f, 0.35f);
            mainRt.offsetMin = new Vector2(8f, 8f);
            mainRt.offsetMax = new Vector2(-8f, -8f);

            // Semi-transparent background. raycastTarget=false để touch xuyên qua xuống
            // joystick (chat panel ở góc dưới-trái trùng chỗ joystick). Tab/input vẫn
            // có raycast riêng nên vẫn bấm được.
            var bg = _panelRoot.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.05f, 0.65f);
            bg.raycastTarget = false;

            // Tab bar
            var tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(_panelRoot.transform, false);
            var tabRt = tabBar.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0f, 0.88f);
            tabRt.anchorMax = new Vector2(1f, 1f);
            _tabRoot = tabBar.transform;
            var hLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.LowerLeft;
            hLayout.spacing = 2f;
            hLayout.padding = new RectOffset(4, 4, 2, 2);
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;

            // Channel tabs
            foreach (ChatChannel ch in new[] { ChatChannel.All, ChatChannel.Private, ChatChannel.Room, ChatChannel.Guild, ChatChannel.Faction, ChatChannel.Other })
            {
                var tabGo = new GameObject($"Tab_{ch}");
                tabGo.transform.SetParent(_tabRoot, false);
                var tabImg = tabGo.AddComponent<Image>();
                tabImg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
                var tabBtn = tabGo.AddComponent<Button>();
                tabBtn.targetGraphic = tabImg;
                var channel = ch;
                tabBtn.onClick.AddListener(() => { _service.SetChannel(channel); Refresh(); });
                var tabTxt = tabGo.AddComponent<Text>();
                tabTxt.text = ChatService.ChannelNameVi(ch);
                tabTxt.font = _font;
                tabTxt.fontSize = 16;
                tabTxt.color = ChatService.ChannelColor(ch);
                tabTxt.alignment = TextAnchor.MiddleCenter;
                var le = tabGo.AddComponent<LayoutElement>();
                le.minWidth = 55f;
                le.minHeight = 22f;
            }

            // Messages area
            var msgGo = new GameObject("Messages");
            msgGo.transform.SetParent(_panelRoot.transform, false);
            var msgRt = msgGo.AddComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0.02f, 0.14f);
            msgRt.anchorMax = new Vector2(0.98f, 0.87f);
            _messagesText = msgGo.AddComponent<Text>();
            _messagesText.font = _font;
            _messagesText.fontSize = 18;
            _messagesText.color = Color.white;
            _messagesText.alignment = TextAnchor.LowerLeft;
            _messagesText.verticalOverflow = VerticalWrapMode.Overflow;
            _messagesText.supportRichText = true;

            // Input area
            var inputBar = new GameObject("InputBar");
            inputBar.transform.SetParent(_panelRoot.transform, false);
            var inputRt = inputBar.AddComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0f, 0f);
            inputRt.anchorMax = new Vector2(1f, 0.12f);
            var inputBg = inputBar.AddComponent<Image>();
            inputBg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            var inputField = new GameObject("InputField");
            inputField.transform.SetParent(inputBar.transform, false);
            var ifRt = inputField.AddComponent<RectTransform>();
            ifRt.anchorMin = new Vector2(0.01f, 0.05f);
            ifRt.anchorMax = new Vector2(0.82f, 0.95f);
            _inputField = inputField.AddComponent<InputField>();
            var ifText = inputField.AddComponent<Text>();
            ifText.font = _font;
            ifText.fontSize = 20;
            ifText.color = Color.white;
            ifText.alignment = TextAnchor.MiddleLeft;
            _inputField.textComponent = ifText;
            _inputField.placeholder = CreatePlaceholder(inputField.transform, "Nhập tin nhắn...");
            _inputField.onEndEdit.AddListener(text => { if (Input.GetKeyDown(KeyCode.Return)) SendInput(); });

            // Send button
            var sendBtn = new GameObject("SendBtn");
            sendBtn.transform.SetParent(inputBar.transform, false);
            var sbRt = sendBtn.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.83f, 0.05f);
            sbRt.anchorMax = new Vector2(0.99f, 0.95f);
            var sbImg = sendBtn.AddComponent<Image>();
            sbImg.color = new Color(0.15f, 0.35f, 0.65f, 0.9f);
            var sbBtn = sendBtn.AddComponent<Button>();
            sbBtn.targetGraphic = sbImg;
            sbBtn.onClick.AddListener(SendInput);
            var sbTxt = sendBtn.AddComponent<Text>();
            sbTxt.text = "Gửi";
            sbTxt.font = _font;
            sbTxt.fontSize = 20;
            sbTxt.color = Color.white;
            sbTxt.alignment = TextAnchor.MiddleCenter;
        }

        private Text CreatePlaceholder(Transform parent, string text)
        {
            var go = new GameObject("Placeholder");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = _font;
            txt.fontSize = 18;
            txt.fontStyle = FontStyle.Italic;
            txt.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            txt.alignment = TextAnchor.MiddleLeft;
            return txt;
        }
    }
}
