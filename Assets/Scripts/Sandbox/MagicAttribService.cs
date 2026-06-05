// -----------------------------------------------------------------------------
// VLTK Mobile — Magic Attribute runtime service
// Wraps PcMagicAttribRegistry. Exposes attribute description lookup.
// Vietnamese: "Thuộc tính ma pháp", "Mô tả thuộc tính".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn thuộc tính ma pháp (Magic Attribute).
    /// PC source: settings/magicattrib.txt.
    /// </summary>
    public class MagicAttribService
    {
        public const string LogTag = "MagicAttrib";
        private PcMagicAttribRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcMagicAttribRegistry registry)
        {
            _registry = registry ?? new PcMagicAttribRegistry();
            OnLoaded?.Invoke();
        }

        public static MagicAttribService LoadFromStreamingAssets(string relativeDir = "Reference/PcItemFull")
        {
            var svc = new MagicAttribService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcMagicAttribParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} thuộc tính ma pháp từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public PcMagicAttribEntry GetAttrib(int id) => _registry.Get(id);
        public List<PcMagicAttribEntry> GetAll() => _registry.GetAll();
        public IEnumerable<PcMagicAttribEntry> All => _registry.All;
    }
}
