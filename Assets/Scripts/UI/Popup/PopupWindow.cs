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

            // Robust modal geometry in the 1280×720 design space. USS percent/
            // translate can resolve to NaN in some Editor playmode refresh paths,
            // making the popup invisible. Content may provide a PC-like fixed hint.
            style.position = Position.Absolute;
            ApplyLayoutHint(content);

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

        private void ApplyLayoutHint(IPopupContent content)
        {
            if (content is IPopupLayoutHint hint)
            {
                style.width = hint.Width;
                style.height = hint.Height;
                style.left = hint.Left;
                style.top = hint.Top;
                return;
            }

            // Default shell size preserves the slice-1 Character Info footprint.
            const float width = 560f;
            const float height = 520f;
            style.width = width;
            style.height = height;
            style.left = (1280f - width) * 0.5f;
            style.top = 70f;
        }

        /// <summary>Fire the close affordance (close button / Esc-equivalent).</summary>
        public void RaiseClosed() => Closed?.Invoke();
    }
}
