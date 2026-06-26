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
}
