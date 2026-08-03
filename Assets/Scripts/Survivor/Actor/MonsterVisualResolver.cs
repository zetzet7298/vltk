using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>Visual spec cho 1 loại quái — PC logical SPR path + proxy fallback.</summary>
    public sealed class NpcVisualSpec
    {
        public string resType;            // PC NpcResType (vd "enemy005")
        public string standPath;          // PC logical path: spr\npcres\{folder}\{res}\{res}_st.spr
        public string walkPath;           // spr\npcres\{folder}\{res}\{res}_wlk.spr
        public Vector2 referencePixel;    // PcNpcVisual.Configure refPixel (mặc định PC 160,192)
        public Color fallbackColor;       // proxy màu khi SPR thiếu (fail-closed)
        public Vector2 fallbackSize;      // proxy size
    }

    /// <summary>
    /// Ticket 35 — map Survivor monster → NPC res JX (read-only, KHÔNG sửa Sandbox).
    /// Pure C# (không MonoBehaviour) → EditMode-testable.
    /// Provenance: mọi entry verify staged thật trong
    /// Assets/StreamingAssets/Generated/NpcSprites/{uid}.spr (st + wlk) theo
    /// Assets/StreamingAssets/NpcSpriteCatalog.json (2026-06-08, schemaVersion 1);
    /// uid = SprRuntimeService.ComputePathUidHex (GB2312 signed) — PcNpcVisual tự resolve.
    /// Fail-closed: Resolve(null/unknown) → null → caller dùng proxy màu. KHÔNG bịa path.
    /// </summary>
    public static class MonsterVisualResolver
    {
        // 7 loại (6 quái thường + 1 boss). Mỗi loại màu proxy riêng → phân biệt được cả khi SPR thiếu.
        private static readonly NpcVisualSpec[] Pool =
        {
            new NpcVisualSpec
            {
                resType = "enemy005", // npcs.txt templateIds [49]
                standPath = @"spr\npcres\enemy\enemy005\enemy005_st.spr",
                walkPath = @"spr\npcres\enemy\enemy005\enemy005_wlk.spr",
                referencePixel = new Vector2(160f, 192f),
                fallbackColor = new Color(0.85f, 0.25f, 0.25f),
                fallbackSize = new Vector2(0.7f, 0.9f),
            },
            new NpcVisualSpec
            {
                resType = "enemy023", // templateIds [56, 551, 589]
                standPath = @"spr\npcres\enemy\enemy023\enemy023_st.spr",
                walkPath = @"spr\npcres\enemy\enemy023\enemy023_wlk.spr",
                referencePixel = new Vector2(160f, 192f),
                fallbackColor = new Color(0.9f, 0.6f, 0.2f),
                fallbackSize = new Vector2(0.7f, 0.9f),
            },
            new NpcVisualSpec
            {
                resType = "enemy036", // templateIds [63, 454, 596, 708]
                standPath = @"spr\npcres\enemy\enemy036\enemy036_st.spr",
                walkPath = @"spr\npcres\enemy\enemy036\enemy036_wlk.spr",
                referencePixel = new Vector2(160f, 192f),
                fallbackColor = new Color(0.4f, 0.85f, 0.3f),
                fallbackSize = new Vector2(0.7f, 0.9f),
            },
            new NpcVisualSpec
            {
                resType = "enemy051", // templateIds [78]
                standPath = @"spr\npcres\enemy\enemy051\enemy051_st.spr",
                walkPath = @"spr\npcres\enemy\enemy051\enemy051_wlk.spr",
                referencePixel = new Vector2(160f, 192f),
                fallbackColor = new Color(0.3f, 0.7f, 0.9f),
                fallbackSize = new Vector2(0.7f, 0.9f),
            },
            new NpcVisualSpec
            {
                resType = "enemy083", // templateIds [110, 506, 703]
                standPath = @"spr\npcres\enemy\enemy083\enemy083_st.spr",
                walkPath = @"spr\npcres\enemy\enemy083\enemy083_wlk.spr",
                referencePixel = new Vector2(160f, 192f),
                fallbackColor = new Color(0.65f, 0.4f, 0.85f),
                fallbackSize = new Vector2(0.7f, 0.9f),
            },
            new NpcVisualSpec
            {
                resType = "enemy205", // templateIds [674, 711]
                standPath = @"spr\npcres\enemy\enemy205\enemy205_st.spr",
                walkPath = @"spr\npcres\enemy\enemy205\enemy205_wlk.spr",
                referencePixel = new Vector2(160f, 192f),
                fallbackColor = new Color(0.5f, 0.8f, 0.8f),
                fallbackSize = new Vector2(0.8f, 1.0f),
            },
            new NpcVisualSpec
            {
                resType = "boss012", // npcs.txt templateId [1489]
                standPath = @"spr\npcres\boss\boss012\boss012_st.spr",
                walkPath = @"spr\npcres\boss\boss012\boss012_wlk.spr",
                referencePixel = new Vector2(160f, 192f),
                fallbackColor = new Color(0.8f, 0.2f, 0.8f),
                fallbackSize = new Vector2(1.1f, 1.4f),
            },
        };

        public static int Count => Pool.Length;

        /// <summary>Resolve theo PC NpcResType; null khi chưa map → caller fail-closed proxy.</summary>
        public static NpcVisualSpec Resolve(string resType)
        {
            if (string.IsNullOrEmpty(resType)) return null;
            foreach (var spec in Pool)
                if (spec.resType == resType) return spec;
            return null;
        }

        /// <summary>Cycle qua pool theo index (spawn order) — phân loại quái tự động khi spawner chưa set res.</summary>
        public static NpcVisualSpec ResolveByIndex(int i)
        {
            int idx = i % Pool.Length;
            if (idx < 0) idx += Pool.Length;
            return Pool[idx];
        }
    }
}
