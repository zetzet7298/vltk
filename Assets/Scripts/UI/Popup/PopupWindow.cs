// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Popup Window shell
// Ornate frame + title + "Đóng" (VI) close button + body slot.
// Chrome is built in C# (no UXML) so the shell is pure-VisualElement testable.
// ADR-3: frame = 玲珑盒内框.spr blank border + USS-reconstructed title/corners.
// -----------------------------------------------------------------------------
using System;
using UnityEngine.UIElements;

namespace VLTK.UI.Popup
{
    /// <summary>
    /// Generic popup shell. Renders chrome (frame, title, close) and mounts an
    /// <see cref="IPopupContent"/> body. Raises <see cref="Closed"/> when the close
    /// button is pressed; the owning <see cref="PopupManager"/> wires that to Close().
    /// </summary>
    public sealed class PopupWindow : VisualElement
    {
        private readonly IPopupContent _content;
        private readonly VisualElement _body;

        /// <summary>The content mounted inside this shell.</summary>
        public IPopupContent Content => _content;

        /// <summary>Raised when the close affordance is activated.</summary>
        public event Action Closed;

        public PopupWindow(IPopupContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            AddToClassList("popup-window");

            var chrome = new VisualElement { name = "PopupChrome" };
            chrome.AddToClassList("popup-chrome");

            var header = new VisualElement { name = "PopupHeader" };
            header.AddToClassList("popup-header");

            var title = new Label(content.TitleVi) { name = "PopupTitle" };
            title.AddToClassList("popup-title");

            var close = new Button { name = "PopupClose", text = "Đóng" };
            close.AddToClassList("popup-close");
            close.clicked += RaiseClosed;

            header.Add(title);
            header.Add(close);

            _body = new VisualElement { name = "PopupBody" };
            _body.AddToClassList("popup-body");

            chrome.Add(header);
            chrome.Add(_body);
            Add(chrome);

            // Let the content populate its body.
            content.Build(_body);
        }

        /// <summary>Fire the close affordance (close button / Esc-equivalent).</summary>
        public void RaiseClosed() => Closed?.Invoke();
    }
}
