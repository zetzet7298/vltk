// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 PopupManager
// Single overlay host on the HUD root. Backdrop + z-order + single-focus.
// ADR-1: static Instance owned by GameHudController; constructable for EditMode.
// ADR-5: single-focus default (Show closes the prior window first).
// -----------------------------------------------------------------------------
using System;
using UnityEngine.UIElements;

namespace VLTK.UI.Popup
{
    /// <summary>
    /// Overlay host that renders one focused popup at a time over the HUD.
    /// Owns the dim backdrop and z-order; enforces single-focus by default.
    /// </summary>
    public sealed class PopupManager
    {
        private readonly VisualElement _host;
        private readonly Func<IPopupContent, PopupWindow> _windowFactory;

        private VisualElement _backdrop;
        private PopupWindow _current;

        /// <summary>True while a window is open.</summary>
        public bool IsOpen => _current != null;

        /// <summary>The content of the currently focused window, or null when none is open.
        /// Enables callers (e.g. GM debug tooling that cannot reference this assembly directly)
        /// to detect which popup is showing without reaching into the window internals.</summary>
        public IPopupContent CurrentContent => _current?.Content;

        /// <summary>
        /// Convenience instance set by <see cref="GameHudController"/>. Null until the
        /// HUD initialises it. Tests construct a <see cref="PopupManager"/> directly
        /// against a temporary host and do not rely on this.
        /// </summary>
        public static PopupManager Instance { get; private set; }

        public PopupManager(VisualElement host, Func<IPopupContent, PopupWindow> windowFactory = null)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _windowFactory = windowFactory ?? (c => new PopupWindow(c));
        }

        /// <summary>Bind the HUD-owned manager as the active instance.</summary>
        public static void SetInstance(PopupManager manager) => Instance = manager;

        /// <summary>
        /// Show <paramref name="content"/> in a new window. Single-focus: any window
        /// already open is closed first. Adds the backdrop + window to the host and
        /// calls <see cref="IPopupContent.OnShow"/>.
        /// </summary>
        public void Show(IPopupContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (IsOpen) Close();

            _backdrop = new VisualElement { name = "PopupBackdrop" };
            _backdrop.AddToClassList("popup-backdrop");
            _backdrop.pickingMode = PickingMode.Position;
            _backdrop.RegisterCallback<PointerDownEvent>(_ => Close());

            _current = _windowFactory(content);
            _current.pickingMode = PickingMode.Position;
            _current.Closed += Close;

            _host.Add(_backdrop);
            _host.Add(_current);

            content.OnShow();
        }

        /// <summary>
        /// Close the focused window. Removes backdrop + window and calls
        /// <see cref="IPopupContent.OnClose"/>. No-op when nothing is open.
        /// </summary>
        public void Close()
        {
            if (!IsOpen) return;

            _current.Content?.OnClose();
            _current.RemoveFromHierarchy();
            _backdrop?.RemoveFromHierarchy();
            _current = null;
            _backdrop = null;
        }
    }
}
