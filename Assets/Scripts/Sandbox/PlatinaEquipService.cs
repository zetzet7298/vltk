// -----------------------------------------------------------------------------
// VLTK Mobile — Trang bị Bạch Kim runtime service
// Wraps PcPlatinaEquipRegistry. Exposes equip lookup by id/series.
// Vietnamese: "Trang bị bạch kim", "Trang bị Bạch Kim".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn trang bị Bạch Kim (Platina Equip).
    /// PC source: settings/platinaequip.txt.
    /// </summary>
    public class PlatinaEquipService
    {
        public const string LogTag = "PlatinaEquip";
        private PcPlatinaEquipRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcPlatinaEquipRegistry registry)
        {
            _registry = registry ?? new PcPlatinaEquipRegistry();
            OnLoaded?.Invoke();
        }

        public static PlatinaEquipService LoadFromStreamingAssets(string relativeDir = "Reference/PcItemFull")
        {
            var svc = new PlatinaEquipService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcPlatinaEquipParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} trang bị Bạch Kim từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public PcPlatinaEquipEntry GetEquip(int genre, int detail, int particular) => _registry.Get(genre, detail, particular);
        public List<PcPlatinaEquipEntry> GetBySeries(int series) => _registry.GetBySeries(series);
        public IEnumerable<PcPlatinaEquipEntry> All => _registry.All;
    }
}
