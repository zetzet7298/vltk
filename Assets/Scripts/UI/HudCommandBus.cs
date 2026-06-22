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
    /// Phase 2 skill panel command contract. Skill adapter publishes page switch,
    /// skill selection, upgrade, and close intents through this bus.
    /// </summary>
    public interface ISkillCommandBus
    {
        event Action<int> OnSkillPageChanged;
        event Action<int> OnSkillSelected;
        event Action<int> OnSkillUpgradeRequested;
        event Action OnSkillCloseRequested;

        void PublishSkillPageChanged(int pageIndex);
        void PublishSkillSelected(int skillId);
        void PublishSkillUpgradeRequested(int skillId);
        void PublishSkillCloseRequested();
    }

    /// <summary>
    /// Default in-process bus. The same instance is shared by adapters and the
    /// GameHudController; controllers wire subscriptions during OnEnable and
    /// unsubscribe during OnDisable so reloads do not leak handlers.
    /// </summary>
    public sealed class HudCommandBus : IHudCommandBus, IChatCommandBus, ISkillCommandBus
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

        public event Action<int> OnSkillPageChanged;
        public event Action<int> OnSkillSelected;
        public event Action<int> OnSkillUpgradeRequested;
        public event Action OnSkillCloseRequested;

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

        public void PublishSkillPageChanged(int pageIndex) => OnSkillPageChanged?.Invoke(pageIndex);
        public void PublishSkillSelected(int skillId) => OnSkillSelected?.Invoke(skillId);
        public void PublishSkillUpgradeRequested(int skillId) => OnSkillUpgradeRequested?.Invoke(skillId);
        public void PublishSkillCloseRequested() => OnSkillCloseRequested?.Invoke();

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
            OnSkillPageChanged = null;
            OnSkillSelected = null;
            OnSkillUpgradeRequested = null;
            OnSkillCloseRequested = null;
        }
    }
}
