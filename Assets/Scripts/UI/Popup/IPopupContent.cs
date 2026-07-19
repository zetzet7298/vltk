// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Popup Window System
// Contract: a feature window supplies its body; the shell supplies chrome.
// Spec: openspec/changes/add-popup-window-system/spec.md (REQ-1)
// -----------------------------------------------------------------------------
using UnityEngine.UIElements;

namespace VLTK.UI.Popup
{
    /// <summary>
    /// Body content a feature window supplies inside the generic PopupWindow shell.
    /// The shell owns the frame/title/close; the content owns tabs/slots/buttons.
    /// </summary>
    public interface IPopupContent
    {
        /// <summary>Vietnamese title shown in the title bar.</summary>
        string TitleVi { get; }

        /// <summary>Populate the body once (heavy: build tabs/slots/buttons).</summary>
        void Build(VisualElement body);

        /// <summary>Refresh live data on each open (cheap).</summary>
        void OnShow();

        /// <summary>Release listeners/data refs on close.</summary>
        void OnClose();
    }

    /// <summary>Optional fixed layout hint for content that needs PC-like sizing.</summary>
    public interface IPopupLayoutHint
    {
        float Width { get; }
        float Height { get; }
        float Left { get; }
        float Top { get; }
    }

    /// <summary>
    /// How a PC sheet wants its chrome rendered. <see cref="PopupWindow"/> reads this
    /// instead of matching on the content type name. The default generic chrome is
    /// used when the content does not implement <see cref="IPopupChromeHint"/>.
    /// </summary>
    public enum PopupChromeKind
    {
        /// <summary>Generic VLTK shell: frame + title bar + Đóng button.</summary>
        Generic,
        /// <summary>
        /// Compact PC skill sheet (UiSkillsLive 205×376). Title and tab captions are
        /// baked into the background sprite, so the shell hides the generic title
        /// and renders only the combat-tab overlay.
        /// </summary>
        PcSkill,
        /// <summary>
        /// Compact PC character sheet (318×438). The four-tab captions, title and
        /// close button are part of the sheet art, so the generic title/chrome is
        /// hidden and the sheet is centred in the 1280×720 design space.
        /// </summary>
        PcCharacter,
    }

    /// <summary>Optional chrome hint a PC sheet implements to opt out of generic chrome.</summary>
    public interface IPopupChromeHint
    {
        PopupChromeKind Chrome { get; }
    }
}
