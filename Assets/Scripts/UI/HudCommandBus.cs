// -----------------------------------------------------------------------------
// VLTK Mobile — HUD command bus
// Lightweight event aggregator replacing vltkunity's MainCanvas.instance singleton
// coupling. UI panels/adapters publish semantic commands; controllers subscribe.
// This avoids MonoBehaviour singletons leaking across scene reloads and keeps
// unit tests free of singleton wiring.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.UI
{
    /// <summary>
    /// Phase 1 vltkunity port contract. Each command corresponds to a single
    /// user-facing action surfaced by a HUD widget. Adapters call the publish
    /// methods; controllers subscribe to the events. Adding new commands is
    /// additive; do not repurpose existing ones (callers cache handler identity).
    /// </summary>
    public interface IHudCommandBus
    {
        event Action OnProfileRequested;
        event Action OnScreenshotRequested;
        event Action OnMinimapMarkerRequested;
        event Action OnToggleMapSizeRequested;
        event Action OnWorldMapRequested;
        event Action OnCaveMapRequested;

        void PublishProfileRequested();
        void PublishScreenshotRequested();
        void PublishMinimapMarkerRequested();
        void PublishToggleMapSizeRequested();
        void PublishWorldMapRequested();
        void PublishCaveMapRequested();
    }

    /// <summary>
    /// Phase 2 chat command contract. The chat adapter publishes open/close/send
    /// intents and category changes; the controller subscribes and routes them to
    /// ChatService. Category IDs match vltkunity's PlayerChat enum order.
    /// </summary>
    public interface IChatCommandBus
    {
        event Action OnChatOpenRequested;
        event Action OnChatCloseRequested;
        event Action<string> OnChatSendRequested;
        event Action<int> OnChatCategoryChanged;

        void PublishChatOpenRequested();
        void PublishChatCloseRequested();
        void PublishChatSendRequested(string message);
        void PublishChatCategoryChanged(int categoryId);
    }

    /// <summary>
    /// Default in-process bus. The same instance is shared by adapters and the
    /// GameHudController; controllers wire subscriptions during OnEnable and
    /// unsubscribe during OnDisable so reloads do not leak handlers.
    /// </summary>
    public sealed class HudCommandBus : IHudCommandBus, IChatCommandBus
    {
        public event Action OnProfileRequested;
        public event Action OnScreenshotRequested;
        public event Action OnMinimapMarkerRequested;
        public event Action OnToggleMapSizeRequested;
        public event Action OnWorldMapRequested;
        public event Action OnCaveMapRequested;

        public event Action OnChatOpenRequested;
        public event Action OnChatCloseRequested;
        public event Action<string> OnChatSendRequested;
        public event Action<int> OnChatCategoryChanged;

        public void PublishProfileRequested() => OnProfileRequested?.Invoke();
        public void PublishScreenshotRequested() => OnScreenshotRequested?.Invoke();
        public void PublishMinimapMarkerRequested() => OnMinimapMarkerRequested?.Invoke();
        public void PublishToggleMapSizeRequested() => OnToggleMapSizeRequested?.Invoke();
        public void PublishWorldMapRequested() => OnWorldMapRequested?.Invoke();
        public void PublishCaveMapRequested() => OnCaveMapRequested?.Invoke();

        public void PublishChatOpenRequested() => OnChatOpenRequested?.Invoke();
        public void PublishChatCloseRequested() => OnChatCloseRequested?.Invoke();
        public void PublishChatSendRequested(string message) => OnChatSendRequested?.Invoke(message);
        public void PublishChatCategoryChanged(int categoryId) => OnChatCategoryChanged?.Invoke(categoryId);

        public void ClearAllSubscribers()
        {
            OnProfileRequested = null;
            OnScreenshotRequested = null;
            OnMinimapMarkerRequested = null;
            OnToggleMapSizeRequested = null;
            OnWorldMapRequested = null;
            OnCaveMapRequested = null;
            OnChatOpenRequested = null;
            OnChatCloseRequested = null;
            OnChatSendRequested = null;
            OnChatCategoryChanged = null;
        }
    }
}
