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
        private readonly PopupChromeKind _chrome;

        /// <summary>The content mounted inside this shell.</summary>
        public IPopupContent Content => _content;

        /// <summary>Raised when the close affordance is activated.</summary>
        public event Action Closed;

        public PopupWindow(IPopupContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            AddToClassList("popup-window");
            // PC sheets own their title and tab captions in the background sprite and
            // want to be centred in the 1280×720 design space. Read a typed hint
            // instead of matching on the content type name so the shell has no
            // assembly coupling and new PC sheets only add a enum value + impl.
            _chrome = (content as IPopupChromeHint)?.Chrome ?? PopupChromeKind.Generic;
            if (_chrome == PopupChromeKind.PcSkill)
                AddToClassList("popup-window--pc-skill");
            else if (_chrome == PopupChromeKind.PcCharacter)
                AddToClassList("popup-window--pc-character");

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

            bool isPcSheet = _chrome != PopupChromeKind.Generic;
            bool hideShellClose = _chrome == PopupChromeKind.PcCharacter;
            if (!isPcSheet)
                header.Add(title);
            if (!hideShellClose)
                header.Add(close);

            // PC sheets bake their title and tab captions into the background sprite.
            // The skill sheet additionally layers a combat-tab overlay so the sheet
            // opens in the combat view rather than looking like generic chrome.
            if (_chrome == PopupChromeKind.PcSkill)
            {
                var combatTab = new VisualElement { name = "PopupSkillCombatTab" };
                combatTab.AddToClassList("popup-skill-combat-tab");
                chrome.Add(combatTab);
            }

            _body = new VisualElement { name = "PopupBody" };
            _body.AddToClassList("popup-body");

            if (_chrome != PopupChromeKind.PcCharacter)
                chrome.Add(header);
            chrome.Add(_body);
            Add(chrome);

            // Let the content populate its body.
            content.Build(_body);

            // PC sheets hide the shell's own close button (the title/close captions
            // are baked into their background sprite). Their body carries a real
            // Close affordance that must raise the same shell close event.
            if (isPcSheet)
            {
                var bodyClose = _body.Q<Button>("Close");
                if (bodyClose != null)
                    bodyClose.clicked += RaiseClosed;
            }
        }

        private void ApplyLayoutHint(IPopupContent content)
        {
            if (content is IPopupLayoutHint hint)
            {
                style.width = hint.Width;
                style.height = hint.Height;
                // Preserve PC pixels inside the sheet, but center that compact sheet
                // in the mobile's 1280x720 design space.  The original PC Left/Top
                // were desktop-window coordinates, not an in-game UX requirement;
                // carrying them across made the modal visibly drift left on wide
                // mobile HUDs. Applies to every PC sheet (skill, character, future).
                if (_chrome != PopupChromeKind.Generic)
                {
                    style.left = (1280f - hint.Width) * 0.5f;
                    style.top = (720f - hint.Height) * 0.5f;
                }
                else
                {
                    style.left = hint.Left;
                    style.top = hint.Top;
                }
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
