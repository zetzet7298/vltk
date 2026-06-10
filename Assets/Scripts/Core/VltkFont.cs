// -----------------------------------------------------------------------------
// VLTK Mobile — Vietnamese Font helper
// Provides consistent access to NotoSans-Regular.ttf (supports Vietnamese diacritics).
// Usage: VltkFont.Regular, VltkFont.Bold
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Core
{
    public static class VltkFont
    {
        private static Font _regular;
        private static Font _bold;

        /// <summary>
        /// NotoSans Regular — supports Vietnamese (ả, ộ, ừ, ẽ, ...).
        /// Falls back to system font if not found.
        /// </summary>
        public static Font Regular
        {
            get
            {
                if (_regular != null) return _regular;
                // Load from Assets/UI/Fonts/ (Unity Resources folder lookup)
                _regular = Resources.Load<Font>("UI/Fonts/NotoSans-Regular");
                if (_regular == null)
                    _regular = Font.CreateDynamicFontFromOSFont("Noto Sans", 14);
                if (_regular == null)
                    _regular = Font.CreateDynamicFontFromOSFont(new[] { "Noto Sans", "Ubuntu", "DejaVu Sans", "Arial" }, 14);
                if (_regular == null)
                    _regular = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _regular;
            }
        }

        /// <summary>NotoSans Bold variant.</summary>
        public static Font Bold
        {
            get
            {
                if (_bold != null) return _bold;
                _bold = Resources.Load<Font>("UI/Fonts/NotoSans-Bold");
                if (_bold == null)
                    _bold = Font.CreateDynamicFontFromOSFont("Noto Sans Bold", 14);
                if (_bold == null)
                    _bold = Regular; // fallback to regular
                return _bold;
            }
        }

        /// <summary>
        /// Apply Vietnamese-capable font to a Text component.
        /// </summary>
        public static void Apply(UnityEngine.UI.Text text, bool bold = false)
        {
            if (text == null) return;
            text.font = bold ? Bold : Regular;
        }
    }
}
