// -----------------------------------------------------------------------------
// VLTK Mobile — HudProgressBar helper
// Reusable UI Toolkit progress-bar helper mirroring vltkunity's ProgressBar.prefab
// (Background = img_carve_progressbg, Fill = per-bar colored carve-progress sprite,
// Label = "current/max" centered). vltkunity has no ProgressBar.cs (prefab-only,
// recon §6a); this helper dedupes the inline SetBar() logic across TopBar's bars
// (P1) and gives Money/Avatar consistent styling hooks.
//
// Source values (recon §0, §6b):
//   - bg sprite: WorldGameUI/Progress/img_carve_progressbg.png
//   - fill sprite: per-bar (hp.png / img_carve_progress_blue.png / img_carve_progress_green.png)
//   - label: "current/max", UTM Cafeta #19.ttf, size 16, yellow 0.96/1/0.41
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UIElements;

namespace VLTK.UI
{
    /// <summary>
    /// Pure C# helper that drives a fill VisualElement (width %) + an optional
    /// "current/max" Label. No MonoBehaviour — fully EditMode-testable. Visual
    /// parity (sprites/font/color) is applied via USS; this helper only computes
    /// the fill fraction and label text.
    /// </summary>
    public sealed class HudProgressBar
    {
        private readonly VisualElement _fill;
        private readonly Label _text;

        /// <summary>Map a UI Toolkit element name to a HUD progress-bar slot.</summary>
        public enum BarKind { Hp, Mana, Stamina, Exp }

        public HudProgressBar(VisualElement fill, Label text)
        {
            _fill = fill;
            _text = text;
        }

        /// <summary>
        /// Set the fill width (0..100%) and the "current/max" label. Matches the
        /// vltkunity ProgressBar text format (recon §6b: "6757/8969").
        /// </summary>
        public void Set(float fraction, int current, int max)
        {
            if (_fill != null)
            {
                float pct = Mathf.Clamp01(fraction) * 100f;
                _fill.style.width = new Length(pct, LengthUnit.Percent);
            }
            if (_text != null)
                _text.text = $"{current}/{max}";
        }

        /// <summary>Set fill width only (no label text update).</summary>
        public void SetFraction(float fraction)
        {
            if (_fill == null) return;
            float pct = Mathf.Clamp01(fraction) * 100f;
            _fill.style.width = new Length(pct, LengthUnit.Percent);
        }
    }
}
