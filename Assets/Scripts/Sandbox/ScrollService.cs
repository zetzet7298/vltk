// -----------------------------------------------------------------------------
// VLTK Mobile — Cuộn dịch chuyển runtime service
// Wraps PcScrollRegistry. Exposes scroll lookup by id/from-map/to-map.
// Vietnamese: "Cuộn dịch chuyển", "Truyền tống".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn cuộn dịch chuyển (Teleport Scroll).
    /// PC source: settings/scroll.txt.
    /// </summary>
    public class ScrollService
    {
        public const string LogTag = "Scroll";
        private PcScrollRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcScrollRegistry registry)
        {
            _registry = registry ?? new PcScrollRegistry();
            OnLoaded?.Invoke();
        }

        public static ScrollService LoadFromStreamingAssets(string relativeDir = "Reference/PcMap")
        {
            var svc = new ScrollService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcScrollParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} cuộn dịch chuyển từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public PcScrollEntry GetScroll(int id) => _registry.Get(id);
        public List<PcScrollEntry> GetByFromMap(int mapId) => _registry.GetByFromMap(mapId);
        public List<PcScrollEntry> GetByToMap(int mapId) => _registry.GetByToMap(mapId);
        public IEnumerable<PcScrollEntry> All => _registry.All;
    }
}
