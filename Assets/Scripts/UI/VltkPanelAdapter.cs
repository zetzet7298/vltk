// -----------------------------------------------------------------------------
// VLTK Mobile — NpcDialog + Faction + Guild + Mail + Shop + Login vltkunity adapter
// Phase 2 Commit 2f bundle. Port of vltkunity's NpcDialog.cs (dialogue choices),
// Guild.cs (guild panel), and the shared pattern for Faction/Mail/Shop/Login
// panels. Each panel uses the same core: a title, a scrollable content area,
// action buttons, and a close button. This adapter provides a single reusable
// pure-C# class that serves all six panel types.
//
// vltkunity source mapping:
//   NpcDialog.textNpcName + textNpcContent   → Title + content labels
//   NpcDialog.ListActions (dynamic buttons)   → Action button list
//   NpcDialog.AddButtonCanncel ("Tạm biệt")   → Close intent
//   NpcDialog.ShowListAction(data split)       → SetActions(string[])
//   Guild.CLose()                               → Close intent
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VLTK.UI
{
    /// <summary>
    /// Identifies which panel a particular adapter instance drives.
    /// Mirrors vltkunity's separate GameObject panels.
    /// </summary>
    public enum PanelType
    {
        NpcDialog = 0,
        Faction = 1,
        Guild = 2,
        Mail = 3,
        Shop = 4,
        Login = 5,
    }

    /// <summary>
    /// Reusable UI Toolkit adapter for the six lightweight vltkunity panels.
    /// Pure C# (no MonoBehaviour) so EditMode tests can construct it directly.
    /// </summary>
    public sealed class VltkPanelAdapter : IDisposable
    {
        private readonly VisualElement _root;
        private readonly IPanelsCommandBus _bus;
        private readonly PanelType _panelType;

        private Label _titleLabel;
        private Label _contentLabel;
        private VisualElement _actionList;
        private VisualElement _closeBtn;

        private readonly List<string> _actions = new();
        private int _renderCount;

        public int RenderCount => _renderCount;
        public PanelType PanelType => _panelType;
        public int ActionCount => _actions.Count;

        public VltkPanelAdapter(VisualElement root, IPanelsCommandBus bus, PanelType panelType)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _panelType = panelType;
        }

        public void Bind()
        {
            CacheElements();
            WireCloseButton();
        }

        private string Prefix => _panelType switch
        {
            PanelType.NpcDialog => "VltkNpc",
            PanelType.Faction => "VltkFaction",
            PanelType.Guild => "VltkGuild",
            PanelType.Mail => "VltkMail",
            PanelType.Shop => "VltkShop",
            PanelType.Login => "VltkLogin",
            _ => "VltkPanel",
        };

        private void CacheElements()
        {
            if (_root == null) return;
            _titleLabel = FindByName(Prefix + "Title") as Label;
            _contentLabel = FindByName(Prefix + "Content") as Label;
            _actionList = FindByName(Prefix + "ActionList");
            _closeBtn = FindByName(Prefix + "CloseBtn");
        }

        private void WireCloseButton()
        {
            var bus = _bus;
            if (bus == null || _closeBtn == null) return;
            _closeBtn.pickingMode = PickingMode.Position;
            _closeBtn.RegisterCallback<ClickEvent>(_ => bus.PublishPanelClosed(_panelType));
        }

        /// <summary>Set the panel title text.</summary>
        public void SetTitle(string text)
        {
            _renderCount++;
            if (_titleLabel != null) _titleLabel.text = text ?? string.Empty;
        }

        /// <summary>Set the panel content text.</summary>
        public void SetContent(string text)
        {
            _renderCount++;
            if (_contentLabel != null) _contentLabel.text = text ?? string.Empty;
        }

        /// <summary>Set the list of action buttons. Mirrors vltkunity NpcDialog.ShowListAction.</summary>
        public void SetActions(IReadOnlyList<string> actions)
        {
            _renderCount++;
            _actions.Clear();
            if (_actionList == null) return;
            _actionList.Clear();

            if (actions == null) return;
            foreach (var action in actions)
            {
                _actions.Add(action);
                var btn = new Button { text = action };
                btn.style.marginBottom = 4;
                btn.style.height = 36;
                var captured = action;
                btn.RegisterCallback<ClickEvent>(_ => _bus.PublishPanelActionSelected(_panelType, captured));
                _actionList.Add(btn);
            }
        }

        public void SimulateCloseClick()
        {
            _bus.PublishPanelClosed(_panelType);
        }

        public void SimulateActionClick(int index)
        {
            if (index < 0 || index >= _actions.Count) return;
            _bus.PublishPanelActionSelected(_panelType, _actions[index]);
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
