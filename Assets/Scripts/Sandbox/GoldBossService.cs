// -----------------------------------------------------------------------------
// VLTK Mobile — Boss Hoàng Kim runtime service
// Wraps PcGoldBossRegistry. Exposes boss lookup by id.
// Vietnamese: "Boss Hoàng Kim", "Boss Vàng".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn Boss Hoàng Kim.
    /// PC source: settings/goldboss.txt.
    /// </summary>
    public class GoldBossService
    {
        public const string LogTag = "GoldBoss";
        private PcGoldBossRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcGoldBossRegistry registry)
        {
            _registry = registry ?? new PcGoldBossRegistry();
            OnLoaded?.Invoke();
        }

        public static GoldBossService LoadFromStreamingAssets(string relativeDir = "Reference/PcNpc")
        {
            var svc = new GoldBossService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcGoldBossParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} boss Hoàng Kim từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public PcGoldBossEntry GetBoss(int id) => _registry.Get(id);
        public IEnumerable<PcGoldBossEntry> All => _registry.All;
    }
}
