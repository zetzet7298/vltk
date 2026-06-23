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

        /// <summary>Currency recharge/shop intent (recon §3d, Y4).</summary>
        event Action<CurrencyType> OnRechargeRequested;

        void PublishProfileRequested();
        void PublishScreenshotRequested();
        void PublishMinimapMarkerRequested();
        void PublishToggleMapSizeRequested();
        void PublishWorldMapRequested();
        void PublishCaveMapRequested();
        void PublishRechargeRequested(CurrencyType type);
    }

    /// <summary>Currency kinds surfaced by the Money widget (recon §3b).</summary>
    public enum CurrencyType
    {
        /// <summary>Đồng tiền (tongqian / copper).</summary>
        Copper,
        /// <summary>Vàng (jinbi / gold).</summary>
        Gold,
        /// <summary>Bạc (yinliang / silver).</summary>
        Silver
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
    /// Phase 2 equipment panel command contract. Equipment adapter publishes tab
    /// changes, attribute increments, and close intents through this bus.
    /// </summary>
    public interface IEquipmentCommandBus
    {
        event Action<int> OnEquipmentTabChanged;
        event Action<string> OnAttributeIncrementRequested;
        event Action OnEquipmentCloseRequested;

        void PublishEquipmentTabChanged(int tabIndex);
        void PublishAttributeIncrementRequested(string attributeName);
        void PublishEquipmentCloseRequested();
    }

    /// <summary>
    /// Phase 2 bag/inventory panel command contract.
    /// </summary>
    public interface IBagCommandBus
    {
        event Action<int> OnBagTabChanged;
        event Action<int> OnItemSelected;
        event Action OnBagCloseRequested;

        void PublishBagTabChanged(int tabIndex);
        void PublishItemSelected(int slotIndex);
        void PublishBagCloseRequested();
    }

    /// <summary>
    /// Phase 2 panels command contract for the six lightweight panels
    /// (NpcDialog, Faction, Guild, Mail, Shop, Login).
    /// </summary>
    public interface IPanelsCommandBus
    {
        event Action<PanelType> OnPanelClosed;
        event Action<PanelType, string> OnPanelActionSelected;

        void PublishPanelClosed(PanelType panelType);
        void PublishPanelActionSelected(PanelType panelType, string action);
    }

    /// <summary>
    /// Default in-process bus. The same instance is shared by adapters and the
    /// GameHudController; controllers wire subscriptions during OnEnable and
    /// unsubscribe during OnDisable so reloads do not leak handlers.
    /// </summary>
    public sealed class HudCommandBus : IHudCommandBus, IChatCommandBus, ISkillCommandBus, IEquipmentCommandBus, IBagCommandBus, IPanelsCommandBus
    {
        public event Action OnProfileRequested;
        public event Action OnScreenshotRequested;
        public event Action OnMinimapMarkerRequested;
        public event Action OnToggleMapSizeRequested;
        public event Action OnWorldMapRequested;
        public event Action OnCaveMapRequested;
        public event Action<CurrencyType> OnRechargeRequested;

        public event Action OnChatOpenRequested;
        public event Action OnChatCloseRequested;
        public event Action<string> OnChatSendRequested;
        public event Action<int> OnChatCategoryChanged;

        public event Action<int> OnSkillPageChanged;
        public event Action<int> OnSkillSelected;
        public event Action<int> OnSkillUpgradeRequested;
        public event Action OnSkillCloseRequested;

        public event Action<int> OnEquipmentTabChanged;
        public event Action<string> OnAttributeIncrementRequested;
        public event Action OnEquipmentCloseRequested;

        public event Action<int> OnBagTabChanged;
        public event Action<int> OnItemSelected;
        public event Action OnBagCloseRequested;

        public event Action<PanelType> OnPanelClosed;
        public event Action<PanelType, string> OnPanelActionSelected;

        public void PublishProfileRequested() => OnProfileRequested?.Invoke();
        public void PublishScreenshotRequested() => OnScreenshotRequested?.Invoke();
        public void PublishMinimapMarkerRequested() => OnMinimapMarkerRequested?.Invoke();
        public void PublishToggleMapSizeRequested() => OnToggleMapSizeRequested?.Invoke();
        public void PublishWorldMapRequested() => OnWorldMapRequested?.Invoke();
        public void PublishCaveMapRequested() => OnCaveMapRequested?.Invoke();
        public void PublishRechargeRequested(CurrencyType type) => OnRechargeRequested?.Invoke(type);

        public void PublishChatOpenRequested() => OnChatOpenRequested?.Invoke();
        public void PublishChatCloseRequested() => OnChatCloseRequested?.Invoke();
        public void PublishChatSendRequested(string message) => OnChatSendRequested?.Invoke(message);
        public void PublishChatCategoryChanged(int categoryId) => OnChatCategoryChanged?.Invoke(categoryId);

        public void PublishSkillPageChanged(int pageIndex) => OnSkillPageChanged?.Invoke(pageIndex);
        public void PublishSkillSelected(int skillId) => OnSkillSelected?.Invoke(skillId);
        public void PublishSkillUpgradeRequested(int skillId) => OnSkillUpgradeRequested?.Invoke(skillId);
        public void PublishSkillCloseRequested() => OnSkillCloseRequested?.Invoke();

        public void PublishEquipmentTabChanged(int tabIndex) => OnEquipmentTabChanged?.Invoke(tabIndex);
        public void PublishAttributeIncrementRequested(string attributeName) => OnAttributeIncrementRequested?.Invoke(attributeName);
        public void PublishEquipmentCloseRequested() => OnEquipmentCloseRequested?.Invoke();

        public void PublishBagTabChanged(int tabIndex) => OnBagTabChanged?.Invoke(tabIndex);
        public void PublishItemSelected(int slotIndex) => OnItemSelected?.Invoke(slotIndex);
        public void PublishBagCloseRequested() => OnBagCloseRequested?.Invoke();

        public void PublishPanelClosed(PanelType panelType) => OnPanelClosed?.Invoke(panelType);
        public void PublishPanelActionSelected(PanelType panelType, string action) => OnPanelActionSelected?.Invoke(panelType, action);

        public void ClearAllSubscribers()
        {
            OnProfileRequested = null;
            OnScreenshotRequested = null;
            OnMinimapMarkerRequested = null;
            OnToggleMapSizeRequested = null;
            OnWorldMapRequested = null;
            OnCaveMapRequested = null;
            OnRechargeRequested = null;
            OnChatOpenRequested = null;
            OnChatCloseRequested = null;
            OnChatSendRequested = null;
            OnChatCategoryChanged = null;
            OnSkillPageChanged = null;
            OnSkillSelected = null;
            OnSkillUpgradeRequested = null;
            OnSkillCloseRequested = null;
            OnEquipmentTabChanged = null;
            OnAttributeIncrementRequested = null;
            OnEquipmentCloseRequested = null;
            OnBagTabChanged = null;
            OnItemSelected = null;
            OnBagCloseRequested = null;
            OnPanelClosed = null;
            OnPanelActionSelected = null;
        }
    }
}
