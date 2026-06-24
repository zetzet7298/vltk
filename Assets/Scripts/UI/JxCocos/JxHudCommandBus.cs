// -----------------------------------------------------------------------------
// VLTK Mobile — JX HUD command bus (no-op wiring impl)
//
// Purpose: satisfy IJxHudCommandBus for adapters (JxMinimapAdapter, toolbar)
// during the wiring phase. Panel show/hide for toolbar menus + world-map overlay
// are wired slice-by-slice; until then this bus simply logs intents so the flow
// is observable without a hard dependency on a specific panel host.
//
// Source intent: KgameWorldVN toolbar callbacks publish open/close intents the
// main UI host reacts to. The host wiring lives in GameHudController / a future
// JxHudPanelHost; this class is the interim bridge.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.UI.JxCocos
{
    /// <summary>
    /// Minimal IJxHudCommandBus that logs published intents. Replace with a host
    /// that actually opens/closes panels once Slice E (toolbar menus) + the
    /// world-map overlay are wired.
    /// </summary>
    public sealed class JxHudCommandBus : IJxHudCommandBus
    {
        public void PublishPanelRequested(JxHudPanel panel)
        {
            Debug.Log($"[JxHud] panel requested: {panel}");
        }

        public void PublishActionRequested(JxHudAction action)
        {
            Debug.Log($"[JxHud] action requested: {action}");
        }
    }
}
