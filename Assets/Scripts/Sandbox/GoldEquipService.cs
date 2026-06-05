// -----------------------------------------------------------------------------
// VLTK Mobile — Trang bị Hoàng Kim runtime service
// Wraps PcGoldEquipRegistry. Exposes equip lookup by id/series/level.
// Vietnamese: "Trang bị vàng", "Trang bị Hoàng Kim".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn trang bị Hoàng Kim (Gold Equip).
    /// PC source: settings/goldequip.txt.
    /// </summary>
    public class GoldEquipService
    {
        public const string LogTag = "GoldEquip";
        private PcGoldEquipRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcGoldEquipRegistry registry)
        {
            _registry = registry ?? new PcGoldEquipRegistry();
            OnLoaded?.Invoke();
        }

        public static GoldEquipService LoadFromStreamingAssets(string relativeDir = "Reference/PcItemFull")
        {
            var svc = new GoldEquipService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcGoldEquipParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} trang bị Hoàng Kim từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public PcGoldEquipEntry GetEquip(int genre, int detail, int particular) => _registry.Get(genre, detail, particular);
        public List<PcGoldEquipEntry> GetBySeries(int series) => _registry.GetBySeries(series);
        public List<PcGoldEquipEntry> GetByLevel(int level) => _registry.GetByLevel(level);
        public IEnumerable<PcGoldEquipEntry> All => _registry.All;
    }
}
