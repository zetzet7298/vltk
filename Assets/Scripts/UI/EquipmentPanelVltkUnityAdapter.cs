// -----------------------------------------------------------------------------
// VLTK Mobile — Equipment panel vltkunity adapter
// Phase 2 port of vltkunity's PanelUser.cs + PanelUserProperties.cs +
// PanelUserEquipment.cs. Renders character attributes (strength/vitality/
// dexterity/energy) + equipment slots + tab switching (Properties/Equipment/
// Items/Series) through UI Toolkit. Uses existing CharacterPanelService for
// data snapshots. Publishes tab switch, attribute increment, close via bus.
//
// vltkunity source mapping:
//   PanelUser tab buttons (equip/properties/series)  → Tab switch
//   PanelUserProperties (name/level/faction/stats)    → Properties tab
//   PanelUserEquipment (equipped slots)                → Equipment tab
//   PanelUserSeries (item series sets)                 → Series tab
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VLTK.UI
{
    /// <summary>
    /// UI Toolkit adapter for the character/equipment panel. Pure C# (no
    /// MonoBehaviour) so EditMode tests can construct it directly.
    /// </summary>
    public sealed class EquipmentPanelVltkUnityAdapter : IDisposable
    {
        public const int TabProperties = 0;
        public const int TabEquipment = 1;
        public const int TabItems = 2;
        public const int TabSeries = 3;

        private readonly VisualElement _root;
        private readonly IEquipmentCommandBus _bus;

        private VisualElement _propertiesTab;
        private VisualElement _equipmentTab;
        private VisualElement _itemsTab;
        private VisualElement _seriesTab;
        private VisualElement _tabBtnProperties;
        private VisualElement _tabBtnEquipment;
        private VisualElement _tabBtnItems;
        private VisualElement _tabBtnSeries;
        private VisualElement _closeBtn;
        private VisualElement _strengthAddBtn;
        private VisualElement _vitalityAddBtn;
        private VisualElement _dexterityAddBtn;
        private VisualElement _energyAddBtn;

        private Label _playerNameLabel;
        private Label _levelLabel;
        private Label _expLabel;
        private Label _hpLabel;
        private Label _mpLabel;
        private Label _staminaLabel;
        private VisualElement _statsContainer;

        private int _activeTab = TabProperties;

        public int RenderCount { get; private set; }
        public int ActiveTab => _activeTab;

        public EquipmentPanelVltkUnityAdapter(VisualElement root, IEquipmentCommandBus bus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public void Bind()
        {
            CacheElements();
            WireButtons();
            ShowTab(TabProperties);
        }

        private void CacheElements()
        {
            if (_root == null) return;
            _propertiesTab = FindByName("VltkEquipPropertiesTab");
            _equipmentTab = FindByName("VltkEquipEquipmentTab");
            _itemsTab = FindByName("VltkEquipItemsTab");
            _seriesTab = FindByName("VltkEquipSeriesTab");
            _tabBtnProperties = FindByName("VltkEquipTabPropertiesBtn");
            _tabBtnEquipment = FindByName("VltkEquipTabEquipmentBtn");
            _tabBtnItems = FindByName("VltkEquipTabItemsBtn");
            _tabBtnSeries = FindByName("VltkEquipTabSeriesBtn");
            _closeBtn = FindByName("VltkEquipCloseBtn");
            _strengthAddBtn = FindByName("VltkEquipStrengthAddBtn");
            _vitalityAddBtn = FindByName("VltkEquipVitalityAddBtn");
            _dexterityAddBtn = FindByName("VltkEquipDexterityAddBtn");
            _energyAddBtn = FindByName("VltkEquipEnergyAddBtn");

            _playerNameLabel = FindByName("VltkEquipPlayerName") as Label;
            _levelLabel = FindByName("VltkEquipLevel") as Label;
            _expLabel = FindByName("VltkEquipExp") as Label;
            _hpLabel = FindByName("VltkEquipHp") as Label;
            _mpLabel = FindByName("VltkEquipMp") as Label;
            _staminaLabel = FindByName("VltkEquipStamina") as Label;
            _statsContainer = FindByName("VltkEquipStatsList");
        }

        private void WireButtons()
        {
            var bus = _bus;
            if (bus == null) return;
            _closeClick = bus.PublishEquipmentCloseRequested;
            _strengthClick = () => bus.PublishAttributeIncrementRequested("Strength");
            _vitalityClick = () => bus.PublishAttributeIncrementRequested("Vitality");
            _dexterityClick = () => bus.PublishAttributeIncrementRequested("Dexterity");
            _energyClick = () => bus.PublishAttributeIncrementRequested("Energy");

            RegisterClick(_closeBtn, _closeClick);
            RegisterClick(_strengthAddBtn, _strengthClick);
            RegisterClick(_vitalityAddBtn, _vitalityClick);
            RegisterClick(_dexterityAddBtn, _dexterityClick);
            RegisterClick(_energyAddBtn, _energyClick);

            RegisterClick(_tabBtnProperties, () => ShowTab(TabProperties));
            RegisterClick(_tabBtnEquipment, () => ShowTab(TabEquipment));
            RegisterClick(_tabBtnItems, () => ShowTab(TabItems));
            RegisterClick(_tabBtnSeries, () => ShowTab(TabSeries));
        }

        private System.Action _closeClick;
        private System.Action _strengthClick, _vitalityClick, _dexterityClick, _energyClick;

        public void SimulateCloseClick() => _closeClick?.Invoke();
        public void SimulateStrengthAdd() => _strengthClick?.Invoke();
        public void SimulateVitalityAdd() => _vitalityClick?.Invoke();
        public void SimulateDexterityAdd() => _dexterityClick?.Invoke();
        public void SimulateEnergyAdd() => _energyClick?.Invoke();
        public void SimulateTabSwitch(int tabIndex) => ShowTab(tabIndex);

        private void ShowTab(int tabIndex)
        {
            _activeTab = tabIndex;
            _bus.PublishEquipmentTabChanged(tabIndex);
            SetVisible(_propertiesTab, tabIndex == TabProperties);
            SetVisible(_equipmentTab, tabIndex == TabEquipment);
            SetVisible(_itemsTab, tabIndex == TabItems);
            SetVisible(_seriesTab, tabIndex == TabSeries);
        }

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (el == null) return;
            el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RegisterClick(VisualElement target, Action handler)
        {
            if (target == null || handler == null) return;
            target.pickingMode = PickingMode.Position;
            target.RegisterCallback<ClickEvent>(_ => handler());
        }

        /// <summary>Apply a CharacterPanelSnapshot to populate the properties tab.</summary>
        public void Apply(CharacterPanelSnapshot snapshot)
        {
            RenderCount++;
            if (snapshot == null) return;

            if (_playerNameLabel != null) _playerNameLabel.text = snapshot.playerName ?? "";
            if (_levelLabel != null) _levelLabel.text = snapshot.level.ToString();
            if (_expLabel != null) _expLabel.text = $"{snapshot.exp}/{snapshot.expMax}";
            if (_hpLabel != null) _hpLabel.text = $"{snapshot.hp}/{snapshot.hpMax}";
            if (_mpLabel != null) _mpLabel.text = $"{snapshot.mp}/{snapshot.mpMax}";
            if (_staminaLabel != null) _staminaLabel.text = $"{snapshot.stamina}/{snapshot.staminaMax}";

            if (_statsContainer != null && snapshot.rows != null)
            {
                _statsContainer.Clear();
                foreach (var row in snapshot.rows)
                {
                    var statRow = new VisualElement();
                    statRow.style.flexDirection = FlexDirection.Row;
                    statRow.style.marginBottom = 3;

                    var name = new Label(row.statName);
                    name.style.fontSize = 12;
                    name.style.color = new UnityEngine.Color(0.85f, 0.85f, 0.85f);
                    name.style.flexGrow = 1;
                    statRow.Add(name);

                    var val = new Label($"{row.baseValue} + {row.equipBonus + row.buffBonus} = {row.totalValue}");
                    val.style.fontSize = 12;
                    val.style.color = new UnityEngine.Color(1f, 0.95f, 0.6f);
                    statRow.Add(val);

                    _statsContainer.Add(statRow);
                }
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

        public void Dispose()
        {
        }
    }
}
