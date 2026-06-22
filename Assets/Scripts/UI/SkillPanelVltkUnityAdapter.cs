// -----------------------------------------------------------------------------
// VLTK Mobile — Skill panel vltkunity adapter
// Phase 2 port of vltkunity's PlayerSkills.cs + SkillItem.cs + SkillDetail.cs.
// Renders skill list rows + active skill slots through UI Toolkit. Uses the
// existing PcSkillPanelService to build snapshots from SkillCatalog + progression.
// Publishes skill intents (switch page, select skill, upgrade, close) via bus.
//
// vltkunity source mapping:
//   PlayerSkills.Skills list + childPrefab   → Skill list container
//   PlayerSkills.SkillActives (2 pages)       → Active slot page toggle
//   PlayerSkills.Switch()                      → Page toggle
//   SkillItem.SetUpSkillSetting(name+level)    → Row label
//   SkillDetail (icon + name + desc)           → Detail panel
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// UI Toolkit adapter for the skill panel. Pure C# (no MonoBehaviour) so
    /// EditMode tests can construct it directly with a synthetic VisualElement.
    /// </summary>
    public sealed class SkillPanelVltkUnityAdapter : IDisposable
    {
        private readonly VisualElement _root;
        private readonly ISkillCommandBus _bus;

        private VisualElement _skillListContainer;
        private VisualElement _detailPanel;
        private Label _detailName;
        private Label _detailLevel;
        private Label _detailDescription;
        private VisualElement _pageSwitchBtn;
        private VisualElement _closeBtn;
        private VisualElement _upgradeBtn;

        private PcSkillPanelSnapshot _snapshot;
        private int _selectedSkillId;
        private bool _isPageTwo;
        private bool _subscribed;

        public int RenderCount { get; private set; }
        public int SelectedSkillId => _selectedSkillId;
        public bool IsPageTwo => _isPageTwo;

        public SkillPanelVltkUnityAdapter(VisualElement root, ISkillCommandBus bus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public void Bind()
        {
            CacheElements();
            WireButtons();
        }

        private void CacheElements()
        {
            if (_root == null) return;
            _skillListContainer = FindByName("VltkSkillList");
            _detailPanel = FindByName("VltkSkillDetail");
            _detailName = FindByName("VltkSkillDetailName") as Label;
            _detailLevel = FindByName("VltkSkillDetailLevel") as Label;
            _detailDescription = FindByName("VltkSkillDetailDesc") as Label;
            _pageSwitchBtn = FindByName("VltkSkillPageSwitchBtn");
            _closeBtn = FindByName("VltkSkillCloseBtn");
            _upgradeBtn = FindByName("VltkSkillUpgradeBtn");
        }

        private void WireButtons()
        {
            var bus = _bus;
            if (bus == null) return;
            _pageSwitchClick = SwitchPage;
            _closeClick = bus.PublishSkillCloseRequested;
            _upgradeClick = () => bus.PublishSkillUpgradeRequested(_selectedSkillId);
            RegisterClick(_pageSwitchBtn, _pageSwitchClick);
            RegisterClick(_closeBtn, _closeClick);
            RegisterClick(_upgradeBtn, _upgradeClick);
        }

        private System.Action _pageSwitchClick;
        private System.Action _closeClick;
        private System.Action _upgradeClick;

        public void SimulatePageSwitchClick() => _pageSwitchClick?.Invoke();
        public void SimulateCloseClick() => _closeClick?.Invoke();
        public void SimulateUpgradeClick() => _upgradeClick?.Invoke();
        public void SimulateSelectSkill(int skillId) => SelectSkill(skillId);

        private void SwitchPage()
        {
            _isPageTwo = !_isPageTwo;
            _bus.PublishSkillPageChanged(_isPageTwo ? 1 : 0);
            RenderFromSnapshot();
        }

        private void RegisterClick(VisualElement target, Action handler)
        {
            if (target == null || handler == null) return;
            target.pickingMode = PickingMode.Position;
            target.RegisterCallback<ClickEvent>(_ => handler());
        }

        /// <summary>Apply a snapshot from PcSkillPanelService. Renders the skill list + detail.</summary>
        public void Apply(PcSkillPanelSnapshot snapshot)
        {
            _snapshot = snapshot;
            if (snapshot != null && snapshot.selectedSkillId > 0)
                _selectedSkillId = snapshot.selectedSkillId;
            RenderFromSnapshot();
        }

        private void RenderFromSnapshot()
        {
            RenderCount++;
            if (_skillListContainer == null || _snapshot?.rows == null) return;

            _skillListContainer.Clear();
            foreach (var row in _snapshot.rows)
            {
                var item = new VisualElement();
                item.style.flexDirection = FlexDirection.Row;
                item.style.marginBottom = 4;

                var nameLabel = new Label(row.displayName);
                nameLabel.style.fontSize = 13;
                nameLabel.style.color = new UnityEngine.Color(1f, 0.95f, 0.6f);
                nameLabel.style.flexGrow = 1;
                item.Add(nameLabel);

                var levelLabel = new Label($"{row.learnedLevel}/{row.maxLevel}");
                levelLabel.style.fontSize = 11;
                levelLabel.style.color = new UnityEngine.Color(0.8f, 0.8f, 0.8f);
                item.Add(levelLabel);

                var skillId = row.skillId;
                item.RegisterCallback<ClickEvent>(_ => SelectSkill(skillId));
                _skillListContainer.Add(item);
            }

            UpdateDetailPanel();
        }

        private void SelectSkill(int skillId)
        {
            _selectedSkillId = skillId;
            _bus.PublishSkillSelected(skillId);
            UpdateDetailPanel();
        }

        private void UpdateDetailPanel()
        {
            if (_detailPanel == null) return;
            PcSkillPanelRow? row = FindRow(_selectedSkillId);
            if (row == null) return;

            if (_detailName != null) _detailName.text = row.Value.displayName;
            if (_detailLevel != null) _detailLevel.text = $"{row.Value.learnedLevel} / {row.Value.maxLevel}";
            if (_detailDescription != null) _detailDescription.text = string.IsNullOrEmpty(row.Value.summary) ? row.Value.upgradeStatus : row.Value.summary;
        }

        private PcSkillPanelRow? FindRow(int skillId)
        {
            if (_snapshot?.rows == null) return null;
            foreach (var row in _snapshot.rows)
            {
                if (row.skillId == skillId)
                    return row;
            }
            return null;
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
