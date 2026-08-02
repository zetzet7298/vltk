using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>Shared procedural sprites for proxy visuals (P1). No asset deps.</summary>
    internal static class ProxyVisuals
    {
        private static Sprite _white;
        private static Sprite _gem;

        public static Sprite White()
        {
            if (_white != null) return _white;
            var tx = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tx.SetPixels32(px);
            tx.Apply();
            tx.filterMode = FilterMode.Point;
            _white = Sprite.Create(tx, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _white;
        }

        public static Sprite Gem()
        {
            if (_gem != null) return _gem;
            // small diamond: reuse white, tinted by caller
            return White();
        }
    }
}
