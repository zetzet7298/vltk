// -----------------------------------------------------------------------------
// VLTK Mobile — Thuốc runtime service
// Wraps PcPotionRegistry. Exposes potion lookup by id/type.
// Vietnamese: "Thuốc", "Hồi máu", "Hồi nội lực", "Cooldown".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn thuốc (vật phẩm tiêu hao).
    /// PC source: settings/potion.txt.
    /// </summary>
    public class PotionService
    {
        public const string LogTag = "Potion";
        private PcPotionRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcPotionRegistry registry)
        {
            _registry = registry ?? new PcPotionRegistry();
            OnLoaded?.Invoke();
        }

        public static PotionService LoadFromStreamingAssets(string relativeDir = "Reference/PcItemFull")
        {
            var svc = new PotionService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcPotionParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} thuốc từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public PcPotionEntry GetPotion(int genre, int detail, int particular) => _registry.Get(genre, detail, particular);
        public List<PcPotionEntry> GetByType(int type) => _registry.GetByType(type);
        public IEnumerable<PcPotionEntry> All => _registry.All;
    }
}
