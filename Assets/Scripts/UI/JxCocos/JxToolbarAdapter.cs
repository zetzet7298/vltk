// -----------------------------------------------------------------------------
// VLTK Mobile — JX Toolbar rendering adapter (UI Toolkit)
//
// Nguồn: KgameWorldVN.cpp toolbar. 9 nút menu CCMenuItemSprite (normal/selected/
// disabled) + callback mở panel. Adapter gắn VisualElement buttons, click →
// toggle qua command bus, render selected-state highlight.
//
// Thuần C# (không MonoBehaviour) — EditMode-testable. Mỗi nút là VisualElement
// tên "jx_btn_<panel>"; USS đổi background theo .jx-btn-selected.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VLTK.UI.JxCocos
{
    /// <summary>
    /// UI Toolkit adapter for the jx-cocos toolbar. Binds 9 menu buttons from a
    /// VisualElement tree, wires click → toggle through a command bus, and
    /// reflects the open-panel selected state as a USS class toggle.
    /// </summary>
    public sealed class JxToolbarAdapter
    {
        private readonly VisualElement _root;
        private readonly JxToolbarState _state;
        private readonly IJxHudCommandBus _bus;

        private readonly Dictionary<JxHudPanel, VisualElement> _buttons = new();

        /// <summary>USS class toggled on the selected/open-panel button.</summary>
        public const string SelectedClass = "jx-btn-selected";

        /// <summary>Element name for a menu button: "jx_btn_" + lowercase panel.</summary>
        public static string ButtonName(JxHudPanel panel) => "jx_btn_" + panel.ToString().ToLowerInvariant();

        public JxToolbarAdapter(VisualElement root, JxToolbarState state, IJxHudCommandBus bus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>Discover the 9 buttons in the tree and register click handlers.</summary>
        public void Bind()
        {
            _buttons.Clear();
            for (int i = 0; i < JxToolbarConfig.Menu.Length; i++)
            {
                var cfg = JxToolbarConfig.Menu[i];
                var btn = Find(ButtonName(cfg.Panel));
                if (btn != null)
                {
                    var panel = cfg.Panel; // capture
                    btn.RegisterCallback<ClickEvent>(_ => OnButtonClicked(panel));
                    _buttons[cfg.Panel] = btn;
                }
            }
            Render();
        }

        private void OnButtonClicked(JxHudPanel panel) => Click(panel);

        /// <summary>
        /// Coordinator for a menu button activation. Toggles local state for snappy
        /// selected-state feedback, publishes the open/close intent through the
        /// command bus (controller + state decide), and re-renders highlights.
        /// Public so the event callback, a keyboard shortcut, and tests all share
        /// one path (UI Toolkit <see cref="SendEvent"/> requires a live panel,
        /// which a synthetic EditMode tree does not have).
        /// </summary>
        public void Click(JxHudPanel panel)
        {
            if (panel == JxHudPanel.None) return;
            _state.Toggle(panel);
            _bus.PublishPanelRequested(panel);
            Render();
        }

        /// <summary>Reflect open-panel state onto button highlight classes.</summary>
        public void Render()
        {
            foreach (var kv in _buttons)
            {
                bool selected = _state.IsSelected(kv.Key);
                kv.Value.EnableInClassList(SelectedClass, selected);
            }
        }

        /// <summary>Re-sync from external state change (e.g. panel close button).</summary>
        public void SyncFromState() => Render();

        private VisualElement Find(string name)
        {
            if (_root == null || string.IsNullOrEmpty(name)) return null;
            var queue = new Queue<VisualElement>();
            queue.Enqueue(_root);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur.name == name) return cur;
                int n = cur.childCount;
                for (int i = 0; i < n; i++) queue.Enqueue(cur[i]);
            }
            return null;
        }
    }
}
