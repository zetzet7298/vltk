// -----------------------------------------------------------------------------
// VLTK Mobile — Đổi ngoại hình (data) runtime service
// Wraps PcChangeFeatureDataRegistry. Exposes feature data lookup.
// Vietnamese: "Đổi ngoại hình", "Trang sức".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn dữ liệu đổi ngoại hình.
    /// PC source: settings/changefeature_data.txt.
    /// </summary>
    public class ChangeFeatureDataService
    {
        public const string LogTag = "ChangeFeatureData";
        private PcChangeFeatureDataRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcChangeFeatureDataRegistry registry)
        {
            _registry = registry ?? new PcChangeFeatureDataRegistry();
            OnLoaded?.Invoke();
        }

        public static ChangeFeatureDataService LoadFromStreamingAssets(string relativeDir = "Reference/PcItemFull")
        {
            var svc = new ChangeFeatureDataService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcChangeFeatureDataParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} mục đổi ngoại hình từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public PcChangeFeatureDataEntry GetFeature(int id) => _registry.Get(id);
        public List<PcChangeFeatureDataEntry> GetByCategory(int cat) => _registry.GetByCategory(cat);
        public IEnumerable<PcChangeFeatureDataEntry> All => _registry.All;
    }
}
