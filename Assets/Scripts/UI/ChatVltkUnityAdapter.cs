// -----------------------------------------------------------------------------
// VLTK Mobile — Chat vltkunity adapter
// Phase 2 port of vltkunity's Chat.cs + OpenChat.cs. Renders chat messages,
// category tabs, and send/close intents through UI Toolkit. Subscribes to the
// existing ChatService (Phase 1 of vltk-mobile) for message history and channel
// changes. Publishes open/close/send/category commands via IChatCommandBus so
// the controller can route them without a MainCanvas.instance singleton.
//
// vltkunity source mapping:
//   Chat.cs Tab buttons (All/Guild/Group)     → Category tab strip
//   Chat.cs ChatResize toggle                 → Panel resize intent
//   OpenChat.cs categories (7 channels)        → Category list
//   OpenChat.cs ChatInput → Send               → PublishChatSendRequested
//   MessageIN/MessageOut/SystemMessage         → Rendered as styled labels
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// UI Toolkit adapter for the chat panel. Pure C# (no MonoBehaviour) so
    /// EditMode tests can construct it directly with a synthetic VisualElement.
    /// </summary>
    public sealed class ChatVltkUnityAdapter : IDisposable
    {
        private readonly VisualElement _root;
        private readonly ChatService _service;
        private readonly IChatCommandBus _bus;

        private VisualElement _messageContainer;
        private VisualElement _categoryStrip;
        private Label _inputPlaceholder;
        private VisualElement _sendBtn;
        private VisualElement _closeBtn;
        private VisualElement _resizeBtn;

        private readonly Dictionary<int, VisualElement> _categoryButtons = new();
        private int _selectedCategoryId;
        private bool _isFullSize;
        private bool _subscribed;

        public int RenderCount { get; private set; }
        public int ActiveCategoryId => _selectedCategoryId;
        public bool IsFullSize => _isFullSize;

        public ChatVltkUnityAdapter(VisualElement root, ChatService service, IChatCommandBus bus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public void Bind()
        {
            CacheElements();
            WireButtons();
            Subscribe();
            RenderMessages();
        }

        private void CacheElements()
        {
            if (_root == null) return;
            _messageContainer = FindByName("VltkChatMessageList");
            _categoryStrip = FindByName("VltkChatCategoryStrip");
            _inputPlaceholder = FindByName("VltkChatInputPlaceholder") as Label;
            _sendBtn = FindByName("VltkChatSendBtn");
            _closeBtn = FindByName("VltkChatCloseBtn");
            _resizeBtn = FindByName("VltkChatResizeBtn");
        }

        private void WireButtons()
        {
            var bus = _bus;
            if (bus == null) return;
            _closeClick = bus.PublishChatCloseRequested;
            _resizeClick = ToggleResize;
            RegisterClick(_closeBtn, _closeClick);
            RegisterClick(_resizeBtn, _resizeClick);
            // Send button signals the controller to read the input field and send.
            // The actual message text is delivered via SendMessage(string).
            RegisterClick(_sendBtn, () => bus.PublishChatSendRequested(null));
        }

        private System.Action _closeClick;
        private System.Action _resizeClick;

        public void SimulateSendClick() => _bus?.PublishChatSendRequested(null);
        public void SimulateCloseClick() => _closeClick?.Invoke();
        public void SimulateResizeClick() => _resizeClick?.Invoke();

        private void ToggleResize()
        {
            _isFullSize = !_isFullSize;
        }

        private void RegisterClick(VisualElement target, Action handler)
        {
            if (target == null || handler == null) return;
            target.pickingMode = PickingMode.Position;
            target.RegisterCallback<ClickEvent>(_ => handler());
        }

        private void Subscribe()
        {
            if (_subscribed || _service == null) return;
            _service.OnMessageReceived += OnMessageReceived;
            _service.OnChannelChanged += OnChannelChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _service == null) return;
            _service.OnMessageReceived -= OnMessageReceived;
            _service.OnChannelChanged -= OnChannelChanged;
            _subscribed = false;
        }

        private void OnMessageReceived(ChatMessage msg) => RenderMessages();
        private void OnChannelChanged(ChatChannel channel) => RenderMessages();

        /// <summary>Add a category tab. categoryId maps to vltkunity PlayerChat enum.</summary>
        public void RegisterCategory(int categoryId, string labelVi, VisualElement button)
        {
            if (button == null) return;
            _categoryButtons[categoryId] = button;
            button.pickingMode = PickingMode.Position;
            var id = categoryId;
            button.RegisterCallback<ClickEvent>(_ => SelectCategory(id));
        }

        /// <summary>Switch the active category and notify via bus.</summary>
        public void SelectCategory(int categoryId)
        {
            _selectedCategoryId = categoryId;
            _bus.PublishChatCategoryChanged(categoryId);
            RenderMessages();
        }

        /// <summary>Send a message via the bus (controller routes to ChatService).</summary>
        public void SendMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            _bus.PublishChatSendRequested(text);
        }

        /// <summary>Re-render messages from ChatService history into the container.</summary>
        public void RenderMessages()
        {
            RenderCount++;
            if (_messageContainer == null || _service == null) return;

            _messageContainer.Clear();
            var messages = _service.GetFilteredMessages(50);
            foreach (var msg in messages)
            {
                var label = new Label();
                string colorHex = UnityEngine.ColorUtility.ToHtmlStringRGBA(msg.color);
                string sender = string.IsNullOrEmpty(msg.senderName) ? "" : msg.senderName + ": ";
                label.text = $"<color=#{colorHex}>{sender}{msg.text}</color>";
                label.style.color = new UnityEngine.Color(1f, 1f, 1f, 0.95f);
                label.style.fontSize = 13;
                label.style.whiteSpace = WhiteSpace.Normal;
                _messageContainer.Add(label);
            }
        }

        private VisualElement FindByName(string name)
        {
            if (_root == null) return null;
            var queue = new Queue<VisualElement>();
            queue.Enqueue(_root);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.name == name) return current;
                int childCount = current.childCount;
                for (int i = 0; i < childCount; i++)
                    queue.Enqueue(current[i]);
            }
            return null;
        }

        public void Dispose() => Unsubscribe();
    }
}
